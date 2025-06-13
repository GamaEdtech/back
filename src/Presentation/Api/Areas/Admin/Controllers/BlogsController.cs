namespace GamaEdtech.Presentation.Api.Areas.Admin.Controllers
{
    using System.Diagnostics.CodeAnalysis;

    using Asp.Versioning;

    using GamaEdtech.Application.Interface;
    using GamaEdtech.Common.Core;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.Specification;
    using GamaEdtech.Common.DataAccess.Specification.Impl;
    using GamaEdtech.Common.Identity;
    using GamaEdtech.Data.Dto.Blog;
    using GamaEdtech.Data.Dto.Contribution;
    using GamaEdtech.Domain.Entity;
    using GamaEdtech.Domain.Enumeration;
    using GamaEdtech.Domain.Specification;
    using GamaEdtech.Presentation.ViewModel.Blog;
    using GamaEdtech.Presentation.ViewModel.School;

    using Microsoft.AspNetCore.Mvc;

    [Common.DataAnnotation.Area(nameof(Admin), "Admin")]
    [Route("api/v{version:apiVersion}/[area]/[controller]")]
    [ApiVersion("1.0")]
    [Permission(Roles = [nameof(Role.Admin)])]
    public class BlogsController(Lazy<ILogger<BlogsController>> logger, Lazy<IBlogService> blogService
        , Lazy<IContributionService> contributionService, Lazy<IFileService> fileService)
        : ApiControllerBase<BlogsController>(logger)
    {
        [HttpGet("contributions"), Produces<ApiResponse<ListDataSource<PostContributionListResponseViewModel>>>()]
        public async Task<IActionResult<ListDataSource<PostContributionListResponseViewModel>>> GetPostContributionList([NotNull, FromQuery] PostContributionListRequestViewModel request)
        {
            try
            {
                ISpecification<Contribution> specification = new CategoryTypeEqualsSpecification<Contribution>(CategoryType.Post);
                if (request.Status is not null)
                {
                    specification = specification.And(new StatusEqualsSpecification<Contribution>(request.Status));
                }

                var result = await contributionService.Value.GetContributionsAsync<PostContributionDto>(new ListRequestDto<Contribution>
                {
                    PagingDto = request.PagingDto,
                    Specification = specification,
                });
                return Ok(new ApiResponse<ListDataSource<PostContributionListResponseViewModel>>(result.Errors)
                {
                    Data = result.Data.List is null ? new() : new()
                    {
                        List = result.Data.List.Select(t => new PostContributionListResponseViewModel
                        {
                            Id = t.Id,
                            CreationUser = t.CreationUser,
                            CreationDate = t.CreationDate,
                            Status = t.Status,
                        }),
                        TotalRecordsCount = result.Data.TotalRecordsCount,
                    }
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<ListDataSource<PostContributionListResponseViewModel>>(new Error { Message = exc.Message }));
            }
        }

        [HttpGet("contributions/{contributionId:long}"), Produces<ApiResponse<PostContributionResponseViewModel>>()]
        public async Task<IActionResult<PostContributionResponseViewModel>> GetPostContribution([FromRoute] long contributionId)
        {
            try
            {
                var specification = new IdEqualsSpecification<Contribution, long>(contributionId)
                    .And(new CategoryTypeEqualsSpecification<Contribution>(CategoryType.Post));
                var contributionResult = await contributionService.Value.GetContributionAsync<PostContributionDto>(specification);
                if (contributionResult.Data?.Data is null)
                {
                    return Ok(new ApiResponse<PostContributionResponseViewModel>(contributionResult.Errors));
                }

                PostContributionResponseViewModel result = new()
                {
                    Title = contributionResult.Data.Data!.Title,
                    Summary = contributionResult.Data.Data!.Summary,
                    Body = contributionResult.Data.Data!.Body,
                    ImageUri = fileService.Value.GetFileUri(contributionResult.Data.Data!.ImageId, ContainerType.Post).Data,
                    Tags = contributionResult.Data.Data!.Tags,
                    PublishDate = contributionResult.Data.Data!.PublishDate.GetValueOrDefault(),
                    VisibilityType = contributionResult.Data.Data!.VisibilityType!,
                    Keywords = contributionResult.Data.Data!.Keywords,
                    Slug = contributionResult.Data.Data!.Slug,
                };

                return Ok(new ApiResponse<PostContributionResponseViewModel>
                {
                    Data = result,
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<PostContributionResponseViewModel>(new Error { Message = exc.Message }));
            }
        }

        [HttpPatch("contributions/{contributionId:long}/confirm"), Produces<ApiResponse<bool>>()]
        public async Task<IActionResult> ConfirmPostContribution([FromRoute] long contributionId)
        {
            try
            {
                var result = await blogService.Value.ConfirmPostContributionAsync(new()
                {
                    ContributionId = contributionId,
                });

                return Ok(new ApiResponse<bool>(result.Errors)
                {
                    Data = result.Data,
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<bool> { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpPatch("contributions/{contributionId:long}/reject"), Produces<ApiResponse<bool>>()]
        public async Task<IActionResult> RejectPostContribution([FromRoute] long contributionId, [NotNull, FromBody] RejectContributionRequestViewModel request)
        {
            try
            {
                var result = await contributionService.Value.RejectContributionAsync(new RejectContributionRequestDto
                {
                    Id = contributionId,
                    Comment = request.Comment,
                });
                return Ok(new ApiResponse<bool>(result.Errors)
                {
                    Data = result.Data,
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<bool> { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpPut("posts/{postId:long}"), Produces<ApiResponse<ManagePostResponseViewModel>>()]
        public async Task<IActionResult<ManagePostResponseViewModel>> UpdatePost([FromRoute] long postId, [NotNull, FromBody] UpdatePostRequestViewModel request)
        {
            try
            {
                ManagePostRequestDto dto = new()
                {
                    Id = postId,
                    Body = request.Body,
                    Image = request.Image,
                    Keywords = request.Keywords,
                    PublishDate = request.PublishDate,
                    Slug = request.Slug,
                    Summary = request.Summary,
                    Tags = request.Tags,
                    Title = request.Title,
                    VisibilityType = request.VisibilityType,
                };
                var result = await blogService.Value.ManagePostAsync(dto);

                return Ok<ManagePostResponseViewModel>(new(result.Errors)
                {
                    Data = new() { Id = result.Data, },
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok<ManagePostResponseViewModel>(new() { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpDelete("posts/{postId:long}"), Produces<ApiResponse<bool>>()]
        public async Task<IActionResult> RemovePost([FromRoute] long postId)
        {
            try
            {
                var specification = new IdEqualsSpecification<Post, long>(postId);
                var result = await blogService.Value.RemovePostAsync(specification);
                return Ok(new ApiResponse<bool>
                {
                    Errors = result.Errors,
                    Data = result.Data
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<bool> { Errors = [new() { Message = exc.Message }] });
            }
        }
    }
}
