using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class CurriculumFrameworkVersionConfiguration
    : IEntityTypeConfiguration<CurriculumFrameworkVersion>
{
    public void Configure(
        EntityTypeBuilder<CurriculumFrameworkVersion> builder)
    {
        builder.ToTable("CurriculumFrameworkVersions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.VersionCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.NormalizedVersionCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.EffectiveFrom)
            .HasColumnType("date");

        builder.Property(x => x.EffectiveTo)
            .HasColumnType("date");

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new
        {
            x.FrameworkId,
            x.NormalizedVersionCode
        }).IsUnique();

        builder.HasOne<CurriculumFramework>()
            .WithMany()
            .HasForeignKey(x => x.FrameworkId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
