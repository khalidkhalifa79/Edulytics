using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Billing;

namespace Edulytics.Web.Extensions;

public static class BillingRegistrationExtensions
{
    public static IServiceCollection AddBillingPhase25D(this IServiceCollection services)
    {
        services.AddScoped<IBillingRepository, BillingRepository>();
        services.AddScoped<IBillingService, BillingService>();
        return services;
    }
}
