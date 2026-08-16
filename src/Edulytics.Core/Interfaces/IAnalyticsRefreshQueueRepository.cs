namespace Edulytics.Core.Interfaces;

public sealed record AnalyticsRefreshLease(
    Guid SchoolId,
    long RequestedVersion,
    int ProcessingAttempts,
    string LeaseOwner,
    Guid LeaseToken,
    DateTime LeaseUntilUtc);

public interface IAnalyticsRefreshQueueRepository
{
    Task RequestAsync(
        Guid schoolId,
        DateTime utcNow,
        TimeSpan debounce,
        TimeSpan maxCoalesceWindow,
        CancellationToken cancellationToken = default);

    Task<AnalyticsRefreshLease?> ClaimNextAsync(
        string leaseOwner,
        DateTime utcNow,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteAsync(
        AnalyticsRefreshLease lease,
        DateTime utcNow,
        TimeSpan debounce,
        TimeSpan maxCoalesceWindow,
        CancellationToken cancellationToken = default);

    Task<bool> MarkFailedAsync(
        AnalyticsRefreshLease lease,
        string error,
        DateTime nextAvailableAtUtc,
        CancellationToken cancellationToken = default);
}
