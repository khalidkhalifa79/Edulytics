using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Analytics;
using Edulytics.Services.Realtime;
using Edulytics.Web.Background;
using Edulytics.Web.Realtime;

namespace Edulytics.Web.Extensions;

public static class RealtimeRegistrationExtensions
{
    public static IServiceCollection
        AddRealtimeDashboardsPhase10(
            this IServiceCollection services)
    {
        services.AddSignalR();

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

        services.AddHostedService<
            OutboxProcessorBackgroundService>();

        services.AddHostedService<
            AnalyticsRefreshBackgroundService>();

        return services;
    }
}
