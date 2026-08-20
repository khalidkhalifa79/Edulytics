using Edulytics.Core.Entities;
using Edulytics.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class UserNotificationConfiguration
    : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(
        EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("UserNotifications");

        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(
            x => new
            {
                x.SchoolId,
                x.Id
            });

        builder.Property(x => x.Kind)
            .HasConversion<int>();

        builder.Property(x => x.TitleKey)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.MessageKey)
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.DeduplicationKey)
            .HasMaxLength(220)
            .IsRequired();

        builder.Property(x => x.RelatedEntityType)
            .HasMaxLength(100);

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
                    x.RecipientUserId,
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
                x.RecipientUserId,
                x.ReadAtUtc
            });

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
