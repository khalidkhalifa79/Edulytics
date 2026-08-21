using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class DemoRequestConfiguration : IEntityTypeConfiguration<DemoRequest>
{
    public void Configure(EntityTypeBuilder<DemoRequest> builder)
    {
        builder.ToTable("DemoRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SchoolName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContactName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.WorkEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.NormalizedWorkEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.CountryCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.City).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000);
        builder.Property(x => x.InternalNote).HasMaxLength(2000);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.PrivacyConsentAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.NormalizedWorkEmail, x.Status });
        builder.HasIndex(x => x.DemoSchoolId).IsUnique();
        builder.HasIndex(x => x.ProvisionedSchoolId).IsUnique();
        builder.HasOne<School>().WithMany().HasForeignKey(x => x.DemoSchoolId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<School>().WithMany().HasForeignKey(x => x.ProvisionedSchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
