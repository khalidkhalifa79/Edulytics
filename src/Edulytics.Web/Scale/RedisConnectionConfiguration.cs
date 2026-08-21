using StackExchange.Redis;

namespace Edulytics.Web.Scale;

public static class RedisConnectionConfiguration
{
    public static string? Read(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(
            configuration);

        return configuration
            .GetConnectionString("Redis")
            ?? configuration["REDIS_URL"];
    }

    public static string ReadRequired(
        IConfiguration configuration)
    {
        var value = Read(configuration);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                "Redis connection string is required "
                + "when Phase 25 scale-out is enabled.");
        }

        return value;
    }

    public static ConfigurationOptions Parse(
        string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            value);

        if (value.Contains(
                "://",
                StringComparison.Ordinal) &&
            Uri.TryCreate(
                value,
                UriKind.Absolute,
                out var uri) &&
            (string.Equals(
                 uri.Scheme,
                 "redis",
                 StringComparison.OrdinalIgnoreCase) ||
             string.Equals(
                 uri.Scheme,
                 "rediss",
                 StringComparison.OrdinalIgnoreCase)))
        {
            var options =
                new ConfigurationOptions
                {
                    AbortOnConnectFail = false,
                    ConnectRetry = 3,
                    Ssl = string.Equals(
                        uri.Scheme,
                        "rediss",
                        StringComparison.OrdinalIgnoreCase)
                };

            options.EndPoints.Add(
                uri.Host,
                uri.IsDefaultPort
                    ? 6379
                    : uri.Port);

            if (!string.IsNullOrWhiteSpace(
                    uri.UserInfo))
            {
                var parts =
                    uri.UserInfo.Split(
                        ':',
                        2);

                if (parts.Length == 2)
                {
                    options.User =
                        Uri.UnescapeDataString(
                            parts[0]);

                    options.Password =
                        Uri.UnescapeDataString(
                            parts[1]);
                }
                else
                {
                    options.Password =
                        Uri.UnescapeDataString(
                            parts[0]);
                }
            }

            return options;
        }

        var parsed =
            ConfigurationOptions.Parse(value);

        parsed.AbortOnConnectFail = false;
        parsed.ConnectRetry =
            Math.Max(
                parsed.ConnectRetry,
                3);

        return parsed;
    }
}
