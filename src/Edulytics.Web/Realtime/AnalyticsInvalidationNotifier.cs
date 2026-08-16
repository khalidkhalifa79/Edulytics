using Edulytics.Core.Realtime;
using Edulytics.Services.Realtime;
using Edulytics.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Edulytics.Web.Realtime;

public sealed class AnalyticsInvalidationNotifier
    : IAnalyticsInvalidationNotifier
{
    private readonly IHubContext<AnalyticsHub> _hub;

    public AnalyticsInvalidationNotifier(
        IHubContext<AnalyticsHub> hub)
    {
        _hub = hub;
    }

    public Task NotifySchoolAnalyticsChangedAsync(
        Guid schoolId,
        Guid refreshId,
        DateTime updatedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var message =
            new AnalyticsInvalidationMessage(
                refreshId,
                schoolId,
                updatedAtUtc);

        return _hub.Clients
            .Group(
                RealtimeGroupNames
                    .SchoolAnalytics(
                        schoolId))
            .SendAsync(
                "AnalyticsUpdated",
                message,
                cancellationToken);
    }
}
