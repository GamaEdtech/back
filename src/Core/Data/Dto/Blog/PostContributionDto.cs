namespace GamaEdtech.Data.Dto.Blog
{
    using System;

    using GamaEdtech.Domain.Enumeration;

    public sealed class PostContributionDto
    {
        public string? Body { get; set; }
        public string? ImageId { get; set; }
        public string? Title { get; set; }
        public string? Slug { get; set; }
        public string? Summary { get; set; }
        public VisibilityType? VisibilityType { get; set; }
        public DateTimeOffset? PublishDate { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public int? CreationUserId { get; set; }
        public IEnumerable<long>? Tags { get; set; }
        public string? Keywords { get; set; }
    }
}
