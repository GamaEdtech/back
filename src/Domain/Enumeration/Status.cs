namespace GamaEdtech.Domain.Enumeration
{
    using GamaEdtech.Common.Data.Enumeration;
    using GamaEdtech.Common.DataAnnotation;

    public sealed class Status : Enumeration<Status, byte>
    {
        [Display]
        public static readonly Status Draft = new(nameof(Draft), 0);

        [Display]
        public static readonly Status Review = new(nameof(Review), 1);

        [Display]
        public static readonly Status Confirmed = new(nameof(Confirmed), 2);

        [Display]
        public static readonly Status Rejected = new(nameof(Rejected), 3);

        [Display]
        public static readonly Status Deleted = new(nameof(Deleted), 4);

        public Status()
        {
        }

        public Status(string name, byte value) : base(name, value)
        {
        }
    }
}
