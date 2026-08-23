using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Academics;
using Edulytics.Services.Auditing;
using Edulytics.Web.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Edulytics.Web.Extensions;

public static class AcademicStructureRegistrationExtensions
{
    public static IServiceCollection AddAcademicStructurePhase06(
        this IServiceCollection services)
    {
        services.AddScoped<
            IAcademicStructureRepository,
            AcademicStructureRepository>();

        services.AddScoped<IAcademicStructureService>(
            provider =>
                new AcademicStructureService(
                    provider.GetRequiredService<
                        IAcademicStructureRepository>(),
                    provider.GetRequiredService<
                        ISchoolRepository>(),
                    provider.GetRequiredService<
                        ISchoolUserRepository>(),
                    provider.GetRequiredService<
                        IAuditService>()));

        services.AddScoped<
            IAuthorizationHandler,
            AcademicStructureAdministrationHandler>();

        return services;
    }
}
