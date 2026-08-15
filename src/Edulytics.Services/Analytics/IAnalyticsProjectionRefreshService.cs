namespace Edulytics.Services.Analytics;

public interface IAnalyticsProjectionRefreshService
{
    Task<AnalyticsCommandResult> RefreshSchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);
}
