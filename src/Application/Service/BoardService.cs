namespace GamaEdtech.Application.Service
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    using EntityFramework.Exceptions.Common;

    using GamaEdtech.Common.Core.Extensions.Linq;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.Specification;
    using GamaEdtech.Common.DataAccess.UnitOfWork;
    using GamaEdtech.Common.Service;
    using GamaEdtech.Common.Core;
    using GamaEdtech.Data.Dto.Board;
    using GamaEdtech.Domain.Entity;

    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    using static GamaEdtech.Common.Core.Constants;
    using GamaEdtech.Application.Interface;

    public class BoardService(Lazy<IUnitOfWorkProvider> unitOfWorkProvider, Lazy<IHttpContextAccessor> httpContextAccessor, Lazy<IStringLocalizer<BoardService>> localizer
        , Lazy<ILogger<BoardService>> logger)
        : LocalizableServiceBase<BoardService>(unitOfWorkProvider, httpContextAccessor, localizer, logger), IBoardService
    {
        public async Task<ResultData<ListDataSource<BoardsDto>>> GetBoardsAsync(ListRequestDto<Board>? requestDto = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var result = await uow.GetRepository<Board, int>().GetManyQueryable(requestDto?.Specification).FilterListAsync(requestDto?.PagingDto);
                var users = await result.List.Select(t => new BoardsDto
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

        public async Task<ResultData<BoardDto>> GetBoardAsync([NotNull] ISpecification<Board> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var board = await uow.GetRepository<Board, int>().GetManyQueryable(specification).Select(t => new BoardDto
                {
                    Id = t.Id,
                    Title = t.Title,
                    Description = t.Description,
                    Icon = t.Icon,
                }).FirstOrDefaultAsync();

                return board is null
                    ? new(OperationResult.NotFound)
                    {
                        Errors = [new() { Message = Localizer.Value["BoardNotFound"] },],
                    }
                    : new(OperationResult.Succeeded) { Data = board };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<int>> ManageBoardAsync([NotNull] ManageBoardRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<Board, int>();
                Board? board = null;

                if (requestDto.Id.HasValue)
                {
                    board = await repository.GetAsync(requestDto.Id.Value);
                    if (board is null)
                    {
                        return new(OperationResult.NotFound)
                        {
                            Errors = [new() { Message = Localizer.Value["BoardNotFound"] },],
                        };
                    }

                    board.Title = requestDto.Title ?? board.Title;
                    board.Description = requestDto.Description ?? board.Description;
                    board.Icon = requestDto.Icon ?? board.Icon;

                    _ = repository.Update(board);
                }
                else
                {
                    board = new Board
                    {
                        Title = requestDto.Title,
                        Description = requestDto.Description,
                        Icon = requestDto.Icon,
                    };
                    repository.Add(board);
                }

                _ = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = board.Id };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        public async Task<ResultData<bool>> RemoveBoardAsync([NotNull] ISpecification<Board> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var board = await uow.GetRepository<Board, int>().GetAsync(specification);
                if (board is null)
                {
                    return new(OperationResult.NotFound)
                    {
                        Data = false,
                        Errors = [new() { Message = Localizer.Value["BoardNotFound"] },],
                    };
                }

                uow.GetRepository<Board, int>().Remove(board);
                _ = await uow.SaveChangesAsync();
                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["BoardCantBeRemoved"], },] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }
    }
}
