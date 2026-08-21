namespace Edulytics.Web.Scale;

public sealed class MultiInstanceScaleOptions
{
    public const string SectionName =
        "Edulytics:Scale";

    public bool Enabled { get; set; }

    public string RuntimeRole { get; set; } =
        RuntimeRoles.Combined;

    public bool RequireRedis { get; set; }

    public bool DistributedSensitiveRateLimitsEnabled
    {
        get;
        set;
    }

    public int ExpectedWebInstances { get; set; } = 2;

    public int ExpectedWorkerInstances { get; set; } = 2;

    public int DatabaseConnectionBudget { get; set; } = 160;

    public string RedisChannelPrefix { get; set; } =
        "edulytics";

    public bool RunsWebTraffic =>
        RuntimeRoles.RunsWebTraffic(
            RuntimeRole);

    public bool RunsBackgroundWorkers =>
        RuntimeRoles.RunsBackgroundWorkers(
            RuntimeRole);

    public int ExpectedProcessCount =>
        checked(
            ExpectedWebInstances +
            ExpectedWorkerInstances);

    public int RequiredDatabasePoolCapacity(
        int perProcessPoolSize) =>
        checked(
            ExpectedProcessCount *
            perProcessPoolSize);

    public static MultiInstanceScaleOptions Read(
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(
            configuration);

        return configuration
            .GetSection(SectionName)
            .Get<MultiInstanceScaleOptions>()
            ?? new MultiInstanceScaleOptions();
    }
}

public static class RuntimeRoles
{
    public const string Combined = "Combined";
    public const string Web = "Web";
    public const string Worker = "Worker";

    public static bool IsValid(
        string? role) =>
        string.Equals(
            role,
            Combined,
            StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            role,
            Web,
            StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            role,
            Worker,
            StringComparison.OrdinalIgnoreCase);

    public static bool RunsWebTraffic(
        string? role) =>
        string.Equals(
            role,
            Combined,
            StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            role,
            Web,
            StringComparison.OrdinalIgnoreCase);

    public static bool RunsBackgroundWorkers(
        string? role) =>
        string.Equals(
            role,
            Combined,
            StringComparison.OrdinalIgnoreCase) ||
        string.Equals(
            role,
            Worker,
            StringComparison.OrdinalIgnoreCase);
}
