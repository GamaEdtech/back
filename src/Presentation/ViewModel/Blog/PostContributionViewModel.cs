namespace GamaEdtech.Presentation.ViewModel.Blog
{
    using System.Text.Json.Serialization;

    using GamaEdtech.Common.Converter;
    using GamaEdtech.Common.Core;
    using GamaEdtech.Common.DataAnnotation;
    using GamaEdtech.Domain.Enumeration;

    using Microsoft.AspNetCore.Http;

    public sealed class PostContributionViewModel
    {
        [Display]
        [Required]
        public string? Title { get; set; }

        [Display]
        [Required]
        public string? Slug { get; set; }

        [Display]
        [Required]
        public string? Summary { get; set; }

        [Display]
        [Required]
        public string? Body { get; set; }

        [Display]
        [Required]
        [FileSize(1024 * 1024 * 2)]//2MB
        [FileExtensions(Constants.ValidImageExtensions)]
        public IFormFile? Image { get; set; }

        [Display]
        [Required]
        [JsonConverter(typeof(EnumerationConverter<VisibilityType, byte>))]
        public VisibilityType? VisibilityType { get; set; }

        [Display]
        [Required]
        public DateTimeOffset? PublishDate { get; set; }

        [Display]
        public string? Keywords { get; set; }

        [Display]
        public IEnumerable<long>? Tags { get; set; }

        [Display]
        public bool Draft { get; set; }
    }
}
