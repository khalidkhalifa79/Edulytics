using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Curriculum;

namespace Edulytics.Web.Extensions;

public static class CurriculumRegistrationExtensions
{
    public static IServiceCollection AddCurriculumPhase07(
        this IServiceCollection services)
    {
        services.AddScoped<ICurriculumRepository, CurriculumRepository>();
        services.AddScoped<ICurriculumService, CurriculumService>();
        return services;
    }
}
