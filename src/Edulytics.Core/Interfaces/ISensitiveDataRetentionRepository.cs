namespace Edulytics.Core.Interfaces;

public sealed record SensitiveDataRetentionResult(
    int ImportPayloadsScrubbed,
    int ExportArtifactsPurged,
    int NotificationDeliveriesDeleted,
    int NotificationsDeleted);

public interface ISensitiveDataRetentionRepository
{
    Task<SensitiveDataRetentionResult>
        ApplyAsync(
            DateTime utcNow,
            TimeSpan importPayloadRetention,
            TimeSpan notificationReadRetention,
            CancellationToken cancellationToken = default);
}
