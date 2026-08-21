using Edulytics.Core.Interfaces;
using Edulytics.Data.Repositories;
using Edulytics.Web.Privacy;
using Edulytics.Web.Scale;

namespace Edulytics.Web.Extensions;

public static class
    SecurityPrivacyRegistrationExtensions
{
    public static IServiceCollection
        AddSecurityPrivacyHardeningPhase23(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
    {
        services
            .AddOptions<DataRetentionOptions>()
            .Bind(
                configuration.GetSection(
                    DataRetentionOptions
                        .SectionName))
            .Validate(
                options =>
                    options.ImportPayloadRetentionHours
                        is >= 1 and <= 168,
                "Import payload retention must be between 1 and 168 hours.")
            .Validate(
                options =>
                    options.NotificationReadRetentionDays
                        is >= 30 and <= 730,
                "Read notification retention must be between 30 and 730 days.")
            .Validate(
                options =>
                    options.SweepIntervalMinutes
                        is >= 5 and <= 1440,
                "Retention sweep interval must be between 5 and 1440 minutes.")
            .ValidateOnStart();

        services.AddScoped<
            ISensitiveDataRetentionRepository,
            SensitiveDataRetentionRepository>();

        var scale =
            MultiInstanceScaleOptions.Read(
                configuration);

        // Integration tests call the retention repository
        // explicitly. Do not start a nondeterministic timer
        // inside the Testing host.
        if (!environment.IsEnvironment(
                "Testing") &&
            scale.RunsBackgroundWorkers)
        {
            services.AddHostedService<
                SensitiveDataRetentionBackgroundService>();
        }

        return services;
    }
}
