namespace Edulytics.Web.Privacy;

public sealed class DataRetentionOptions
{
    public const string SectionName =
        "Edulytics:Privacy";

    public bool Enabled { get; set; } =
        true;

    public int ImportPayloadRetentionHours
        { get; set; } = 24;

    public int NotificationReadRetentionDays
        { get; set; } = 180;

    public int SweepIntervalMinutes
        { get; set; } = 15;
}
