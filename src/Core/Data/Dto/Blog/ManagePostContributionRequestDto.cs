namespace GamaEdtech.Data.Dto.Blog
{
    using System.Collections.Generic;

    using GamaEdtech.Domain.Enumeration;

    using Microsoft.AspNetCore.Http;

    public sealed class ManagePostContributionRequestDto
    {
        public long? ContributionId { get; set; }
        public int UserId { get; set; }
        public string? Title { get; set; }
        public string? Slug { get; set; }
        public string? Summary { get; set; }
        public string? Body { get; set; }
        public VisibilityType? VisibilityType { get; set; }
        public DateTimeOffset? PublishDate { get; set; }
        public IFormFile? Image { get; set; }
        public IEnumerable<long>? Tags { get; set; }
        public string? Keywords { get; set; }
        public bool? Draft { get; set; }
    }
}
