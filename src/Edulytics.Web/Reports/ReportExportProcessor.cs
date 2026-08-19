using System.Globalization;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Reports;
using Edulytics.Services.Auditing;
using Edulytics.Services.Reports;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Reports;

public sealed class ReportExportProcessor
    : IReportExportProcessor
{
    private readonly IReportExportRepository
        _exports;

    private readonly IReportQueryService
        _reports;

    private readonly IAuditService _audit;

    private readonly
        IStringLocalizer<ReportResource>
        _text;

    private readonly ReportOptions _options;

    public ReportExportProcessor(
        IReportExportRepository exports,
        IReportQueryService reports,
        IAuditService audit,
        IStringLocalizer<ReportResource> text,
        ReportOptions options)
    {
        _exports = exports;
        _reports = reports;
        _audit = audit;
        _text = text;
        _options = options;
    }

    public async Task ProcessAsync(
        Guid schoolId,
        Guid exportJobId,
        CancellationToken cancellationToken = default)
    {
        var job =
            await _exports.GetForUpdateAsync(
                schoolId,
                exportJobId,
                cancellationToken);

        if (job is null)
        {
            throw new InvalidOperationException(
                "Report export job does not exist.");
        }

        if (job.Status is
            ReportExportJobStatus.Completed or
            ReportExportJobStatus.Failed or
            ReportExportJobStatus.Expired)
        {
            return;
        }

        if (job.ExpiresAtUtc <=
            DateTime.UtcNow)
        {
            job.Status =
                ReportExportJobStatus.Expired;

            job.FileContent = null;

            await _audit.QueueAsync(
                new AuditEvent(
                    job.SchoolId,
                    "Report.ExportExpired",
                    "ReportExportJob",
                    job.Id.ToString("D"),
                    "Reports",
                    ResultSummary:
                        "Report export expired before completion."),
                cancellationToken);

            await SaveOrThrow(
                cancellationToken);

            return;
        }

        var result =
            await _reports.BuildAsync(
                job.RequestedByUserId,
                ReportExportService
                    .ToRequest(job),
                _options.MaxExportRows,
                cancellationToken);

        if (result.Value is null)
        {
            await FailTerminallyAsync(
                job,
                result.Error
                    ?.ToString()
                ?? "ReportBuildFailed",
                cancellationToken);

            return;
        }

        if (result.Value.Truncated)
        {
            await FailTerminallyAsync(
                job,
                ReportErrorCode
                    .ReportTooLarge
                    .ToString(),
                cancellationToken);

            return;
        }

        var culture =
            CultureInfo.GetCultureInfo(
                job.Culture == "pl"
                    ? "pl-PL"
                    : "en");

        var oldCulture =
            CultureInfo.CurrentCulture;

        var oldUiCulture =
            CultureInfo.CurrentUICulture;

        ReportExportArtifact artifact;

        try
        {
            CultureInfo.CurrentCulture =
                culture;

            CultureInfo.CurrentUICulture =
                culture;

            artifact =
                await ReportExportRenderer
                    .RenderAsync(
                        result.Value,
                        job.ExportFormat,
                        key =>
                            _text[key].Value,
                        culture,
                        cancellationToken);
        }
        finally
        {
            CultureInfo.CurrentCulture =
                oldCulture;

            CultureInfo.CurrentUICulture =
                oldUiCulture;
        }

        if (artifact.Content.Length >
            _options.MaxExportBytes)
        {
            await FailTerminallyAsync(
                job,
                ReportErrorCode
                    .ReportTooLarge
                    .ToString(),
                cancellationToken);

            return;
        }

        job.Status =
            ReportExportJobStatus.Completed;

        job.RowCount =
            result.Value.TotalRowCount;

        job.FileName =
            BuildFileName(
                job,
                artifact.Extension);

        job.ContentType =
            artifact.ContentType;

        job.FileContent =
            artifact.Content;

        job.CompletedAtUtc =
            DateTime.UtcNow;

        job.LastError = null;

        await _audit.QueueAsync(
            new AuditEvent(
                job.SchoolId,
                "Report.ExportCompleted",
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
                    "Report export generated."),
            cancellationToken);

        await SaveOrThrow(
            cancellationToken);
    }

    public async Task MarkDeadLetteredAsync(
        Guid schoolId,
        Guid exportJobId,
        CancellationToken cancellationToken = default)
    {
        var job =
            await _exports.GetForUpdateAsync(
                schoolId,
                exportJobId,
                cancellationToken);

        if (job is null ||
            job.Status !=
                ReportExportJobStatus.Pending)
        {
            return;
        }

        job.Status =
            ReportExportJobStatus.Failed;

        job.LastError =
            "BackgroundDeliveryDeadLettered";

        job.CompletedAtUtc =
            DateTime.UtcNow;

        await _audit.QueueAsync(
            new AuditEvent(
                job.SchoolId,
                "Report.ExportFailed",
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

                        ["FailureCode"] =
                            job.LastError
                    },
                ResultSummary:
                    "Report export background job failed."),
            cancellationToken);

        await SaveOrThrow(
            cancellationToken);
    }

    private async Task FailTerminallyAsync(
        Edulytics.Core.Entities.ReportExportJob job,
        string failureCode,
        CancellationToken cancellationToken)
    {
        job.Status =
            ReportExportJobStatus.Failed;

        job.LastError =
            failureCode.Length <= 300
                ? failureCode
                : failureCode[..300];

        job.CompletedAtUtc =
            DateTime.UtcNow;

        job.FileContent = null;

        await _audit.QueueAsync(
            new AuditEvent(
                job.SchoolId,
                "Report.ExportFailed",
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

                        ["FailureCode"] =
                            job.LastError
                    },
                ResultSummary:
                    "Report export generation failed."),
            cancellationToken);

        await SaveOrThrow(
            cancellationToken);
    }

    private async Task SaveOrThrow(
        CancellationToken cancellationToken)
    {
        if (!await _exports.SaveAsync(
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Report export persistence conflict.");
        }
    }

    private static string BuildFileName(
        Edulytics.Core.Entities.ReportExportJob job,
        string extension) =>
        "edulytics-"
        + job.ReportKind
            .ToString()
            .ToLowerInvariant()
        + "-"
        + job.CreatedAtUtc
            .ToString(
                "yyyyMMddHHmm",
                CultureInfo.InvariantCulture)
        + "-"
        + job.Id.ToString("N")
        + "."
        + extension;
}
