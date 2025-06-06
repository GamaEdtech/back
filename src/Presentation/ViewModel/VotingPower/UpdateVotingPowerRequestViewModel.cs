namespace GamaEdtech.Presentation.ViewModel.VotingPower
{
    using GamaEdtech.Common.DataAnnotation;

    public sealed class UpdateVotingPowerRequestViewModel
    {
        [Display]
        public string? ProposalId { get; set; }

        [Display]
        public string? WalletAddress { get; set; }

        [Display]
        public decimal? Amount { get; set; }

        [Display]
        public string? TokenAccount { get; set; }
    }
}
