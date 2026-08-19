using Edulytics.Core.Realtime;
using Edulytics.Services.Realtime;
using Edulytics.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Edulytics.Web.Realtime;

public sealed class DashboardRealtimeNotifier
    : IDashboardRealtimeNotifier
{
    private readonly IHubContext<AnalyticsHub> _hub;

    public DashboardRealtimeNotifier(
        IHubContext<AnalyticsHub> hub)
    {
        _hub = hub;
    }

    public async Task NotifyAssessmentResultChangedAsync(
        AssessmentResultChangedEvent change,
        CancellationToken cancellationToken = default)
    {
        var message =
            new DashboardUpdatedMessage(
                change.EventId,
                change.AssessmentId,
                change.ClassGroupId,
                change.SubjectId,
                DateTime.UtcNow);

        await Task.WhenAll(
            _hub.Clients
                .Group(
                    RealtimeGroupNames.SchoolAdmins(
                        change.SchoolId))
                .SendAsync(
                    "AnalyticsUpdated",
                    message,
                    cancellationToken),

            _hub.Clients
                .Group(
                    RealtimeGroupNames.Teachers(
                        change.SchoolId,
                        change.ClassGroupId,
                        change.SubjectId))
                .SendAsync(
                    "AnalyticsUpdated",
                    message,
                    cancellationToken),

            _hub.Clients
                .Group(
                    RealtimeGroupNames.SubjectSupervisors(
                        change.SchoolId,
                        change.SubjectId))
                .SendAsync(
                    "AnalyticsUpdated",
                    message,
                    cancellationToken));
    }
}
