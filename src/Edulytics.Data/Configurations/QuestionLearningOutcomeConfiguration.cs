using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class QuestionLearningOutcomeConfiguration
    : IEntityTypeConfiguration<QuestionLearningOutcome>
{
    public void Configure(EntityTypeBuilder<QuestionLearningOutcome> builder)
    {
        builder.ToTable("QuestionLearningOutcomes");
        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.AssessmentQuestionId,
            x.LearningOutcomeId
        }).IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AssessmentQuestion>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.AssessmentQuestionId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LearningOutcome>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.LearningOutcomeId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
