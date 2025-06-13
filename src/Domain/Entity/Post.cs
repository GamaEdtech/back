namespace GamaEdtech.Domain.Entity
{
    using System.Diagnostics.CodeAnalysis;

    using GamaEdtech.Common.Data;
    using GamaEdtech.Common.Data.Enumeration;
    using GamaEdtech.Common.DataAccess.Entities;
    using GamaEdtech.Common.DataAnnotation;
    using GamaEdtech.Common.DataAnnotation.Schema;
    using GamaEdtech.Domain.Entity.Identity;
    using GamaEdtech.Domain.Enumeration;

    using Microsoft.EntityFrameworkCore.Metadata.Builders;

    [Table(nameof(Post))]
    public class Post : VersionableEntity<ApplicationUser, int, int?>, IEntity<Post, long>
    {
        [System.ComponentModel.DataAnnotations.Key]
        [Column(nameof(Id), DataType.Long)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Required]
        public long Id { get; set; }

        [Column(nameof(Slug), DataType.UnicodeString)]
        [StringLength(500)]
        public string? Slug { get; set; }

        [Column(nameof(Title), DataType.UnicodeString)]
        [StringLength(500)]
        [Required]
        public string? Title { get; set; }

        [Column(nameof(Summary), DataType.UnicodeString)]
        [Required]
        [StringLength(2000)]
        public string? Summary { get; set; }

        [Column(nameof(Body), DataType.UnicodeMaxString)]
        [Required]
        public string? Body { get; set; }

        [Column(nameof(ImageId), DataType.String)]
        [Required]
        [StringLength(100)]
        public string? ImageId { get; set; }

        [Column(nameof(LikeCount), DataType.Int)]
        [Required]
        public int LikeCount { get; set; }

        [Column(nameof(DislikeCount), DataType.Int)]
        [Required]
        public int DislikeCount { get; set; }

        [Column(nameof(PublishDate), DataType.DateTimeOffset)]
        [Required]
        public DateTimeOffset PublishDate { get; set; }

        [Column(nameof(VisibilityType), DataType.Byte)]
        [Required]
        public VisibilityType VisibilityType { get; set; }

        [Column(nameof(Keywords), DataType.UnicodeString)]
        [StringLength(500)]
        public string? Keywords { get; set; }

        public virtual ICollection<PostTag>? PostTags { get; set; }

        public void Configure([NotNull] EntityTypeBuilder<Post> builder)
        {
            _ = builder.OwnEnumeration<Post, VisibilityType, byte>(t => t.VisibilityType);
            _ = builder.HasIndex(t => t.Slug).IsUnique(true);
        }
    }
}
