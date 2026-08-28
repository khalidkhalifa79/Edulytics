using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class CurriculumPedagogicalLessonOutcomeConfiguration
    : IEntityTypeConfiguration<CurriculumPedagogicalLessonOutcome>
{
    public void Configure(EntityTypeBuilder<CurriculumPedagogicalLessonOutcome> b)
    {
        b.ToTable("CurriculumPedagogicalLessonOutcomes");
        b.HasKey(x => new { x.PedagogicalLessonId, x.OutcomeNodeId });

        b.Property(x => x.FrameworkVersionId).IsRequired();
        b.Property(x => x.SortOrder).IsRequired();

        b.HasIndex(x => new { x.FrameworkVersionId, x.OutcomeNodeId });

        b.HasOne<CurriculumPedagogicalLesson>()
            .WithMany()
            .HasForeignKey(x => x.PedagogicalLessonId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<CurriculumPackContentNode>()
            .WithMany()
            .HasForeignKey(x => x.OutcomeNodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
