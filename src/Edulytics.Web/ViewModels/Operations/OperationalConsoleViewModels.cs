using Edulytics.Core.Interfaces;

namespace Edulytics.Web.ViewModels.Operations;

public sealed record OperationalWorkerViewModel(
    bool Started,
    DateTime? StartedAtUtc,
    DateTime? LastHeartbeatUtc,
    double? HeartbeatAgeSeconds);

public sealed record OperationalConnectorViewModel(
    bool Enabled,
    string Status,
    int ConsecutiveFailures,
    DateTime? CircuitOpenUntilUtc);

public sealed class OperationalConsoleViewModel
{
    public string ReleaseSha { get; init; } =
        "unknown";

    public string MigrationVersion { get; init; } =
        "unknown";

    public OperationalWorkerViewModel Worker { get; init; } =
        new(
            false,
            null,
            null,
            null);

    public OperationalConnectorViewModel EmailConnector { get; init; } =
        new(
            false,
            "Disabled",
            0,
            null);

    public OperationalOutboxSummary OutboxSummary { get; init; } =
        new(
            0,
            0,
            0,
            null,
            null);

    public IReadOnlyList<OperationalOutboxItem>
        OutboxBacklog { get; init; } =
        [];

    public IReadOnlyList<OutboxDeadLetter>
        DeadLetters { get; init; } =
        [];

    public IReadOnlyList<OperationalAnalyticsFreshness>
        AnalyticsFreshness { get; init; } =
        [];

    public IReadOnlyList<OperationalImportFailure>
        ImportFailures { get; init; } =
        [];
}
