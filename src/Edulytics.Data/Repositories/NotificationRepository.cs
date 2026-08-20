using Edulytics.Core.Entities;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Notifications;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class NotificationRepository
    : INotificationRepository
{
    private readonly EdulyticsDbContext _db;

    public NotificationRepository(
        EdulyticsDbContext db)
    {
        _db = db;
    }

    public Task<UserNotification?>
        GetByDeduplicationKeyAsync(
            Guid schoolId,
            Guid recipientUserId,
            string deduplicationKey,
            CancellationToken cancellationToken = default) =>
        _db.UserNotifications
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.RecipientUserId ==
                        recipientUserId &&
                    x.DeduplicationKey ==
                        deduplicationKey,
                cancellationToken);

    public Task<bool> DeliveryExistsAsync(
        Guid schoolId,
        string deduplicationKey,
        CancellationToken cancellationToken = default) =>
        _db.NotificationDeliveryJobs
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.DeduplicationKey ==
                        deduplicationKey,
                cancellationToken);

    public Task AddNotificationAsync(
        UserNotification notification,
        CancellationToken cancellationToken = default) =>
        _db.UserNotifications
            .AddAsync(
                notification,
                cancellationToken)
            .AsTask();

    public Task AddDeliveryJobAsync(
        NotificationDeliveryJob job,
        CancellationToken cancellationToken = default) =>
        _db.NotificationDeliveryJobs
            .AddAsync(
                job,
                cancellationToken)
            .AsTask();

    public Task AddOutboxAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default) =>
        _db.OutboxMessages
            .AddAsync(
                message,
                cancellationToken)
            .AsTask();

    public async Task<IReadOnlyList<NotificationInboxRecord>>
        ListInboxAsync(
            Guid schoolId,
            Guid recipientUserId,
            int maxCount,
            CancellationToken cancellationToken = default)
    {
        var notifications =
            await _db.UserNotifications
                .AsNoTracking()
                .Where(
                    x =>
                        x.SchoolId == schoolId &&
                        x.RecipientUserId ==
                            recipientUserId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Take(maxCount)
                .ToArrayAsync(cancellationToken);

        if (notifications.Length == 0)
        {
            return [];
        }

        var ids =
            notifications
                .Select(x => x.Id)
                .ToArray();

        var deliveries =
            await _db.NotificationDeliveryJobs
                .AsNoTracking()
                .Where(
                    x =>
                        x.SchoolId == schoolId &&
                        ids.Contains(
                            x.NotificationId))
                .OrderByDescending(x => x.CreatedAtUtc)
                .ToArrayAsync(cancellationToken);

        var latest =
            deliveries
                .GroupBy(x => x.NotificationId)
                .ToDictionary(
                    g => g.Key,
                    g => g.First().Status);

        return notifications
            .Select(
                x =>
                    new NotificationInboxRecord(
                        x.Id,
                        x.Kind,
                        x.TitleKey,
                        x.MessageKey,
                        x.CreatedAtUtc,
                        x.ReadAtUtc,
                        x.RelatedEntityType,
                        x.RelatedEntityId,
                        latest.TryGetValue(
                            x.Id,
                            out var status)
                            ? status
                            : null))
            .ToArray();
    }

    public Task<UserNotification?>
        GetNotificationForUpdateAsync(
            Guid schoolId,
            Guid recipientUserId,
            Guid notificationId,
            CancellationToken cancellationToken = default) =>
        _db.UserNotifications
            .SingleOrDefaultAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.RecipientUserId ==
                        recipientUserId &&
                    x.Id == notificationId,
                cancellationToken);

    public Task<NotificationDeliveryJob?>
        GetDeliveryForUpdateAsync(
            Guid schoolId,
            Guid deliveryJobId,
            CancellationToken cancellationToken = default) =>
        _db.NotificationDeliveryJobs
            .SingleOrDefaultAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.Id == deliveryJobId,
                cancellationToken);

    public async Task<bool> SaveAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(
                cancellationToken);

            return true;
        }
        catch (
            DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return false;
        }
    }
}
