namespace GamaEdtech.Application.Service
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    using EntityFramework.Exceptions.Common;

    using GamaEdtech.Application.Interface;
    using GamaEdtech.Common.Core;
    using GamaEdtech.Common.Core.Extensions.Linq;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.Specification;
    using GamaEdtech.Common.DataAccess.Specification.Impl;
    using GamaEdtech.Common.DataAccess.UnitOfWork;
    using GamaEdtech.Common.Service;
    using GamaEdtech.Data.Dto.Contribution;
    using GamaEdtech.Data.Dto.School;
    using GamaEdtech.Data.Dto.Tag;
    using GamaEdtech.Domain.Entity;
    using GamaEdtech.Domain.Entity.Identity;
    using GamaEdtech.Domain.Enumeration;
    using GamaEdtech.Domain.Specification;
    using GamaEdtech.Domain.Specification.School;
    using GamaEdtech.Domain.Specification.Tag;

    using MetadataExtractor;
    using MetadataExtractor.Formats.Exif;

    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    using NetTopologySuite;
    using NetTopologySuite.Geometries;

    using static GamaEdtech.Common.Core.Constants;

    public class SchoolService(Lazy<IUnitOfWorkProvider> unitOfWorkProvider, Lazy<IHttpContextAccessor> httpContextAccessor, Lazy<IStringLocalizer<FileService>> localizer
        , Lazy<ILogger<FileService>> logger, Lazy<IFileService> fileService, Lazy<IContributionService> contributionService, Lazy<IIdentityService> identityService
        , Lazy<IConfiguration> configuration, Lazy<ITagService> tagService, Lazy<IReactionService> reactionService)
        : LocalizableServiceBase<FileService>(unitOfWorkProvider, httpContextAccessor, localizer, logger), ISchoolService
    {
        #region Schools

        public async Task<ResultData<ListDataSource<SchoolsDto>>> GetSchoolsAsync(ListRequestDto<School>? requestDto = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var result = await uow.GetRepository<School>().GetManyQueryable(requestDto?.Specification).FilterListAsync(requestDto?.PagingDto);
                var schools = await result.List.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.LocalName,
                    DefaultImageId = t.SchoolImages.OrderByDescending(i => i.IsDefault).Select(i => i.FileId).FirstOrDefault(),
                }).ToListAsync();

                var lst = schools.Select(t => new SchoolsDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    LocalName = t.LocalName,
                    DefaultImageUri = fileService.Value.GetFileUri(t.DefaultImageId, ContainerType.School).Data,
                });

                return new(OperationResult.Succeeded) { Data = new() { List = lst, TotalRecordsCount = result.TotalRecordsCount } };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<ListDataSource<SchoolInfoDto>>> GetSchoolsListAsync(ListRequestDto<School>? requestDto = null, Point? point = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var lst = uow.GetRepository<School>().GetManyQueryable(requestDto?.Specification);
                int? total = requestDto?.PagingDto?.PageFilter?.ReturnTotalRecordsCount == true ? await lst.CountAsync() : null;
                var query = lst.Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.CreationDate,
                    t.LastModifyDate,
                    t.WebSite,
                    t.Email,
                    t.PhoneNumber,
                    t.Coordinates,
                    t.Score,
                    CityTitle = t.City == null ? "" : t.City.Title,
                    CountryTitle = t.Country == null ? "" : t.Country.Title,
                    StateTitle = t.State == null ? "" : t.State.Title,
                    Distance = point != null && t.Coordinates != null ? t.Coordinates.Distance(point) : (double?)null,
                    DefaultImageUri = t.SchoolImages.OrderByDescending(i => i.IsDefault).Select(i => i.FileId).FirstOrDefault(),
                });

                (query, var sortApplied) = query.OrderBy(requestDto?.PagingDto?.SortFilter);
                if (!sortApplied)
                {
                    query = point is not null ? query.OrderBy(t => t.Distance) : query.OrderByDescending(t => t.Id);
                }
                if (requestDto?.PagingDto?.PageFilter is not null)
                {
                    query = query.Skip(requestDto.PagingDto.PageFilter.Skip)
                        .Take(requestDto.PagingDto.PageFilter.Size);
                }
                var items = await query.ToListAsync();

                var result = items.Select(t => new SchoolInfoDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    CityTitle = t.CityTitle,
                    Coordinates = t.Coordinates,
                    CountryTitle = t.CountryTitle,
                    Distance = t.Distance,
                    LastModifyDate = t.LastModifyDate ?? t.CreationDate,
                    Score = t.Score,
                    StateTitle = t.StateTitle,
                    HasEmail = !string.IsNullOrEmpty(t.Email),
                    HasPhoneNumber = !string.IsNullOrEmpty(t.PhoneNumber),
                    HasWebSite = !string.IsNullOrEmpty(t.WebSite),
                    DefaultImageUri = fileService.Value.GetFileUri(t.DefaultImageUri, ContainerType.School).Data,
                });

                return new(OperationResult.Succeeded) { Data = new() { List = result, TotalRecordsCount = total } };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<SchoolDto>> GetSchoolAsync([NotNull] ISpecification<School> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var school = await uow.GetRepository<School>().GetManyQueryable(specification).Select(t => new
                {
                    t.Id,
                    t.Name,
                    t.LocalName,
                    t.Address,
                    t.LocalAddress,
                    t.Coordinates,
                    t.SchoolType,
                    t.ZipCode,
                    t.CityId,
                    CityTitle = t.City != null ? t.City.Title : "",
                    t.CountryId,
                    CountryTitle = t.Country != null ? t.Country.Title : "",
                    t.StateId,
                    StateTitle = t.State != null ? t.State.Title : "",
                    t.WebSite,
                    t.FaxNumber,
                    t.PhoneNumber,
                    t.Email,
                    t.Quarter,
                    t.OsmId,
                    DefaultImageId = t.SchoolImages.OrderByDescending(i => i.IsDefault).Select(i => i.FileId).FirstOrDefault(),
                    Tags = t.SchoolTags.Select(s => new TagDto
                    {
                        Icon = s.Tag.Icon,
                        Id = s.TagId,
                        Name = s.Tag.Name,
                        TagType = s.Tag.TagType,
                    }),
                }).FirstOrDefaultAsync();
                if (school is null)
                {
                    return new(OperationResult.NotFound)
                    {
                        Errors = [new() { Message = Localizer.Value["SchoolNotFound"] },],
                    };
                }

                SchoolDto result = new()
                {
                    Id = school.Id,
                    Name = school.Name,
                    LocalName = school.LocalName,
                    Address = school.Address,
                    LocalAddress = school.LocalAddress,
                    Coordinates = school.Coordinates,
                    SchoolType = school.SchoolType,
                    ZipCode = school.ZipCode,
                    CityId = school.CityId,
                    CityTitle = school.CityTitle,
                    CountryId = school.CountryId,
                    CountryTitle = school.CountryTitle,
                    StateId = school.StateId,
                    StateTitle = school.StateTitle,
                    WebSite = school.WebSite,
                    FaxNumber = school.FaxNumber,
                    PhoneNumber = school.PhoneNumber,
                    Email = school.Email,
                    Quarter = school.Quarter,
                    OsmId = school.OsmId,
                    DefaultImageUri = fileService.Value.GetFileUri(school.DefaultImageId, ContainerType.School).Data,
                    Tags = school.Tags,
                };
                return new(OperationResult.Succeeded) { Data = result };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<IReadOnlyList<KeyValuePair<long, string?>>>> GetSchoolsNameAsync([NotNull] ISpecification<School> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var name = await uow.GetRepository<School>().GetManyQueryable(specification).Select(t => new KeyValuePair<long, string?>(t.Id, t.Name)).ToListAsync();

                return new(OperationResult.Succeeded) { Data = name };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<long>> ManageSchoolAsync([NotNull] ManageSchoolRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<School>();
                School? school = null;

                if (requestDto.Id.HasValue)
                {
                    school = await repository.GetAsync(requestDto.Id.Value, includes: (t) => t.Include(s => s.SchoolTags));
                    if (school is null)
                    {
                        return new(OperationResult.NotFound)
                        {
                            Errors = [new() { Message = Localizer.Value["SchoolNotFound"] },],
                        };
                    }

                    school.Name = requestDto.Name ?? school.Name;
                    school.LocalName = requestDto.LocalName ?? school.LocalName;
                    school.Address = requestDto.Address ?? school.Address;
                    school.Coordinates = requestDto.Coordinates ?? school.Coordinates;
                    school.SchoolType = requestDto.SchoolType ?? school.SchoolType;
                    school.StateId = requestDto.StateId ?? school.StateId;
                    school.ZipCode = requestDto.ZipCode ?? school.ZipCode;
                    school.WebSite = requestDto.WebSite ?? school.WebSite;
                    school.Quarter = requestDto.Quarter ?? school.Quarter;
                    school.PhoneNumber = requestDto.PhoneNumber ?? school.PhoneNumber;
                    school.LocalAddress = requestDto.LocalAddress ?? school.LocalAddress;
                    school.FaxNumber = requestDto.FaxNumber ?? school.FaxNumber;
                    school.Email = requestDto.Email ?? school.Email;
                    school.CityId = requestDto.CityId ?? school.CityId;
                    school.CountryId = requestDto.CountryId ?? school.CountryId;
                    school.OsmId = requestDto.OsmId ?? school.OsmId;
                    school.LastModifyDate = requestDto.Date;
                    school.LastModifyUserId = requestDto.UserId;

                    _ = repository.Update(school);

                    if (requestDto.Tags?.Any() == true)
                    {
                        var schoolTagRepository = uow.GetRepository<SchoolTag>();

                        var removedTags = school.SchoolTags?.Where(t => requestDto.Tags is null || !requestDto.Tags.Contains(t.TagId));
                        var newTags = requestDto.Tags?.Where(t => school.SchoolTags is null || school.SchoolTags.All(s => s.TagId != t));

                        if (removedTags is not null)
                        {
                            foreach (var item in removedTags)
                            {
                                schoolTagRepository.Remove(item);
                            }
                        }

                        if (newTags is not null)
                        {
                            foreach (var item in newTags)
                            {
                                schoolTagRepository.Add(new SchoolTag
                                {
                                    SchoolId = requestDto.Id.Value,
                                    TagId = item,
                                    CreationDate = requestDto.Date,
                                    CreationUserId = requestDto.UserId,
                                });
                            }
                        }
                    }
                }
                else
                {
                    school = new School
                    {
                        Name = requestDto.Name,
                        LocalName = requestDto.LocalName,
                        Address = requestDto.Address,
                        Coordinates = requestDto.Coordinates,
                        SchoolType = requestDto.SchoolType,
                        StateId = requestDto.StateId,
                        ZipCode = requestDto.ZipCode,
                        WebSite = requestDto.WebSite,
                        Quarter = requestDto.Quarter,
                        PhoneNumber = requestDto.PhoneNumber,
                        LocalAddress = requestDto.LocalAddress,
                        FaxNumber = requestDto.FaxNumber,
                        Email = requestDto.Email,
                        CityId = requestDto.CityId,
                        CountryId = requestDto.CountryId,
                        OsmId = requestDto.OsmId,
                    };
                    if (requestDto.Tags is not null)
                    {
                        school.SchoolTags = [.. requestDto.Tags.Select(t => new SchoolTag {
                            TagId = t,
                            CreationUserId=requestDto.UserId,
                            CreationDate=requestDto.Date,
                        })];
                    }
                    repository.Add(school);
                }

                _ = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = school.Id };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["InvalidStateId"], }] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        public async Task<ResultData<bool>> RemoveSchoolAsync([NotNull] ISpecification<School> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<School>();
                var school = await repository.GetAsync(specification);
                if (school is null)
                {
                    return new(OperationResult.NotFound)
                    {
                        Data = false,
                        Errors = [new() { Message = Localizer.Value["SchoolNotFound"] },],
                    };
                }

                repository.Remove(school);
                _ = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["SchoolCantBeRemoved"], },] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> ExistsSchoolAsync([NotNull] ISpecification<School> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var exists = await uow.GetRepository<School>().AnyAsync(specification);
                return new(OperationResult.Succeeded) { Data = exists };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        #endregion

        #region Rate and Comments

        public async Task<ResultData<SchoolRateDto>> GetSchoolRateAsync([NotNull] ISpecification<SchoolComment> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var result = await uow.GetRepository<SchoolComment>().GetManyQueryable(specification)
                    .GroupBy(t => t.SchoolId)
                    .Select(t => new SchoolRateDto
                    {
                        AverageRate = t.Average(c => c.AverageRate),
                        TuitionRatioRate = t.Average(c => c.TuitionRatioRate),
                        SafetyAndHappinessRate = t.Average(c => c.SafetyAndHappinessRate),
                        ITTrainingRate = t.Average(c => c.ITTrainingRate),
                        FacilitiesRate = t.Average(c => c.FacilitiesRate),
                        EducationRate = t.Average(c => c.EducationRate),
                        ClassesQualityRate = t.Average(c => c.ClassesQualityRate),
                        BehaviorRate = t.Average(c => c.BehaviorRate),
                        ArtisticActivitiesRate = t.Average(c => c.ArtisticActivitiesRate),
                        TotalCount = t.Count(),
                    }).FirstOrDefaultAsync();

                return new(OperationResult.Succeeded) { Data = result };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<ListDataSource<SchoolCommentDto>>> GetSchoolCommentsAsync(ListRequestDto<SchoolComment>? requestDto = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var result = await uow.GetRepository<SchoolComment>().GetManyQueryable(requestDto?.Specification).FilterListAsync(requestDto?.PagingDto);
                var users = await result.List.Select(t => new SchoolCommentDto
                {
                    Id = t.Id,
                    Comment = t.Comment,
                    CreationUser = t.CreationUser!.FirstName + " " + t.CreationUser.LastName,
                    CreationUserAvatar = t.CreationUser!.Avatar,
                    CreationDate = t.CreationDate,
                    LikeCount = t.LikeCount,
                    DislikeCount = t.DislikeCount,
                    AverageRate = t.AverageRate,
                }).ToListAsync();
                return new(OperationResult.Succeeded) { Data = new() { List = users, TotalRecordsCount = result.TotalRecordsCount } };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<bool>> LikeSchoolCommentAsync([NotNull] SchoolCommentReactionRequestDto requestDto)
        {
            try
            {
                var specification = new IdEqualsSpecification<SchoolComment, long>(requestDto.CommentId)
                    .And(new SchoolIdEqualsSpecification<SchoolComment>(requestDto.SchoolId));
                var exists = await CommentExistsAsync(specification);
                if (!exists.Data)
                {
                    return new(OperationResult.NotFound)
                    {
                        Errors = [new() { Message = Localizer.Value["InvalidRequest"] },],
                    };
                }

                var reactionResult = await reactionService.Value.ManageReactionAsync(new()
                {
                    CategoryType = CategoryType.SchoolComment,
                    CreationDate = DateTimeOffset.UtcNow,
                    CreationUserId = HttpContextAccessor.Value.HttpContext.UserId(),
                    IdentifierId = requestDto.CommentId,
                    IsLike = true,
                });
                if (reactionResult.OperationResult is not OperationResult.Succeeded)
                {
                    return new(OperationResult.Failed) { Errors = reactionResult.Errors };
                }

                var result = await UpdateSchoolCommentReactionsAsync(requestDto.CommentId);

                return result;
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> DislikeSchoolCommentAsync([NotNull] SchoolCommentReactionRequestDto requestDto)
        {
            try
            {
                var specification = new IdEqualsSpecification<SchoolComment, long>(requestDto.CommentId)
                    .And(new SchoolIdEqualsSpecification<SchoolComment>(requestDto.SchoolId));
                var exists = await CommentExistsAsync(specification);
                if (!exists.Data)
                {
                    return new(OperationResult.NotFound)
                    {
                        Errors = [new() { Message = Localizer.Value["InvalidRequest"] },],
                    };
                }

                var reactionResult = await reactionService.Value.ManageReactionAsync(new()
                {
                    CategoryType = CategoryType.SchoolComment,
                    CreationDate = DateTimeOffset.UtcNow,
                    CreationUserId = HttpContextAccessor.Value.HttpContext.UserId(),
                    IdentifierId = requestDto.CommentId,
                    IsLike = false,
                });
                if (reactionResult.OperationResult is not OperationResult.Succeeded)
                {
                    return new(OperationResult.Failed) { Errors = reactionResult.Errors };
                }

                var result = await UpdateSchoolCommentReactionsAsync(requestDto.CommentId);

                return result;
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<long>> CreateSchoolCommentContributionAsync([NotNull] ManageSchoolCommentContributionRequestDto requestDto)
        {
            try
            {
                var commentSpecification = new SchoolIdEqualsSpecification<SchoolComment>(requestDto.SchoolId)
                        .And(new CreationUserIdEqualsSpecification<SchoolComment, ApplicationUser, int>(requestDto.CreationUserId));
                var commentExists = await CommentExistsAsync(commentSpecification);
                if (commentExists.Data)
                {
                    return new(OperationResult.Failed) { Errors = [new() { Message = "Comment Exists for Current User and School", }] };
                }

                var contributionSpecification = new CreationUserIdEqualsSpecification<Contribution, ApplicationUser, int>(requestDto.CreationUserId)
                    .And(new IdentifierIdEqualsSpecification<Contribution>(requestDto.SchoolId))
                    .And(
                        new StatusEqualsSpecification<Contribution>(Status.Draft)
                        .Or(new StatusEqualsSpecification<Contribution>(Status.Review))
                    );
                var contributionExists = await contributionService.Value.ExistsContributionAsync(contributionSpecification);
                if (contributionExists.Data)
                {
                    return new(OperationResult.Failed) { Errors = [new() { Message = "there is a pending Comment", }] };
                }

                SchoolCommentContributionDto dto = new()
                {
                    SchoolId = requestDto.SchoolId,
                    Comment = requestDto.Comment,
                    CreationDate = requestDto.CreationDate,
                    CreationUserId = requestDto.CreationUserId,
                    ArtisticActivitiesRate = requestDto.ArtisticActivitiesRate,
                    BehaviorRate = requestDto.BehaviorRate,
                    ClassesQualityRate = requestDto.ClassesQualityRate,
                    EducationRate = requestDto.EducationRate,
                    FacilitiesRate = requestDto.FacilitiesRate,
                    ITTrainingRate = requestDto.ITTrainingRate,
                    SafetyAndHappinessRate = requestDto.SafetyAndHappinessRate,
                    TuitionRatioRate = requestDto.TuitionRatioRate,
                    AverageRate = CalculateAverageRate(requestDto),
                };

                var contributionResult = await contributionService.Value.ManageContributionAsync(new ManageContributionRequestDto<SchoolCommentContributionDto>
                {
                    CategoryType = CategoryType.SchoolComment,
                    IdentifierId = requestDto.SchoolId,
                    Status = Status.Review,
                    Data = dto,
                });
                if (contributionResult.OperationResult is not OperationResult.Succeeded)
                {
                    return new(contributionResult.OperationResult) { Errors = contributionResult.Errors };
                }

                var hasAutoConfirmSchoolComment = await identityService.Value.HasClaimAsync(requestDto.CreationUserId, SystemClaim.AutoConfirmSchoolComment);
                if (hasAutoConfirmSchoolComment.Data || configuration.Value.GetValue<bool>("AutoConfirmComments"))
                {
                    _ = await ConfirmSchoolCommentContributionAsync(new()
                    {
                        ContributionId = contributionResult.Data,
                    });
                }

                return new(OperationResult.Succeeded) { Data = contributionResult.Data };

                static double CalculateAverageRate(ManageSchoolCommentContributionRequestDto dto) => (
                        dto.ArtisticActivitiesRate +
                        dto.BehaviorRate +
                        dto.ClassesQualityRate +
                        dto.EducationRate +
                        dto.FacilitiesRate +
                        dto.ITTrainingRate +
                        dto.SafetyAndHappinessRate +
                        dto.TuitionRatioRate
                        ) / 8;
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        public async Task<ResultData<bool>> ConfirmSchoolCommentContributionAsync([NotNull] ConfirmSchoolCommentContributionRequestDto requestDto)
        {
            try
            {
                var contributionSpecification = new IdEqualsSpecification<Contribution, long>(requestDto.ContributionId)
                    .And(new CategoryTypeEqualsSpecification<Contribution>(CategoryType.SchoolComment));
                var result = await contributionService.Value.ConfirmContributionAsync<SchoolCommentContributionDto>(contributionSpecification);
                if (result.Data is null)
                {
                    return new(OperationResult.Failed) { Errors = result.Errors };
                }

                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var schoolImageRepository = uow.GetRepository<SchoolComment>();
                schoolImageRepository.Add(new()
                {
                    SchoolId = result.Data.Data!.SchoolId,
                    Comment = result.Data.Data!.Comment,
                    AverageRate = result.Data.Data!.AverageRate,
                    ArtisticActivitiesRate = result.Data.Data!.ArtisticActivitiesRate,
                    BehaviorRate = result.Data.Data!.BehaviorRate,
                    ClassesQualityRate = result.Data.Data!.ClassesQualityRate,
                    EducationRate = result.Data.Data!.EducationRate,
                    FacilitiesRate = result.Data.Data!.FacilitiesRate,
                    ITTrainingRate = result.Data.Data!.ITTrainingRate,
                    SafetyAndHappinessRate = result.Data.Data!.SafetyAndHappinessRate,
                    TuitionRatioRate = result.Data.Data!.TuitionRatioRate,
                    CreationUserId = result.Data.Data!.CreationUserId,
                    CreationDate = result.Data.Data!.CreationDate,
                });
                _ = await uow.SaveChangesAsync();

                //this is temporary
                _ = await UpdateSchoolScoreAsync(result.Data.Data!.SchoolId);

                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> CommentExistsAsync([NotNull] ISpecification<SchoolComment> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var exists = await uow.GetRepository<SchoolComment>().AnyAsync(specification);

                return new(OperationResult.Succeeded) { Data = exists };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        #endregion

        #region Images

        public async Task<ResultData<ListDataSource<SchoolImageDto>>> GetSchoolImagesAsync(ListRequestDto<SchoolImage>? requestDto = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var result = await uow.GetRepository<SchoolImage>().GetManyQueryable(requestDto?.Specification).FilterListAsync(requestDto?.PagingDto);
                var users = await result.List.Select(t => new SchoolImageDto
                {
                    Id = t.Id,
                    CreationUser = t.CreationUser!.FirstName + " " + t.CreationUser.LastName,
                    CreationUserAvatar = t.CreationUser!.Avatar,
                    CreationDate = t.CreationDate,
                    FileId = t.FileId,
                    FileType = t.FileType,
                    SchoolId = t.SchoolId,
                    SchoolName = t.School!.Name,
                    TagId = t.TagId,
                    IsDefault = t.IsDefault,
                }).ToListAsync();
                return new(OperationResult.Succeeded) { Data = new() { List = users, TotalRecordsCount = result.TotalRecordsCount } };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<IEnumerable<SchoolImageInfoDto>>> GetSchoolImagesListAsync([NotNull] ISpecification<SchoolImage> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var result = await uow.GetRepository<SchoolImage>().GetManyQueryable(specification)
                    .Select(t => new
                    {
                        t.Id,
                        t.FileId,
                        t.CreationUserId,
                        CreationUser = t.CreationUser.FirstName + " " + t.CreationUser.LastName,
                        TagName = t.Tag != null ? t.Tag.Name : null,
                        TagIcon = t.Tag != null ? t.Tag.Icon : null,
                        t.TagId,
                        t.IsDefault,
                    }).ToListAsync();

                return new(OperationResult.Succeeded)
                {
                    Data = result.Select(t =>
                        new SchoolImageInfoDto
                        {
                            Id = t.Id,
                            FileUri = fileService.Value.GetFileUri(t.FileId, ContainerType.School).Data,
                            CreationUserId = t.CreationUserId,
                            CreationUser = t.CreationUser,
                            TagName = t.TagName,
                            TagIcon = t.TagIcon,
                            TagId = t.TagId,
                            IsDefault = t.IsDefault,
                        })
                };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<long>> CreateSchoolImageContributionAsync([NotNull] ManageSchoolImageContributionRequestDto requestDto)
        {
            try
            {
                if (requestDto.TagId.HasValue)
                {
                    var specification = new IdEqualsSpecification<Domain.Entity.Tag, long>(requestDto.TagId.Value)
                        .And(new TagTypeEqualsSpecification(TagType.School));
                    var exists = await tagService.Value.ExistsTagAsync(specification);
                    if (!exists.Data)
                    {
                        return new(OperationResult.Failed) { Errors = [new() { Message = "Tag not found", }] };
                    }
                }

                using MemoryStream stream = new();
                await requestDto.File.CopyToAsync(stream);

                var fileId = await fileService.Value.UploadFileAsync(new()
                {
                    File = stream.ToArray(),
                    ContainerType = ContainerType.School,
                    FileExtension = Path.GetExtension(requestDto.File.FileName),
                });
                if (fileId.OperationResult is not OperationResult.Succeeded)
                {
                    return new(fileId.OperationResult) { Errors = fileId.Errors, };
                }

                SchoolImageContributionDto dto = new()
                {
                    FileId = fileId.Data,
                    FileType = requestDto.FileType,
                    SchoolId = requestDto.SchoolId,
                    TagId = requestDto.TagId,
                    IsDefault = requestDto.IsDefault,
                };
                var contributionResult = await contributionService.Value.ManageContributionAsync(new ManageContributionRequestDto<SchoolImageContributionDto>
                {
                    CategoryType = CategoryType.SchoolImage,
                    IdentifierId = requestDto.SchoolId,
                    Status = Status.Review,
                    Data = dto,
                });
                if (contributionResult.OperationResult is not OperationResult.Succeeded)
                {
                    return new(contributionResult.OperationResult) { Errors = contributionResult.Errors };
                }

                var hasAutoConfirmSchoolImage = await identityService.Value.HasClaimAsync(requestDto.CreationUserId, SystemClaim.AutoConfirmSchoolImage);
                if (hasAutoConfirmSchoolImage.Data || await IsImageLocationNearSchoolAsync())
                {
                    _ = await ConfirmSchoolImageContributionAsync(new()
                    {
                        ContributionId = contributionResult.Data,
                    });
                }

                return new(OperationResult.Succeeded) { Data = contributionResult.Data };

                async Task<bool> IsImageLocationNearSchoolAsync()
                {
                    try
                    {
                        var gps = ImageMetadataReader.ReadMetadata(stream)
                             .OfType<GpsDirectory>()
                             .FirstOrDefault()?.GetGeoLocation();
                        if (gps is not null)
                        {
                            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(4326);
                            var point = geometryFactory.CreatePoint(new Coordinate(gps.Longitude, gps.Latitude));

                            var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                            var schoolRepository = uow.GetRepository<School>();
                            var schoolCoordinates = await schoolRepository.GetManyQueryable(t => t.Id == requestDto.SchoolId).Select(t => t.Coordinates).FirstOrDefaultAsync();
                            if (schoolCoordinates is not null && schoolCoordinates.Distance(point) < 200)
                            {
                                return true;
                            }
                        }
                    }
                    catch
                    {
                        //ignore
                    }

                    return false;
                }
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        public async Task<ResultData<bool>> ConfirmSchoolImageContributionAsync([NotNull] ConfirmSchoolImageContributionRequestDto requestDto)
        {
            try
            {
                var contributionSpecification = new IdEqualsSpecification<Contribution, long>(requestDto.ContributionId)
                    .And(new CategoryTypeEqualsSpecification<Contribution>(CategoryType.SchoolImage));
                var result = await contributionService.Value.ConfirmContributionAsync<SchoolImageContributionDto>(contributionSpecification);
                if (result.Data is null)
                {
                    return new(OperationResult.Failed) { Errors = result.Errors };
                }

                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var schoolImageRepository = uow.GetRepository<SchoolImage>();
                var schoolImage = new SchoolImage
                {
                    FileId = result.Data.Data!.FileId,
                    FileType = result.Data.Data!.FileType,
                    SchoolId = result.Data.Data!.SchoolId,
                    TagId = result.Data.Data!.TagId,
                    IsDefault = result.Data.Data!.IsDefault,
                    CreationUserId = result.Data.CreationUserId,
                    CreationDate = result.Data.CreationDate,
                    ContributionId = requestDto.ContributionId,
                };
                schoolImageRepository.Add(schoolImage);
                _ = await uow.SaveChangesAsync();

                if (result.Data.Data!.IsDefault)
                {
                    _ = await SetDefaultSchoolImageAsync(new()
                    {
                        Id = schoolImage.Id,
                        SchoolId = schoolImage.SchoolId,
                    });
                }

                var schoolRepository = uow.GetRepository<School>();
                _ = await schoolRepository.GetManyQueryable(t => t.Id == result.Data.Data!.SchoolId).ExecuteUpdateAsync(t => t
                    .SetProperty(p => p.LastModifyUserId, result.Data.CreationUserId)
                    .SetProperty(p => p.LastModifyDate, DateTimeOffset.UtcNow));

                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> RemoveSchoolImageAsync([NotNull] ISpecification<SchoolImage> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<SchoolImage>();
                var schoolImage = await repository.GetAsync(specification);
                if (schoolImage is null)
                {
                    return new(OperationResult.NotFound) { Errors = [new() { Message = Localizer.Value["SchoolImageNotFound"] },], };
                }

                repository.Remove(schoolImage);
                _ = await uow.SaveChangesAsync();

                _ = await fileService.Value.RemoveFileAsync(new()
                {
                    FileId = schoolImage.FileId!,
                    ContainerType = ContainerType.School
                });

                _ = await contributionService.Value.DeleteContributionAsync(new IdEqualsSpecification<Contribution, long>(schoolImage.ContributionId.GetValueOrDefault()));

                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> ManageSchoolImageAsync([NotNull] ManageSchoolImageRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<SchoolImage>();

                var specification = new IdEqualsSpecification<SchoolImage, long>(requestDto.Id)
                    .And(new SchoolIdEqualsSpecification<SchoolImage>(requestDto.SchoolId));
                var schoolImage = await repository.GetAsync(specification);
                if (schoolImage is null)
                {
                    return new(OperationResult.NotFound) { Errors = [new() { Message = Localizer.Value["SchoolImageNotFound"] },], };
                }

                if (requestDto.TagId.HasValue)
                {
                    var tagSpecification = new IdEqualsSpecification<Domain.Entity.Tag, long>(requestDto.TagId.Value)
                        .And(new TagTypeEqualsSpecification(TagType.School));
                    var exists = await tagService.Value.ExistsTagAsync(tagSpecification);
                    if (!exists.Data)
                    {
                        return new(OperationResult.NotFound) { Errors = [new() { Message = Localizer.Value["TagNotFound"] },], };
                    }
                }

                schoolImage.TagId = requestDto.TagId;
                schoolImage.IsDefault = requestDto.IsDefault;

                _ = repository.Update(schoolImage);
                _ = await uow.SaveChangesAsync();

                if (requestDto.IsDefault)
                {
                    _ = await SetDefaultSchoolImageAsync(new()
                    {
                        Id = schoolImage.Id,
                        SchoolId = schoolImage.SchoolId,
                    });
                }

                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["InvalidTagId"], }] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        public async Task<ResultData<bool>> SetDefaultSchoolImageAsync([NotNull] SetDefaultSchoolImageRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var affectedRows = await uow.GetRepository<SchoolImage>().GetManyQueryable(t => t.SchoolId == requestDto.SchoolId)
                    .ExecuteUpdateAsync(t => t.SetProperty(p => p.IsDefault, p => p.Id == requestDto.Id));

                return new(OperationResult.Succeeded) { Data = affectedRows > 0 };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        #endregion

        #region Contributions

        public async Task<ResultData<long>> ManageSchoolContributionAsync([NotNull] ManageSchoolContributionRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var exists = await uow.GetRepository<School>().AnyAsync(t => t.Id == requestDto.SchoolId);
                if (!exists)
                {
                    return new(OperationResult.Failed) { Errors = [new() { Message = "School not found", },] };
                }

                if (requestDto.Id.HasValue)
                {
                    var specification = new IdEqualsSpecification<Contribution, long>(requestDto.Id.Value)
                        .And(new CreationUserIdEqualsSpecification<Contribution, ApplicationUser, int>(requestDto.UserId))
                        .And(new CategoryTypeEqualsSpecification<Contribution>(CategoryType.School))
                        .And(
                            new StatusEqualsSpecification<Contribution>(Status.Draft)
                            .Or(new StatusEqualsSpecification<Contribution>(Status.Rejected))
                            .Or(new StatusEqualsSpecification<Contribution>(Status.Review))
                        );
                    if (requestDto.SchoolId.HasValue)
                    {
                        specification = specification.And(new IdentifierIdEqualsSpecification<Contribution>(requestDto.SchoolId.Value));
                    }

                    var data = await contributionService.Value.ExistsContributionAsync(specification);
                    if (!data.Data)
                    {
                        return new(data.OperationResult) { Errors = data.Errors };
                    }
                }

                var contributionResult = await contributionService.Value.ManageContributionAsync(new ManageContributionRequestDto<SchoolContributionDto>
                {
                    CategoryType = CategoryType.School,
                    IdentifierId = requestDto.SchoolId,
                    Status = Status.Review,
                    Data = requestDto.SchoolContribution,
                    Id = requestDto.Id,
                });
                if (contributionResult.OperationResult is not OperationResult.Succeeded)
                {
                    return new(contributionResult.OperationResult) { Errors = contributionResult.Errors };
                }

                var hasAutoConfirmSchoolContribution = await identityService.Value.HasClaimAsync(requestDto.UserId, SystemClaim.AutoConfirmSchoolContribution);
                if (hasAutoConfirmSchoolContribution.Data)
                {
                    _ = await ConfirmSchoolContributionAsync(new()
                    {
                        ContributionId = contributionResult.Data,
                        SchoolId = requestDto.SchoolId,
                    });
                }

                return new(OperationResult.Succeeded) { Data = contributionResult.Data };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> ConfirmSchoolContributionAsync([NotNull] ConfirmSchoolContributionRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();

                if (requestDto.SchoolId.HasValue)
                {
                    var schoolExists = await uow.GetRepository<School>().AnyAsync(t => t.Id == requestDto.SchoolId.Value);
                    if (!schoolExists)
                    {
                        return new(OperationResult.Failed) { Errors = [new() { Message = "School not found", },] };
                    }
                }

                var contributionSpecification = new IdEqualsSpecification<Contribution, long>(requestDto.ContributionId)
                    .And(new CategoryTypeEqualsSpecification<Contribution>(CategoryType.School));
                if (requestDto.SchoolId.HasValue)
                {
                    contributionSpecification = contributionSpecification.And(new IdentifierIdEqualsSpecification<Contribution>(requestDto.SchoolId.Value));
                }

                var contributionResult = await contributionService.Value.ConfirmContributionAsync<SchoolContributionDto>(contributionSpecification);
                if (contributionResult.Data is null)
                {
                    return new(OperationResult.Failed) { Errors = contributionResult.Errors };
                }

                ManageSchoolRequestDto manageSchoolRequestDto = new()
                {
                    Address = contributionResult.Data.Data!.Address,
                    CityId = contributionResult.Data.Data!.CityId,
                    CountryId = contributionResult.Data.Data!.CountryId,
                    Email = contributionResult.Data.Data!.Email,
                    FaxNumber = contributionResult.Data.Data!.FaxNumber,
                    LocalAddress = contributionResult.Data.Data!.LocalAddress,
                    LocalName = contributionResult.Data.Data!.LocalName,
                    Name = contributionResult.Data.Data!.Name,
                    PhoneNumber = contributionResult.Data.Data!.PhoneNumber,
                    Quarter = contributionResult.Data.Data!.Quarter,
                    SchoolType = contributionResult.Data.Data!.SchoolType,
                    StateId = contributionResult.Data.Data!.StateId,
                    WebSite = contributionResult.Data.Data!.WebSite,
                    ZipCode = contributionResult.Data.Data!.ZipCode,
                    Id = requestDto.SchoolId,
                    Tags = contributionResult.Data.Data!.Tags,
                    UserId = contributionResult.Data.CreationUserId,
                    Date = contributionResult.Data.CreationDate,
                };
                if (contributionResult.Data.Data!.Latitude.HasValue && contributionResult.Data.Data!.Longitude.HasValue)
                {
                    var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(4326);
                    manageSchoolRequestDto.Coordinates = geometryFactory.CreatePoint(new Coordinate(contributionResult.Data.Data!.Longitude.Value, contributionResult.Data.Data!.Latitude.Value));
                }
                var manageSchoolResult = await ManageSchoolAsync(manageSchoolRequestDto);

                if (contributionResult.Data.Data!.DefaultImageId.HasValue && requestDto.SchoolId.HasValue)
                {
                    _ = await SetDefaultSchoolImageAsync(new()
                    {
                        Id = contributionResult.Data.Data!.DefaultImageId.Value,
                        SchoolId = requestDto.SchoolId.Value,
                    });
                }

                return manageSchoolResult.OperationResult is OperationResult.Succeeded
                    ? new(OperationResult.Failed) { Errors = contributionResult.Errors }
                    : new(OperationResult.Succeeded) { Data = true };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        #endregion

        #region Issues

        public async Task<ResultData<long>> CreateSchoolIssuesContributionAsync([NotNull] CreateSchoolIssuesContributionRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<School>();
                var schoolExists = await repository.AnyAsync(t => t.Id == requestDto.SchoolId && !t.IsDeleted);
                if (!schoolExists)
                {
                    return new(OperationResult.Failed) { Errors = [new() { Message = "School not found", },] };
                }

                var contributionSpecification = new CreationUserIdEqualsSpecification<Contribution, ApplicationUser, int>(requestDto.CreationUserId)
                    .And(new IdentifierIdEqualsSpecification<Contribution>(requestDto.SchoolId))
                    .And(
                        new StatusEqualsSpecification<Contribution>(Status.Draft)
                        .Or(new StatusEqualsSpecification<Contribution>(Status.Review))
                    );
                var contributionExists = await contributionService.Value.ExistsContributionAsync(contributionSpecification);
                if (contributionExists.Data)
                {
                    return new(OperationResult.Failed) { Errors = [new() { Message = "there is a pending issues", }] };
                }

                var contributionResult = await contributionService.Value.ManageContributionAsync<string>(new()
                {
                    CategoryType = CategoryType.SchoolIssues,
                    IdentifierId = requestDto.SchoolId,
                    Status = Status.Review,
                    Data = requestDto.Description,
                });
                if (contributionResult.OperationResult is not OperationResult.Succeeded)
                {
                    return new(contributionResult.OperationResult) { Errors = contributionResult.Errors };
                }

                var hasAutoConfirmSchoolContribution = await identityService.Value.HasClaimAsync(requestDto.CreationUserId, SystemClaim.AutoConfirmSchoolContribution);
                if (hasAutoConfirmSchoolContribution.Data)
                {
                    _ = await ConfirmSchoolIssuesContributionAsync(new()
                    {
                        ContributionId = contributionResult.Data,
                    });
                }

                return new(OperationResult.Succeeded) { Data = contributionResult.Data };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        public async Task<ResultData<bool>> ConfirmSchoolIssuesContributionAsync([NotNull] ConfirmSchoolIssuesContributionRequestDto requestDto)
        {
            try
            {
                var specification = new IdEqualsSpecification<Contribution, long>(requestDto.ContributionId)
                    .And(new CategoryTypeEqualsSpecification<Contribution>(CategoryType.SchoolIssues));
                var result = await contributionService.Value.ConfirmContributionAsync<string>(specification);
                if (result.Data is null)
                {
                    return new(OperationResult.Failed) { Errors = result.Errors };
                }

                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<School>();
                var school = await repository.GetAsync(result.Data.IdentifierId.GetValueOrDefault());
                if (school is null)
                {
                    return new(OperationResult.Failed) { Errors = [new() { Message = "School not found", },] };
                }

                school.IsDeleted = true;
                _ = repository.Update(school);
                _ = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        #endregion

        #region Job

        public async Task<ResultData<bool>> UpdateSchoolScoreAsync(long? schoolId = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var where = schoolId.HasValue ? $"WHERE s.Id={schoolId.Value}" : "";
                var query = $@"UPDATE s SET
                    Score=(SELECT AVG(c.AverageRate) FROM SchoolComments c WHERE c.SchoolId = s.Id)
                FROM Schools s {where}";
                _ = await uow.ExecuteSqlCommandAsync(query);

                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> UpdateSchoolCommentReactionsAsync(long? schoolCommentId = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var where = schoolCommentId.HasValue ? $"WHERE c.Id={schoolCommentId.Value}" : "";
                var query = $@"UPDATE c SET
                    LikeCount=(SELECT COUNT(1) FROM Reactions r WHERE r.CategoryType={CategoryType.SchoolComment.Value} AND r.IdentifierId=c.Id AND r.IsLike=1)
                    ,DislikeCount=(SELECT COUNT(1) FROM Reactions r WHERE r.CategoryType={CategoryType.SchoolComment.Value} AND r.IdentifierId=c.Id AND r.IsLike=0)
                FROM SchoolComments c {where}";
                _ = await uow.ExecuteSqlCommandAsync(query);

                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> RemoveOldRejectedSchoolImagesAsync()
        {
            try
            {
                var lst = await contributionService.Value.GetContributionsAsync<SchoolImageContributionDto>(new()
                {
                    PagingDto = new() { PageFilter = new() { ReturnTotalRecordsCount = false, Size = 1000 } },
                    Specification = new CategoryTypeEqualsSpecification<Contribution>(CategoryType.SchoolImage)
                        .And(new StatusEqualsSpecification<Contribution>(Status.Rejected)),
                }, true);

                if (lst.Data.List is null)
                {
                    return new(OperationResult.Succeeded) { Data = true };
                }

                foreach (var item in lst.Data.List)
                {
                    var days = configuration.Value.GetValue<int>("DaysDistanceForRemoveOldRejectedSchoolImages") * -1;
                    if (!item.LastModifyDate.HasValue || item.LastModifyDate.Value > DateTimeOffset.UtcNow.AddDays(days))
                    {
                        continue;
                    }

                    if (item.Data is null)
                    {
                        continue;
                    }

                    var result = await fileService.Value.RemoveFileAsync(new()
                    {
                        FileId = item.Data.FileId!,
                        ContainerType = ContainerType.School,
                    });
                    if (result.Data)
                    {
                        _ = await contributionService.Value.ManageContributionAsync<SchoolImageContributionDto>(new()
                        {
                            CategoryType = item.CategoryType!,
                            Id = item.Id,
                            Status = Status.Deleted,
                            IdentifierId = item.IdentifierId,
                            Data = item.Data,
                            Comment = item.Comment,
                        });
                    }
                }

                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        #endregion
    }
}
