using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Edulytics.Data.Configurations;

public sealed class CurriculumLessonContentTranslationConfiguration : IEntityTypeConfiguration<CurriculumLessonContentTranslation>
{
    public void Configure(EntityTypeBuilder<CurriculumLessonContentTranslation> b)
    {
        b.ToTable("CurriculumLessonContentTranslations");
        b.HasKey(x => x.Id);
        b.Property(x => x.CultureCode).HasMaxLength(20).IsRequired();
        b.Property(x => x.Title).HasMaxLength(600).IsRequired();
        b.Property(x => x.Explanation).HasColumnType("text").IsRequired();
        b.Property(x => x.KeyConceptsAndRules).HasColumnType("text").IsRequired();
        b.Property(x => x.WorkedExamples).HasColumnType("text").IsRequired();
        b.Property(x => x.StepByStepSolutions).HasColumnType("text").IsRequired();
        b.Property(x => x.CommonMistakes).HasColumnType("text").IsRequired();
        b.Property(x => x.QuickSummary).HasColumnType("text").IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
        b.HasIndex(x => new { x.CurriculumLessonContentId, x.CultureCode }).IsUnique();
        b.HasOne<CurriculumLessonContent>().WithMany().HasForeignKey(x => x.CurriculumLessonContentId).OnDelete(DeleteBehavior.Cascade);
    }
}
