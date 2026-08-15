using Edulytics.Core.Constants;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Assessments;

namespace Edulytics.Web.Extensions;

public static class AssessmentRegistrationExtensions
{
    public static IServiceCollection AddAssessmentsPhase08(
        this IServiceCollection services)
    {
        services.AddScoped<IAssessmentRepository, AssessmentRepository>();
        services.AddScoped<IAssessmentService, AssessmentService>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(
                "AssessmentManagement",
                policy => policy.RequireRole(
                    RoleNames.SchoolAdmin,
                    RoleNames.Teacher));
        });

        return services;
    }
}
