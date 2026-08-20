namespace Edulytics.Core.Notifications;

public enum NotificationKind
{
    AccountInvitation = 1
}

public enum NotificationDeliveryChannel
{
    Email = 1
}

public enum NotificationDeliveryStatus
{
    Pending = 1,
    Sent = 2,
    Failed = 3
}

public static class NotificationEventTypes
{
    public const string DeliveryRequested =
        "Notifications.DeliveryRequested";
}

public sealed record NotificationDeliveryRequestedEvent(
    Guid SchoolId,
    Guid DeliveryJobId);

public sealed record NotificationInboxRecord(
    Guid Id,
    NotificationKind Kind,
    string TitleKey,
    string MessageKey,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc,
    string? RelatedEntityType,
    Guid? RelatedEntityId,
    NotificationDeliveryStatus? EmailDeliveryStatus);
