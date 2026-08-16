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

                options.UseNpgsql(connectionString);
            });

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;

                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 4;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;

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

            options.AddPolicy(
                "UserManagement",
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new UserManagementRequirement());
                });

            options.AddPolicy(
                "SchoolAccess",
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new SchoolAccessRequirement());
                });

            options.AddPolicy(
                "AcademicStructureAdministration",
                policy =>
                {
                    policy.RequireAuthenticatedUser();

                    policy.AddRequirements(
                        new AcademicStructureAdministrationRequirement());
                });
        });

        services.AddScoped<
            IAuthorizationHandler,
            PlatformAdministrationHandler>();

        services.AddScoped<
            IAuthorizationHandler,
            UserManagementHandler>();

        services.AddScoped<
            IAuthorizationHandler,
            SchoolAccessHandler>();

        return services;
    }
}
