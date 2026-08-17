using System.Security.Cryptography.X509Certificates;
using Edulytics.Core.Constants;
using Edulytics.Data.Contexts;
using Edulytics.Data.Identity;
using Edulytics.Web.Authorization;
using Edulytics.Web.Bootstrap;
using Microsoft.AspNetCore.DataProtection;
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

                var commandTimeoutSeconds =
                    runtimeConfiguration.GetValue<int?>(
                        "Edulytics:Resilience:DatabaseCommandTimeoutSeconds")
                    ?? 15;

                var maxPoolSize =
                    runtimeConfiguration.GetValue<int?>(
                        "Edulytics:Resilience:NpgsqlMaxPoolSize")
                    ?? 40;

                var connectionBuilder =
                    new Npgsql.NpgsqlConnectionStringBuilder(
                        connectionString);

                if (!connectionBuilder.ContainsKey(
                        "Maximum Pool Size"))
                {
                    connectionBuilder.MaxPoolSize =
                        maxPoolSize;
                }

                options.UseNpgsql(
                    connectionBuilder.ConnectionString,
                    npgsql =>
                        npgsql.CommandTimeout(
                            commandTimeoutSeconds));
            });

        var dataProtectionApplicationName =
            configuration[
                "Edulytics:Hosting:DataProtectionApplicationName"]
            ?? "Edulytics";

        var dataProtectionBuilder =
            services
                .AddDataProtection()
                .SetApplicationName(
                    dataProtectionApplicationName)
                .PersistKeysToDbContext<
                    EdulyticsDbContext>();

        var requireDataProtectionCertificate =
            configuration.GetValue<bool>(
                "Edulytics:Hosting:RequireDataProtectionCertificate");

        var dataProtectionCertificateBase64 =
            configuration[
                "Edulytics:Hosting:DataProtectionCertificateBase64"];

        var dataProtectionCertificatePassword =
            configuration[
                "Edulytics:Hosting:DataProtectionCertificatePassword"];

        if (string.IsNullOrWhiteSpace(
                dataProtectionCertificateBase64))
        {
            if (requireDataProtectionCertificate)
            {
                throw new InvalidOperationException(
                    "A Data Protection certificate is required "
                    + "for this environment.");
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(
                    dataProtectionCertificatePassword))
            {
                throw new InvalidOperationException(
                    "The Data Protection certificate password "
                    + "is required when a certificate is configured.");
            }

            byte[] certificateBytes;

            try
            {
                certificateBytes =
                    Convert.FromBase64String(
                        dataProtectionCertificateBase64);
            }
            catch (FormatException exception)
            {
                throw new InvalidOperationException(
                    "The configured Data Protection certificate "
                    + "is not valid Base64.",
                    exception);
            }

            var certificate =
                X509CertificateLoader.LoadPkcs12(
                    certificateBytes,
                    dataProtectionCertificatePassword,
                    X509KeyStorageFlags.EphemeralKeySet);

            if (!certificate.HasPrivateKey)
            {
                throw new InvalidOperationException(
                    "The configured Data Protection certificate "
                    + "must contain a private key.");
            }

            dataProtectionBuilder
                .ProtectKeysWithCertificate(
                    certificate);
        }

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
