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

        public async Task<ResultData<long>> ManageVotingPowerAsync([NotNull] ManageVotingPowerRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<VotingPower>();
                VotingPower? votingPower = null;

                if (requestDto.Id.HasValue)
                {
                    votingPower = await repository.GetAsync(requestDto.Id.Value);
                    if (votingPower is null)
                    {
                        return new(OperationResult.NotFound)
                        {
                            Errors = [new() { Message = Localizer.Value["VotingPowerNotFound"] },],
                        };
                    }

                    votingPower.ProposalId = string.IsNullOrEmpty(requestDto.ProposalId) ? votingPower.ProposalId : requestDto.ProposalId;
                    votingPower.WalletAddress = string.IsNullOrEmpty(requestDto.WalletAddress) ? votingPower.WalletAddress : requestDto.WalletAddress;
                    votingPower.Amount = requestDto.Amount ?? votingPower.Amount;
                    votingPower.TokenAccount = string.IsNullOrEmpty(requestDto.TokenAccount) ? votingPower.TokenAccount : requestDto.TokenAccount;

                    _ = repository.Update(votingPower);
                }
                else
                {
                    votingPower = new VotingPower
                    {
                        ProposalId = requestDto.ProposalId,
                        WalletAddress = requestDto.WalletAddress,
                        Amount = requestDto.Amount.GetValueOrDefault(),
                        TokenAccount = requestDto.TokenAccount,
                    };
                    repository.Add(votingPower);
                }

                _ = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = votingPower.Id };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }
    }
}
