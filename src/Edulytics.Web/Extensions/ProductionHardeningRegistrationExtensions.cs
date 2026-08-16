using Edulytics.Web.Health;
using Edulytics.Web.Production;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Edulytics.Web.Extensions;

public static class
    ProductionHardeningRegistrationExtensions
{
    public static IServiceCollection
        AddProductionHardeningPhase12(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        ArgumentNullException.ThrowIfNull(
            configuration);

        ArgumentNullException.ThrowIfNull(
            environment);

        var skipConnectionValidation =
            environment.IsEnvironment(
                "Testing");

        services
            .AddOptions<
                ProductionOptions>()
            .Bind(
                configuration.GetSection(
                    ProductionOptions
                        .SectionName))
            .Validate(
                options =>
                    options.WorkerStaleAfterSeconds
                        is >= 10 and <= 600,
                "WorkerStaleAfterSeconds must be between 10 and 600.")
            .Validate(
                options =>
                    options.DatabaseTimeoutSeconds
                        is >= 1 and <= 60,
                "DatabaseTimeoutSeconds must be between 1 and 60.")
            .Validate(
                _ =>
                    skipConnectionValidation ||
                    !string.IsNullOrWhiteSpace(
                        configuration
                            .GetConnectionString(
                                "DefaultConnection")),
                "Connection string 'DefaultConnection' is required.")
            .ValidateOnStart();

        services.AddSingleton<
            OutboxWorkerHealthState>();

        services
            .AddHealthChecks()
            .AddCheck(
                "self",
                () =>
                    HealthCheckResult
                        .Healthy(
                            "Application process is alive."),
                tags:
                    ["live"])
            .AddCheck<
                DatabaseReadinessHealthCheck>(
                    "database",
                    failureStatus:
                        HealthStatus
                            .Unhealthy,
                    tags:
                        ["ready"])
            .AddCheck<
                OutboxWorkerReadinessHealthCheck>(
                    "outbox-worker",
                    failureStatus:
                        HealthStatus
                            .Unhealthy,
                    tags:
                        ["ready"]);

        if (!environment.IsDevelopment() &&
            !environment.IsEnvironment(
                "Testing"))
        {
            services.ConfigureApplicationCookie(
                options =>
                {
                    options.Cookie
                        .SecurePolicy =
                        CookieSecurePolicy.Always;
                });

            services.AddAntiforgery(
                options =>
                {
                    options.Cookie
                        .SecurePolicy =
                        CookieSecurePolicy.Always;
                });
        }

        return services;
    }
}
