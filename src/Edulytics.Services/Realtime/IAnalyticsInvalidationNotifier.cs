namespace Edulytics.Services.Realtime;

public interface IAnalyticsInvalidationNotifier
{
    Task NotifySchoolAnalyticsChangedAsync(
        Guid schoolId,
        Guid refreshId,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default);
}
