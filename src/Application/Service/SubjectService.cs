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

    using GamaEdtech.Data.Dto.Subject;
    using GamaEdtech.Domain.Entity;

    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    using static GamaEdtech.Common.Core.Constants;

    public class SubjectService(Lazy<IUnitOfWorkProvider> unitOfWorkProvider, Lazy<IHttpContextAccessor> httpContextAccessor, Lazy<IStringLocalizer<FileService>> localizer
        , Lazy<ILogger<FileService>> logger)
        : LocalizableServiceBase<FileService>(unitOfWorkProvider, httpContextAccessor, localizer, logger), ISubjectService
    {
        public async Task<ResultData<ListDataSource<SubjectsDto>>> GetSubjectsAsync(ListRequestDto<Subject>? requestDto = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var result = await uow.GetRepository<Subject, int>().GetManyQueryable(requestDto?.Specification).FilterListAsync(requestDto?.PagingDto);
                var users = await result.List.Select(t => new SubjectsDto
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

        public async Task<ResultData<SubjectDto>> GetSubjectAsync([NotNull] ISpecification<Subject> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var subject = await uow.GetRepository<Subject, int>().GetManyQueryable(specification).Select(t => new SubjectDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Order = t.Order,
                }).FirstOrDefaultAsync();

                return subject is null
                    ? new(OperationResult.NotFound)
                    {
                        Errors = [new() { Message = Localizer.Value["SubjectNotFound"] },],
                    }
                    : new(OperationResult.Succeeded) { Data = subject };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<int>> ManageSubjectAsync([NotNull] ManageSubjectRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<Subject, int>();
                Subject? subject = null;

                if (requestDto.Id.HasValue)
                {
                    subject = await repository.GetAsync(requestDto.Id.Value);
                    if (subject is null)
                    {
                        return new(OperationResult.NotFound)
                        {
                            Errors = [new() { Message = Localizer.Value["SubjectNotFound"] },],
                        };
                    }

                    subject.Title = requestDto.Title ?? subject.Title;
                    subject.Order = requestDto.Order ?? subject.Order;

                    _ = repository.Update(subject);
                }
                else
                {
                    subject = new Subject
                    {
                        Title = requestDto.Title,
                        Order = requestDto.Order.GetValueOrDefault(),
                    };
                    repository.Add(subject);
                }

                _ = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = subject.Id };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["InvalidGradeId"], }] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        public async Task<ResultData<bool>> RemoveSubjectAsync([NotNull] ISpecification<Subject> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var subject = await uow.GetRepository<Subject, int>().GetAsync(specification);
                if (subject is null)
                {
                    return new(OperationResult.NotFound)
                    {
                        Data = false,
                        Errors = [new() { Message = Localizer.Value["SubjectNotFound"] },],
                    };
                }

                uow.GetRepository<Subject, int>().Remove(subject);
                _ = await uow.SaveChangesAsync();
                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["SubjectCantBeRemoved"], },] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }
    }
}
