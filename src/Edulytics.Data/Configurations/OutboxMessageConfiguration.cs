using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class OutboxMessageConfiguration
    : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(
        EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.PayloadJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        builder.Property(x => x.AvailableAtUtc)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<int>()
            .HasDefaultValue(
                OutboxMessageStatus.Pending)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.LastError)
            .HasMaxLength(2000);

        builder.Property(x => x.LeaseOwner)
            .HasMaxLength(200);

        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => x.CorrelationId)
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.Status,
            x.AvailableAtUtc,
            x.LeaseUntilUtc,
            x.OccurredAtUtc
        });

        // Compatibility/readback index retained for accepted Phase 10
        // contracts and operational processed-message queries.
        builder.HasIndex(x => new
        {
            x.ProcessedAtUtc,
            x.AvailableAtUtc,
            x.OccurredAtUtc
        });

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.Status,
            x.OccurredAtUtc
        });

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
