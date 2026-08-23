using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class SchoolSubscriptionConfiguration
    : IEntityTypeConfiguration<SchoolSubscription>
{
    public void Configure(
        EntityTypeBuilder<SchoolSubscription> builder)
    {
        builder.ToTable("SchoolSubscriptions");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(
            x => new { x.SchoolId, x.Id });

        builder.Property(x => x.Term)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.BillingCadence)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CommercialCurrency)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.PricePerStudentPerMonth)
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.UpdatedAtUtc)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => x.SchoolId)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.Status,
            x.CurrentTermEndsAtUtc
        });

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
