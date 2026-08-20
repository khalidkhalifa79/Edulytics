using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Web.Operations;

namespace Edulytics.Web.Extensions;

public static class OperationalAdminRegistrationExtensions
{
    public static IServiceCollection
        AddOperationalAdminPhase22(
            this IServiceCollection services)
    {
        services.AddScoped<
            IOperationsRepository,
            OperationsRepository>();

        services.AddScoped<
            OperationalConsoleService>();

        return services;
    }
}
