namespace GamaEdtech.Presentation.ViewModel.Grade
{
    using GamaEdtech.Common.DataAnnotation;

    public sealed class UpdateGradeRequestViewModel
    {
        [Display]
        public string? Title { get; set; }

        [Display]
        public string? Description { get; set; }

        [Display]
        public string? Icon { get; set; }

        [Display]
        public int? BoardId { get; set; }
    }
}
