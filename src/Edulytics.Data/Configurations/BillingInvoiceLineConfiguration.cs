using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class BillingInvoiceLineConfiguration
    : IEntityTypeConfiguration<BillingInvoiceLine>
{
    public void Configure(EntityTypeBuilder<BillingInvoiceLine> builder)
    {
        builder.ToTable("BillingInvoiceLines");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.Property(x => x.Kind).HasConversion<int>().IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.UnitMonthlyPrice).HasPrecision(12, 2);
        builder.Property(x => x.NetAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.SchoolId, x.InvoiceId });
        builder.HasIndex(x => x.SubscriptionSeatChangeId).IsUnique();

        builder.HasOne<BillingInvoice>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.InvoiceId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SubscriptionSeatChange>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.SubscriptionSeatChangeId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
