using Edulytics.Core.Reports;

namespace Edulytics.Services.Reports;

public enum ReportErrorCode
{
    AccessDenied,
    SchoolNotActive,
    InvalidFilter,
    ReportTooLarge,
    NotFound,
    NotReady,
    Expired,
    PersistenceError,
    UnsupportedFormat
}

public enum ReportCellKind
{
    Text,
    Integer,
    Decimal,
    Percentage,
    DateTime
}

public sealed record ReportRequest(
    ReportKind Kind,
    Guid? AcademicYearId = null,
    Guid? ClassGroupId = null,
    Guid? SubjectId = null,
    Guid? StudentProfileId = null,
    Guid? LearningOutcomeId = null);

public sealed record ReportFilterItem(
    Guid Id,
    string Name);

public sealed record ReportCatalog(
    Guid SchoolId,
    string SchoolName,
    string Role,
    IReadOnlyList<ReportKind> AllowedKinds,
    IReadOnlyList<ReportFilterItem> AcademicYears,
    IReadOnlyList<ReportFilterItem> ClassGroups,
    IReadOnlyList<ReportFilterItem> Subjects,
    IReadOnlyList<ReportFilterItem> Students,
    IReadOnlyList<ReportFilterItem> LearningOutcomes);

public sealed record ReportColumn(
    string HeaderKey,
    ReportCellKind Kind);

public sealed record ReportCell(
    ReportCellKind Kind,
    string? TextValue = null,
    decimal? NumberValue = null,
    DateTime? DateTimeValue = null)
{
    public static ReportCell Text(
        string? value) =>
        new(
            ReportCellKind.Text,
            value ?? string.Empty);

    public static ReportCell Integer(
        int value) =>
        new(
            ReportCellKind.Integer,
            NumberValue: value);

    public static ReportCell Decimal(
        decimal value) =>
        new(
            ReportCellKind.Decimal,
            NumberValue: value);

    public static ReportCell Percentage(
        decimal value) =>
        new(
            ReportCellKind.Percentage,
            NumberValue: value);

    public static ReportCell DateTime(
        System.DateTime value) =>
        new(
            ReportCellKind.DateTime,
            DateTimeValue: value);
}

public sealed record ReportRow(
    IReadOnlyList<ReportCell> Cells);

public sealed record ReportDocument(
    ReportKind Kind,
    string TitleKey,
    DateTime GeneratedAtUtc,
    IReadOnlyList<ReportColumn> Columns,
    IReadOnlyList<ReportRow> Rows,
    int TotalRowCount,
    bool Truncated);

public sealed record ReportQueryResult<T>(
    T? Value,
    ReportErrorCode? Error)
    where T : class
{
    public static ReportQueryResult<T> Success(
        T value) =>
        new(value, null);

    public static ReportQueryResult<T> Failure(
        ReportErrorCode error) =>
        new(null, error);
}

public sealed record ReportCommandResult(
    bool Succeeded,
    Guid? Id,
    ReportErrorCode? Error)
{
    public static ReportCommandResult Success(
        Guid id) =>
        new(true, id, null);

    public static ReportCommandResult Failure(
        ReportErrorCode error) =>
        new(false, null, error);
}

public sealed record ReportExportListItem(
    Guid Id,
    ReportKind ReportKind,
    ReportExportFormat ExportFormat,
    Edulytics.Core.Reports.ReportExportJobStatus Status,
    int? RowCount,
    string? FileName,
    DateTime CreatedAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? CompletedAtUtc);

public sealed record ReportDownload(
    string FileName,
    string ContentType,
    byte[] Content);

public sealed class ReportOptions
{
    public const string SectionName =
        "Edulytics:Reports";

    public int MaxHtmlRows { get; set; } = 500;
    public int MaxExportRows { get; set; } = 25000;
    public int MaxExportBytes { get; set; } =
        10 * 1024 * 1024;

    public int ExportRetentionHours { get; set; } = 24;
    public int RecentJobsLimit { get; set; } = 20;
}

public interface IReportQueryService
{
    Task<ReportQueryResult<ReportCatalog>>
        GetCatalogAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default);

    Task<ReportQueryResult<ReportCatalog>>
        ValidateAsync(
            Guid actorUserId,
            ReportRequest request,
            CancellationToken cancellationToken = default);

    Task<ReportQueryResult<ReportDocument>>
        BuildAsync(
            Guid actorUserId,
            ReportRequest request,
            int maxRows,
            CancellationToken cancellationToken = default);
}

public interface IReportExportService
{
    Task<ReportCommandResult> RequestAsync(
        Guid actorUserId,
        ReportRequest request,
        ReportExportFormat format,
        string culture,
        CancellationToken cancellationToken = default);

    Task<ReportQueryResult<
        IReadOnlyList<ReportExportListItem>>>
        ListAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default);

    Task<ReportQueryResult<ReportDownload>>
        DownloadAsync(
            Guid actorUserId,
            Guid exportJobId,
            CancellationToken cancellationToken = default);
}

public interface IReportExportProcessor
{
    Task ProcessAsync(
        Guid schoolId,
        Guid exportJobId,
        CancellationToken cancellationToken = default);

    Task MarkDeadLetteredAsync(
        Guid schoolId,
        Guid exportJobId,
        CancellationToken cancellationToken = default);
}
