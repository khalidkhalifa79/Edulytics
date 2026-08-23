using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class BillingInvoiceConfiguration
    : IEntityTypeConfiguration<BillingInvoice>
{
    public void Configure(EntityTypeBuilder<BillingInvoice> builder)
    {
        builder.ToTable("BillingInvoices");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.Property(x => x.InvoiceNumber).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Kind).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.InvoiceCurrency).HasConversion<int>().IsRequired();
        builder.Property(x => x.SettlementCurrencyCode).HasMaxLength(3);
        builder.Property(x => x.SettlementEquivalentAmount).HasPrecision(14, 2);

        builder.Property(x => x.LegalNameSnapshot).HasMaxLength(256).IsRequired();
        builder.Property(x => x.BillingAddressSnapshot).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.CountryCodeSnapshot).HasMaxLength(2).IsRequired();
        builder.Property(x => x.TaxIdentifierSnapshot).HasMaxLength(128).IsRequired();
        builder.Property(x => x.InvoiceEmailSnapshot).HasMaxLength(320).IsRequired();
        builder.Property(x => x.TaxTreatmentCodeSnapshot).HasMaxLength(128);
        builder.Property(x => x.PaymentInstructionsSnapshot).HasMaxLength(2000).IsRequired();

        builder.Property(x => x.NetAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(x => x.TaxAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(x => x.TotalAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(x => x.PaidAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(x => x.RefundedAmount).HasPrecision(14, 2).IsRequired();
        builder.Property(x => x.IssueDateUtc).IsRequired();
        builder.Property(x => x.DueDateUtc).IsRequired();
        builder.Property(x => x.GraceEndsAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => new { x.SchoolId, x.Status, x.DueDateUtc });
        builder.HasIndex(x => new { x.SubscriptionId, x.Kind, x.InstallmentNumber })
            .IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<SchoolSubscription>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.SubscriptionId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
