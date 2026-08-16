namespace Edulytics.Web.Production;

public sealed class OutboxWorkerHealthState
{
    private readonly object _sync = new();

    private DateTime? _startedAtUtc;
    private DateTime? _lastHeartbeatUtc;

    public void MarkStarted(
        DateTime utcNow)
    {
        lock (_sync)
        {
            _startedAtUtc ??= utcNow;
            _lastHeartbeatUtc = utcNow;
        }
    }

    public void RecordHeartbeat(
        DateTime utcNow)
    {
        lock (_sync)
        {
            _lastHeartbeatUtc = utcNow;
        }
    }

    public OutboxWorkerHealthSnapshot Snapshot()
    {
        lock (_sync)
        {
            return new OutboxWorkerHealthSnapshot(
                _startedAtUtc,
                _lastHeartbeatUtc);
        }
    }
}

public sealed record OutboxWorkerHealthSnapshot(
    DateTime? StartedAtUtc,
    DateTime? LastHeartbeatUtc)
{
    public bool Started =>
        StartedAtUtc.HasValue;
}
