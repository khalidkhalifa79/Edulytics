using Edulytics.Core.Realtime;
using Edulytics.Services.Imports;
using Edulytics.Web.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Edulytics.Web.Realtime;

public sealed class ImportDashboardRealtimeNotifier
    : IImportDashboardRealtimeNotifier
{
    private readonly IHubContext<AnalyticsHub> _hub;

    public ImportDashboardRealtimeNotifier(
        IHubContext<AnalyticsHub> hub)
    {
        _hub = hub;
    }

    public async Task NotifyAsync(
        ImportBatchCompletedEvent completed,
        CancellationToken cancellationToken = default)
    {
        var groups =
            new HashSet<string>(
                StringComparer.Ordinal)
            {
                RealtimeGroupNames
                    .SchoolAdmins(
                        completed.SchoolId)
            };

        foreach (var scope in
                 completed.AffectedScopes)
        {
            groups.Add(
                RealtimeGroupNames
                    .Teachers(
                        completed.SchoolId,
                        scope.ClassGroupId,
                        scope.SubjectId));
        }

        await _hub.Clients
            .Groups(groups.ToArray())
            .SendAsync(
                "AnalyticsUpdated",
                new
                {
                    completed.EventId,
                    completed.ImportBatchId,
                    completed.ImportType,
                    UpdatedAtUtc =
                        DateTime.UtcNow
                },
                cancellationToken);
    }
}
