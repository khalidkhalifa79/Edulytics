using Edulytics.Core.Constants;
using Edulytics.Data.Contexts;
using Edulytics.Data.Identity;
using Edulytics.Web.Authorization;
using Edulytics.Web.Bootstrap;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Web.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEdulyticsIdentityAndData(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<EdulyticsDbContext>(
            (serviceProvider, options) =>
            {
                var runtimeConfiguration =
                    serviceProvider.GetRequiredService<IConfiguration>();

                var connectionString =
                    runtimeConfiguration.GetConnectionString(
                        "DefaultConnection");

                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        "Connection string 'DefaultConnection' is missing.");
                }

                options.UseSqlServer(connectionString);
            });

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan =
                    TimeSpan.FromMinutes(15);
            })
            .AddEntityFrameworkStores<EdulyticsDbContext>()
            .AddDefaultTokenProviders();

        services.ConfigureApplicationCookie(options =>
        {
            options.LoginPath = "/account/login";
            options.AccessDeniedPath = "/access-denied";

            options.SlidingExpiration = true;
            options.ExpireTimeSpan = TimeSpan.FromHours(8);

            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Strict;
        });

        services.AddScoped<EdulyticsDatabaseBootstrapper>();

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy =
                new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();

            options.AddPolicy(
                "PlatformAdministration",
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new PlatformAdministrationRequirement());
                });
        });

        services.AddScoped<
            IAuthorizationHandler,
            PlatformAdministrationHandler>();

        return services;
    }
}
