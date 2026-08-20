using Edulytics.Core.Enums;

namespace Edulytics.Core.Interfaces;

public sealed record OperationalOutboxSummary(
    int PendingCount,
    int ProcessingCount,
    int DeadLetterCount,
    DateTime? OldestPendingAtUtc,
    DateTime? OldestProcessingAtUtc);

public sealed record OperationalOutboxItem(
    Guid Id,
    Guid? SchoolId,
    string EventType,
    OutboxMessageStatus Status,
    int ProcessingAttempts,
    DateTime OccurredAtUtc,
    DateTime AvailableAtUtc,
    DateTime? DeadLetteredAtUtc,
    string? LastError,
    string CorrelationId);

public sealed record OperationalAnalyticsFreshness(
    Guid SchoolId,
    long RequestedVersion,
    long CompletedVersion,
    DateTime LastRequestedAtUtc,
    DateTime AvailableAtUtc,
    DateTime? LeaseUntilUtc,
    int ProcessingAttempts,
    string? LastError,
    DateTime? LatestSnapshotCalculatedAtUtc)
{
    public bool IsBehind =>
        CompletedVersion < RequestedVersion;
}

public sealed record OperationalImportFailure(
    Guid Id,
    Guid SchoolId,
    ImportType ImportType,
    int RowCount,
    int ErrorCount,
    DateTime CreatedAtUtc);

public interface IOperationsRepository
{
    Task<OperationalOutboxSummary>
        GetOutboxSummaryAsync(
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalOutboxItem>>
        GetOutboxBacklogAsync(
            int maxCount,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalAnalyticsFreshness>>
        GetAnalyticsFreshnessAsync(
            int maxCount,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OperationalImportFailure>>
        GetImportFailuresAsync(
            int maxCount,
            CancellationToken cancellationToken = default);

    Task<string> GetLatestMigrationAsync(
        CancellationToken cancellationToken = default);
}
