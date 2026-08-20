using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class OperationsRepository
    : IOperationsRepository
{
    private const int MaximumRows = 200;

    private readonly EdulyticsDbContext _db;

    public OperationsRepository(
        EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<OperationalOutboxSummary>
        GetOutboxSummaryAsync(
            CancellationToken cancellationToken = default)
    {
        var counts =
            await _db.OutboxMessages
                .AsNoTracking()
                .Where(
                    x =>
                        x.Status !=
                        OutboxMessageStatus.Processed)
                .GroupBy(x => x.Status)
                .Select(
                    g => new
                    {
                        Status = g.Key,
                        Count = g.Count()
                    })
                .ToListAsync(cancellationToken);

        int Count(OutboxMessageStatus status) =>
            counts
                .Where(x => x.Status == status)
                .Select(x => x.Count)
                .SingleOrDefault();

        var oldestPending =
            await _db.OutboxMessages
                .AsNoTracking()
                .Where(
                    x =>
                        x.Status ==
                        OutboxMessageStatus.Pending)
                .OrderBy(x => x.OccurredAtUtc)
                .Select(
                    x =>
                        (DateTime?)
                        x.OccurredAtUtc)
                .FirstOrDefaultAsync(
                    cancellationToken);

        var oldestProcessing =
            await _db.OutboxMessages
                .AsNoTracking()
                .Where(
                    x =>
                        x.Status ==
                        OutboxMessageStatus.Processing)
                .OrderBy(x => x.OccurredAtUtc)
                .Select(
                    x =>
                        (DateTime?)
                        x.OccurredAtUtc)
                .FirstOrDefaultAsync(
                    cancellationToken);

        return new OperationalOutboxSummary(
            Count(OutboxMessageStatus.Pending),
            Count(OutboxMessageStatus.Processing),
            Count(OutboxMessageStatus.DeadLetter),
            oldestPending,
            oldestProcessing);
    }

    public async Task<
        IReadOnlyList<OperationalOutboxItem>>
        GetOutboxBacklogAsync(
            int maxCount,
            CancellationToken cancellationToken = default)
    {
        maxCount =
            Math.Clamp(
                maxCount,
                1,
                MaximumRows);

        return await _db.OutboxMessages
            .AsNoTracking()
            .Where(
                x =>
                    x.Status ==
                        OutboxMessageStatus.Pending ||
                    x.Status ==
                        OutboxMessageStatus.Processing)
            .OrderBy(x => x.AvailableAtUtc)
            .ThenBy(x => x.OccurredAtUtc)
            .Take(maxCount)
            .Select(
                x =>
                    new OperationalOutboxItem(
                        x.Id,
                        x.SchoolId,
                        x.EventType,
                        x.Status,
                        x.ProcessingAttempts,
                        x.OccurredAtUtc,
                        x.AvailableAtUtc,
                        x.DeadLetteredAtUtc,
                        x.LastError,
                        x.CorrelationId))
            .ToListAsync(cancellationToken);
    }

    public async Task<
        IReadOnlyList<
            OperationalAnalyticsFreshness>>
        GetAnalyticsFreshnessAsync(
            int maxCount,
            CancellationToken cancellationToken = default)
    {
        maxCount =
            Math.Clamp(
                maxCount,
                1,
                MaximumRows);

        var states =
            await _db.AnalyticsRefreshStates
                .AsNoTracking()
                .OrderByDescending(
                    x => x.LastRequestedAtUtc)
                .Take(maxCount)
                .ToListAsync(
                    cancellationToken);

        if (states.Count == 0)
        {
            return [];
        }

        var schoolIds =
            states
                .Select(x => x.SchoolId)
                .Distinct()
                .ToArray();

        var snapshots =
            await _db.SchoolAnalyticsSnapshots
                .AsNoTracking()
                .Where(
                    x =>
                        schoolIds.Contains(
                            x.SchoolId))
                .GroupBy(x => x.SchoolId)
                .Select(
                    group => new
                    {
                        SchoolId = group.Key,
                        CalculatedAtUtc =
                            group.Max(
                                x =>
                                    x.CalculatedAtUtc)
                    })
                .ToDictionaryAsync(
                    x => x.SchoolId,
                    x => x.CalculatedAtUtc,
                    cancellationToken);

        return states
            .Select(
                x =>
                    new OperationalAnalyticsFreshness(
                        x.SchoolId,
                        x.RequestedVersion,
                        x.CompletedVersion,
                        x.LastRequestedAtUtc,
                        x.AvailableAtUtc,
                        x.LeaseUntilUtc,
                        x.ProcessingAttempts,
                        x.LastError,
                        snapshots.TryGetValue(
                            x.SchoolId,
                            out var calculated)
                            ? calculated
                            : null))
            .ToArray();
    }

    public async Task<
        IReadOnlyList<OperationalImportFailure>>
        GetImportFailuresAsync(
            int maxCount,
            CancellationToken cancellationToken = default)
    {
        maxCount =
            Math.Clamp(
                maxCount,
                1,
                MaximumRows);

        // Deliberately do not expose RowsJson, FileHash,
        // or OriginalFileName in the operator projection.
        return await _db.ImportBatches
            .AsNoTracking()
            .Where(
                x =>
                    x.Status ==
                    ImportBatchStatus.ValidationFailed)
            .OrderByDescending(
                x => x.CreatedAtUtc)
            .Take(maxCount)
            .Select(
                x =>
                    new OperationalImportFailure(
                        x.Id,
                        x.SchoolId,
                        x.ImportType,
                        x.RowCount,
                        x.ErrorCount,
                        x.CreatedAtUtc))
            .ToListAsync(
                cancellationToken);
    }

    public async Task<string>
        GetLatestMigrationAsync(
            CancellationToken cancellationToken = default)
    {
        var migrations =
            await _db.Database
                .GetAppliedMigrationsAsync(
                    cancellationToken);

        return migrations
            .LastOrDefault()
            ?? "none";
    }
}
