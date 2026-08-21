using Edulytics.Web.Resilience;
using Edulytics.Web.Scale;
using StackExchange.Redis;

namespace Edulytics.Web.Extensions;

public static class
    MultiInstanceScaleRegistrationExtensions
{
    public static IServiceCollection
        AddMultiInstanceScalePhase25(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(
            services);

        ArgumentNullException.ThrowIfNull(
            configuration);

        ArgumentNullException.ThrowIfNull(
            environment);

        var scale =
            MultiInstanceScaleOptions.Read(
                configuration);

        Validate(
            scale,
            configuration,
            environment);

        services.AddSingleton(scale);

        if (scale.Enabled &&
            scale.RequireRedis)
        {
            var redisConnection =
                RedisConnectionConfiguration
                    .ReadRequired(
                        configuration);

            services.AddSingleton<
                IConnectionMultiplexer>(
                _ =>
                    ConnectionMultiplexer.Connect(
                        RedisConnectionConfiguration
                            .Parse(
                                redisConnection)));

            if (scale
                .DistributedSensitiveRateLimitsEnabled)
            {
                services.AddSingleton<
                    IDistributedSensitiveRateLimiter,
                    RedisDistributedSensitiveRateLimiter>();
            }
            else
            {
                services.AddSingleton<
                    IDistributedSensitiveRateLimiter,
                    DisabledDistributedSensitiveRateLimiter>();
            }
        }
        else
        {
            services.AddSingleton<
                IDistributedSensitiveRateLimiter,
                DisabledDistributedSensitiveRateLimiter>();
        }

        return services;
    }

    private static void Validate(
        MultiInstanceScaleOptions scale,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        if (!RuntimeRoles.IsValid(
                scale.RuntimeRole))
        {
            throw new InvalidOperationException(
                "Edulytics:Scale:RuntimeRole must be "
                + "Combined, Web, or Worker.");
        }

        if (!scale.Enabled)
        {
            return;
        }

        if (scale.ExpectedWebInstances <= 0 ||
            scale.ExpectedWorkerInstances <= 0)
        {
            throw new InvalidOperationException(
                "Phase 25 expected web/worker instance "
                + "counts must both be positive.");
        }

        if (scale.DatabaseConnectionBudget <= 0)
        {
            throw new InvalidOperationException(
                "Phase 25 database connection budget "
                + "must be positive.");
        }

        var perProcessPoolSize =
            configuration.GetValue<int?>(
                BackendResilienceOptions
                    .SectionName
                + ":NpgsqlMaxPoolSize")
            ?? 40;

        if (perProcessPoolSize <= 0)
        {
            throw new InvalidOperationException(
                "NpgsqlMaxPoolSize must be positive.");
        }

        var requiredPoolCapacity =
            scale.RequiredDatabasePoolCapacity(
                perProcessPoolSize);

        if (requiredPoolCapacity >
            scale.DatabaseConnectionBudget)
        {
            throw new InvalidOperationException(
                "Configured Phase 25 process topology "
                + $"requires {requiredPoolCapacity} "
                + "Npgsql pooled connections but the "
                + "configured application budget is "
                + $"{scale.DatabaseConnectionBudget}.");
        }

        if (scale.RequireRedis ||
            scale
                .DistributedSensitiveRateLimitsEnabled)
        {
            if (string.IsNullOrWhiteSpace(
                    RedisConnectionConfiguration
                        .Read(
                            configuration)) &&
                !environment.IsEnvironment(
                    "Testing"))
            {
                throw new InvalidOperationException(
                    "Redis is required by the enabled "
                    + "Phase 25 scale configuration.");
            }
        }

        if (scale
                .DistributedSensitiveRateLimitsEnabled &&
            !scale.RequireRedis)
        {
            throw new InvalidOperationException(
                "Distributed sensitive rate limits "
                + "require Redis scale-out.");
        }
    }
}
