using Edulytics.Core.Interfaces;
using Microsoft.Extensions.Options;

namespace Edulytics.Web.Privacy;

public sealed class
    SensitiveDataRetentionBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory
        _scopeFactory;

    private readonly DataRetentionOptions
        _options;

    private readonly ILogger<
        SensitiveDataRetentionBackgroundService>
        _logger;

    public SensitiveDataRetentionBackgroundService(
        IServiceScopeFactory scopeFactory,
        IOptions<DataRetentionOptions> options,
        ILogger<
            SensitiveDataRetentionBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            return;
        }

        // Keep application startup/readiness independent
        // from retention housekeeping.
        await Task.Delay(
            TimeSpan.FromSeconds(30),
            stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunOnceAsync(
                    stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken
                    .IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                // Housekeeping failure is observable but
                // must not crash the web process.
                _logger.LogError(
                    exception,
                    "Sensitive-data retention sweep failed.");
            }

            await Task.Delay(
                TimeSpan.FromMinutes(
                    _options
                        .SweepIntervalMinutes),
                stoppingToken);
        }
    }

    private async Task RunOnceAsync(
        CancellationToken cancellationToken)
    {
        await using var scope =
            _scopeFactory.CreateAsyncScope();

        var retention =
            scope.ServiceProvider
                .GetRequiredService<
                    ISensitiveDataRetentionRepository>();

        var result =
            await retention.ApplyAsync(
                DateTime.UtcNow,
                TimeSpan.FromHours(
                    _options
                        .ImportPayloadRetentionHours),
                TimeSpan.FromDays(
                    _options
                        .NotificationReadRetentionDays),
                cancellationToken);

        // Counts only. No user IDs, email addresses,
        // filenames, payloads or tokens are logged.
        _logger.LogInformation(
            "Sensitive-data retention sweep completed. "
            + "ImportsScrubbed={ImportsScrubbed} "
            + "ExportsPurged={ExportsPurged} "
            + "DeliveriesDeleted={DeliveriesDeleted} "
            + "NotificationsDeleted={NotificationsDeleted}",
            result.ImportPayloadsScrubbed,
            result.ExportArtifactsPurged,
            result.NotificationDeliveriesDeleted,
            result.NotificationsDeleted);
    }
}
