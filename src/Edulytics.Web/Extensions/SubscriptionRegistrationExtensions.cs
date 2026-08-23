using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Subscriptions;

namespace Edulytics.Web.Extensions;

public static class SubscriptionRegistrationExtensions
{
    public static IServiceCollection AddSubscriptionsPhase25C(
        this IServiceCollection services)
    {
        services.AddScoped<
            ISchoolSubscriptionRepository,
            SchoolSubscriptionRepository>();

        services.AddScoped<
            ISchoolSubscriptionService,
            SchoolSubscriptionService>();

        return services;
    }
}
