namespace GamaEdtech.Application.Interface
{
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.Specification;
    using System.Diagnostics.CodeAnalysis;

    using GamaEdtech.Common.DataAnnotation;

    using GamaEdtech.Data.Dto.Tag;
    using GamaEdtech.Domain.Entity;

    [Injectable]
    public interface ITagService
    {
        Task<ResultData<ListDataSource<TagsDto>>> GetTagsAsync(ListRequestDto<Tag>? requestDto = null);
        Task<ResultData<TagDto>> GetTagAsync([NotNull] ISpecification<Tag> specification);
        Task<ResultData<string?>> GetTagNameAsync([NotNull] ISpecification<Tag> specification);
        Task<ResultData<long>> ManageTagAsync([NotNull] ManageTagRequestDto requestDto);
        Task<ResultData<bool>> RemoveTagAsync([NotNull] ISpecification<Tag> specification);
        Task<ResultData<bool>> ExistsTagAsync([NotNull] ISpecification<Tag> specification);
        Task<ResultData<int>> GetTagsCountAsync([NotNull] ISpecification<Tag> specification);
    }
}
