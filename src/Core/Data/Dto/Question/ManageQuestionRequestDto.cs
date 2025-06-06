namespace GamaEdtech.Data.Dto.Question
{
    public sealed class ManageQuestionRequestDto
    {
        public long? Id { get; set; }
        public string? Body { get; set; }
        public IEnumerable<OptionDto>? Options { get; set; }
    }
}
