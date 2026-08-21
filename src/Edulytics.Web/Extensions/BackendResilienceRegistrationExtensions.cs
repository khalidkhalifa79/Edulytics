using System.Security.Claims;
using System.Threading.RateLimiting;
using Edulytics.Core.Resilience;
using Edulytics.Data.Repositories;
using Edulytics.Services.Resilience;
using Edulytics.Web.Resilience;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace Edulytics.Web.Extensions;

public static class BackendResilienceRegistrationExtensions
{
    public static WebApplicationBuilder AddBackendResiliencePhase14(
        this WebApplicationBuilder builder)
    {
        builder.Services
            .AddOptions<BackendResilienceOptions>()
            .Bind(
                builder.Configuration.GetSection(
                    BackendResilienceOptions.SectionName))
            .Validate(
                x =>
                    x.InteractiveReadTimeoutSeconds > 0 &&
                    x.InteractiveWriteTimeoutSeconds > 0 &&
                    x.ImportTimeoutSeconds > 0 &&
                    x.AnalyticsTimeoutSeconds > 0 &&
                    x.ReportTimeoutSeconds > 0 &&
                    x.OperationalTimeoutSeconds > 0 &&
                    x.DatabaseCommandTimeoutSeconds > 0 &&
                    x.NpgsqlMaxPoolSize > 0 &&
                    x.HeavyWritePermitLimit > 0 &&
                    x.HeavyWriteQueueLimit >= 0 &&
                    x.ImportPermitLimit > 0 &&
                    x.ImportQueueLimit >= 0 &&
                    x.AnalyticsPermitLimit > 0 &&
                    x.AnalyticsQueueLimit >= 0 &&
                    x.ReportPermitLimit > 0 &&
                    x.ReportQueueLimit >= 0 &&
                    x.MaxRequestBodyBytes >= 6 * 1024 * 1024 &&
                    x.RequestHeadersTimeoutSeconds > 0 &&
                    x.KeepAliveTimeoutSeconds > 0,
                "Invalid Phase 14 backend resilience configuration.")
            .ValidateOnStart();

        var settings =
            builder.Configuration
                .GetSection(
                    BackendResilienceOptions.SectionName)
                .Get<BackendResilienceOptions>()
            ?? new BackendResilienceOptions();

        builder.WebHost.ConfigureKestrel(
            options =>
            {
                options.Limits.MaxRequestBodySize =
                    settings.MaxRequestBodyBytes;

                options.Limits.RequestHeadersTimeout =
                    TimeSpan.FromSeconds(
                        settings.RequestHeadersTimeoutSeconds);

                options.Limits.KeepAliveTimeout =
                    TimeSpan.FromSeconds(
                        settings.KeepAliveTimeoutSeconds);
            });

        builder.Services.AddRequestTimeouts(
            options =>
            {
                options.AddPolicy(
                    BackendResiliencePolicyNames.InteractiveRead,
                    TimeSpan.FromSeconds(
                        settings.InteractiveReadTimeoutSeconds));

                options.AddPolicy(
                    BackendResiliencePolicyNames.InteractiveWrite,
                    TimeSpan.FromSeconds(
                        settings.InteractiveWriteTimeoutSeconds));

                options.AddPolicy(
                    BackendResiliencePolicyNames.Import,
                    TimeSpan.FromSeconds(
                        settings.ImportTimeoutSeconds));

                options.AddPolicy(
                    BackendResiliencePolicyNames.Analytics,
                    TimeSpan.FromSeconds(
                        settings.AnalyticsTimeoutSeconds));

                options.AddPolicy(
                    BackendResiliencePolicyNames.Report,
                    TimeSpan.FromSeconds(
                        settings.ReportTimeoutSeconds));

                options.AddPolicy(
                    BackendResiliencePolicyNames.Operational,
                    TimeSpan.FromSeconds(
                        settings.OperationalTimeoutSeconds));
            });

        builder.Services.AddRateLimiter(
            options =>
            {
                options.RejectionStatusCode =
                    StatusCodes.Status429TooManyRequests;

                options.OnRejected =
                    async (context, cancellationToken) =>
                    {
                        if (context.Lease.TryGetMetadata(
                                MetadataName.RetryAfter,
                                out var retryAfter))
                        {
                            context.HttpContext
                                .Response.Headers.RetryAfter =
                                Math.Max(
                                    1,
                                    (int)Math.Ceiling(
                                        retryAfter.TotalSeconds))
                                    .ToString(
                                        System.Globalization
                                            .CultureInfo.InvariantCulture);
                        }
                        else
                        {
                            context.HttpContext
                                .Response.Headers.RetryAfter = "1";
                        }

                        await ValueTask.CompletedTask;
                    };

                options.AddPolicy(
                    "Login",
                    context =>
                    {
                        var ip =
                            context.Connection.RemoteIpAddress
                                ?.ToString()
                            ?? "unknown";

                        return RateLimitPartition
                            .GetFixedWindowLimiter(
                                ip,
                                _ => new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 20,
                                    Window = TimeSpan.FromMinutes(5),
                                    QueueLimit = 0,
                                    QueueProcessingOrder =
                                        QueueProcessingOrder.OldestFirst,
                                    AutoReplenishment = true
                                });
                    });

                options.AddPolicy(
                    "OperationalMutation",
                    context =>
                    {
                        var actor =
                            ActorPartition(context);

                        return RateLimitPartition
                            .GetFixedWindowLimiter(
                                actor,
                                _ => new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 30,
                                    Window = TimeSpan.FromMinutes(10),
                                    QueueLimit = 0,
                                    QueueProcessingOrder =
                                        QueueProcessingOrder.OldestFirst,
                                    AutoReplenishment = true
                                });
                    });

                options.AddPolicy(
                    "RequestDemo",
                    context =>
                    {
                        var ip =
                            context.Connection.RemoteIpAddress
                                ?.ToString()
                            ?? "unknown";

                        return RateLimitPartition
                            .GetFixedWindowLimiter(
                                ip,
                                _ => new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 5,
                                    Window = TimeSpan.FromHours(1),
                                    QueueLimit = 0,
                                    QueueProcessingOrder =
                                        QueueProcessingOrder.OldestFirst,
                                    AutoReplenishment = true
                                });
                    });

                options.AddPolicy(
                    "SchoolUserCreate",
                    context =>
                    {
                        var actor = ActorPartition(context);

                        return RateLimitPartition
                            .GetFixedWindowLimiter(
                                actor,
                                _ => new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 20,
                                    Window = TimeSpan.FromMinutes(10),
                                    QueueLimit = 0,
                                    QueueProcessingOrder =
                                        QueueProcessingOrder.OldestFirst,
                                    AutoReplenishment = true
                                });
                    });

                options.AddPolicy(
                    "InvitationResend",
                    context =>
                    {
                        var actor = ActorPartition(context);
                        var target =
                            context.Request.RouteValues["id"]
                                ?.ToString()
                            ?? "unknown";

                        return RateLimitPartition
                            .GetFixedWindowLimiter(
                                $"{actor}:{target}",
                                _ => new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 3,
                                    Window = TimeSpan.FromMinutes(10),
                                    QueueLimit = 0,
                                    QueueProcessingOrder =
                                        QueueProcessingOrder.OldestFirst,
                                    AutoReplenishment = true
                                });
                    });

                options.AddPolicy(
                    "PasswordSetup",
                    context =>
                    {
                        var ip =
                            context.Connection.RemoteIpAddress
                                ?.ToString()
                            ?? "unknown";

                        return RateLimitPartition
                            .GetFixedWindowLimiter(
                                ip,
                                _ => new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 10,
                                    Window = TimeSpan.FromMinutes(15),
                                    QueueLimit = 0,
                                    QueueProcessingOrder =
                                        QueueProcessingOrder.OldestFirst,
                                    AutoReplenishment = true
                                });
                    });

                options.AddPolicy(
                    BackendResiliencePolicyNames
                        .ReportExportRate,
                    context =>
                    {
                        var actor = ActorPartition(context);

                        return RateLimitPartition
                            .GetFixedWindowLimiter(
                                actor,
                                _ => new FixedWindowRateLimiterOptions
                                {
                                    PermitLimit = 12,
                                    Window =
                                        TimeSpan.FromMinutes(10),
                                    QueueLimit = 0,
                                    QueueProcessingOrder =
                                        QueueProcessingOrder.OldestFirst,
                                    AutoReplenishment = true
                                });
                    });

                options.AddConcurrencyLimiter(
                    BackendResiliencePolicyNames
                        .HeavyWriteConcurrency,
                    limiter =>
                    {
                        limiter.PermitLimit =
                            settings.HeavyWritePermitLimit;
                        limiter.QueueLimit =
                            settings.HeavyWriteQueueLimit;
                        limiter.QueueProcessingOrder =
                            QueueProcessingOrder.OldestFirst;
                    });

                options.AddConcurrencyLimiter(
                    BackendResiliencePolicyNames
                        .ImportConcurrency,
                    limiter =>
                    {
                        limiter.PermitLimit =
                            settings.ImportPermitLimit;
                        limiter.QueueLimit =
                            settings.ImportQueueLimit;
                        limiter.QueueProcessingOrder =
                            QueueProcessingOrder.OldestFirst;
                    });

                options.AddConcurrencyLimiter(
                    BackendResiliencePolicyNames
                        .AnalyticsConcurrency,
                    limiter =>
                    {
                        limiter.PermitLimit =
                            settings.AnalyticsPermitLimit;
                        limiter.QueueLimit =
                            settings.AnalyticsQueueLimit;
                        limiter.QueueProcessingOrder =
                            QueueProcessingOrder.OldestFirst;
                    });

                options.AddConcurrencyLimiter(
                    BackendResiliencePolicyNames
                        .ReportConcurrency,
                    limiter =>
                    {
                        limiter.PermitLimit =
                            settings.ReportPermitLimit;
                        limiter.QueueLimit =
                            settings.ReportQueueLimit;
                        limiter.QueueProcessingOrder =
                            QueueProcessingOrder.OldestFirst;
                    });

                options.AddConcurrencyLimiter(
                    BackendResiliencePolicyNames
                        .OperationalConcurrency,
                    limiter =>
                    {
                        limiter.PermitLimit = 2;
                        limiter.QueueLimit = 2;
                        limiter.QueueProcessingOrder =
                            QueueProcessingOrder.OldestFirst;
                    });
            });

        builder.Services.AddScoped<
            IIdempotencyRepository,
            IdempotencyRepository>();

        builder.Services.AddScoped<
            IIdempotencyService,
            IdempotencyService>();

        return builder;
    }

    private static string ActorPartition(
        HttpContext context) =>
        context.User.FindFirst(
                ClaimTypes.NameIdentifier)
            ?.Value
        ?? context.Connection.RemoteIpAddress
            ?.ToString()
        ?? "anonymous";
}
