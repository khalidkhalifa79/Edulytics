using Edulytics.Core.Interfaces;
using Edulytics.Core.Notifications;

namespace Edulytics.Core.Entities;

public sealed class UserNotification : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid RecipientUserId { get; set; }

    public NotificationKind Kind { get; set; }

    public string TitleKey { get; set; } =
        string.Empty;

    public string MessageKey { get; set; } =
        string.Empty;

    public string DeduplicationKey { get; set; } =
        string.Empty;

    public string? RelatedEntityType { get; set; }
    public Guid? RelatedEntityId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ReadAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
