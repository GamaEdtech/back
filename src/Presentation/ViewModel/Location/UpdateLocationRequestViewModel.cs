namespace GamaEdtech.Presentation.ViewModel.Location
{
    using GamaEdtech.Common.DataAnnotation;

    public sealed class UpdateLocationRequestViewModel
    {
        [Display]
        [StringLength(100)]
        public string? Title { get; set; }

        [Display]
        [StringLength(100)]
        public string? LocalTitle { get; set; }

        [Display]
        [StringLength(50)]
        public string? Code { get; set; }

        [Display]
        public int? ParentId { get; set; }

        [Display]
        public double? Latitude { get; set; }

        [Display]
        public double? Longitude { get; set; }
    }
}
