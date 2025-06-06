namespace GamaEdtech.Presentation.Api.Areas.Admin.Controllers
{
    using System.Diagnostics.CodeAnalysis;

    using Asp.Versioning;

    using GamaEdtech.Application.Interface;
    using GamaEdtech.Common.Core;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.Specification.Impl;
    using GamaEdtech.Common.Identity;
    using GamaEdtech.Data.Dto.Tag;
    using GamaEdtech.Domain.Entity;
    using GamaEdtech.Domain.Enumeration;
    using GamaEdtech.Domain.Specification.Tag;
    using GamaEdtech.Presentation.ViewModel.Tag;

    using Microsoft.AspNetCore.Mvc;

    [Common.DataAnnotation.Area(nameof(Admin), "Admin")]
    [Route("api/v{version:apiVersion}/[area]/[controller]")]
    [ApiVersion("1.0")]
    [Permission(Roles = [nameof(Role.Admin)])]
    public class TagsController(Lazy<ILogger<TagsController>> logger, Lazy<ITagService> tagService)
        : ApiControllerBase<TagsController>(logger)
    {
        [HttpGet, Produces<ApiResponse<ListDataSource<TagsResponseViewModel>>>()]
        public async Task<IActionResult> GetTags([NotNull, FromQuery] TagsRequestViewModel request)
        {
            try
            {
                var result = await tagService.Value.GetTagsAsync(new ListRequestDto<Tag>
                {
                    PagingDto = request.PagingDto,
                    Specification = request.TagType is not null ? new TagTypeEqualsSpecification(request.TagType) : null,
                });
                return Ok(new ApiResponse<ListDataSource<TagsResponseViewModel>>
                {
                    Errors = result.Errors,
                    Data = result.Data.List is null ? new() : new()
                    {
                        List = result.Data.List.Select(t => new TagsResponseViewModel
                        {
                            Id = t.Id,
                            Name = t.Name,
                            Icon = t.Icon,
                            TagType = t.TagType,
                        }),
                        TotalRecordsCount = result.Data.TotalRecordsCount,
                    }
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<ListDataSource<TagsResponseViewModel>> { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpGet("{id:long}"), Produces<ApiResponse<TagResponseViewModel>>()]
        public async Task<IActionResult> GetTag([FromRoute] long id)
        {
            try
            {
                var result = await tagService.Value.GetTagAsync(new IdEqualsSpecification<Tag, long>(id));
                return Ok(new ApiResponse<TagResponseViewModel>
                {
                    Errors = result.Errors,
                    Data = result.Data is null ? null : new()
                    {
                        Id = result.Data.Id,
                        Name = result.Data.Name,
                        Icon = result.Data.Icon,
                        TagType = result.Data.TagType,
                    }
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<TagResponseViewModel> { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpPost, Produces<ApiResponse<ManageTagResponseViewModel>>()]
        public async Task<IActionResult> CreateTag([NotNull] ManageTagRequestViewModel request)
        {
            try
            {
                var result = await tagService.Value.ManageTagAsync(new ManageTagRequestDto
                {
                    Name = request.Name,
                    Icon = request.Icon,
                    TagType = request.TagType,
                });
                return Ok(new ApiResponse<ManageTagResponseViewModel>
                {
                    Errors = result.Errors,
                    Data = new() { Id = result.Data, },
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<ManageTagResponseViewModel> { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpPut("{id:long}"), Produces<ApiResponse<ManageTagResponseViewModel>>()]
        public async Task<IActionResult> UpdateTag([FromRoute] long id, [NotNull, FromBody] UpdateTagRequestViewModel request)
        {
            try
            {
                var result = await tagService.Value.ManageTagAsync(new ManageTagRequestDto
                {
                    Id = id,
                    Name = request.Name,
                    Icon = request.Icon,
                    TagType = request.TagType,
                });
                return Ok(new ApiResponse<ManageTagResponseViewModel>
                {
                    Errors = result.Errors,
                    Data = new() { Id = result.Data, },
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<ManageTagResponseViewModel> { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpDelete("{id:long}"), Produces<ApiResponse<bool>>()]
        public async Task<IActionResult> RemoveTag([FromRoute] long id)
        {
            try
            {
                var result = await tagService.Value.RemoveTagAsync(new IdEqualsSpecification<Tag, long>(id));
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
