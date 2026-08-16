using Edulytics.Web.Production;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Edulytics.Web.Health;

public sealed class OutboxWorkerReadinessHealthCheck
    : IHealthCheck
{
    private readonly OutboxWorkerHealthState _state;

    private readonly IOptions<
        ProductionOptions> _options;

    public OutboxWorkerReadinessHealthCheck(
        OutboxWorkerHealthState state,
        IOptions<ProductionOptions> options)
    {
        _state =
            state ??
            throw new ArgumentNullException(
                nameof(state));

        _options =
            options ??
            throw new ArgumentNullException(
                nameof(options));
    }

    public Task<HealthCheckResult>
        CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken =
                default)
    {
        var snapshot =
            _state.Snapshot();

        if (!snapshot.Started ||
            !snapshot.LastHeartbeatUtc
                .HasValue)
        {
            return Task.FromResult(
                HealthCheckResult
                    .Unhealthy(
                        "Outbox worker has not started."));
        }

        var age =
            DateTime.UtcNow -
            snapshot
                .LastHeartbeatUtc
                .Value;

        if (age >
            TimeSpan.FromSeconds(
                _options.Value
                    .WorkerStaleAfterSeconds))
        {
            return Task.FromResult(
                HealthCheckResult
                    .Unhealthy(
                        "Outbox worker heartbeat is stale.",
                        data:
                            new Dictionary<
                                string,
                                object>
                            {
                                [
                                    "HeartbeatAgeSeconds"
                                ] =
                                    Math.Round(
                                        age.TotalSeconds,
                                        2)
                            }));
        }

        return Task.FromResult(
            HealthCheckResult
                .Healthy(
                    "Outbox worker is active.",
                    data:
                        new Dictionary<
                            string,
                            object>
                        {
                            [
                                "HeartbeatAgeSeconds"
                            ] =
                                Math.Round(
                                    age.TotalSeconds,
                                    2)
                        }));
    }
}
