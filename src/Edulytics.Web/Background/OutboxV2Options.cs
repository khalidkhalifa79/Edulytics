namespace Edulytics.Web.Background;

public sealed class OutboxV2Options
{
    public const string SectionName =
        "Edulytics:OutboxV2";

    public int PollDelayMilliseconds { get; set; } = 300;
    public int ErrorDelayMilliseconds { get; set; } = 1000;
    public int BatchSize { get; set; } = 20;
    public int LeaseSeconds { get; set; } = 30;
    public int MessageProcessingTimeoutSeconds { get; set; } = 20;
    public int MaxAttempts { get; set; } = 6;
    public int RetryBaseSeconds { get; set; } = 2;
    public int RetryMaxSeconds { get; set; } = 120;
    public int RetryJitterMilliseconds { get; set; } = 750;
    public int AnalyticsPollDelayMilliseconds { get; set; } = 250;
    public int AnalyticsDebounceMilliseconds { get; set; } = 400;
    public int AnalyticsMaxCoalesceMilliseconds { get; set; } = 2000;
    public int AnalyticsLeaseSeconds { get; set; } = 60;
    public int AnalyticsRefreshTimeoutSeconds { get; set; } = 40;
    public int ShutdownGraceSeconds { get; set; } = 45;
}
