using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Auditing;
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

        services.AddScoped<ISchoolUserManagementService>(
            provider =>
                new SchoolUserManagementService(
                    provider.GetRequiredService<
                        ISchoolUserRepository>(),
                    provider.GetRequiredService<
                        ISchoolRepository>(),
                    provider.GetRequiredService<
                        IAuditService>(),
                    provider.GetRequiredService<
                        IApplicationTransactionManager>(),
                    provider.GetRequiredService<
                        ICustomerOnboardingRepository>(),
                    provider.GetRequiredService<
                        ISchoolSubscriptionRepository>()));

        return services;
    }
}
