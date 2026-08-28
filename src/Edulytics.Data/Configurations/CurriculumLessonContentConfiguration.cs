using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class CurriculumLessonContentConfiguration
    : IEntityTypeConfiguration<CurriculumLessonContent>
{
    public void Configure(EntityTypeBuilder<CurriculumLessonContent> b)
    {
        b.ToTable("CurriculumLessonContents");
        b.HasKey(x => x.Id);

        // Zero-loss corrective: retain the current physical column name in this phase.
        // Its semantic FK target changes from the official pack node to the Edulytics
        // pedagogical lesson identity.
        b.Property(x => x.PedagogicalLessonId)
            .HasColumnName("LessonNodeId")
            .IsRequired();

        b.Property(x => x.ContentVersion).HasMaxLength(80).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        b.HasIndex(x => x.PedagogicalLessonId).IsUnique();
        b.HasIndex(x => new { x.FrameworkVersionId, x.Status });

        b.HasOne<CurriculumFrameworkVersion>()
            .WithMany()
            .HasForeignKey(x => x.FrameworkVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<CurriculumPedagogicalLesson>()
            .WithMany()
            .HasForeignKey(x => x.PedagogicalLessonId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
