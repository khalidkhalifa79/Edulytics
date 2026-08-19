namespace Edulytics.Web.Resilience;

public sealed class BackendResilienceOptions
{
    public const string SectionName =
        "Edulytics:Resilience";

    public int InteractiveReadTimeoutSeconds { get; set; } = 15;
    public int InteractiveWriteTimeoutSeconds { get; set; } = 20;
    public int ImportTimeoutSeconds { get; set; } = 45;
    public int AnalyticsTimeoutSeconds { get; set; } = 30;
    public int ReportTimeoutSeconds { get; set; } = 30;
    public int OperationalTimeoutSeconds { get; set; } = 20;
    public int DatabaseCommandTimeoutSeconds { get; set; } = 15;
    public int NpgsqlMaxPoolSize { get; set; } = 40;
    public int HeavyWritePermitLimit { get; set; } = 8;
    public int HeavyWriteQueueLimit { get; set; } = 8;
    public int ImportPermitLimit { get; set; } = 2;
    public int ImportQueueLimit { get; set; } = 2;
    public int AnalyticsPermitLimit { get; set; } = 1;
    public int AnalyticsQueueLimit { get; set; } = 1;
    public int ReportPermitLimit { get; set; } = 2;
    public int ReportQueueLimit { get; set; } = 2;
    public long MaxRequestBodyBytes { get; set; } = 6 * 1024 * 1024;
    public int RequestHeadersTimeoutSeconds { get; set; } = 15;
    public int KeepAliveTimeoutSeconds { get; set; } = 120;
}

public static class BackendResiliencePolicyNames
{
    public const string InteractiveRead = "InteractiveRead";
    public const string InteractiveWrite = "InteractiveWrite";
    public const string Import = "Import";
    public const string Analytics = "Analytics";
    public const string Report = "Report";
    public const string Operational = "Operational";
    public const string HeavyWriteConcurrency = "HeavyWriteConcurrency";
    public const string ImportConcurrency = "ImportConcurrency";
    public const string AnalyticsConcurrency = "AnalyticsConcurrency";
    public const string ReportConcurrency = "ReportConcurrency";
    public const string ReportExportRate = "ReportExport";
    public const string OperationalConcurrency = "OperationalConcurrency";
}
