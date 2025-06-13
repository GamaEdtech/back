namespace GamaEdtech.Presentation.ViewModel.VotingPower
{
    using GamaEdtech.Common.DataAnnotation;

    public sealed class CreateVotingPowerRequestViewModel
    {
        [Display]
        [Required]
        public IEnumerable<VotingPowerViewModel> Data { get; set; }

        [Display]
        [Required]
        public string? PublicKey { get; set; }

        [Display]
        [Required]
        public string? Message { get; set; }

        [Display]
        [Required]
        public string? SignedMessage { get; set; }
    }
}
