namespace GamaEdtech.Common.DataAccess.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Dynamic.Core;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using GamaEdtech.Common.DataAccess.Entities;
    using GamaEdtech.Common.DataAccess.Query;
    using GamaEdtech.Common.DataAccess.Specification;

    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    public abstract class EntityRepositoryBase<TContext, TEntity, TKey>([NotNull] ILogger<IDataAccess> logger, TContext? context)
        : RepositoryBase<TContext>(logger, context), IRepository<TEntity, TKey>
        where TContext : DbContext
        where TEntity : class, IEntity<TEntity, TKey>, new()
        where TKey : IEquatable<TKey>
    {
        private readonly OrderBy<TEntity> defaultOrderBy = new(t => t.OrderBy(e => e.Id));

        public IQueryable<TEntity> GetManyQueryable(Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false) => QueryDb(predicate, orderBy, includes, tracking);

        public IQueryable<TEntity> GetManyQueryable(ISpecification<TEntity>? specification, bool tracking = false)
        {
            var query = QueryDb(specification?.Expression(), specification?.Order, null, tracking);
            if (specification?.PageFilter is not null)
            {
                query = query.Skip(specification.PageFilter.Skip).Take(specification.PageFilter.Size);
            }

            return query;
        }

        public ICollection<TEntity>? GetAll(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false) => [.. QueryDb(null, orderBy, includes, tracking)];

        public async Task<ICollection<TEntity>?> GetAllAsync(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false) => await QueryDb(null, orderBy, includes, tracking).ToListAsync();

        public ICollection<TEntity>? GetPage(int startRow, int pageLength, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false)
        {
            orderBy ??= defaultOrderBy.Expression;
            return [.. QueryDb(null, orderBy, includes, tracking).Skip(startRow).Take(pageLength)];
        }

        public async Task<ICollection<TEntity>?> GetPageAsync(int startRow, int pageLength, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false)
        {
            orderBy ??= defaultOrderBy.Expression;
            return await QueryDb(null, orderBy, includes, tracking).Skip(startRow).Take(pageLength).ToListAsync();
        }

        public TEntity? Get(TKey id, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = true) => QueryDb(t => t.Id.Equals(id), null, includes, tracking).FirstOrDefault();

        public TEntity? Get(ISpecification<TEntity>? specification, bool tracking = true) => QueryDb(specification?.Expression(), specification?.Order, null, tracking).FirstOrDefault();

        public async Task<TEntity?> GetAsync(TKey id, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = true) => await QueryDb(t => t.Id.Equals(id), null, includes, tracking).FirstOrDefaultAsync();

        public async Task<TEntity?> GetAsync([NotNull] Expression<Func<TEntity, bool>> predicate, bool tracking = true) => await QueryDb(predicate, null, null, tracking).FirstOrDefaultAsync();

        public async Task<TEntity?> GetAsync(ISpecification<TEntity>? specification, bool tracking = true) => await QueryDb(specification?.Expression(), specification?.Order, null, tracking).FirstOrDefaultAsync();

        public ICollection<TEntity>? Query(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false) => [.. QueryDb(predicate, orderBy, includes, tracking)];

        public ICollection<TEntity>? Query(ISpecification<TEntity>? specification, bool tracking = false)
        {
            var query = QueryDb(specification?.Expression(), specification?.Order, null, tracking);
            if (specification?.PageFilter is not null)
            {
                query = query.Skip(specification.PageFilter.Skip).Take(specification.PageFilter.Size);
            }

            return [.. query];
        }

        public async Task<ICollection<TEntity>?> QueryAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false) => await QueryDb(predicate, orderBy, includes, tracking).ToListAsync();

        public async Task<ICollection<TEntity>?> QueryAsync(ISpecification<TEntity>? specification, bool tracking = false)
        {
            var query = QueryDb(specification?.Expression(), specification?.Order, null, tracking);
            if (specification?.PageFilter is not null)
            {
                query = query.Skip(specification.PageFilter.Skip).Take(specification.PageFilter.Size);
            }

            return await query.ToListAsync();
        }

        public ICollection<TEntity>? QueryPage(int startRow, int pageLength, Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false)
        {
            orderBy ??= defaultOrderBy.Expression;
            return [.. QueryDb(predicate, orderBy, includes, tracking).Skip(startRow).Take(pageLength)];
        }

        public async Task<ICollection<TEntity>?> QueryPageAsync(int startRow, int pageLength, Expression<Func<TEntity, bool>>? predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false)
        {
            orderBy ??= defaultOrderBy.Expression;
            return await QueryDb(predicate, orderBy, includes, tracking).Skip(startRow).Take(pageLength).ToListAsync();
        }

        public void Add(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            _ = Context!.Set<TEntity>().Add(entity);
        }

        public void AddRange(IEnumerable<TEntity> entities)
        {
            ArgumentNullException.ThrowIfNull(entities);

            Context!.Set<TEntity>().AddRange(entities);
        }

        public TEntity? Update(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            return Context?.Set<TEntity>().Update(entity).Entity;
        }

        public void Remove(TKey id)
        {
            var entity = new TEntity() { Id = id };
            Remove(entity);
        }

        public void Remove(TEntity entity)
        {
            ArgumentNullException.ThrowIfNull(entity);

            _ = Context?.Set<TEntity>().Attach(entity);
            Context!.Entry(entity).State = EntityState.Deleted;
            _ = Context.Set<TEntity>().Remove(entity);
        }

        public bool Any(Expression<Func<TEntity, bool>>? predicate = null) => QueryDb(predicate, null, null, false).Any();

        public bool Any(ISpecification<TEntity>? specification) => QueryDb(specification?.Expression(), null, null, false).Any();

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null) => await QueryDb(predicate, null, null, false).AnyAsync();

        public async Task<bool> AnyAsync(ISpecification<TEntity>? specification) => await QueryDb(specification?.Expression(), null, null, false).AnyAsync();

        public int Count(Expression<Func<TEntity, bool>>? predicate = null) => QueryDb(predicate, null, null, false).Count();

        public int Count(ISpecification<TEntity>? specification) => QueryDb(specification?.Expression(), null, null, false).Count();

        public async Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null) => await QueryDb(predicate, null, null, false).CountAsync();

        public async Task<int> CountAsync(ISpecification<TEntity>? specification) => await QueryDb(specification?.Expression(), null, null, false).CountAsync();

        public void SetUnchanged(TEntity entitieit) => Context!.Entry(entitieit).State = EntityState.Unchanged;

        protected IQueryable<TEntity> QueryDb(Expression<Func<TEntity, bool>>? predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes, bool tracking)
        {
            IQueryable<TEntity> query = Context!.Set<TEntity>();

            if (predicate is not null)
            {
                query = query.Where(predicate);
            }

            if (includes is not null)
            {
                query = includes(query);
            }

            if (orderBy is not null)
            {
                query = orderBy(query);
            }

            if (!tracking)
            {
                query = query.AsNoTracking();
            }

            return query;
        }
    }
}
