namespace GamaEdtech.Application.Interface
{
    using System.Diagnostics.CodeAnalysis;

    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.Specification;
    using GamaEdtech.Common.DataAnnotation;
    using GamaEdtech.Data.Dto.Contribution;
    using GamaEdtech.Domain.Entity;

    [Injectable]
    public interface IContributionService
    {
        Task<ResultData<ListDataSource<ContributionsDto<T>>>> GetContributionsAsync<T>(ListRequestDto<Contribution>? requestDto = null, bool includeData = false);
        Task<ResultData<ContributionDto<T>>> GetContributionAsync<T>([NotNull] ISpecification<Contribution> specification);
        Task<ResultData<T?>> GetContributionDataAsync<T>([NotNull] ISpecification<Contribution> specification);
        Task<ResultData<long>> ManageContributionAsync<T>([NotNull] ManageContributionRequestDto<T> requestDto);
        Task<ResultData<bool>> ExistsContributionAsync([NotNull] ISpecification<Contribution> specification);
        Task<ResultData<ContributionDto<T>>> ConfirmContributionAsync<T>([NotNull] ISpecification<Contribution> specification);
        Task<ResultData<bool>> RejectContributionAsync([NotNull] RejectContributionRequestDto requestDto);
        Task<ResultData<bool>> DeleteContributionAsync([NotNull] ISpecification<Contribution> specification);
        Task<ResultData<bool>> IsCreatorOfContributionAsync(long contributionId, int userId);
    }
}
