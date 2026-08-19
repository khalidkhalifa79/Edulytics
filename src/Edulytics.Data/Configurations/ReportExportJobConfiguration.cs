using Edulytics.Core.Entities;
using Edulytics.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class ReportExportJobConfiguration
    : IEntityTypeConfiguration<ReportExportJob>
{
    public void Configure(
        EntityTypeBuilder<ReportExportJob> builder)
    {
        builder.ToTable("ReportExportJobs");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(
            x => new
            {
                x.SchoolId,
                x.Id
            });

        builder.Property(x => x.ReportKind)
            .HasConversion<int>();

        builder.Property(x => x.ExportFormat)
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.Culture)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasMaxLength(260);

        builder.Property(x => x.ContentType)
            .HasMaxLength(160);

        builder.Property(x => x.LastError)
            .HasMaxLength(300);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(
            x => new
            {
                x.SchoolId,
                x.RequestedByUserId,
                x.CreatedAtUtc
            });

        builder.HasIndex(
            x => new
            {
                x.SchoolId,
                x.Status,
                x.CreatedAtUtc
            });

        builder.HasIndex(x => x.ExpiresAtUtc);

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
