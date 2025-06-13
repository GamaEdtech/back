namespace GamaEdtech.Application.Service
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    using GamaEdtech.Application.Interface;
    using GamaEdtech.Common.Core;
    using GamaEdtech.Common.Core.Extensions.Linq;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.UnitOfWork;
    using GamaEdtech.Common.Service;
    using GamaEdtech.Data.Dto.VotingPower;
    using GamaEdtech.Domain.Entity;

    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    using static GamaEdtech.Common.Core.Constants;

    public class VotingPowerService(Lazy<IUnitOfWorkProvider> unitOfWorkProvider, Lazy<IHttpContextAccessor> httpContextAccessor, Lazy<IStringLocalizer<VotingPowerService>> localizer
        , Lazy<ILogger<VotingPowerService>> logger)
        : LocalizableServiceBase<VotingPowerService>(unitOfWorkProvider, httpContextAccessor, localizer, logger), IVotingPowerService
    {
        public async Task<ResultData<ListDataSource<VotingPowerDto>>> GetVotingPowersAsync(ListRequestDto<VotingPower>? requestDto = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var lst = await uow.GetRepository<VotingPower>().GetManyQueryable(requestDto?.Specification).FilterListAsync(requestDto?.PagingDto);
                var result = await lst.List.Select(t => new VotingPowerDto
                {
                    Id = t.Id,
                    Amount = t.Amount,
                    WalletAddress = t.WalletAddress,
                    ProposalId = t.ProposalId,
                    TokenAccount = t.TokenAccount,
                }).ToListAsync();

                return new(OperationResult.Succeeded) { Data = new() { List = result, TotalRecordsCount = lst.TotalRecordsCount } };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<bool>> BulkImportVotingPowersAsync([NotNull] IEnumerable<ManageVotingPowerRequestDto> requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<VotingPower>();

                var entities = requestDto.Select(t => new VotingPower
                {
                    ProposalId = t.ProposalId,
                    WalletAddress = t.WalletAddress,
                    Amount = t.Amount.GetValueOrDefault(),
                    TokenAccount = t.TokenAccount,
                });
                repository.AddRange(entities);
                var affectedRows = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = affectedRows > 0 };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }
    }
}
