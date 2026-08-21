using System.Security.Cryptography;
using System.Text;
using StackExchange.Redis;

namespace Edulytics.Web.Scale;

public readonly record struct
    DistributedRateLimitDecision(
        bool Allowed,
        TimeSpan RetryAfter);

public interface IDistributedSensitiveRateLimiter
{
    Task<DistributedRateLimitDecision>
        TryAcquireAsync(
            string policyName,
            string partition,
            int permitLimit,
            TimeSpan window,
            CancellationToken cancellationToken);
}

public sealed class
    DisabledDistributedSensitiveRateLimiter
    : IDistributedSensitiveRateLimiter
{
    public Task<DistributedRateLimitDecision>
        TryAcquireAsync(
            string policyName,
            string partition,
            int permitLimit,
            TimeSpan window,
            CancellationToken cancellationToken)
    {
        cancellationToken
            .ThrowIfCancellationRequested();

        return Task.FromResult(
            new DistributedRateLimitDecision(
                true,
                TimeSpan.Zero));
    }
}

public sealed class
    RedisDistributedSensitiveRateLimiter
    : IDistributedSensitiveRateLimiter
{
    private const string Script =
        """
        local current = redis.call('INCR', KEYS[1])
        if current == 1 then
          redis.call('PEXPIRE', KEYS[1], ARGV[1])
        end
        local ttl = redis.call('PTTL', KEYS[1])
        return { current, ttl }
        """;

    private readonly IConnectionMultiplexer
        _connection;

    public RedisDistributedSensitiveRateLimiter(
        IConnectionMultiplexer connection)
    {
        _connection =
            connection ??
            throw new ArgumentNullException(
                nameof(connection));
    }

    public async Task<
        DistributedRateLimitDecision>
        TryAcquireAsync(
            string policyName,
            string partition,
            int permitLimit,
            TimeSpan window,
            CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            policyName);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            partition);

        if (permitLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(permitLimit));
        }

        if (window <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(window));
        }

        cancellationToken
            .ThrowIfCancellationRequested();

        var database =
            _connection.GetDatabase();

        var key =
            BuildKey(
                policyName,
                partition);

        var result =
            await database.ScriptEvaluateAsync(
                Script,
                [key],
                [
                    (long)Math.Ceiling(
                        window.TotalMilliseconds)
                ]);

        cancellationToken
            .ThrowIfCancellationRequested();

        var values =
            (RedisResult[])result!;

        var current =
            (long)values[0];

        var ttlMilliseconds =
            (long)values[1];

        if (current <= permitLimit)
        {
            return new DistributedRateLimitDecision(
                true,
                TimeSpan.Zero);
        }

        var retryAfter =
            TimeSpan.FromMilliseconds(
                Math.Max(
                    1000,
                    ttlMilliseconds));

        return new DistributedRateLimitDecision(
            false,
            retryAfter);
    }

    private static RedisKey BuildKey(
        string policyName,
        string partition)
    {
        var bytes =
            SHA256.HashData(
                Encoding.UTF8.GetBytes(
                    partition));

        var hash =
            Convert
                .ToHexString(bytes)
                .ToLowerInvariant();

        return
            $"edulytics:ratelimit:"
            + $"{policyName}:{hash}";
    }
}
