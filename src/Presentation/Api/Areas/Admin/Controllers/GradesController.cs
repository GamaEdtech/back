namespace GamaEdtech.Presentation.Api.Areas.Admin.Controllers
{
    using System.Diagnostics.CodeAnalysis;

    using Asp.Versioning;

    using GamaEdtech.Application.Interface;
    using GamaEdtech.Common.Core;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.Specification.Impl;
    using GamaEdtech.Common.Identity;
    using GamaEdtech.Data.Dto.Grade;
    using GamaEdtech.Domain.Entity;
    using GamaEdtech.Domain.Enumeration;
    using GamaEdtech.Domain.Specification.Grade;
    using GamaEdtech.Presentation.ViewModel.Grade;

    using Microsoft.AspNetCore.Mvc;

    [Common.DataAnnotation.Area(nameof(Admin), "Admin")]
    [Route("api/v{version:apiVersion}/[area]/[controller]")]
    [ApiVersion("1.0")]
    [Permission(Roles = [nameof(Role.Admin)])]
    public class GradesController(Lazy<ILogger<GradesController>> logger, Lazy<IGradeService> gradeService)
        : ApiControllerBase<GradesController>(logger)
    {
        [HttpGet, Produces<ApiResponse<ListDataSource<GradesResponseViewModel>>>()]
        public async Task<IActionResult<ListDataSource<GradesResponseViewModel>>> GetGrades([NotNull, FromQuery] GradesRequestViewModel request)
        {
            try
            {
                var result = await gradeService.Value.GetGradesAsync(new ListRequestDto<Grade>
                {
                    PagingDto = request.PagingDto,
                    Specification = request.BoardId.HasValue ? new BoardIdEqualsSpecification(request.BoardId.Value) : null,
                });
                return Ok<ListDataSource<GradesResponseViewModel>>(new(result.Errors)
                {
                    Data = result.Data.List is null ? new() : new()
                    {
                        List = result.Data.List.Select(t => new GradesResponseViewModel
                        {
                            Id = t.Id,
                            Title = t.Title,
                            Icon = t.Icon,
                        }),
                        TotalRecordsCount = result.Data.TotalRecordsCount,
                    }
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok<ListDataSource<GradesResponseViewModel>>(new() { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpGet("{id:int}"), Produces<ApiResponse<GradeResponseViewModel>>()]
        public async Task<IActionResult<GradeResponseViewModel>> GetGrade([FromRoute] int id)
        {
            try
            {
                var result = await gradeService.Value.GetGradeAsync(new IdEqualsSpecification<Grade, int>(id));
                return Ok<GradeResponseViewModel>(new(result.Errors)
                {
                    Data = result.Data is null ? null : new()
                    {
                        Id = result.Data.Id,
                        Title = result.Data.Title,
                        Description = result.Data.Description,
                        Icon = result.Data.Icon,
                    }
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok<GradeResponseViewModel>(new() { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpPost, Produces<ApiResponse<ManageGradeResponseViewModel>>()]
        public async Task<IActionResult<ManageGradeResponseViewModel>> CreateGrade([NotNull] ManageGradeRequestViewModel request)
        {
            try
            {
                var result = await gradeService.Value.ManageGradeAsync(new ManageGradeRequestDto
                {
                    Title = request.Title,
                    Description = request.Description,
                    Icon = request.Icon,
                });
                return Ok<ManageGradeResponseViewModel>(new(result.Errors)
                {
                    Data = new() { Id = result.Data, },
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok<ManageGradeResponseViewModel>(new() { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpPut("{id:int}"), Produces<ApiResponse<ManageGradeResponseViewModel>>()]
        public async Task<IActionResult<ManageGradeResponseViewModel>> UpdateGrade([FromRoute] int id, [NotNull, FromBody] UpdateGradeRequestViewModel request)
        {
            try
            {
                var result = await gradeService.Value.ManageGradeAsync(new ManageGradeRequestDto
                {
                    Id = id,
                    Title = request.Title,
                    Description = request.Description,
                    Icon = request.Icon,
                });
                return Ok<ManageGradeResponseViewModel>(new(result.Errors)
                {
                    Data = new() { Id = result.Data, },
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok<ManageGradeResponseViewModel>(new() { Errors = [new() { Message = exc.Message }] });
            }
        }

        [HttpDelete("{id:int}"), Produces<ApiResponse<bool>>()]
        public async Task<IActionResult<bool>> RemoveGrade([FromRoute] int id)
        {
            try
            {
                var result = await gradeService.Value.RemoveGradeAsync(new IdEqualsSpecification<Grade, int>(id));
                return Ok<bool>(new(result.Errors)
                {
                    Data = result.Data
                });
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);

                return Ok<bool>(new() { Errors = [new() { Message = exc.Message }] });
            }
        }
    }
}
