namespace GamaEdtech.Application.Service
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    using EntityFramework.Exceptions.Common;

    using GamaEdtech.Application.Interface;
    using GamaEdtech.Common.Core;
    using GamaEdtech.Common.Core.Extensions.Linq;
    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.DataAccess.Specification;
    using GamaEdtech.Common.DataAccess.Specification.Impl;
    using GamaEdtech.Common.DataAccess.UnitOfWork;
    using GamaEdtech.Common.Service;
    using GamaEdtech.Data.Dto.Blog;
    using GamaEdtech.Data.Dto.Contribution;
    using GamaEdtech.Data.Dto.Tag;
    using GamaEdtech.Domain.Entity;
    using GamaEdtech.Domain.Entity.Identity;
    using GamaEdtech.Domain.Enumeration;
    using GamaEdtech.Domain.Specification;
    using GamaEdtech.Domain.Specification.Post;

    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;

    using static GamaEdtech.Common.Core.Constants;

    public class BlogService(Lazy<IUnitOfWorkProvider> unitOfWorkProvider, Lazy<IHttpContextAccessor> httpContextAccessor, Lazy<IStringLocalizer<BlogService>> localizer
        , Lazy<ILogger<BlogService>> logger, Lazy<IReactionService> reactionService, Lazy<IFileService> fileService, Lazy<IIdentityService> identityService
        , Lazy<IContributionService> contributionService, Lazy<ITagService> tagService, Lazy<IConfiguration> configuration)
        : LocalizableServiceBase<BlogService>(unitOfWorkProvider, httpContextAccessor, localizer, logger), IBlogService
    {
        public async Task<ResultData<ListDataSource<PostsDto>>> GetPostsAsync(ListRequestDto<Post>? requestDto = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var lst = await uow.GetRepository<Post>().GetManyQueryable(requestDto?.Specification).FilterListAsync(requestDto?.PagingDto);
                var blogs = await lst.List.Select(t => new
                {
                    t.Id,
                    t.Title,
                    t.Slug,
                    t.Summary,
                    t.LikeCount,
                    t.DislikeCount,
                    t.ImageId,
                    t.PublishDate,
                    t.VisibilityType,
                }).ToListAsync();

                var result = blogs.Select(t => new PostsDto
                {
                    Id = t.Id,
                    DislikeCount = t.DislikeCount,
                    LikeCount = t.LikeCount,
                    Summary = t.Summary,
                    Title = t.Title,
                    Slug = t.Slug,
                    ImageUri = fileService.Value.GetFileUri(t.ImageId, ContainerType.Post).Data,
                    PublishDate = t.PublishDate,
                    VisibilityType = t.VisibilityType,
                });

                return new(OperationResult.Succeeded) { Data = new() { List = result, TotalRecordsCount = lst.TotalRecordsCount } };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<PostDto>> GetPostAsync([NotNull] ISpecification<Post> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var post = await uow.GetRepository<Post>().GetManyQueryable(specification).Select(t => new
                {
                    t.Title,
                    t.Slug,
                    t.Summary,
                    t.Body,
                    t.ImageId,
                    t.LikeCount,
                    t.DislikeCount,
                    t.VisibilityType,
                    CreationUser = t.CreationUser.FirstName + " " + t.CreationUser.LastName,
                    t.PublishDate,
                    t.Keywords,
                    Tags = t.PostTags == null ? null : t.PostTags.Select(s => new TagDto
                    {
                        Icon = s.Tag.Icon,
                        Id = s.TagId,
                        Name = s.Tag.Name,
                        TagType = s.Tag.TagType,
                    }),
                }).FirstOrDefaultAsync();
                if (post is null)
                {
                    return new(OperationResult.NotFound)
                    {
                        Errors = [new() { Message = Localizer.Value["PostNotFound"] },],
                    };
                }

                PostDto result = new()
                {
                    Title = post.Title,
                    Slug = post.Slug,
                    Summary = post.Summary,
                    Body = post.Body,
                    ImageUri = fileService.Value.GetFileUri(post.ImageId, ContainerType.Post).Data,
                    LikeCount = post.LikeCount,
                    DislikeCount = post.DislikeCount,
                    CreationUser = post.CreationUser,
                    Tags = post.Tags,
                    VisibilityType = post.VisibilityType,
                    PublishDate = post.PublishDate,
                    Keywords = post.Keywords,
                };

                return new(OperationResult.Succeeded) { Data = result };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message },] };
            }
        }

        public async Task<ResultData<long>> ManagePostContributionAsync([NotNull] ManagePostContributionRequestDto requestDto)
        {
            try
            {
                if (requestDto.ContributionId.HasValue)
                {
                    var specification = new IdEqualsSpecification<Contribution, long>(requestDto.ContributionId.Value)
                        .And(new CreationUserIdEqualsSpecification<Contribution, ApplicationUser, int>(requestDto.UserId))
                        .And(new CategoryTypeEqualsSpecification<Contribution>(CategoryType.Post))
                        .And(
                            new StatusEqualsSpecification<Contribution>(Status.Draft)
                            .Or(new StatusEqualsSpecification<Contribution>(Status.Rejected))
                            .Or(new StatusEqualsSpecification<Contribution>(Status.Review))
                        );
                    var data = await contributionService.Value.ExistsContributionAsync(specification);

                    if (data.OperationResult is not OperationResult.Succeeded)
                    {
                        return new(data.OperationResult) { Errors = data.Errors };
                    }

                    if (!data.Data)
                    {
                        return new(OperationResult.NotValid) { Errors = [new() { Message = "Invalid Blog Status", }] };
                    }
                }

                var exists = await PostExistsAsync(new SlugEqualsSpecification(requestDto.Slug!));
                if (exists.Data)
                {
                    return new(OperationResult.Duplicate) { Errors = [new() { Message = Localizer.Value["DuplicateSlug"] },], };
                }

                if (requestDto.Tags?.Any() == true)
                {
                    var count = await tagService.Value.GetTagsCountAsync(new IdContainsSpecification<Tag, long>(requestDto.Tags));
                    if (count.Data != requestDto.Tags.Count())
                    {
                        return new(OperationResult.Duplicate) { Errors = [new() { Message = Localizer.Value["InvalidTag"] },], };
                    }
                }

                var (imageId, errors) = await SaveImageAsync(requestDto.Image);
                if (errors is not null)
                {
                    return new(OperationResult.Failed)
                    {
                        Errors = errors,
                    };
                }

                PostContributionDto dto = new()
                {
                    CreationDate = DateTimeOffset.UtcNow,
                    CreationUserId = requestDto.UserId,
                    Body = requestDto.Body,
                    ImageId = imageId,
                    Summary = requestDto.Summary,
                    Tags = requestDto.Tags,
                    Title = requestDto.Title,
                    Slug = requestDto.Slug,
                    PublishDate = requestDto.PublishDate,
                    VisibilityType = requestDto.VisibilityType,
                    Keywords = requestDto.Keywords,
                    Draft = requestDto.Draft,
                };

                var contributionResult = await contributionService.Value.ManageContributionAsync(new ManageContributionRequestDto<PostContributionDto>
                {
                    CategoryType = CategoryType.Post,
                    IdentifierId = null,
                    Status = requestDto.Draft.GetValueOrDefault() ? Status.Draft : Status.Review,
                    Data = dto,
                    Id = requestDto.ContributionId,
                });
                if (contributionResult.OperationResult is not OperationResult.Succeeded)
                {
                    return new(contributionResult.OperationResult) { Errors = contributionResult.Errors };
                }

                if (!requestDto.Draft.GetValueOrDefault())
                {
                    var hasAutoConfirmSchoolComment = await identityService.Value.HasClaimAsync(requestDto.UserId, SystemClaim.AutoConfirmPost);
                    if (hasAutoConfirmSchoolComment.Data || configuration.Value.GetValue<bool>("AutoConfirmPosts"))
                    {
                        _ = await ConfirmPostContributionAsync(new()
                        {
                            ContributionId = contributionResult.Data,
                        });
                    }
                }

                return new(OperationResult.Succeeded) { Data = contributionResult.Data };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        public async Task<ResultData<long>> ManagePostAsync([NotNull] ManagePostRequestDto requestDto)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var repository = uow.GetRepository<Post>();
                Post? post = null;

                var (imageId, errors) = await SaveImageAsync(requestDto.Image);
                if (errors is not null)
                {
                    return new(OperationResult.Failed)
                    {
                        Errors = errors,
                    };
                }

                if (requestDto.Id.HasValue)
                {
                    post = await repository.GetAsync(requestDto.Id.Value, includes: (t) => t.Include(s => s.PostTags));
                    if (post is null)
                    {
                        return new(OperationResult.NotFound)
                        {
                            Errors = [new() { Message = Localizer.Value["SchoolNotFound"] },],
                        };
                    }

                    post.Slug = requestDto.Slug ?? post.Slug;
                    post.Title = requestDto.Title ?? post.Title;
                    post.Summary = requestDto.Summary ?? post.Summary;
                    post.Body = requestDto.Body ?? post.Body;
                    post.ImageId = imageId ?? post.ImageId;
                    post.PublishDate = requestDto.PublishDate ?? post.PublishDate;
                    post.VisibilityType = requestDto.VisibilityType ?? post.VisibilityType;
                    post.Keywords = requestDto.Keywords ?? post.Keywords;

                    _ = repository.Update(post);

                    if (requestDto.Tags?.Any() == true)
                    {
                        var postTagRepository = uow.GetRepository<PostTag>();

                        var removedTags = post.PostTags?.Where(t => requestDto.Tags is null || !requestDto.Tags.Contains(t.TagId));
                        var newTags = requestDto.Tags?.Where(t => post.PostTags is null || post.PostTags.All(s => s.TagId != t));

                        if (removedTags is not null)
                        {
                            foreach (var item in removedTags)
                            {
                                postTagRepository.Remove(item);
                            }
                        }

                        if (newTags is not null)
                        {
                            foreach (var item in newTags)
                            {
                                postTagRepository.Add(new PostTag
                                {
                                    PostId = requestDto.Id.Value,
                                    TagId = item,
                                    CreationDate = DateTimeOffset.UtcNow,
                                    CreationUserId = HttpContextAccessor.Value.HttpContext.UserId(),
                                });
                            }
                        }
                    }
                }
                else
                {
                    post = new Post
                    {
                        Slug = requestDto.Slug,
                        Title = requestDto.Title,
                        Summary = requestDto.Summary,
                        Body = requestDto.Body,
                        PublishDate = requestDto.PublishDate.GetValueOrDefault(),
                        VisibilityType = requestDto.VisibilityType!,
                        Keywords = requestDto.Keywords,
                        ImageId = imageId,
                    };
                    if (requestDto.Tags is not null)
                    {
                        post.PostTags = [.. requestDto.Tags.Select(t => new PostTag {
                            TagId = t,
                            CreationUserId = HttpContextAccessor.Value.HttpContext.UserId(),
                            CreationDate = DateTimeOffset.UtcNow,
                        })];
                    }
                    repository.Add(post);
                }

                _ = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = post.Id };
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

        public async Task<ResultData<bool>> LikePostAsync([NotNull] PostReactionRequestDto requestDto)
        {
            try
            {
                var specification = new IdEqualsSpecification<Post, long>(requestDto.PostId);
                var exists = await PostExistsAsync(specification);
                if (!exists.Data)
                {
                    return new(OperationResult.NotFound)
                    {
                        Errors = [new() { Message = Localizer.Value["InvalidRequest"] },],
                    };
                }

                var reactionResult = await reactionService.Value.ManageReactionAsync(new()
                {
                    CategoryType = CategoryType.Post,
                    CreationDate = DateTimeOffset.UtcNow,
                    CreationUserId = HttpContextAccessor.Value.HttpContext.UserId(),
                    IdentifierId = requestDto.PostId,
                    IsLike = true,
                });
                if (reactionResult.OperationResult is not OperationResult.Succeeded)
                {
                    return new(OperationResult.Failed) { Errors = reactionResult.Errors };
                }

                var result = await UpdatePostReactionsAsync(requestDto.PostId);

                return result;
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> DislikePostAsync([NotNull] PostReactionRequestDto requestDto)
        {
            try
            {
                var specification = new IdEqualsSpecification<Post, long>(requestDto.PostId);
                var exists = await PostExistsAsync(specification);
                if (!exists.Data)
                {
                    return new(OperationResult.NotFound)
                    {
                        Errors = [new() { Message = Localizer.Value["InvalidRequest"] },],
                    };
                }

                var reactionResult = await reactionService.Value.ManageReactionAsync(new()
                {
                    CategoryType = CategoryType.Post,
                    CreationDate = DateTimeOffset.UtcNow,
                    CreationUserId = HttpContextAccessor.Value.HttpContext.UserId(),
                    IdentifierId = requestDto.PostId,
                    IsLike = false,
                });
                if (reactionResult.OperationResult is not OperationResult.Succeeded)
                {
                    return new(OperationResult.Failed) { Errors = reactionResult.Errors };
                }

                var result = await UpdatePostReactionsAsync(requestDto.PostId);

                return result;
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> RemovePostAsync([NotNull] ISpecification<Post> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var postRepository = uow.GetRepository<Post>();
                var post = await postRepository.GetAsync(specification);
                if (post is null)
                {
                    return new(OperationResult.NotFound) { Errors = [new() { Message = Localizer.Value["PostNotFound"] },], };
                }

                //remove post
                postRepository.Remove(post);
                _ = await uow.SaveChangesAsync();

                //remove reactions
                var reactionSpecification = new IdentifierIdEqualsSpecification<Reaction>(post.Id)
                    .And(new CategoryTypeEqualsSpecification<Reaction>(CategoryType.Post));
                _ = reactionService.Value.RemoveReactionAsync(reactionSpecification);

                if (!string.IsNullOrEmpty(post.ImageId))
                {
                    //remove image
                    _ = await fileService.Value.RemoveFileAsync(new()
                    {
                        ContainerType = ContainerType.Post,
                        FileId = post.ImageId,
                    });
                }

                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (ReferenceConstraintException)
            {
                return new(OperationResult.NotValid) { Errors = [new() { Message = Localizer.Value["PostCantBeRemoved"], },] };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> PostExistsAsync([NotNull] ISpecification<Post> specification)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var exists = await uow.GetRepository<Post>().AnyAsync(specification);

                return new(OperationResult.Succeeded) { Data = exists };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> ConfirmPostContributionAsync([NotNull] ConfirmPostContributionRequestDto requestDto)
        {
            try
            {
                var contributionSpecification = new IdEqualsSpecification<Contribution, long>(requestDto.ContributionId)
                    .And(new CategoryTypeEqualsSpecification<Contribution>(CategoryType.Post));
                var result = await contributionService.Value.ConfirmContributionAsync<PostContributionDto>(contributionSpecification);
                if (result.Data is null)
                {
                    return new(OperationResult.Failed) { Errors = result.Errors };
                }

                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var postRepository = uow.GetRepository<Post>();
                postRepository.Add(new()
                {
                    Body = result.Data.Data!.Body,
                    CreationUserId = result.Data.Data!.CreationUserId.GetValueOrDefault(),
                    CreationDate = result.Data.Data!.CreationDate.GetValueOrDefault(),
                    ImageId = result.Data.Data!.ImageId,
                    Title = result.Data.Data!.Title,
                    Slug = result.Data.Data!.Slug,
                    Summary = result.Data.Data!.Summary,
                    PublishDate = result.Data.Data!.PublishDate.GetValueOrDefault(),
                    VisibilityType = result.Data.Data!.VisibilityType!,
                    Keywords = result.Data.Data!.Keywords,
                    PostTags = result.Data.Data!.Tags?.Select(t => new PostTag
                    {
                        CreationUserId = result.Data.Data!.CreationUserId.GetValueOrDefault(),
                        CreationDate = result.Data.Data!.CreationDate.GetValueOrDefault(),
                        TagId = t,
                    }).ToList(),
                });
                _ = await uow.SaveChangesAsync();

                return new(OperationResult.Succeeded) { Data = true };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, },] };
            }
        }

        public async Task<ResultData<bool>> IsCreatorOfPostAsync(long postId, int userId)
        {
            try
            {
                var specification = new IdEqualsSpecification<Post, long>(postId)
                    .And(new CreationUserIdEqualsSpecification<Post, ApplicationUser, int>(userId));

                var exists = await PostExistsAsync(specification);

                return new(OperationResult.Succeeded) { Data = exists.Data };
            }
            catch (Exception exc)
            {
                Logger.Value.LogException(exc);
                return new(OperationResult.Failed) { Errors = [new() { Message = exc.Message, }] };
            }
        }

        private async Task<(string? ImageId, IEnumerable<Error>? Errors)> SaveImageAsync(IFormFile? file)
        {
            if (file is null)
            {
                return (null, null);
            }

            using MemoryStream stream = new();
            await file.CopyToAsync(stream);

            var fileId = await fileService.Value.UploadFileAsync(new()
            {
                File = stream.ToArray(),
                ContainerType = ContainerType.Post,
                FileExtension = Path.GetExtension(file.FileName),
            });

            return fileId.OperationResult is OperationResult.Succeeded
                ? ((string? ImageId, IEnumerable<Error>? Errors))(fileId.Data, null)
                : new(null, fileId.Errors);
        }

        #region Job

        public async Task<ResultData<bool>> UpdatePostReactionsAsync(long? postId = null)
        {
            try
            {
                var uow = UnitOfWorkProvider.Value.CreateUnitOfWork();
                var where = postId.HasValue ? $"WHERE p.Id={postId.Value}" : "";
                var query = $@"UPDATE p SET
                    LikeCount=(SELECT COUNT(1) FROM Reactions r WHERE r.CategoryType={CategoryType.Post.Value} AND r.IdentifierId=p.Id AND r.IsLike=1)
                    ,DislikeCount=(SELECT COUNT(1) FROM Reactions r WHERE r.CategoryType={CategoryType.Post.Value} AND r.IdentifierId=p.Id AND r.IsLike=0)
                FROM Posts p {where}";
                _ = await uow.ExecuteSqlCommandAsync(query);

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
