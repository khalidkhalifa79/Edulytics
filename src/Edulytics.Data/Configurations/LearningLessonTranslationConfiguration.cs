using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class LearningLessonTranslationConfiguration
    : IEntityTypeConfiguration<LearningLessonTranslation>
{
    public void Configure(EntityTypeBuilder<LearningLessonTranslation> builder)
    {
        builder.ToTable("LearningLessonTranslations");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.Property(x => x.CultureCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Explanation).IsRequired();
        builder.Property(x => x.KeyConceptsAndRules).IsRequired();
        builder.Property(x => x.WorkedExamples).IsRequired();
        builder.Property(x => x.StepByStepSolutions).IsRequired();
        builder.Property(x => x.CommonMistakes).IsRequired();
        builder.Property(x => x.QuickSummary).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new { x.SchoolId, x.LessonId, x.CultureCode })
            .IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LearningLesson>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.LessonId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
