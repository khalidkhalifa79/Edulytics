using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class AuditLogConfiguration
    : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(
        EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ActorRole)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Action)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.EntityType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.EntityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.CorrelationId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.IpAddress)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.UserAgent)
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.OldValuesJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.NewValuesJson)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(x => x.ResultSummary)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.Source)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Feature)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.OccurredAtUtc)
            .IsRequired();

        // Deliberately no FK to School/User:
        // audit history must survive lifecycle changes/deletion
        // of the referenced business/identity record.

        builder.HasIndex(
                x => new
                {
                    x.SchoolId,
                    x.OccurredAtUtc
                })
            .HasDatabaseName(
                "IX_AuditLogs_SchoolId_OccurredAtUtc");

        builder.HasIndex(x => x.CorrelationId)
            .HasDatabaseName(
                "IX_AuditLogs_CorrelationId");

        builder.HasIndex(
                x => new
                {
                    x.ActorUserId,
                    x.OccurredAtUtc
                })
            .HasDatabaseName(
                "IX_AuditLogs_ActorUserId_OccurredAtUtc");

        builder.HasIndex(
                x => new
                {
                    x.Action,
                    x.OccurredAtUtc
                })
            .HasDatabaseName(
                "IX_AuditLogs_Action_OccurredAtUtc");
    }
}
