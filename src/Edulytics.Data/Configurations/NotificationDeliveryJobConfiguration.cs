using Edulytics.Core.Entities;
using Edulytics.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class NotificationDeliveryJobConfiguration
    : IEntityTypeConfiguration<NotificationDeliveryJob>
{
    public void Configure(
        EntityTypeBuilder<NotificationDeliveryJob> builder)
    {
        builder.ToTable("NotificationDeliveryJobs");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(
            x => new
            {
                x.SchoolId,
                x.Id
            });

        builder.Property(x => x.Channel)
            .HasConversion<int>();

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.Culture)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(x => x.BaseUrl)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(x => x.DeduplicationKey)
            .HasMaxLength(220)
            .IsRequired();

        builder.Property(x => x.LastErrorCode)
            .HasMaxLength(120);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(
                x => new
                {
                    x.SchoolId,
                    x.DeduplicationKey
                })
            .IsUnique();

        builder.HasIndex(
            x => new
            {
                x.SchoolId,
                x.RecipientUserId,
                x.CreatedAtUtc
            });

        builder.HasIndex(
            x => new
            {
                x.SchoolId,
                x.Status,
                x.CreatedAtUtc
            });

        builder.HasOne<UserNotification>()
            .WithMany()
            .HasForeignKey(x => x.NotificationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
