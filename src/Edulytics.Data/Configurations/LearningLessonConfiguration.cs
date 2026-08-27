using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class LearningLessonConfiguration
    : IEntityTypeConfiguration<LearningLesson>
{
    public void Configure(EntityTypeBuilder<LearningLesson> builder)
    {
        builder.ToTable("LearningLessons");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.Property(x => x.Order).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new { x.SchoolId, x.TopicId, x.Order })
            .IsUnique();
        builder.HasIndex(x => new { x.SchoolId, x.Status });

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CurriculumTopic>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.TopicId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
