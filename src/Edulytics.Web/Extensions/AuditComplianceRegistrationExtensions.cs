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

        services.AddScoped<
            Edulytics.Core.Interfaces.IAuditQueryRepository,
            Edulytics.Data.Repositories.AuditQueryRepository>();

        services.AddScoped<
            Edulytics.Services.Auditing.IAuditQueryService,
            Edulytics.Services.Auditing.AuditQueryService>();

        services.AddScoped<
            Edulytics.Core.Interfaces.IApplicationTransactionManager,
            Edulytics.Data.Transactions.EfApplicationTransactionManager>();

        return services;
    }
}
