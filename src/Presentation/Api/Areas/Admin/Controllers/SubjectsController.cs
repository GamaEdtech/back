namespace GamaEdtech.Presentation.Api.Areas.Admin.Controllers
{
    using System.Diagnostics.CodeAnalysis;

    using Asp.Versioning;

    using GamaEdtech.Application.Interface;
    using GamaEdtech.Common.Core;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.Specification.Impl;
    using GamaEdtech.Common.Identity;
    using GamaEdtech.Data.Dto.Subject;
    using GamaEdtech.Domain.Entity;
    using GamaEdtech.Domain.Enumeration;
    using GamaEdtech.Domain.Specification.Subject;
    using GamaEdtech.Presentation.ViewModel.Subject;

    using Microsoft.AspNetCore.Mvc;

    [Common.DataAnnotation.Area(nameof(Admin), "Admin")]
    [Route("api/v{version:apiVersion}/[area]/[controller]")]
    [ApiVersion("1.0")]
    [Permission(Roles = [nameof(Role.Admin)])]
    public class SubjectsController(Lazy<ILogger<SubjectsController>> logger, Lazy<ISubjectService> subjectService)
        : ApiControllerBase<SubjectsController>(logger)
    {
        [HttpGet, Produces<ApiResponse<ListDataSource<SubjectsResponseViewModel>>>()]
        public async Task<IActionResult> GetSubjects([NotNull, FromQuery] SubjectsRequestViewModel request)
        {
            try
            {
                var result = await subjectService.Value.GetSubjectsAsync(new ListRequestDto<Subject>
                {
                    PagingDto = request.PagingDto,
                    Specification = request.GradeId.HasValue ? new GradeIdEqualsSpecification(request.GradeId.Value) : null,
                });
                return Ok(new ApiResponse<ListDataSource<SubjectsResponseViewModel>>
                {
                    Errors = result.Errors,
                    Data = result.Data.List is null ? new() : new()
                    {
                        List = result.Data.List.Select(t => new SubjectsResponseViewModel
                        {
                            Id = t.Id,
                            Title = t.Title,
                            Order = t.Order,
                        }),
                        TotalRecordsCount = result.Data.TotalRecordsCount,
                    }
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<ListDataSource<SubjectsResponseViewModel>> { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpGet("{id:int}"), Produces<ApiResponse<SubjectResponseViewModel>>()]
        public async Task<IActionResult> GetSubject([FromRoute] int id)
        {
            try
            {
                var result = await subjectService.Value.GetSubjectAsync(new IdEqualsSpecification<Subject, int>(id));
                return Ok(new ApiResponse<SubjectResponseViewModel>
                {
                    Errors = result.Errors,
                    Data = result.Data is null ? null : new()
                    {
                        Id = result.Data.Id,
                        Title = result.Data.Title,
                        Order = result.Data.Order,
                    }
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<SubjectResponseViewModel> { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpPost, Produces<ApiResponse<ManageSubjectResponseViewModel>>()]
        public async Task<IActionResult> CreateSubject([NotNull] ManageSubjectRequestViewModel request)
        {
            try
            {
                var result = await subjectService.Value.ManageSubjectAsync(new ManageSubjectRequestDto
                {
                    Title = request.Title,
                    Order = request.Order,
                });
                return Ok(new ApiResponse<ManageSubjectResponseViewModel>
                {
                    Errors = result.Errors,
                    Data = new() { Id = result.Data, },
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<ManageSubjectResponseViewModel> { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpPut("{id:int}"), Produces<ApiResponse<ManageSubjectResponseViewModel>>()]
        public async Task<IActionResult> UpdateSubject([FromRoute] int id, [NotNull, FromBody] UpdateSubjectRequestViewModel request)
        {
            try
            {
                var result = await subjectService.Value.ManageSubjectAsync(new ManageSubjectRequestDto
                {
                    Id = id,
                    Title = request.Title,
                    Order = request.Order,
                });
                return Ok(new ApiResponse<ManageSubjectResponseViewModel>
                {
                    Errors = result.Errors,
                    Data = new() { Id = result.Data, },
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok(new ApiResponse<ManageSubjectResponseViewModel> { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpDelete("{id:int}"), Produces<ApiResponse<bool>>()]
        public async Task<IActionResult> RemoveSubject([FromRoute] int id)
        {
            try
            {
                var result = await subjectService.Value.RemoveSubjectAsync(new IdEqualsSpecification<Subject, int>(id));
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
