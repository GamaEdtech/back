namespace GamaEdtech.Data.Dto.Contribution
{
    using System;

    using GamaEdtech.Domain.Enumeration;

    public sealed class ContributionDto<T>
    {
        public long Id { get; set; }
        public string? Comment { get; set; }
        public T? Data { get; set; }
        public long? IdentifierId { get; set; }
        public CategoryType? CategoryType { get; set; }
        public int CreationUserId { get; set; }
        public DateTimeOffset CreationDate { get; set; }
    }
}
