namespace GamaEdtech.Presentation.ViewModel.Subject
{
    using GamaEdtech.Common.DataAnnotation;

    public sealed class UpdateSubjectRequestViewModel
    {
        [Display]
        public string? Title { get; set; }

        [Display]
        public int? Order { get; set; }
    }
}
