using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class SubscriptionSeatChangeConfiguration
    : IEntityTypeConfiguration<SubscriptionSeatChange>
{
    public void Configure(
        EntityTypeBuilder<SubscriptionSeatChange> builder)
    {
        builder.ToTable("SubscriptionSeatChanges");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(
            x => new { x.SchoolId, x.Id });

        builder.Property(x => x.ChangeType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(x => x.EffectiveAtUtc)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.SubscriptionId,
            x.EffectiveAtUtc
        });

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.EffectiveAtUtc
        });

        builder.HasOne<SchoolSubscription>()
            .WithMany()
            .HasForeignKey(
                x => new
                {
                    x.SchoolId,
                    x.SubscriptionId
                })
            .HasPrincipalKey(
                x => new
                {
                    x.SchoolId,
                    x.Id
                })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
