namespace GamaEdtech.Presentation.ViewModel.Blog
{
    using System.Text.Json.Serialization;

    using GamaEdtech.Common.Converter;
    using GamaEdtech.Domain.Enumeration;

    public sealed class PostContributionResponseViewModel
    {
        public string? Title { get; set; }
        public string? Slug { get; set; }
        public string? Summary { get; set; }
        public string? Body { get; set; }
        public Uri? ImageUri { get; set; }
        public string? Keywords { get; set; }

        [JsonConverter(typeof(EnumerationConverter<VisibilityType, byte>))]
        public VisibilityType VisibilityType { get; set; }

        public DateTimeOffset PublishDate { get; set; }

        public IEnumerable<long>? Tags { get; set; }
    }
}
