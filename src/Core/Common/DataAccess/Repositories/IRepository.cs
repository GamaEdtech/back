namespace GamaEdtech.Common.DataAccess.Repositories
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Threading.Tasks;

    using GamaEdtech.Common.DataAccess.Entities;
    using GamaEdtech.Common.DataAccess.Specification;

    [DataAnnotation.Injectable]
    public interface IRepository<TEntity, TKey>
        where TEntity : class, IEntity<TEntity, TKey>
        where TKey : IEquatable<TKey>
    {
        IQueryable<TEntity> GetManyQueryable(Expression<Func<TEntity, bool>>? predicate = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false);

        IQueryable<TEntity> GetManyQueryable(ISpecification<TEntity>? specification, bool tracking = false);

        ICollection<TEntity>? GetAll(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false);

        Task<ICollection<TEntity>?> GetAllAsync(Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false);

        ICollection<TEntity>? GetPage(int startRow, int pageLength, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false);

        Task<ICollection<TEntity>?> GetPageAsync(int startRow, int pageLength, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false);

        TEntity? Get(TKey id, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = true);

        TEntity? Get(ISpecification<TEntity>? specification, bool tracking = true);

        Task<TEntity?> GetAsync(TKey id, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = true);

        Task<TEntity?> GetAsync([NotNull] Expression<Func<TEntity, bool>> predicate, bool tracking = true);

        Task<TEntity?> GetAsync(ISpecification<TEntity>? specification, bool tracking = true);

        ICollection<TEntity>? Query(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false);

        ICollection<TEntity>? Query(ISpecification<TEntity>? specification, bool tracking = false);

        Task<ICollection<TEntity>?> QueryAsync(Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false);

        Task<ICollection<TEntity>?> QueryAsync(ISpecification<TEntity>? specification, bool tracking = false);

        ICollection<TEntity>? QueryPage(int startRow, int pageLength, Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false);

        Task<ICollection<TEntity>?> QueryPageAsync(int startRow, int pageLength, Expression<Func<TEntity, bool>> predicate, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? includes = null, bool tracking = false);

        void Add(TEntity entity);

        void AddRange(IEnumerable<TEntity> entities);

        TEntity? Update(TEntity entity);

        void Remove(TEntity entity);

        void Remove(TKey id);

        bool Any(Expression<Func<TEntity, bool>>? predicate = null);

        bool Any(ISpecification<TEntity>? specification);

        Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null);

        Task<bool> AnyAsync(ISpecification<TEntity>? specification);

        int Count(Expression<Func<TEntity, bool>>? predicate = null);

        int Count(ISpecification<TEntity>? specification);

        Task<int> CountAsync(Expression<Func<TEntity, bool>>? predicate = null);

        Task<int> CountAsync(ISpecification<TEntity>? specification);

        void SetUnchanged(TEntity entitieit);
    }
}
