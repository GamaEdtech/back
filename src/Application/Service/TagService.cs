namespace GamaEdtech.Application.Service
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using EntityFramework.Exceptions.Common;

    using GamaEdtech.Application.Interface;

    using GamaEdtech.Common.Core;
    using GamaEdtech.Common.Core.Extensions.Linq;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.Specification;
    using GamaEdtech.Common.DataAccess.UnitOfWork;
    using GamaEdtech.Common.Service;

    using GamaEdtech.Data.Dto.Tag;
    using GamaEdtech.Domain.Entity;

    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    using static GamaEdtech.Common.Core.Constants;

    public class TagService(Lazy<IUnitOfWorkProvider> unitOfWorkProvider, Lazy<IHttpContextAccessor> httpContextAccessor, Lazy<IStringLocalizer<FileService>> localizer
        , Lazy<ILogger<FileService>> logger)
        : LocalizableServiceBase<FileService>(unitOfWorkProvider, httpContextAccessor, localizer, logger), ITagService
    {
        public async Task<ResultData<ListDataSource<TagsDto>>> GetTagsAsync(ListRequestDto<Tag>? requestDto = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var result = await uow.GetRepository<Tag>().GetManyQueryable(requestDto?.Specification).FilterListAsync(requestDto?.PagingDto);
                var users = await result.List.Select(t => new TagsDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Icon = t.Icon,
                    TagType = t.TagType,
                }).ToListAsync();
                return new(OperationResult.Succeeded) { Data = new() { List = users, TotalRecordsCount = result.TotalRecordsCount } };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<TagDto>> GetTagAsync([NotNull] ISpecification<Tag> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var tag = await uow.GetRepository<Tag>().GetManyQueryable(specification).Select(t => new TagDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Icon = t.Icon,
                    TagType = t.TagType,
                }).FirstOrDefaultAsync();

                return tag is null
                    ? new(OperationResult.NotFound)
                    {
                        Errors = [new() { Message = Localizer.Value["TagNotFound"] },],
                    }
                    : new(OperationResult.Succeeded) { Data = tag };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<string?>> GetTagNameAsync([NotNull] ISpecification<Tag> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var tagName = await uow.GetRepository<Tag>().GetManyQueryable(specification).Select(t => t.Name).FirstOrDefaultAsync();

                return new(OperationResult.Succeeded) { Data = tagName };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<long>> ManageTagAsync([NotNull] ManageTagRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<Tag>();
                Tag? tag = null;

                if (requestDto.Id.HasValue)
                {
                    tag = await repository.GetAsync(requestDto.Id.Value);
                    if (tag is null)
                    {
                        return new(OperationResult.NotFound)
                        {
                            Errors = [new() { Message = Localizer.Value["TagNotFound"] },],
                        };
                    }

                    tag.Name = requestDto.Name ?? tag.Name;
                    tag.Icon = requestDto.Icon ?? tag.Icon;
                    tag.TagType = requestDto.TagType ?? tag.TagType;

                    _ = repository.Update(tag);
                }
                else
                {
                    tag = new Tag
                    {
                        Name = requestDto.Name,
                        Icon = requestDto.Icon,
                        TagType = requestDto.TagType,
                    };
                    repository.Add(tag);
                }

                _ = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = tag.Id };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["InvalidSubjectId"], }] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        public async Task<ResultData<bool>> RemoveTagAsync([NotNull] ISpecification<Tag> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<Tag>();
                var tag = await repository.GetAsync(specification);
                if (tag is null)
                {
                    return new(OperationResult.NotFound)
                    {
                        Data = false,
                        Errors = [new() { Message = Localizer.Value["TagNotFound"] },],
                    };
                }

                repository.Remove(tag);
                _ = await uow.SaveChangesAsync();
                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["TagCantBeRemoved"], },] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> ExistsTagAsync([NotNull] ISpecification<Tag> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var exists = await uow.GetRepository<Tag>().AnyAsync(specification);

                return new(OperationResult.Succeeded) { Data = exists };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<int>> GetTagsCountAsync([NotNull] ISpecification<Tag> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var count = await uow.GetRepository<Tag>().CountAsync(specification);

                return new(OperationResult.Succeeded) { Data = count };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }
    }
}
