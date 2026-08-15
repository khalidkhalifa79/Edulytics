using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Analytics;
using Edulytics.Services.Realtime;
using Edulytics.Web.Background;
using Edulytics.Web.Realtime;

namespace Edulytics.Web.Extensions;

public static class RealtimeRegistrationExtensions
{
    public static IServiceCollection AddRealtimeDashboardsPhase10(
        this IServiceCollection services)
    {
        services.AddSignalR();

        services.AddScoped<
            IOutboxRepository,
            OutboxRepository>();

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

        services.AddHostedService<
            OutboxProcessorBackgroundService>();

        return services;
    }
}
