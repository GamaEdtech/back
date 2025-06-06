namespace GamaEdtech.Presentation.Api.Controllers
{
    using System.Diagnostics.CodeAnalysis;

    using Asp.Versioning;

    using GamaEdtech.Application.Interface;
    using GamaEdtech.Common.Core;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.Specification;
    using GamaEdtech.Common.DataAccess.Specification.Impl;
    using GamaEdtech.Common.Identity;
    using GamaEdtech.Data.Dto.VotingPower;
    using GamaEdtech.Domain.Entity;
    using GamaEdtech.Domain.Entity.Identity;
    using GamaEdtech.Domain.Specification.VotingPower;
    using GamaEdtech.Presentation.ViewModel.VotingPower;

    using Microsoft.AspNetCore.Mvc;

    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Permission(policy: null)]
    public class VotingPowersController(Lazy<ILogger<VotingPowersController>> logger, Lazy<IVotingPowerService> votingPowerService)
        : ApiControllerBase<VotingPowersController>(logger)
    {
        [HttpGet, Produces<ApiResponse<ListDataSource<VotingPowersResponseViewModel>>>()]
        public async Task<IActionResult<ListDataSource<VotingPowersResponseViewModel>>> GetVotingPowers([NotNull, FromQuery] VotingPowersRequestViewModel request)
        {
            try
            {
                ISpecification<VotingPower> specification = new CreationUserIdEqualsSpecification<VotingPower, ApplicationUser, int>(User.UserId());

                if (!string.IsNullOrEmpty(request.WalletAddress))
                {
                    specification = specification.And(new WalletAddressContainsSpecification(request.WalletAddress));
                }

                if (!string.IsNullOrEmpty(request.ProposalId))
                {
                    specification = specification.And(new ProposalIdContainsSpecification(request.ProposalId));
                }

                var result = await votingPowerService.Value.GetVotingPowersAsync(new ListRequestDto<VotingPower>
                {
                    PagingDto = request.PagingDto,
                    Specification = specification,
                });
                return Ok<ListDataSource<VotingPowersResponseViewModel>>(new(result.Errors)
                {
                    Data = result.Data.List is null ? new() : new()
                    {
                        List = result.Data.List.Select(t => new VotingPowersResponseViewModel
                        {
                            Id = t.Id,
                            Amount = t.Amount,
                            ProposalId = t.ProposalId,
                            TokenAccount = t.TokenAccount,
                            WalletAddress = t.WalletAddress,
                        }),
                        TotalRecordsCount = result.Data.TotalRecordsCount,
                    }
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok<ListDataSource<VotingPowersResponseViewModel>>(new() { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpPost, Produces<ApiResponse<ManageVotingPowerResponseViewModel>>()]
        public async Task<IActionResult<ManageVotingPowerResponseViewModel>> CreateVotingPower([NotNull, FromBody] CreateVotingPowerRequestViewModel request)
        {
            try
            {
                ManageVotingPowerRequestDto dto = new()
                {
                    Amount = request.Amount.GetValueOrDefault(),
                    WalletAddress = request.WalletAddress,
                    TokenAccount = request.TokenAccount,
                    ProposalId = request.ProposalId,
                };
                var result = await votingPowerService.Value.ManageVotingPowerAsync(dto);

                return Ok<ManageVotingPowerResponseViewModel>(new(result.Errors)
                {
                    Data = new() { Id = result.Data, },
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok<ManageVotingPowerResponseViewModel>(new() { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpPut("{id:long}"), Produces<ApiResponse<ManageVotingPowerResponseViewModel>>()]
        public async Task<IActionResult<ManageVotingPowerResponseViewModel>> UpdateVotingPower([FromRoute] long id, [NotNull, FromBody] UpdateVotingPowerRequestViewModel request)
        {
            try
            {
                ManageVotingPowerRequestDto dto = new()
                {
                    Id = id,
                    Amount = request.Amount.GetValueOrDefault(),
                    WalletAddress = request.WalletAddress,
                    TokenAccount = request.TokenAccount,
                    ProposalId = request.ProposalId,
                };
                var result = await votingPowerService.Value.ManageVotingPowerAsync(dto);

                return Ok<ManageVotingPowerResponseViewModel>(new(result.Errors)
                {
                    Data = new() { Id = result.Data, },
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok<ManageVotingPowerResponseViewModel>(new() { Errors = [new() { Message = exc.Message }] });
            }
        }
    }
}
