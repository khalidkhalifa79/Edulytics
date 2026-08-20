using Edulytics.Core.Interfaces;
using Edulytics.Web.Email;
using Edulytics.Web.Production;
using Edulytics.Web.ViewModels.Operations;
using Microsoft.Extensions.Options;

namespace Edulytics.Web.Operations;

public sealed class OperationalConsoleService
{
    private const int MaximumRows = 100;

    private readonly IOperationsRepository
        _operations;

    private readonly IOutboxRepository
        _outbox;

    private readonly OutboxWorkerHealthState
        _worker;

    private readonly EmailConnectorCircuitBreaker
        _emailCircuit;

    private readonly SmtpEmailOptions
        _emailOptions;

    private readonly IConfiguration
        _configuration;

    public OperationalConsoleService(
        IOperationsRepository operations,
        IOutboxRepository outbox,
        OutboxWorkerHealthState worker,
        EmailConnectorCircuitBreaker emailCircuit,
        IOptions<SmtpEmailOptions> emailOptions,
        IConfiguration configuration)
    {
        _operations = operations;
        _outbox = outbox;
        _worker = worker;
        _emailCircuit = emailCircuit;
        _emailOptions = emailOptions.Value;
        _configuration = configuration;
    }

    public async Task<OperationalConsoleViewModel>
        GetAsync(
            CancellationToken cancellationToken = default)
    {
        // Sequential on purpose: the scoped repository
        // shares one EF DbContext.
        var summary =
            await _operations
                .GetOutboxSummaryAsync(
                    cancellationToken);

        var backlog =
            await _operations
                .GetOutboxBacklogAsync(
                    MaximumRows,
                    cancellationToken);

        var deadLetters =
            await _outbox
                .GetDeadLettersAsync(
                    MaximumRows,
                    cancellationToken);

        var analytics =
            await _operations
                .GetAnalyticsFreshnessAsync(
                    MaximumRows,
                    cancellationToken);

        var importFailures =
            await _operations
                .GetImportFailuresAsync(
                    MaximumRows,
                    cancellationToken);

        var migration =
            await _operations
                .GetLatestMigrationAsync(
                    cancellationToken);

        var now =
            DateTime.UtcNow;

        var worker =
            _worker.Snapshot();

        var circuit =
            _emailCircuit.Snapshot();

        return new OperationalConsoleViewModel
        {
            ReleaseSha =
                ResolveReleaseSha(),

            MigrationVersion =
                migration,

            Worker =
                new OperationalWorkerViewModel(
                    worker.Started,
                    worker.StartedAtUtc,
                    worker.LastHeartbeatUtc,
                    worker.LastHeartbeatUtc
                        .HasValue
                        ? Math.Max(
                            0,
                            (
                                now -
                                worker.LastHeartbeatUtc
                                    .Value
                            ).TotalSeconds)
                        : null),

            EmailConnector =
                new OperationalConnectorViewModel(
                    _emailOptions.Enabled,
                    ResolveConnectorStatus(
                        now,
                        circuit
                            .ConsecutiveFailures,
                        circuit.OpenUntilUtc),
                    circuit.ConsecutiveFailures,
                    circuit.OpenUntilUtc),

            OutboxSummary =
                summary,

            OutboxBacklog =
                backlog,

            DeadLetters =
                deadLetters,

            AnalyticsFreshness =
                analytics,

            ImportFailures =
                importFailures
        };
    }

    private string ResolveReleaseSha()
    {
        var candidates =
            new[]
            {
                Environment.GetEnvironmentVariable(
                    "RENDER_GIT_COMMIT"),
                Environment.GetEnvironmentVariable(
                    "GITHUB_SHA"),
                Environment.GetEnvironmentVariable(
                    "SOURCE_VERSION"),
                _configuration[
                    "Edulytics:ReleaseSha"]
            };

        var value =
            candidates.FirstOrDefault(
                x =>
                    !string.IsNullOrWhiteSpace(
                        x));

        if (string.IsNullOrWhiteSpace(value))
        {
            return "unknown";
        }

        value = value.Trim();

        return value.Length <= 80
            ? value
            : value[..80];
    }

    private string ResolveConnectorStatus(
        DateTime utcNow,
        int consecutiveFailures,
        DateTime? openUntilUtc)
    {
        if (!_emailOptions.Enabled)
        {
            return "Disabled";
        }

        if (!EmailConfigurationValid())
        {
            return "InvalidConfiguration";
        }

        if (openUntilUtc.HasValue &&
            openUntilUtc.Value > utcNow)
        {
            return "CircuitOpen";
        }

        if (consecutiveFailures > 0)
        {
            return "Degraded";
        }

        return "Healthy";
    }

    private bool EmailConfigurationValid()
    {
        if (string.IsNullOrWhiteSpace(
                _emailOptions.Host) ||
            _emailOptions.Port <= 0 ||
            string.IsNullOrWhiteSpace(
                _emailOptions.FromAddress) ||
            _emailOptions.TimeoutSeconds <= 0 ||
            _emailOptions
                .CircuitFailureThreshold <= 0 ||
            _emailOptions.CircuitBreakSeconds <= 0)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(
                _emailOptions.Username) &&
            string.IsNullOrWhiteSpace(
                _emailOptions.Password))
        {
            return false;
        }

        return true;
    }
}
