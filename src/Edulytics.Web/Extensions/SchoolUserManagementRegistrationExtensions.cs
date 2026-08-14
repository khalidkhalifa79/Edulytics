using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Users;

namespace Edulytics.Web.Extensions;

public static class SchoolUserManagementRegistrationExtensions
{
    public static IServiceCollection
        AddSchoolUserManagementPhase05(
            this IServiceCollection services)
    {
        services.AddScoped<
            ISchoolUserRepository,
            IdentitySchoolUserRepository>();

        services.AddScoped<
            ISchoolUserManagementService,
            SchoolUserManagementService>();

        return services;
    }
}
