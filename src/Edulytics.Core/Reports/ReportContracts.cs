namespace Edulytics.Core.Reports;

public enum ReportKind
{
    School = 1,
    Class = 2,
    Subject = 3,
    Student = 4,
    LearningOutcome = 5
}

public enum ReportExportFormat
{
    Csv = 1,
    Xlsx = 2
}

public enum ReportExportJobStatus
{
    Pending = 1,
    Completed = 2,
    Failed = 3,
    Expired = 4
}

public static class ReportEventTypes
{
    public const string ExportRequested =
        "Reports.ExportRequested";
}

public sealed record ReportExportRequestedEvent(
    Guid SchoolId,
    Guid ExportJobId);
