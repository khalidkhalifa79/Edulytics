using Edulytics.Core.Entities;
using Edulytics.Core.Notifications;

namespace Edulytics.Core.Interfaces;

public interface INotificationRepository
{
    Task<UserNotification?>
        GetByDeduplicationKeyAsync(
            Guid schoolId,
            Guid recipientUserId,
            string deduplicationKey,
            CancellationToken cancellationToken = default);

    Task<bool> DeliveryExistsAsync(
        Guid schoolId,
        string deduplicationKey,
        CancellationToken cancellationToken = default);

    Task AddNotificationAsync(
        UserNotification notification,
        CancellationToken cancellationToken = default);

    Task AddDeliveryJobAsync(
        NotificationDeliveryJob job,
        CancellationToken cancellationToken = default);

    Task AddOutboxAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationInboxRecord>>
        ListInboxAsync(
            Guid schoolId,
            Guid recipientUserId,
            int maxCount,
            CancellationToken cancellationToken = default);

    Task<UserNotification?>
        GetNotificationForUpdateAsync(
            Guid schoolId,
            Guid recipientUserId,
            Guid notificationId,
            CancellationToken cancellationToken = default);

    Task<NotificationDeliveryJob?>
        GetDeliveryForUpdateAsync(
            Guid schoolId,
            Guid deliveryJobId,
            CancellationToken cancellationToken = default);

    Task<bool> SaveAsync(
        CancellationToken cancellationToken = default);
}
