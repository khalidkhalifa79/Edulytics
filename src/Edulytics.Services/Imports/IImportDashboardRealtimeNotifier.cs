using Edulytics.Core.Realtime;

namespace Edulytics.Services.Imports;

public interface IImportDashboardRealtimeNotifier
{
    Task NotifyAsync(
        ImportBatchCompletedEvent completed,
        CancellationToken cancellationToken = default);
}
