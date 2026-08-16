using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class IdempotencyRecordConfiguration
    : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(
        EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Operation)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.RequestHash)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.ExpiresAtUtc)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new
            {
                x.ActorUserId,
                x.Operation,
                x.IdempotencyKey
            })
            .IsUnique()
            .HasDatabaseName(
                "UX_IdempotencyRecords_Actor_Operation_Key");

        builder.HasIndex(x => new
            {
                x.SchoolId,
                x.CreatedAtUtc
            });

        builder.HasIndex(x => x.ExpiresAtUtc);
    }
}
