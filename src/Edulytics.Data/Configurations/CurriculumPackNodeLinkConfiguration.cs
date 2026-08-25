using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class CurriculumPackNodeLinkConfiguration : IEntityTypeConfiguration<CurriculumPackNodeLink>
{
    public void Configure(EntityTypeBuilder<CurriculumPackNodeLink> b)
    {
        b.ToTable("CurriculumPackNodeLinks");
        b.HasKey(x => x.Id);
        b.Property(x => x.LinkKind).HasMaxLength(80).IsRequired();
        b.Property(x => x.AlignmentConfidence).HasMaxLength(80).IsRequired();
        b.Property(x => x.EvidenceNote).HasColumnType("text").IsRequired();
        b.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
        b.HasIndex(x => new { x.FrameworkVersionId, x.FromNodeId, x.ToNodeId, x.LinkKind }).IsUnique();
        b.HasIndex(x => new { x.FromNodeId, x.SortOrder });
        b.HasOne<CurriculumFrameworkVersion>().WithMany().HasForeignKey(x => x.FrameworkVersionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CurriculumPackContentNode>().WithMany().HasForeignKey(x => x.FromNodeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CurriculumPackContentNode>().WithMany().HasForeignKey(x => x.ToNodeId).OnDelete(DeleteBehavior.Restrict);
    }
}
