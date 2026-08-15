using Edulytics.Core.Interfaces;

namespace Edulytics.Services.Analytics;

public sealed class AnalyticsProjectionRefreshService
    : IAnalyticsProjectionRefreshService
{
    private readonly IAnalyticsRepository _analytics;
    private readonly AnalyticsProjectionBuilder _builder;

    public AnalyticsProjectionRefreshService(
        IAnalyticsRepository analytics,
        AnalyticsProjectionBuilder builder)
    {
        _analytics = analytics;
        _builder = builder;
    }

    public async Task<AnalyticsCommandResult> RefreshSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var source =
                await _analytics.GetSourceSnapshotAsync(
                    schoolId,
                    cancellationToken);

            var projections =
                _builder.Build(
                    source,
                    DateTime.UtcNow);

            var result =
                await _analytics.ReplaceProjectionsAsync(
                    schoolId,
                    projections,
                    cancellationToken);

            return result.Succeeded
                ? AnalyticsCommandResult.Success()
                : AnalyticsCommandResult.Failure(
                    AnalyticsErrorCode.PersistenceError);
        }
        catch (InvalidOperationException)
        {
            return AnalyticsCommandResult.Failure(
                AnalyticsErrorCode.InvalidSourceData);
        }
    }
}
