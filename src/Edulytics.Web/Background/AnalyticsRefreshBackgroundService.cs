using Edulytics.Core.Interfaces;
using Edulytics.Core.Reliability;
using Edulytics.Services.Analytics;
using Edulytics.Services.Realtime;
using Microsoft.Extensions.Options;

namespace Edulytics.Web.Background;

public sealed class AnalyticsRefreshBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<
        AnalyticsRefreshBackgroundService> _logger;
    private readonly OutboxV2Options _options;
    private readonly string _workerId;

    public AnalyticsRefreshBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<
            AnalyticsRefreshBackgroundService> logger,
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

        _options =
            options?.Value
            ?? throw new ArgumentNullException(
                nameof(options));

        _workerId =
            $"{Environment.MachineName}:analytics:"
            + $"{Guid.NewGuid():N}";
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "Analytics coalescing worker "
            + "{WorkerId} started.",
            _workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var found =
                    await ProcessOneAsync();

                if (!found)
                {
                    await Task.Delay(
                        TimeSpan.FromMilliseconds(
                            _options
                                .AnalyticsPollDelayMilliseconds),
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
                    "Analytics coalescing loop "
                    + "failed.");

                await Task.Delay(
                    TimeSpan.FromMilliseconds(
                        _options
                            .ErrorDelayMilliseconds),
                    stoppingToken);
            }
        }

        _logger.LogInformation(
            "Analytics coalescing worker "
            + "{WorkerId} stopped.",
            _workerId);
    }

    private async Task<bool> ProcessOneAsync()
    {
        AnalyticsRefreshLease? lease;

        using (var claimScope =
               _scopeFactory.CreateScope())
        {
            var queue =
                claimScope.ServiceProvider
                    .GetRequiredService<
                        IAnalyticsRefreshQueueRepository>();

            lease =
                await queue.ClaimNextAsync(
                    _workerId,
                    DateTime.UtcNow,
                    TimeSpan.FromSeconds(
                        _options
                            .AnalyticsLeaseSeconds),
                    CancellationToken.None);
        }

        if (lease is null)
            return false;

        using var timeout =
            new CancellationTokenSource(
                TimeSpan.FromSeconds(
                    _options
                        .AnalyticsRefreshTimeoutSeconds));

        try
        {
            using var workScope =
                _scopeFactory.CreateScope();

            var refresh =
                workScope.ServiceProvider
                    .GetRequiredService<
                        IAnalyticsProjectionRefreshService>();

            var notifier =
                workScope.ServiceProvider
                    .GetRequiredService<
                        IAnalyticsInvalidationNotifier>();

            var result =
                await refresh.RefreshSchoolAsync(
                    lease.SchoolId,
                    timeout.Token);

            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    "Analytics refresh failed: "
                    + result.Error);
            }

            // Publish before completing the lease. If publication
            // succeeds and the process dies before completion, the
            // later retry can publish a duplicate invalidation; the
            // browser reconciliation contract makes that safe.
            await notifier
                .NotifySchoolAnalyticsChangedAsync(
                    lease.SchoolId,
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    timeout.Token);

            using var completeScope =
                _scopeFactory.CreateScope();

            var queue =
                completeScope.ServiceProvider
                    .GetRequiredService<
                        IAnalyticsRefreshQueueRepository>();

            var completed =
                await queue.CompleteAsync(
                    lease,
                    DateTime.UtcNow,
                    TimeSpan.FromMilliseconds(
                        _options
                            .AnalyticsDebounceMilliseconds),
                    TimeSpan.FromMilliseconds(
                        _options
                            .AnalyticsMaxCoalesceMilliseconds),
                    timeout.Token);

            if (!completed)
            {
                _logger.LogWarning(
                    "Analytics refresh for school "
                    + "{SchoolId} lost its lease; "
                    + "stale completion was rejected.",
                    lease.SchoolId);
            }
        }
        catch (Exception ex)
        {
            await RecordFailureAsync(
                lease,
                ex);
        }

        return true;
    }

    private async Task RecordFailureAsync(
        AnalyticsRefreshLease lease,
        Exception exception)
    {
        var delay =
            OutboxRetryPolicy
                .ComputeDelay(
                    Math.Max(
                        1,
                        lease.ProcessingAttempts),
                    _options.RetryBaseSeconds,
                    _options.RetryMaxSeconds,
                    _options
                        .RetryJitterMilliseconds);

        try
        {
            using var failureScope =
                _scopeFactory.CreateScope();

            var queue =
                failureScope.ServiceProvider
                    .GetRequiredService<
                        IAnalyticsRefreshQueueRepository>();

            using var markTimeout =
                new CancellationTokenSource(
                    TimeSpan.FromSeconds(5));

            var recorded =
                await queue.MarkFailedAsync(
                    lease,
                    exception.Message,
                    DateTime.UtcNow.Add(delay),
                    markTimeout.Token);

            if (recorded)
            {
                _logger.LogWarning(
                    exception,
                    "Analytics refresh for school "
                    + "{SchoolId} will retry after "
                    + "{Delay}.",
                    lease.SchoolId,
                    delay);
            }
            else
            {
                _logger.LogWarning(
                    "Analytics failure for school "
                    + "{SchoolId} could not be "
                    + "recorded because the lease "
                    + "is stale.",
                    lease.SchoolId);
            }
        }
        catch (Exception markException)
        {
            _logger.LogError(
                markException,
                "Failed to record analytics "
                + "refresh failure for school "
                + "{SchoolId}.",
                lease.SchoolId);
        }
    }
}
