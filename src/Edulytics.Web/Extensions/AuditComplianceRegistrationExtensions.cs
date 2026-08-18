using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Auditing;
using Edulytics.Web.Auditing;

namespace Edulytics.Web.Extensions;

public static class AuditComplianceRegistrationExtensions
{
    public static IServiceCollection
        AddAuditCompliancePhase18(
            this IServiceCollection services)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<
            IAuditRepository,
            AuditRepository>();

        services.AddScoped<
            IAuditRequestMetadataProvider,
            HttpAuditRequestMetadataProvider>();

        services.AddScoped<
            IAuditService,
            AuditService>();

        return services;
    }
}
