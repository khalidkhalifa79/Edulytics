namespace Edulytics.Services.Analytics;

public interface IAnalyticsService
{
    Task<AnalyticsQueryResult<AnalyticsDashboard>>
        GetDashboardAsync(
            Guid actorUserId,
            Guid? academicYearId = null,
            Guid? classGroupId = null,
            Guid? subjectId = null,
            CancellationToken cancellationToken = default);

    Task<AnalyticsCommandResult> RecalculateAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
