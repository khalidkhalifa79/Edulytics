using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Onboarding;

using Edulytics.Services.Auditing;
namespace Edulytics.Web.Extensions;

public static class CustomerOnboardingRegistrationExtensions
{
    public static IServiceCollection AddCustomerOnboardingPhase25B(
        this IServiceCollection services)
    {
        services.AddScoped<ICustomerOnboardingRepository, CustomerOnboardingRepository>();
        services.AddScoped<ICustomerOnboardingService>(
            provider =>
                new CustomerOnboardingService(
                    provider.GetRequiredService<
                        ICustomerOnboardingRepository>(),
                    provider.GetRequiredService<
                        IAuditService>()));
        return services;
    }
}
