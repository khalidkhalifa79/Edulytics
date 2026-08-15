using Edulytics.Core.Analytics;

namespace Edulytics.Core.Interfaces;

public interface IAnalyticsRepository
{
    Task<AnalyticsSourceSnapshot> GetSourceSnapshotAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<AnalyticsProjectionSnapshot> GetProjectionSnapshotAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<DateTime?> GetLatestSourceUpdateAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<AnalyticsPersistenceResult> ReplaceProjectionsAsync(
        Guid schoolId,
        AnalyticsProjectionSet projections,
        CancellationToken cancellationToken = default);
}
