namespace Edulytics.Web.Email;

public sealed class EmailConnectorCircuitBreaker
{
    private readonly object _sync = new();

    private int _consecutiveFailures;
    private DateTime? _openUntilUtc;

    public bool CanExecute(
        DateTime nowUtc)
    {
        lock (_sync)
        {
            if (!_openUntilUtc.HasValue)
            {
                return true;
            }

            if (_openUntilUtc.Value > nowUtc)
            {
                return false;
            }

            _openUntilUtc = null;
            _consecutiveFailures = 0;

            return true;
        }
    }

    public void RecordSuccess()
    {
        lock (_sync)
        {
            _consecutiveFailures = 0;
            _openUntilUtc = null;
        }
    }

    public void RecordFailure(
        DateTime nowUtc,
        int failureThreshold,
        int breakSeconds)
    {
        lock (_sync)
        {
            _consecutiveFailures++;

            if (_consecutiveFailures <
                failureThreshold)
            {
                return;
            }

            _openUntilUtc =
                nowUtc.AddSeconds(
                    breakSeconds);
        }
    }

    public (
        int ConsecutiveFailures,
        DateTime? OpenUntilUtc)
        Snapshot()
    {
        lock (_sync)
        {
            return (
                _consecutiveFailures,
                _openUntilUtc
            );
        }
    }
}
