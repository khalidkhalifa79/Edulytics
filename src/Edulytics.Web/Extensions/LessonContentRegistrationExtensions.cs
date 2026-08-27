using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.LessonContent;

namespace Edulytics.Web.Extensions;

public static class LessonContentRegistrationExtensions
{
    public static IServiceCollection AddLessonContentPhase29(
        this IServiceCollection services)
    {
        services.AddScoped<ILessonContentRepository, LessonContentRepository>();
        services.AddScoped<ILessonContentService, LessonContentService>();
        return services;
    }
}
