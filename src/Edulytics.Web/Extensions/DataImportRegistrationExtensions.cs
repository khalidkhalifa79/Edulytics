using Edulytics.Core.Constants;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Imports;
using Edulytics.Web.Realtime;

namespace Edulytics.Web.Extensions;

public static class DataImportRegistrationExtensions
{
    public static IServiceCollection AddDataImportPhase11(
        this IServiceCollection services)
    {
        services.AddScoped<
            IImportRepository,
            ImportRepository>();

        services.AddScoped<
            IDataImportService,
            DataImportService>();

        services.AddSingleton<
            ImportFileParser>();

        services.AddSingleton<
            ImportValidationEngine>();

        services.AddSingleton<
            ImportPlanBuilder>();

        services.AddSingleton<
            IImportDashboardRealtimeNotifier,
            ImportDashboardRealtimeNotifier>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                "DataImport",
                policy =>
                    policy.RequireRole(
                        RoleNames.SchoolAdmin,
                        RoleNames.Teacher));
        });

        return services;
    }
}
