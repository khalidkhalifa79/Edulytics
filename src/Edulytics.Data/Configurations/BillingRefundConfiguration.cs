using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class BillingRefundConfiguration
    : IEntityTypeConfiguration<BillingRefund>
{
    public void Configure(EntityTypeBuilder<BillingRefund> builder)
    {
        builder.ToTable("BillingRefunds");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.Property(x => x.Amount).HasPrecision(14, 2).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.RecordedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.SchoolId, x.InvoiceId, x.RecordedAtUtc });

        builder.HasOne<BillingInvoice>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.InvoiceId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<BankTransferPayment>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.PaymentId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);
    }
}
