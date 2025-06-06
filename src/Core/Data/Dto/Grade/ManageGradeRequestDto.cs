namespace GamaEdtech.Data.Dto.Grade
{
    public sealed class ManageGradeRequestDto
    {
        public int? Id { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public int? BoardId { get; set; }
    }
}
