using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.SubjectSupervisors;

namespace Edulytics.Web.Extensions;

public static class SubjectSupervisorRegistrationExtensions
{
    public static IServiceCollection
        AddSubjectSupervisorCompletionPhase19(
            this IServiceCollection services)
    {
        services.AddScoped<
            ISubjectSupervisorAssignmentRepository,
            SubjectSupervisorAssignmentRepository>();

        services.AddScoped<
            ISubjectSupervisorAssignmentService,
            SubjectSupervisorAssignmentService>();

        return services;
    }
}
