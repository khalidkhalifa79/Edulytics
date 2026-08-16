using System.Text.Json;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Realtime;
using Edulytics.Core.Reliability;
using Edulytics.Services.Imports;
using Edulytics.Web.Production;
using Microsoft.Extensions.Options;

namespace Edulytics.Web.Background;

public sealed class OutboxProcessorBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<
        OutboxProcessorBackgroundService> _logger;
    private readonly OutboxWorkerHealthState _health;
    private readonly OutboxV2Options _options;
    private readonly string _workerId;

    public OutboxProcessorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<
            OutboxProcessorBackgroundService> logger,
        OutboxWorkerHealthState health,
        IOptions<OutboxV2Options> options)
    {
        _scopeFactory =
            scopeFactory
            ?? throw new ArgumentNullException(
                nameof(scopeFactory));

        _logger =
            logger
            ?? throw new ArgumentNullException(
                nameof(logger));

        _health =
            health
            ?? throw new ArgumentNullException(
                nameof(health));

        _options =
            options?.Value
            ?? throw new ArgumentNullException(
                nameof(options));

        _workerId =
            $"{Environment.MachineName}:outbox:"
            + $"{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _health.MarkStarted(
            DateTime.UtcNow);

        _logger.LogInformation(
            "Outbox v2 processor {WorkerId} started.",
            _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            _health.RecordHeartbeat(
                DateTime.UtcNow);

            try
            {
                var found =
                    await ProcessBatchAsync();

                _health.RecordHeartbeat(
                    DateTime.UtcNow);

                if (!found)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(
                            _options
                                .PollDelayMilliseconds),
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (
                    stoppingToken
                        .IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Outbox v2 polling failed.");

                await Task.Delay(
                    TimeSpan.FromMilliseconds(
                        _options
                            .ErrorDelayMilliseconds),
                    stoppingToken);
            }
        }

        _logger.LogInformation(
            "Outbox v2 processor {WorkerId} stopped.",
            _workerId);
    }

    private async Task<bool> ProcessBatchAsync()
    {
        IReadOnlyList<OutboxLease> leases;

        using (var claimScope =
               _scopeFactory.CreateScope())
        {
            var repository =
                claimScope.ServiceProvider
                    .GetRequiredService<
                        IOutboxRepository>();

            leases =
                await repository.ClaimBatchAsync(
                    _workerId,
                    DateTime.UtcNow,
                    TimeSpan.FromSeconds(
                        _options.LeaseSeconds),
                    _options.BatchSize,
                    CancellationToken.None);
        }

        if (leases.Count == 0)
            return false;

        foreach (var lease in leases)
        {
            await ProcessLeaseAsync(
                lease);
        }

        return true;
    }

    private async Task ProcessLeaseAsync(
        OutboxLease lease)
    {
        using var timeout =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(
                    _options
                        .MessageProcessingTimeoutSeconds));

        try
        {
            using var scope =
                _scopeFactory.CreateScope();

            var refreshQueue =
                scope.ServiceProvider
                    .GetRequiredService<
                        IAnalyticsRefreshQueueRepository>();

            await QueueAnalyticsRefreshAsync(
                lease,
                refreshQueue,
                timeout.Token);

            var outbox =
                scope.ServiceProvider
                    .GetRequiredService<
                        IOutboxRepository>();

            var completed =
                await outbox.MarkProcessedAsync(
                    lease.Id,
                    lease.LeaseOwner,
                    lease.LeaseToken,
                    DateTime.UtcNow,
                    timeout.Token);

            if (!completed)
            {
                _logger.LogWarning(
                    "Outbox {OutboxId} lost lease "
                    + "before completion; stale "
                    + "completion was rejected.",
                    lease.Id);
            }
        }
        catch (Exception ex)
        {
            await RecordFailureAsync(
                lease,
                ex);
        }
    }

    private async Task QueueAnalyticsRefreshAsync(
        OutboxLease lease,
        IAnalyticsRefreshQueueRepository refreshQueue,
        CancellationToken cancellationToken)
    {
        var schoolId =
            ValidateAndResolveSchool(
                lease);

        await refreshQueue.RequestAsync(
            schoolId,
            DateTime.UtcNow,
            TimeSpan.FromMilliseconds(
                _options
                    .AnalyticsDebounceMilliseconds),
            TimeSpan.FromMilliseconds(
                _options
                    .AnalyticsMaxCoalesceMilliseconds),
            cancellationToken);
    }

    private static Guid ValidateAndResolveSchool(
        OutboxLease lease)
    {
        if (!lease.SchoolId.HasValue)
        {
            throw new InvalidOperationException(
                "School-scoped realtime outbox "
                + "message has no SchoolId.");
        }

        if (lease.EventType ==
                RealtimeEventTypes
                    .AssessmentResultEntered ||
            lease.EventType ==
                RealtimeEventTypes
                    .AssessmentResultUpdated)
        {
            var change =
                JsonSerializer.Deserialize<
                    AssessmentResultChangedEvent>(
                        lease.PayloadJson);

            if (change is null ||
                change.SchoolId !=
                    lease.SchoolId.Value)
            {
                throw new InvalidOperationException(
                    "Invalid assessment-result "
                    + "outbox message.");
            }

            return change.SchoolId;
        }

        if (lease.EventType ==
            RealtimeEventTypes
                .ImportBatchCompleted)
        {
            var completed =
                JsonSerializer.Deserialize<
                    ImportBatchCompletedEvent>(
                        lease.PayloadJson);

            if (completed is null ||
                completed.SchoolId !=
                    lease.SchoolId.Value)
            {
                throw new InvalidOperationException(
                    "Invalid import outbox message.");
            }

            return completed.SchoolId;
        }

        throw new InvalidOperationException(
            $"Unsupported event type: "
            + $"{lease.EventType}");
    }

    private async Task RecordFailureAsync(
        OutboxLease lease,
        Exception exception)
    {
        var delay =
            OutboxRetryPolicy
                .ComputeDelay(
                    lease.ProcessingAttempts,
                    _options.RetryBaseSeconds,
                    _options.RetryMaxSeconds,
                    _options
                        .RetryJitterMilliseconds);

        try
        {
            using var failureScope =
                _scopeFactory.CreateScope();

            var repository =
                failureScope.ServiceProvider
                    .GetRequiredService<
                        IOutboxRepository>();

            using var markTimeout =
                new CancellationTokenSource(
                    TimeSpan.FromSeconds(5));

            var disposition =
                await repository.MarkFailedAsync(
                    lease.Id,
                    lease.LeaseOwner,
                    lease.LeaseToken,
                    exception.Message,
                    DateTime.UtcNow,
                    DateTime.UtcNow.Add(delay),
                    _options.MaxAttempts,
                    markTimeout.Token);

            if (disposition ==
                OutboxFailureDisposition
                    .DeadLettered)
            {
                _logger.LogError(
                    exception,
                    "Outbox {OutboxId} moved to "
                    + "dead letter after {Attempts} "
                    + "attempts.",
                    lease.Id,
                    lease.ProcessingAttempts);
            }
            else if (disposition ==
                OutboxFailureDisposition
                    .RetryScheduled)
            {
                _logger.LogWarning(
                    exception,
                    "Outbox {OutboxId} retry "
                    + "scheduled after {Delay}.",
                    lease.Id,
                    delay);
            }
            else
            {
                _logger.LogWarning(
                    "Outbox {OutboxId} failure "
                    + "could not be recorded because "
                    + "the lease is stale.",
                    lease.Id);
            }
        }
        catch (Exception markException)
        {
            _logger.LogError(
                markException,
                "Failed to record outbox v2 "
                + "failure for {OutboxId}.",
                lease.Id);
        }
    }
}
