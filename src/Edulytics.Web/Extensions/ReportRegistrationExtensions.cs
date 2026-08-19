using Edulytics.Core.Constants;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Services.Reports;
using Edulytics.Web.Reports;

namespace Edulytics.Web.Extensions;

public static class ReportRegistrationExtensions
{
    public static IServiceCollection AddReportsPhase20(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options =
            configuration
                .GetSection(
                    ReportOptions.SectionName)
                .Get<ReportOptions>()
            ?? new ReportOptions();

        if (options.MaxHtmlRows <= 0 ||
            options.MaxExportRows <
                options.MaxHtmlRows ||
            options.MaxExportBytes <
                1024 * 1024 ||
            options.ExportRetentionHours <= 0 ||
            options.RecentJobsLimit <= 0)
        {
            throw new InvalidOperationException(
                "Invalid Phase20 report configuration.");
        }

        services.AddSingleton(options);

        services.AddScoped<
            IReportExportRepository,
            ReportExportRepository>();

        services.AddScoped<
            IReportQueryService,
            ReportQueryService>();

        services.AddScoped<
            IReportExportService,
            ReportExportService>();

        services.AddScoped<
            IReportExportProcessor,
            ReportExportProcessor>();

        services.AddAuthorization(
            authorization =>
            {
                authorization.AddPolicy(
                    "ReportRead",
                    policy =>
                        policy.RequireRole(
                            RoleNames.SchoolAdmin,
                            RoleNames.SubjectSupervisor,
                            RoleNames.Teacher));
            });

        return services;
    }
}
