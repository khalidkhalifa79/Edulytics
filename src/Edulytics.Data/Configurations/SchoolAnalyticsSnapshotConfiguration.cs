using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class SchoolAnalyticsSnapshotConfiguration
    : IEntityTypeConfiguration<SchoolAnalyticsSnapshot>
{
    public void Configure(
        EntityTypeBuilder<SchoolAnalyticsSnapshot> builder)
    {
        builder.ToTable("SchoolAnalyticsSnapshots");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OverallMasteryPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(x => x.CalculatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.AcademicYearId
        }).IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AcademicYear>()
            .WithMany()
            .HasForeignKey(
                x => new
                {
                    x.SchoolId,
                    x.AcademicYearId
                })
            .HasPrincipalKey(
                x => new
                {
                    x.SchoolId,
                    x.Id
                })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
