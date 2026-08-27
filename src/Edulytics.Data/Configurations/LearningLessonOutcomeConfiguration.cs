using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class LearningLessonOutcomeConfiguration
    : IEntityTypeConfiguration<LearningLessonOutcome>
{
    public void Configure(EntityTypeBuilder<LearningLessonOutcome> builder)
    {
        builder.ToTable("LearningLessonOutcomes");
        builder.HasKey(x => new { x.SchoolId, x.LessonId, x.LearningOutcomeId });

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LearningLesson>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.LessonId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LearningOutcome>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.LearningOutcomeId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
