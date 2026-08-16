using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class OutboxRepository
    : IOutboxRepository
{
    private readonly EdulyticsDbContext _db;

    public OutboxRepository(
        EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OutboxLease>>
        ClaimBatchAsync(
            string leaseOwner,
            DateTime utcNow,
            TimeSpan leaseDuration,
            int maxCount,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            leaseOwner);

        if (leaseOwner.Length > 200)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseOwner));
        }

        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseDuration));
        }

        if (maxCount <= 0)
            return [];

        var pending =
            (int)OutboxMessageStatus.Pending;

        var processing =
            (int)OutboxMessageStatus.Processing;

        await using var transaction =
            await _db.Database
                .BeginTransactionAsync(
                    cancellationToken);

        // One oldest eligible row per SchoolId is selected first.
        // The outer FOR UPDATE SKIP LOCKED makes fetch+claim one
        // PostgreSQL transaction while preventing a hot school from
        // filling an entire batch.
        var rows =
            await _db.OutboxMessages
                .FromSqlInterpolated(
                    $@"SELECT o.*
FROM ""OutboxMessages"" AS o
INNER JOIN (
    SELECT DISTINCT ON (q.""SchoolId"")
        q.""Id"",
        q.""OccurredAtUtc""
    FROM ""OutboxMessages"" AS q
    WHERE
        q.""AvailableAtUtc"" <= {utcNow}
        AND (
            q.""Status"" = {pending}
            OR (
                q.""Status"" = {processing}
                AND q.""LeaseUntilUtc"" <= {utcNow}
            )
        )
    ORDER BY
        q.""SchoolId"",
        q.""OccurredAtUtc"",
        q.""Id""
) AS head
    ON head.""Id"" = o.""Id""
ORDER BY
    head.""OccurredAtUtc"",
    o.""Id""
LIMIT {maxCount}
FOR UPDATE OF o SKIP LOCKED")
                .ToListAsync(
                    cancellationToken);

        if (rows.Count == 0)
        {
            await transaction.CommitAsync(
                cancellationToken);

            return [];
        }

        var leaseToken =
            Guid.NewGuid();

        var leaseUntilUtc =
            utcNow.Add(leaseDuration);

        foreach (var row in rows)
        {
            row.Status =
                OutboxMessageStatus.Processing;

            row.ProcessingAttempts++;
            row.LeaseOwner = leaseOwner;
            row.LeaseToken = leaseToken;
            row.LeaseUntilUtc = leaseUntilUtc;
            row.DeadLetteredAtUtc = null;
        }

        await _db.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return rows
            .Select(
                row =>
                    new OutboxLease(
                        row.Id,
                        row.SchoolId,
                        row.EventType,
                        row.PayloadJson,
                        row.OccurredAtUtc,
                        row.ProcessingAttempts,
                        row.CorrelationId,
                        leaseOwner,
                        leaseToken,
                        leaseUntilUtc))
            .ToArray();
    }

    public async Task<bool> MarkProcessedAsync(
        Guid id,
        string leaseOwner,
        Guid leaseToken,
        DateTime processedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _db.Database
                .BeginTransactionAsync(
                    cancellationToken);

        var entity =
            await LockAsync(
                id,
                cancellationToken);

        if (!OwnsLease(
                entity,
                leaseOwner,
                leaseToken))
        {
            await transaction.CommitAsync(
                cancellationToken);

            return false;
        }

        entity!.Status =
            OutboxMessageStatus.Processed;

        entity.ProcessedAtUtc =
            processedAtUtc;

        entity.AvailableAtUtc =
            processedAtUtc;

        entity.DeadLetteredAtUtc = null;
        entity.LastError = null;
        entity.LeaseOwner = null;
        entity.LeaseToken = null;
        entity.LeaseUntilUtc = null;

        await _db.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return true;
    }

    public async Task<OutboxFailureDisposition>
        MarkFailedAsync(
            Guid id,
            string leaseOwner,
            Guid leaseToken,
            string error,
            DateTime utcNow,
            DateTime nextAvailableAtUtc,
            int maxAttempts,
            CancellationToken cancellationToken = default)
    {
        if (maxAttempts <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxAttempts));
        }

        await using var transaction =
            await _db.Database
                .BeginTransactionAsync(
                    cancellationToken);

        var entity =
            await LockAsync(
                id,
                cancellationToken);

        if (!OwnsLease(
                entity,
                leaseOwner,
                leaseToken))
        {
            await transaction.CommitAsync(
                cancellationToken);

            return OutboxFailureDisposition
                .StaleLease;
        }

        entity!.LastError =
            TrimError(error);

        entity.LeaseOwner = null;
        entity.LeaseToken = null;
        entity.LeaseUntilUtc = null;

        OutboxFailureDisposition disposition;

        if (entity.ProcessingAttempts >=
            maxAttempts)
        {
            entity.Status =
                OutboxMessageStatus.DeadLetter;

            entity.DeadLetteredAtUtc =
                utcNow;

            entity.AvailableAtUtc =
                utcNow;

            disposition =
                OutboxFailureDisposition
                    .DeadLettered;
        }
        else
        {
            entity.Status =
                OutboxMessageStatus.Pending;

            entity.DeadLetteredAtUtc =
                null;

            entity.AvailableAtUtc =
                nextAvailableAtUtc;

            disposition =
                OutboxFailureDisposition
                    .RetryScheduled;
        }

        await _db.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return disposition;
    }

    public async Task<IReadOnlyList<OutboxDeadLetter>>
        GetDeadLettersAsync(
            int maxCount,
            CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0)
            return [];

        return await _db.OutboxMessages
            .AsNoTracking()
            .Where(
                x =>
                    x.Status ==
                    OutboxMessageStatus
                        .DeadLetter)
            .OrderBy(
                x => x.DeadLetteredAtUtc)
            .ThenBy(
                x => x.OccurredAtUtc)
            .Take(maxCount)
            .Select(
                x =>
                    new OutboxDeadLetter(
                        x.Id,
                        x.SchoolId,
                        x.EventType,
                        x.ProcessingAttempts,
                        x.LastError,
                        x.OccurredAtUtc,
                        x.DeadLetteredAtUtc))
            .ToListAsync(
                cancellationToken);
    }

    public async Task<bool> RequeueDeadLetterAsync(
        Guid id,
        Guid actorUserId,
        string reason,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new ArgumentException(
                "Operator actor is required.",
                nameof(actorUserId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(
            reason);

        reason = reason.Trim();

        if (reason.Length > 500)
        {
            throw new ArgumentOutOfRangeException(
                nameof(reason));
        }

        await using var transaction =
            await _db.Database
                .BeginTransactionAsync(
                    cancellationToken);

        var entity =
            await LockAsync(
                id,
                cancellationToken);

        if (entity is null ||
            entity.Status !=
                OutboxMessageStatus.DeadLetter)
        {
            await transaction.CommitAsync(
                cancellationToken);

            return false;
        }

        _db.OutboxRequeueAudits.Add(
            new OutboxRequeueAudit
            {
                Id = Guid.NewGuid(),
                OutboxMessageId = entity.Id,
                ActorUserId = actorUserId,
                Reason = reason,
                PreviousAttempts =
                    entity.ProcessingAttempts,
                RequeuedAtUtc = utcNow
            });

        entity.Status =
            OutboxMessageStatus.Pending;

        entity.ProcessingAttempts = 0;
        entity.AvailableAtUtc = utcNow;
        entity.ProcessedAtUtc = null;
        entity.DeadLetteredAtUtc = null;
        entity.LastError = null;
        entity.LeaseOwner = null;
        entity.LeaseToken = null;
        entity.LeaseUntilUtc = null;

        await _db.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return true;
    }

    private async Task<OutboxMessage?>
        LockAsync(
            Guid id,
            CancellationToken cancellationToken)
    {
        return await _db.OutboxMessages
            .FromSqlInterpolated(
                $@"SELECT *
FROM ""OutboxMessages""
WHERE ""Id"" = {id}
FOR UPDATE")
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    private static bool OwnsLease(
        OutboxMessage? entity,
        string leaseOwner,
        Guid leaseToken) =>
        entity is not null &&
        entity.Status ==
            OutboxMessageStatus.Processing &&
        entity.LeaseToken == leaseToken &&
        string.Equals(
            entity.LeaseOwner,
            leaseOwner,
            StringComparison.Ordinal);

    private static string TrimError(
        string? error)
    {
        var value =
            string.IsNullOrWhiteSpace(error)
                ? "Unknown outbox processing failure."
                : error.Trim();

        return value.Length <= 2000
            ? value
            : value[..2000];
    }
}
