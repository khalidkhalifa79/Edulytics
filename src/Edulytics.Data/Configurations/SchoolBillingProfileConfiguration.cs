using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class SchoolBillingProfileConfiguration
    : IEntityTypeConfiguration<SchoolBillingProfile>
{
    public void Configure(EntityTypeBuilder<SchoolBillingProfile> builder)
    {
        builder.ToTable("SchoolBillingProfiles");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.Property(x => x.LegalName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.BillingAddress).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.CountryCode).HasMaxLength(2).IsRequired();
        builder.Property(x => x.TaxIdentifier).HasMaxLength(128).IsRequired();
        builder.Property(x => x.InvoiceEmail).HasMaxLength(320).IsRequired();
        builder.Property(x => x.TaxTreatmentCode).HasMaxLength(128);
        builder.Property(x => x.DefaultSettlementCurrencyCode).HasMaxLength(3);
        builder.Property(x => x.PaymentInstructions).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => x.SchoolId).IsUnique();
        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
