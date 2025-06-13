namespace GamaEdtech.Application.Interface
{
    using System.Diagnostics.CodeAnalysis;

    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAnnotation;
    using GamaEdtech.Data.Dto.VotingPower;
    using GamaEdtech.Domain.Entity;

    [Injectable]
    public interface IVotingPowerService
    {
        Task<ResultData<ListDataSource<VotingPowerDto>>> GetVotingPowersAsync(ListRequestDto<VotingPower>? requestDto = null);
        Task<ResultData<bool>> BulkImportVotingPowersAsync([NotNull] IEnumerable<ManageVotingPowerRequestDto> requestDto);
    }
}
