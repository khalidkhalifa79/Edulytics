using Edulytics.Core.Interfaces;
using Edulytics.Core.Notifications;

namespace Edulytics.Core.Entities;

public sealed class NotificationDeliveryJob : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid NotificationId { get; set; }
    public Guid RecipientUserId { get; set; }

    public NotificationDeliveryChannel Channel { get; set; }

    public NotificationDeliveryStatus Status { get; set; } =
        NotificationDeliveryStatus.Pending;

    public string Culture { get; set; } = "en";

    // Safe origin only. Never contains password-setup tokens.
    public string BaseUrl { get; set; } =
        string.Empty;

    public string DeduplicationKey { get; set; } =
        string.Empty;

    public int AttemptCount { get; set; }
    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime? SentAtUtc { get; set; }

    public string? LastErrorCode { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
