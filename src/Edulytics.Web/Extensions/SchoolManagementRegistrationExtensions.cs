using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Schools;

namespace Microsoft.Extensions.DependencyInjection;

public static class SchoolManagementRegistrationExtensions
{
    public static IServiceCollection AddSchoolManagementPhase04(
        this IServiceCollection services)
    {
        services.AddScoped<ISchoolRepository, SchoolRepository>();
        services.AddScoped<ISchoolManagementService, SchoolManagementService>();

        return services;
    }
}
