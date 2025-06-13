namespace GamaEdtech.Presentation.Api.Controllers
{
    using System.Diagnostics.CodeAnalysis;

    using Asp.Versioning;

    using GamaEdtech.Application.Interface;
    using GamaEdtech.Common.Core;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Data.Dto.VotingPower;
    using GamaEdtech.Presentation.ViewModel.VotingPower;

    using Microsoft.AspNetCore.Mvc;

    using Org.BouncyCastle.Crypto.Parameters;
    using Org.BouncyCastle.Security;

    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    public class VotingPowersController(Lazy<ILogger<VotingPowersController>> logger, Lazy<IVotingPowerService> votingPowerService)
        : ApiControllerBase<VotingPowersController>(logger)
    {
        [HttpPost, Produces<ApiResponse<Void>>()]
        public async Task<IActionResult<Void>> CreateVotingPower([NotNull, FromBody] CreateVotingPowerRequestViewModel request)
        {
            try
            {
                var signer = SignerUtilities.GetSigner("Ed25519");
                var publicKey = new Ed25519PublicKeyParameters(System.Text.Encoding.ASCII.GetBytes(request.PublicKey!));
                signer.Init(false, publicKey);
                signer.BlockUpdate(System.Text.Encoding.ASCII.GetBytes(request.Message!), 0, request.Message!.Length);

                var valid = signer.VerifySignature(System.Text.Encoding.ASCII.GetBytes(request.SignedMessage!));
                if (!valid)
                {
                    return Ok<Void>(new() { Errors = [new() { Message = "Invalid Signature" }] });
                }

                var lst = request.Data.Select(t => new ManageVotingPowerRequestDto
                {
                    Amount = t.Amount.GetValueOrDefault(),
                    WalletAddress = t.WalletAddress,
                    TokenAccount = t.TokenAccount,
                    ProposalId = t.ProposalId,
                });
                var result = await votingPowerService.Value.BulkImportVotingPowersAsync(lst);

                return Ok<Void>(new(result.Errors)
                {
                    Data = new(),
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok<Void>(new() { Errors = [new() { Message = exc.Message }] });
            }
        }
    }
}
