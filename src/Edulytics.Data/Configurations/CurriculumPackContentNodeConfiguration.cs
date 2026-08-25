using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class CurriculumPackContentNodeConfiguration : IEntityTypeConfiguration<CurriculumPackContentNode>
{
    public void Configure(EntityTypeBuilder<CurriculumPackContentNode> b)
    {
        b.ToTable("CurriculumPackContentNodes");
        b.HasKey(x => x.Id);
        b.Property(x => x.FrameworkCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.VersionCode).HasMaxLength(80).IsRequired();
        b.Property(x => x.NodeKind).HasMaxLength(40).IsRequired();
        b.Property(x => x.Code).HasMaxLength(300).IsRequired();
        b.Property(x => x.NativeLevel).HasMaxLength(200).IsRequired();
        b.Property(x => x.Pathway).HasMaxLength(300);
        b.Property(x => x.Title).HasMaxLength(600).IsRequired();
        b.Property(x => x.OfficialText).HasColumnType("text");
        b.Property(x => x.AuthorDescription).HasColumnType("text");
        b.Property(x => x.SourceAuthority).HasMaxLength(400).IsRequired();
        b.Property(x => x.SourceUrl).HasMaxLength(2500).IsRequired();
        b.Property(x => x.SourceLocator).HasMaxLength(2000).IsRequired();
        b.Property(x => x.Attribution).HasMaxLength(2500).IsRequired();
        b.Property(x => x.ContentHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.CreatedAtUtc).IsRequired();
        b.Property(x => x.UpdatedAtUtc).IsRequired();
        b.Property(x => x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
        b.HasIndex(x => new { x.FrameworkVersionId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.FrameworkCode, x.VersionCode, x.NodeKind, x.LogicalLevelFrom, x.LogicalLevelTo });
        b.HasIndex(x => new { x.ParentId, x.SortOrder });
        b.HasOne<CurriculumFrameworkVersion>().WithMany().HasForeignKey(x => x.FrameworkVersionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<CurriculumPackContentNode>().WithMany().HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict);
    }
}
