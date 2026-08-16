using Edulytics.Data.Contexts;
using Edulytics.Web.Production;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace Edulytics.Web.Health;

public sealed class DatabaseReadinessHealthCheck
    : IHealthCheck
{
    private readonly IServiceScopeFactory _scopeFactory;

    private readonly IOptions<
        ProductionOptions> _options;

    private readonly ILogger<
        DatabaseReadinessHealthCheck> _logger;

    public DatabaseReadinessHealthCheck(
        IServiceScopeFactory scopeFactory,
        IOptions<ProductionOptions> options,
        ILogger<
            DatabaseReadinessHealthCheck> logger)
    {
        _scopeFactory =
            scopeFactory ??
            throw new ArgumentNullException(
                nameof(scopeFactory));

        _options =
            options ??
            throw new ArgumentNullException(
                nameof(options));

        _logger =
            logger ??
            throw new ArgumentNullException(
                nameof(logger));
    }

    public async Task<HealthCheckResult>
        CheckHealthAsync(
            HealthCheckContext context,
            CancellationToken cancellationToken =
                default)
    {
        using var timeout =
            CancellationTokenSource
                .CreateLinkedTokenSource(
                    cancellationToken);

        timeout.CancelAfter(
            TimeSpan.FromSeconds(
                _options.Value
                    .DatabaseTimeoutSeconds));

        try
        {
            using var scope =
                _scopeFactory
                    .CreateScope();

            var db =
                scope.ServiceProvider
                    .GetRequiredService<
                        EdulyticsDbContext>();

            var canConnect =
                await db.Database
                    .CanConnectAsync(
                        timeout.Token);

            if (!canConnect)
            {
                return HealthCheckResult
                    .Unhealthy(
                        "Database connection failed.");
            }

            var pending =
                (await db.Database
                    .GetPendingMigrationsAsync(
                        timeout.Token))
                    .ToArray();

            if (pending.Length > 0)
            {
                return HealthCheckResult
                    .Unhealthy(
                        "Database has pending migrations.",
                        data:
                            new Dictionary<
                                string,
                                object>
                            {
                                [
                                    "PendingMigrationCount"
                                ] =
                                    pending.Length
                            });
            }

            return HealthCheckResult
                .Healthy(
                    "Database reachable and migrations current.");
        }
        catch (OperationCanceledException)
            when (!cancellationToken
                .IsCancellationRequested)
        {
            return HealthCheckResult
                .Unhealthy(
                    "Database readiness timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Database readiness failed with {ExceptionType}.",
                ex.GetType().Name);

            return HealthCheckResult
                .Unhealthy(
                    "Database readiness failed.");
        }
    }
}
