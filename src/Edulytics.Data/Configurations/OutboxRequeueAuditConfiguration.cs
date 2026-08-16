using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class OutboxRequeueAuditConfiguration
    : IEntityTypeConfiguration<OutboxRequeueAudit>
{
    public void Configure(
        EntityTypeBuilder<OutboxRequeueAudit> builder)
    {
        builder.ToTable("OutboxRequeueAudits");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.OutboxMessageId,
            x.RequeuedAtUtc
        });

        builder.HasIndex(x => new
        {
            x.ActorUserId,
            x.RequeuedAtUtc
        });

        builder.HasOne<OutboxMessage>()
            .WithMany()
            .HasForeignKey(x => x.OutboxMessageId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
