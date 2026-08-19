using System.Text.Json;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Reports;
using Edulytics.Services.Auditing;

namespace Edulytics.Services.Reports;

public sealed class ReportExportService
    : IReportExportService
{
    private readonly IReportQueryService _reports;
    private readonly IReportExportRepository _exports;
    private readonly IAuditService _audit;

    private readonly
        IAuditRequestMetadataProvider
        _metadata;

    private readonly ReportOptions _options;

    public ReportExportService(
        IReportQueryService reports,
        IReportExportRepository exports,
        IAuditService audit,
        IAuditRequestMetadataProvider metadata,
        ReportOptions options)
    {
        _reports = reports;
        _exports = exports;
        _audit = audit;
        _metadata = metadata;
        _options = options;
    }

    public async Task<ReportCommandResult>
        RequestAsync(
            Guid actorUserId,
            ReportRequest request,
            ReportExportFormat format,
            string culture,
            CancellationToken cancellationToken = default)
    {
        if (format is not
            ReportExportFormat.Csv and not
            ReportExportFormat.Xlsx)
        {
            return ReportCommandResult
                .Failure(
                    ReportErrorCode
                        .UnsupportedFormat);
        }

        var validation =
            await _reports.ValidateAsync(
                actorUserId,
                request,
                cancellationToken);

        if (validation.Value is null)
        {
            return ReportCommandResult
                .Failure(
                    validation.Error!.Value);
        }

        culture =
            culture.StartsWith(
                "pl",
                StringComparison.OrdinalIgnoreCase)
                ? "pl"
                : "en";

        var now =
            DateTime.UtcNow;

        var job =
            new ReportExportJob
            {
                Id = Guid.NewGuid(),
                SchoolId =
                    validation.Value.SchoolId,
                RequestedByUserId =
                    actorUserId,
                ReportKind =
                    request.Kind,
                ExportFormat =
                    format,
                AcademicYearId =
                    request.AcademicYearId,
                ClassGroupId =
                    request.ClassGroupId,
                SubjectId =
                    request.SubjectId,
                StudentProfileId =
                    request.StudentProfileId,
                LearningOutcomeId =
                    request.LearningOutcomeId,
                Culture =
                    culture,
                Status =
                    ReportExportJobStatus.Pending,
                CreatedAtUtc =
                    now,
                ExpiresAtUtc =
                    now.AddHours(
                        _options
                            .ExportRetentionHours)
            };

        var requestMetadata =
            _metadata.GetCurrent();

        var eventPayload =
            new ReportExportRequestedEvent(
                job.SchoolId,
                job.Id);

        var outbox =
            new OutboxMessage
            {
                Id = Guid.NewGuid(),
                SchoolId = job.SchoolId,
                EventType =
                    ReportEventTypes
                        .ExportRequested,
                PayloadJson =
                    JsonSerializer.Serialize(
                        eventPayload),
                OccurredAtUtc = now,
                AvailableAtUtc = now,
                Status =
                    OutboxMessageStatus.Pending,
                CorrelationId =
                    requestMetadata.CorrelationId
            };

        await _exports.AddAsync(
            job,
            cancellationToken);

        await _exports.AddOutboxAsync(
            outbox,
            cancellationToken);

        await _audit.QueueAsync(
            new AuditEvent(
                job.SchoolId,
                "Report.ExportRequested",
                "ReportExportJob",
                job.Id.ToString("D"),
                "Reports",
                NewValues:
                    ScopeValues(
                        request,
                        format),
                ResultSummary:
                    "Report export queued."),
            cancellationToken);

        if (!await _exports.SaveAsync(
                cancellationToken))
        {
            return ReportCommandResult
                .Failure(
                    ReportErrorCode
                        .PersistenceError);
        }

        return ReportCommandResult
            .Success(job.Id);
    }

    public async Task<
        ReportQueryResult<
            IReadOnlyList<
                ReportExportListItem>>>
        ListAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default)
    {
        var catalog =
            await _reports.GetCatalogAsync(
                actorUserId,
                cancellationToken);

        if (catalog.Value is null)
        {
            return ReportQueryResult<
                IReadOnlyList<
                    ReportExportListItem>>
                .Failure(
                    catalog.Error!.Value);
        }

        var rows =
            await _exports.ListRecentAsync(
                catalog.Value.SchoolId,
                actorUserId,
                _options.RecentJobsLimit,
                cancellationToken);

        return ReportQueryResult<
            IReadOnlyList<
                ReportExportListItem>>
            .Success(
                rows
                    .Select(
                        x =>
                            new ReportExportListItem(
                                x.Id,
                                x.ReportKind,
                                x.ExportFormat,
                                x.Status,
                                x.RowCount,
                                x.FileName,
                                x.CreatedAtUtc,
                                x.ExpiresAtUtc,
                                x.CompletedAtUtc))
                    .ToArray());
    }

    public async Task<
        ReportQueryResult<ReportDownload>>
        DownloadAsync(
            Guid actorUserId,
            Guid exportJobId,
            CancellationToken cancellationToken = default)
    {
        var catalog =
            await _reports.GetCatalogAsync(
                actorUserId,
                cancellationToken);

        if (catalog.Value is null)
        {
            return ReportQueryResult<ReportDownload>
                .Failure(
                    catalog.Error!.Value);
        }

        var job =
            await _exports.GetAsync(
                catalog.Value.SchoolId,
                exportJobId,
                cancellationToken);

        if (job is null ||
            job.RequestedByUserId !=
                actorUserId)
        {
            return ReportQueryResult<ReportDownload>
                .Failure(
                    ReportErrorCode.NotFound);
        }

        if (job.ExpiresAtUtc <=
            DateTime.UtcNow)
        {
            return ReportQueryResult<ReportDownload>
                .Failure(
                    ReportErrorCode.Expired);
        }

        if (job.Status !=
                ReportExportJobStatus.Completed ||
            job.FileContent is null ||
            string.IsNullOrWhiteSpace(
                job.FileName) ||
            string.IsNullOrWhiteSpace(
                job.ContentType))
        {
            return ReportQueryResult<ReportDownload>
                .Failure(
                    ReportErrorCode.NotReady);
        }

        var access =
            await _reports.ValidateAsync(
                actorUserId,
                ToRequest(job),
                cancellationToken);

        if (access.Value is null)
        {
            return ReportQueryResult<ReportDownload>
                .Failure(
                    ReportErrorCode.AccessDenied);
        }

        await _audit.RecordAsync(
            new AuditEvent(
                job.SchoolId,
                "Report.Downloaded",
                "ReportExportJob",
                job.Id.ToString("D"),
                "Reports",
                NewValues:
                    new Dictionary<
                        string,
                        object?>
                    {
                        ["ReportKind"] =
                            job.ReportKind
                                .ToString(),

                        ["Format"] =
                            job.ExportFormat
                                .ToString(),

                        ["RowCount"] =
                            job.RowCount
                    },
                ResultSummary:
                    "Generated report downloaded."),
            cancellationToken);

        return ReportQueryResult<ReportDownload>
            .Success(
                new ReportDownload(
                    job.FileName,
                    job.ContentType,
                    job.FileContent));
    }

    public static ReportRequest ToRequest(
        ReportExportJob job) =>
        new(
            job.ReportKind,
            job.AcademicYearId,
            job.ClassGroupId,
            job.SubjectId,
            job.StudentProfileId,
            job.LearningOutcomeId);

    public static IReadOnlyDictionary<
        string,
        object?>
        ScopeValues(
            ReportRequest request,
            ReportExportFormat format) =>
        new Dictionary<string, object?>
        {
            ["ReportKind"] =
                request.Kind.ToString(),

            ["Format"] =
                format.ToString(),

            ["AcademicYearId"] =
                request.AcademicYearId,

            ["ClassGroupId"] =
                request.ClassGroupId,

            ["SubjectId"] =
                request.SubjectId,

            ["StudentProfileId"] =
                request.StudentProfileId,

            ["LearningOutcomeId"] =
                request.LearningOutcomeId
        };
}
