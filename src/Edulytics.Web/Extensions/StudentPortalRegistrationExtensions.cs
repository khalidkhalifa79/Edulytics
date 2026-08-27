using Edulytics.Core.Constants;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.StudentPortal;
using Edulytics.Services.StudentSetup;

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

        services.AddScoped<
            IStudentRoleProvisioningOperations,
            StudentRoleProvisioningOperations>();

        services.AddScoped<
            IStudentRoleProvisioningService,
            StudentRoleProvisioningService>();

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
