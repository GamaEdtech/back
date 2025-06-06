namespace GamaEdtech.Presentation.ViewModel.Question
{
    using GamaEdtech.Common.DataAnnotation;

    public sealed class UpdateQuestionRequestViewModel
    {
        [Display]
        public string? Body { get; set; }

        [Display]
        public IEnumerable<OptionViewModel>? Options { get; set; }
    }
}
