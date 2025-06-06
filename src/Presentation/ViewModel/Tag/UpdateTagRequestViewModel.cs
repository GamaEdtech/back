namespace GamaEdtech.Presentation.ViewModel.Tag
{
    using GamaEdtech.Common.Converter;
    using System.Text.Json.Serialization;

    using GamaEdtech.Common.DataAnnotation;
    using GamaEdtech.Domain.Enumeration;

    public sealed class UpdateTagRequestViewModel
    {
        [Display]
        public string? Name { get; set; }

        [Display]
        public string? Icon { get; set; }

        [Display]
        [JsonConverter(typeof(EnumerationConverter<TagType, byte>))]
        public TagType? TagType { get; set; }
    }
}
