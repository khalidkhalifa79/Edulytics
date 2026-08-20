using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Notifications;
using Edulytics.Core.Reports;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class SensitiveDataRetentionRepository
    : ISensitiveDataRetentionRepository
{
    private const int NotificationSweepBatchSize =
        5000;

    private readonly EdulyticsDbContext _db;

    public SensitiveDataRetentionRepository(
        EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<SensitiveDataRetentionResult>
        ApplyAsync(
            DateTime utcNow,
            TimeSpan importPayloadRetention,
            TimeSpan notificationReadRetention,
            CancellationToken cancellationToken = default)
    {
        if (importPayloadRetention <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(importPayloadRetention));
        }

        if (notificationReadRetention <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(notificationReadRetention));
        }

        var importCutoff =
            utcNow.Subtract(
                importPayloadRetention);

        var notificationCutoff =
            utcNow.Subtract(
                notificationReadRetention);

        // RowsJson contains the raw normalized import
        // payload. OriginalFileName may contain personal
        // or workstation information.
        //
        // FileHash is intentionally retained because it is
        // the durable upload-idempotency key.
        //
        // Validated batches are NOT scrubbed because their
        // payload is still required for confirmation.
        var importsScrubbed =
            await _db.ImportBatches
                .Where(
                    x =>
                        (
                            x.Status ==
                                ImportBatchStatus.Completed ||
                            x.Status ==
                                ImportBatchStatus.ValidationFailed
                        ) &&
                        (
                            x.CompletedAtUtc ??
                            x.CreatedAtUtc
                        ) <= importCutoff &&
                        (
                            x.RowsJson != string.Empty ||
                            x.OriginalFileName != string.Empty
                        ))
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                x => x.RowsJson,
                                string.Empty)
                            .SetProperty(
                                x => x.OriginalFileName,
                                string.Empty),
                    cancellationToken);

        // Download authorization already fails when
        // ExpiresAtUtc is reached. Phase23 additionally
        // destroys the persisted binary artifact so expiry
        // is physical, not merely logical.
        var exportsPurged =
            await _db.ReportExportJobs
                .Where(
                    x =>
                        x.ExpiresAtUtc <= utcNow &&
                        (
                            x.Status !=
                                ReportExportJobStatus.Expired ||
                            x.FileContent != null ||
                            x.FileName != null ||
                            x.ContentType != null
                        ))
                .ExecuteUpdateAsync(
                    setters =>
                        setters
                            .SetProperty(
                                x => x.Status,
                                ReportExportJobStatus.Expired)
                            .SetProperty(
                                x => x.FileContent,
                                (byte[]?)null)
                            .SetProperty(
                                x => x.FileName,
                                (string?)null)
                            .SetProperty(
                                x => x.ContentType,
                                (string?)null),
                    cancellationToken);

        // Only read notifications are eligible. Pending
        // delivery jobs prevent notification deletion.
        var oldReadNotificationIds =
            await _db.UserNotifications
                .AsNoTracking()
                .Where(
                    x =>
                        x.ReadAtUtc.HasValue &&
                        x.ReadAtUtc.Value <=
                            notificationCutoff)
                .OrderBy(x => x.ReadAtUtc)
                .Select(x => x.Id)
                .Take(NotificationSweepBatchSize)
                .ToArrayAsync(
                    cancellationToken);

        var deliveriesDeleted = 0;
        var notificationsDeleted = 0;

        if (oldReadNotificationIds.Length > 0)
        {
            deliveriesDeleted =
                await _db.NotificationDeliveryJobs
                    .Where(
                        x =>
                            oldReadNotificationIds
                                .Contains(
                                    x.NotificationId) &&
                            x.Status !=
                                NotificationDeliveryStatus.Pending)
                    .ExecuteDeleteAsync(
                        cancellationToken);

            notificationsDeleted =
                await _db.UserNotifications
                    .Where(
                        x =>
                            oldReadNotificationIds
                                .Contains(x.Id) &&
                            !_db.NotificationDeliveryJobs
                                .Any(
                                    delivery =>
                                        delivery.NotificationId ==
                                        x.Id))
                    .ExecuteDeleteAsync(
                        cancellationToken);
        }

        return new SensitiveDataRetentionResult(
            importsScrubbed,
            exportsPurged,
            deliveriesDeleted,
            notificationsDeleted);
    }
}
