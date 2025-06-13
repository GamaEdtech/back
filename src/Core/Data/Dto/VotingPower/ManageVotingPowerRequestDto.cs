namespace GamaEdtech.Data.Dto.VotingPower
{
    public sealed class ManageVotingPowerRequestDto
    {
        public string? ProposalId { get; set; }
        public string? WalletAddress { get; set; }
        public decimal? Amount { get; set; }
        public string? TokenAccount { get; set; }
    }
}
