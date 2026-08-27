using Edulytics.Core.Constants;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.StudentPortal;

namespace Edulytics.Web.Extensions;

public static class StudentPortalRegistrationExtensions
{
    public static IServiceCollection AddStudentPortalPhase28(
        this IServiceCollection services)
    {
        services.AddScoped<
            IStudentPortalRepository,
            StudentPortalRepository>();

        services.AddScoped<
            IStudentPortalService,
            StudentPortalService>();

        services.AddAuthorization(
            options =>
            {
                options.AddPolicy(
                    "StudentPortal",
                    policy =>
                        policy.RequireRole(
                            RoleNames.Student));
            });

        return services;
    }
}
