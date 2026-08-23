using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class BankTransferPaymentConfiguration
    : IEntityTypeConfiguration<BankTransferPayment>
{
    public void Configure(EntityTypeBuilder<BankTransferPayment> builder)
    {
        builder.ToTable("BankTransferPayments");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.Property(x => x.VerificationStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.PaymentReference).HasMaxLength(256).IsRequired();
        builder.Property(x => x.EvidenceNote).HasMaxLength(2000);
        builder.Property(x => x.ReceivedAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(x => x.ReceivedCurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.AppliedAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.ReceivedAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new { x.SchoolId, x.InvoiceId, x.ReceivedAtUtc });
        builder.HasIndex(x => new { x.SchoolId, x.PaymentReference });

        builder.HasOne<BillingInvoice>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.InvoiceId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
