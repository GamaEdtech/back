namespace GamaEdtech.Presentation.ViewModel.Subject
{
    using GamaEdtech.Common.DataAnnotation;

    public sealed class ManageSubjectRequestViewModel
    {
        [Display]
        [Required]
        public string? Title { get; set; }

        [Display]
        [Required]
        public int? Order { get; set; }
    }
}
