namespace Edulytics.Core.Reliability;

public static class OutboxRetryPolicy
{
    public static TimeSpan ComputeDelay(
        int attempt,
        int baseSeconds,
        int maxSeconds,
        int jitterMilliseconds,
        int? deterministicJitterMilliseconds = null)
    {
        if (attempt <= 0)
            throw new ArgumentOutOfRangeException(nameof(attempt));

        if (baseSeconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(baseSeconds));

        if (maxSeconds < baseSeconds)
            throw new ArgumentOutOfRangeException(nameof(maxSeconds));

        if (jitterMilliseconds < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(jitterMilliseconds));
        }

        var exponent =
            Math.Min(
                20,
                attempt - 1);

        var exponentialSeconds =
            Math.Min(
                maxSeconds,
                baseSeconds * Math.Pow(2, exponent));

        var jitter =
            deterministicJitterMilliseconds
            ?? (
                jitterMilliseconds == 0
                    ? 0
                    : Random.Shared.Next(
                        0,
                        jitterMilliseconds + 1)
            );

        if (jitter < 0 ||
            jitter > jitterMilliseconds)
        {
            throw new ArgumentOutOfRangeException(
                nameof(deterministicJitterMilliseconds));
        }

        return TimeSpan.FromSeconds(
                exponentialSeconds)
            + TimeSpan.FromMilliseconds(jitter);
    }
}
