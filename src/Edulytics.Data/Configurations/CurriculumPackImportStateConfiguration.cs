using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class CurriculumPackImportStateConfiguration : IEntityTypeConfiguration<CurriculumPackImportState>
{
    public void Configure(EntityTypeBuilder<CurriculumPackImportState> b)
    {
        b.ToTable("CurriculumPackImportStates");
        b.HasKey(x => x.Id);
        b.Property(x => x.FrameworkCode).HasMaxLength(50).IsRequired();
        b.Property(x => x.VersionCode).HasMaxLength(80).IsRequired();
        b.Property(x => x.SourceDigest).HasMaxLength(64).IsRequired();
        b.Property(x => x.ContentDigest).HasMaxLength(64).IsRequired();
        b.Property(x => x.ImportedAtUtc).IsRequired();
        b.Property(x => x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
        b.HasIndex(x => new { x.FrameworkCode, x.VersionCode }).IsUnique();
        b.HasIndex(x => x.FrameworkVersionId).IsUnique();
        b.HasOne<CurriculumFrameworkVersion>().WithMany().HasForeignKey(x => x.FrameworkVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
