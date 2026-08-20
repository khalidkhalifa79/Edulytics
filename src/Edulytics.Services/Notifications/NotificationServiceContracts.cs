using Edulytics.Core.Notifications;

namespace Edulytics.Services.Notifications;

public enum NotificationErrorCode
{
    AccessDenied,
    SchoolNotActive,
    InvalidRequest,
    NotFound,
    PersistenceError
}

public sealed record NotificationQueueResult(
    bool Succeeded,
    Guid? NotificationId,
    Guid? DeliveryJobId,
    bool Deduplicated,
    NotificationErrorCode? Error)
{
    public static NotificationQueueResult Success(
        Guid notificationId,
        Guid deliveryJobId,
        bool deduplicated) =>
        new(
            true,
            notificationId,
            deliveryJobId,
            deduplicated,
            null);

    public static NotificationQueueResult Failure(
        NotificationErrorCode error) =>
        new(
            false,
            null,
            null,
            false,
            error);
}

public sealed record NotificationInboxItem(
    Guid Id,
    NotificationKind Kind,
    string TitleKey,
    string MessageKey,
    DateTime CreatedAtUtc,
    DateTime? ReadAtUtc,
    NotificationDeliveryStatus? EmailDeliveryStatus);

public sealed record NotificationQueryResult<T>(
    T? Value,
    NotificationErrorCode? Error)
    where T : class
{
    public static NotificationQueryResult<T>
        Success(T value) =>
        new(value, null);

    public static NotificationQueryResult<T>
        Failure(NotificationErrorCode error) =>
        new(null, error);
}

public interface INotificationService
{
    Task<NotificationQueueResult>
        QueuePasswordSetupInvitationAsync(
            Guid actorUserId,
            Guid recipientUserId,
            string culture,
            string baseUrl,
            string deliveryReason,
            CancellationToken cancellationToken = default);

    Task<NotificationQueryResult<
        IReadOnlyList<NotificationInboxItem>>>
        ListInboxAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default);

    Task<NotificationQueryResult<
        NotificationInboxItem>>
        SetReadStateAsync(
            Guid actorUserId,
            Guid notificationId,
            bool isRead,
            CancellationToken cancellationToken = default);
}
