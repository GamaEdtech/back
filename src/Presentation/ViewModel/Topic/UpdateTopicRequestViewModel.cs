namespace GamaEdtech.Presentation.ViewModel.Topic
{
    using GamaEdtech.Common.DataAnnotation;

    public sealed class UpdateTopicRequestViewModel
    {
        [Display]
        public string? Title { get; set; }

        [Display]
        public int? Order { get; set; }
    }
}
