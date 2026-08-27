using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Edulytics.Data.Configurations;

public sealed class CurriculumLessonContentConfiguration : IEntityTypeConfiguration<CurriculumLessonContent>
{
    public void Configure(EntityTypeBuilder<CurriculumLessonContent> b)
    {
        b.ToTable("CurriculumLessonContents");
        b.HasKey(x => x.Id);
        b.Property(x => x.ContentVersion).HasMaxLength(80).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
        b.HasIndex(x => x.LessonNodeId).IsUnique();
        b.HasIndex(x => new { x.FrameworkVersionId, x.Status });
        b.HasOne<CurriculumFrameworkVersion>().WithMany().HasForeignKey(x => x.FrameworkVersionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CurriculumPackContentNode>().WithMany().HasForeignKey(x => x.LessonNodeId).OnDelete(DeleteBehavior.Restrict);
    }
}
