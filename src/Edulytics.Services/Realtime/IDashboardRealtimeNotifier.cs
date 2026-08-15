using Edulytics.Core.Realtime;

namespace Edulytics.Services.Realtime;

public interface IDashboardRealtimeNotifier
{
    Task NotifyAssessmentResultChangedAsync(
        AssessmentResultChangedEvent change,
        CancellationToken cancellationToken = default);
}
