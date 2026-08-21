using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class DemoAccessConfiguration : IEntityTypeConfiguration<DemoAccess>
{
    public void Configure(EntityTypeBuilder<DemoAccess> builder)
    {
        builder.ToTable("DemoAccesses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StartsAtUtc).IsRequired();
        builder.Property(x => x.ExpiresAtUtc).IsRequired();
        builder.Property(x => x.RevokedReason).HasMaxLength(500);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();
        builder.HasIndex(x => x.DemoRequestId).IsUnique();
        builder.HasIndex(x => x.SchoolId).IsUnique();
        builder.HasIndex(x => new { x.ExpiresAtUtc, x.RevokedAtUtc, x.ConvertedAtUtc });
        builder.HasOne<DemoRequest>().WithMany().HasForeignKey(x => x.DemoRequestId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<School>().WithMany().HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
