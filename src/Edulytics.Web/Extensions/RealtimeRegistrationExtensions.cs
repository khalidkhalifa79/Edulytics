using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Analytics;
using Edulytics.Services.Realtime;
using Edulytics.Web.Background;
using Edulytics.Web.Realtime;
using Edulytics.Web.Scale;

namespace Edulytics.Web.Extensions;

public static class RealtimeRegistrationExtensions
{
    public static IServiceCollection
        AddRealtimeDashboardsPhase10(
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

        var scale =
            MultiInstanceScaleOptions.Read(
                configuration);

        var signalR =
            services.AddSignalR();

        if (scale.Enabled &&
            scale.RequireRedis)
        {
            var redisConnection =
                RedisConnectionConfiguration
                    .ReadRequired(
                        configuration);

            signalR.AddStackExchangeRedis(
                options =>
                {
                    options.Configuration =
                        RedisConnectionConfiguration
                            .Parse(
                                redisConnection);

                    options.Configuration
                        .ChannelPrefix =
                        StackExchange.Redis
                            .RedisChannel
                            .Literal(
                                scale
                                    .RedisChannelPrefix);
                });
        }

        services.AddOptions<
                OutboxV2Options>()
            .BindConfiguration(
                OutboxV2Options.SectionName)
            .Validate(
                options =>
                    options.PollDelayMilliseconds > 0 &&
                    options.ErrorDelayMilliseconds > 0 &&
                    options.BatchSize > 0 &&
                    options.LeaseSeconds > 0 &&
                    options.MessageProcessingTimeoutSeconds > 0 &&
                    options.MessageProcessingTimeoutSeconds <
                        options.LeaseSeconds &&
                    options.MaxAttempts > 0 &&
                    options.RetryBaseSeconds > 0 &&
                    options.RetryMaxSeconds >=
                        options.RetryBaseSeconds &&
                    options.RetryJitterMilliseconds >= 0 &&
                    options.AnalyticsPollDelayMilliseconds > 0 &&
                    options.AnalyticsDebounceMilliseconds > 0 &&
                    options.AnalyticsMaxCoalesceMilliseconds >=
                        options.AnalyticsDebounceMilliseconds &&
                    options.AnalyticsLeaseSeconds > 0 &&
                    options.AnalyticsRefreshTimeoutSeconds > 0 &&
                    options.AnalyticsRefreshTimeoutSeconds <
                        options.AnalyticsLeaseSeconds &&
                    options.ShutdownGraceSeconds >=
                        options.MessageProcessingTimeoutSeconds,
                "Invalid Outbox v2 configuration.")
            .ValidateOnStart();

        services.Configure<HostOptions>(
            options =>
            {
                // In-flight work is intentionally bounded below
                // this host shutdown grace period.
                options.ShutdownTimeout =
                    TimeSpan.FromSeconds(45);
            });

        services.AddScoped<
            IOutboxRepository,
            OutboxRepository>();

        services.AddScoped<
            IAnalyticsRefreshQueueRepository,
            AnalyticsRefreshQueueRepository>();

        services.AddScoped<
            IRealtimeAccessRepository,
            RealtimeAccessRepository>();

        services.AddScoped<
            IRealtimeGroupService,
            RealtimeGroupService>();

        services.AddScoped<
            IAnalyticsProjectionRefreshService,
            AnalyticsProjectionRefreshService>();

        services.AddSingleton<
            IDashboardRealtimeNotifier,
            DashboardRealtimeNotifier>();

        services.AddSingleton<
            IAnalyticsInvalidationNotifier,
            AnalyticsInvalidationNotifier>();

        // Phase 15 workers require PostgreSQL transactional
        // semantics (FOR UPDATE / SKIP LOCKED). The integration
        // test host deliberately uses EF InMemory for MVC/Identity
        // tests, so starting these workers there would create false
        // background failures unrelated to the request under test.
        //
        // Development and Production remain unchanged: both workers
        // are registered and exercised against PostgreSQL.
        if (!environment.IsEnvironment(
                "Testing") &&
            scale.RunsBackgroundWorkers)
        {
            services.AddHostedService<
                OutboxProcessorBackgroundService>();

            services.AddHostedService<
                AnalyticsRefreshBackgroundService>();
        }

        return services;
    }
}
