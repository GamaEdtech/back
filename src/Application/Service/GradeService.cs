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

    using GamaEdtech.Data.Dto.Grade;
    using GamaEdtech.Domain.Entity;

    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    using static GamaEdtech.Common.Core.Constants;

    public class GradeService(Lazy<IUnitOfWorkProvider> unitOfWorkProvider, Lazy<IHttpContextAccessor> httpContextAccessor, Lazy<IStringLocalizer<FileService>> localizer
        , Lazy<ILogger<FileService>> logger)
        : LocalizableServiceBase<FileService>(unitOfWorkProvider, httpContextAccessor, localizer, logger), IGradeService
    {
        public async Task<ResultData<ListDataSource<GradesDto>>> GetGradesAsync(ListRequestDto<Grade>? requestDto = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var result = await uow.GetRepository<Grade, int>().GetManyQueryable(requestDto?.Specification).FilterListAsync(requestDto?.PagingDto);
                var users = await result.List.Select(t => new GradesDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Icon = t.Icon,
                }).ToListAsync();
                return new(OperationResult.Succeeded) { Data = new() { List = users, TotalRecordsCount = result.TotalRecordsCount } };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<GradeDto>> GetGradeAsync([NotNull] ISpecification<Grade> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var grade = await uow.GetRepository<Grade, int>().GetManyQueryable(specification).Select(t => new GradeDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Icon = t.Icon,
                    BoardId = t.BoardId,
                    BoardTitle = t.Board!.Title,
                }).FirstOrDefaultAsync();

                return grade is null
                    ? new(OperationResult.NotFound)
                    {
                        Errors = [new() { Message = Localizer.Value["GradeNotFound"] },],
                    }
                    : new(OperationResult.Succeeded) { Data = grade };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<int>> ManageGradeAsync([NotNull] ManageGradeRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<Grade, int>();
                Grade? grade = null;

                if (requestDto.Id.HasValue)
                {
                    grade = await repository.GetAsync(requestDto.Id.Value);
                    if (grade is null)
                    {
                        return new(OperationResult.NotFound)
                        {
                            Errors = [new() { Message = Localizer.Value["GradeNotFound"] },],
                        };
                    }

                    grade.Title = requestDto.Title ?? grade.Title;
                    grade.Description = requestDto.Description ?? grade.Description;
                    grade.Icon = requestDto.Icon ?? grade.Icon;
                    grade.BoardId = requestDto.BoardId ?? grade.BoardId;

                    _ = repository.Update(grade);
                }
                else
                {
                    grade = new Grade
                    {
                        Title = requestDto.Title,
                        Description = requestDto.Description,
                        Icon = requestDto.Icon,
                        BoardId = requestDto.BoardId.GetValueOrDefault(),
                    };
                    repository.Add(grade);
                }

                _ = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = grade.Id };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["InvalidBoardId"], }] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        public async Task<ResultData<bool>> RemoveGradeAsync([NotNull] ISpecification<Grade> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var grade = await uow.GetRepository<Grade, int>().GetAsync(specification);
                if (grade is null)
                {
                    return new(OperationResult.NotFound)
                    {
                        Data = false,
                        Errors = [new() { Message = Localizer.Value["GradeNotFound"] },],
                    };
                }

                uow.GetRepository<Grade, int>().Remove(grade);
                _ = await uow.SaveChangesAsync();
                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["GradeCantBeRemoved"], },] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }
    }
}
