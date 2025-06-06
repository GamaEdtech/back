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

    using GamaEdtech.Data.Dto.Topic;
    using GamaEdtech.Domain.Entity;

    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    using static GamaEdtech.Common.Core.Constants;

    public class TopicService(Lazy<IUnitOfWorkProvider> unitOfWorkProvider, Lazy<IHttpContextAccessor> httpContextAccessor, Lazy<IStringLocalizer<FileService>> localizer
        , Lazy<ILogger<FileService>> logger)
        : LocalizableServiceBase<FileService>(unitOfWorkProvider, httpContextAccessor, localizer, logger), ITopicService
    {
        public async Task<ResultData<ListDataSource<TopicsDto>>> GetTopicsAsync(ListRequestDto<Topic>? requestDto = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var result = await uow.GetRepository<Topic, int>().GetManyQueryable(requestDto?.Specification).FilterListAsync(requestDto?.PagingDto);
                var users = await result.List.Select(t => new TopicsDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Order = t.Order,
                }).ToListAsync();
                return new(OperationResult.Succeeded) { Data = new() { List = users, TotalRecordsCount = result.TotalRecordsCount } };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<TopicDto>> GetTopicAsync([NotNull] ISpecification<Topic> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var topic = await uow.GetRepository<Topic, int>().GetManyQueryable(specification).Select(t => new TopicDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Order = t.Order,
                }).FirstOrDefaultAsync();

                return topic is null
                    ? new(OperationResult.NotFound)
                    {
                        Errors = [new() { Message = Localizer.Value["TopicNotFound"] },],
                    }
                    : new(OperationResult.Succeeded) { Data = topic };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<int>> ManageTopicAsync([NotNull] ManageTopicRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<Topic, int>();
                Topic? topic = null;

                if (requestDto.Id.HasValue)
                {
                    topic = await repository.GetAsync(requestDto.Id.Value);
                    if (topic is null)
                    {
                        return new(OperationResult.NotFound)
                        {
                            Errors = [new() { Message = Localizer.Value["TopicNotFound"] },],
                        };
                    }

                    topic.Title = requestDto.Title ?? topic.Title;
                    topic.Order = requestDto.Order ?? topic.Order;

                    _ = repository.Update(topic);
                }
                else
                {
                    topic = new Topic
                    {
                        Title = requestDto.Title,
                        Order = requestDto.Order.GetValueOrDefault(),
                    };
                    repository.Add(topic);
                }

                _ = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = topic.Id };
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

        public async Task<ResultData<bool>> RemoveTopicAsync([NotNull] ISpecification<Topic> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var topic = await uow.GetRepository<Topic, int>().GetAsync(specification);
                if (topic is null)
                {
                    return new(OperationResult.NotFound)
                    {
                        Data = false,
                        Errors = [new() { Message = Localizer.Value["TopicNotFound"] },],
                    };
                }

                uow.GetRepository<Topic, int>().Remove(topic);
                _ = await uow.SaveChangesAsync();
                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["TopicCantBeRemoved"], },] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }
    }
}
