using Edulytics.Core.Constants;
using Edulytics.Data.Contexts;
using Edulytics.Data.Identity;
using Edulytics.Web.Bootstrap;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEdulyticsIdentityAndData(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Connection string 'DefaultConnection' is missing.");
        }

        services.AddDbContext<EdulyticsDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<EdulyticsDbContext>();

        services.AddScoped<EdulyticsDatabaseBootstrapper>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("PlatformAdministration", policy =>
            {
                policy.RequireRole(RoleNames.SuperAdmin);
            });
        });

        return services;
    }
}
