using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class CurriculumPedagogicalLessonConfiguration
    : IEntityTypeConfiguration<CurriculumPedagogicalLesson>
{
    public void Configure(EntityTypeBuilder<CurriculumPedagogicalLesson> b)
    {
        b.ToTable("CurriculumPedagogicalLessons");
        b.HasKey(x => x.Id);

        b.Property(x => x.Code).HasMaxLength(600).IsRequired();
        b.Property(x => x.UnitKey).HasMaxLength(600).IsRequired();
        b.Property(x => x.UnitTitle).HasMaxLength(600).IsRequired();
        b.Property(x => x.Title).HasMaxLength(600).IsRequired();
        b.Property(x => x.NativeLevel).HasMaxLength(160).IsRequired();
        b.Property(x => x.Pathway).HasMaxLength(200);
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        b.HasIndex(x => new { x.FrameworkVersionId, x.Code }).IsUnique();
        b.HasIndex(x => new
        {
            x.FrameworkVersionId,
            x.LogicalLevelFrom,
            x.LogicalLevelTo,
            x.Pathway,
            x.SortOrder
        });

        b.HasIndex(x => x.OfficialLessonNodeId).IsUnique();

        b.HasOne<CurriculumFrameworkVersion>()
            .WithMany()
            .HasForeignKey(x => x.FrameworkVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne<CurriculumPackContentNode>()
            .WithMany()
            .HasForeignKey(x => x.OfficialLessonNodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
