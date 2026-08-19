using Edulytics.Core.Constants;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Analytics;

namespace Edulytics.Web.Extensions;

public static class AnalyticsRegistrationExtensions
{
    public static IServiceCollection AddAnalyticsPhase09(
        this IServiceCollection services)
    {
        services.AddScoped<
            IAnalyticsRepository,
            AnalyticsRepository>();

        services.AddScoped<
            IAnalyticsService,
            AnalyticsService>();

        services.AddSingleton<
            AnalyticsProjectionBuilder>();

        services.AddAuthorization(
            options =>
            {
                options.AddPolicy(
                    "AnalyticsRead",
                    policy =>
                        policy.RequireRole(
                            RoleNames.SchoolAdmin,
                            RoleNames.SubjectSupervisor,
                            RoleNames.Teacher));
            });

        return services;
    }
}
