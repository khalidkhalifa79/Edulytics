using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class AnalyticsRefreshStateConfiguration
    : IEntityTypeConfiguration<AnalyticsRefreshState>
{
    public void Configure(
        EntityTypeBuilder<AnalyticsRefreshState> builder)
    {
        builder.ToTable("AnalyticsRefreshStates");

        builder.HasKey(x => x.SchoolId);

        builder.Property(x => x.LeaseOwner)
            .HasMaxLength(200);

        builder.Property(x => x.LastError)
            .HasMaxLength(2000);

        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new
        {
            x.AvailableAtUtc,
            x.LeaseUntilUtc
        });

        builder.HasIndex(x => new
        {
            x.RequestedVersion,
            x.CompletedVersion
        });

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
