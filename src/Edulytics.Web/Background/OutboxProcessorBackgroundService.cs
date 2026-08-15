using System.Text.Json;
using Edulytics.Core.Entities;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Realtime;
using Edulytics.Services.Analytics;
using Edulytics.Services.Imports;
using Edulytics.Services.Realtime;

namespace Edulytics.Web.Background;

public sealed class OutboxProcessorBackgroundService
    : BackgroundService
{
    private static readonly TimeSpan EmptyDelay =
        TimeSpan.FromMilliseconds(400);

    private static readonly TimeSpan ErrorDelay =
        TimeSpan.FromSeconds(1);

    private static readonly TimeSpan LeaseDuration =
        TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<
        OutboxProcessorBackgroundService> _logger;

    public OutboxProcessorBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<
            OutboxProcessorBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var found =
                    await ProcessBatchAsync(
                        stoppingToken);

                if (!found)
                {
                    await Task.Delay(
                        EmptyDelay,
                        stoppingToken);
                }
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Outbox polling failed.");

                await Task.Delay(
                    ErrorDelay,
                    stoppingToken);
            }
        }
    }

    private async Task<bool> ProcessBatchAsync(
        CancellationToken cancellationToken)
    {
        using var scope =
            _scopeFactory.CreateScope();

        var outbox =
            scope.ServiceProvider
                .GetRequiredService<
                    IOutboxRepository>();

        var refresh =
            scope.ServiceProvider
                .GetRequiredService<
                    IAnalyticsProjectionRefreshService>();

        var resultNotifier =
            scope.ServiceProvider
                .GetRequiredService<
                    IDashboardRealtimeNotifier>();

        var importNotifier =
            scope.ServiceProvider
                .GetRequiredService<
                    IImportDashboardRealtimeNotifier>();

        var now =
            DateTime.UtcNow;

        var messages =
            await outbox.GetPendingAsync(
                now,
                20,
                cancellationToken);

        if (messages.Count == 0)
            return false;

        foreach (var message in messages)
        {
            var claimed =
                await outbox.TryClaimAsync(
                    message.Id,
                    message.RowVersion,
                    DateTime.UtcNow,
                    DateTime.UtcNow.Add(
                        LeaseDuration),
                    cancellationToken);

            if (!claimed)
                continue;

            try
            {
                await ProcessMessageAsync(
                    message,
                    refresh,
                    resultNotifier,
                    importNotifier,
                    cancellationToken);

                if (!await outbox.MarkProcessedAsync(
                        message.Id,
                        DateTime.UtcNow,
                        cancellationToken))
                {
                    throw new InvalidOperationException(
                        "Claimed outbox message disappeared.");
                }
            }
            catch (Exception ex)
            {
                var attempt =
                    message.ProcessingAttempts + 1;

                var delaySeconds =
                    Math.Min(
                        60,
                        Math.Pow(
                            2,
                            Math.Min(
                                attempt,
                                6)));

                try
                {
                    await outbox.MarkFailedAsync(
                        message.Id,
                        ex.Message,
                        DateTime.UtcNow
                            .AddSeconds(
                                delaySeconds),
                        cancellationToken);
                }
                catch (Exception markEx)
                {
                    _logger.LogError(
                        markEx,
                        "Failed to record outbox failure for {OutboxId}.",
                        message.Id);
                }

                _logger.LogWarning(
                    ex,
                    "Outbox {OutboxId} will retry.",
                    message.Id);
            }
        }

        return true;
    }

    private static async Task ProcessMessageAsync(
        OutboxMessage message,
        IAnalyticsProjectionRefreshService refresh,
        IDashboardRealtimeNotifier resultNotifier,
        IImportDashboardRealtimeNotifier importNotifier,
        CancellationToken cancellationToken)
    {
        if (message.EventType ==
                RealtimeEventTypes
                    .AssessmentResultEntered ||
            message.EventType ==
                RealtimeEventTypes
                    .AssessmentResultUpdated)
        {
            var change =
                JsonSerializer.Deserialize<
                    AssessmentResultChangedEvent>(
                        message.PayloadJson);

            if (change is null ||
                !message.SchoolId.HasValue ||
                message.SchoolId.Value !=
                    change.SchoolId)
            {
                throw new InvalidOperationException(
                    "Invalid assessment-result outbox message.");
            }

            var refreshed =
                await refresh.RefreshSchoolAsync(
                    change.SchoolId,
                    cancellationToken);

            if (!refreshed.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Analytics refresh failed: {refreshed.Error}");
            }

            await resultNotifier
                .NotifyAssessmentResultChangedAsync(
                    change,
                    cancellationToken);

            return;
        }

        if (message.EventType ==
            RealtimeEventTypes.ImportBatchCompleted)
        {
            var completed =
                JsonSerializer.Deserialize<
                    ImportBatchCompletedEvent>(
                        message.PayloadJson);

            if (completed is null ||
                !message.SchoolId.HasValue ||
                message.SchoolId.Value !=
                    completed.SchoolId)
            {
                throw new InvalidOperationException(
                    "Invalid import outbox message.");
            }

            var refreshed =
                await refresh.RefreshSchoolAsync(
                    completed.SchoolId,
                    cancellationToken);

            if (!refreshed.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Analytics refresh failed: {refreshed.Error}");
            }

            await importNotifier.NotifyAsync(
                completed,
                cancellationToken);

            return;
        }

        throw new InvalidOperationException(
            $"Unsupported event type: {message.EventType}");
    }
}
