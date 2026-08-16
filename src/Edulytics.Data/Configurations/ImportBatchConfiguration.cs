using Edulytics.Core.Entities;
using Edulytics.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class ImportBatchConfiguration
    : IEntityTypeConfiguration<ImportBatch>
{
    public void Configure(
        EntityTypeBuilder<ImportBatch> builder)
    {
        builder.ToTable("ImportBatches");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new
        {
            x.SchoolId,
            x.Id
        });

        builder.Property(x => x.ImportType)
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.OriginalFileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(x => x.FileHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.RowsJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.UploadedByUserId,
            x.ImportType,
            x.FileHash
        }).IsUnique();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.CreatedAtUtc
        });

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CompletedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
