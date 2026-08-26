using System.Globalization;
using System.Security.Claims;
using Edulytics.Core.Reports;
using Edulytics.Services.Reports;
using Edulytics.Web.Resilience;
using Edulytics.Web.ViewModels.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;
using Edulytics.Core.Constants;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "ReportRead")]
[Route("school/reports")]
public sealed class ReportsController
    : Controller
{
    private readonly IReportQueryService
        _reports;

    private readonly IReportExportService
        _exports;

    private readonly ReportOptions _options;

    private readonly
        IStringLocalizer<ReportResource>
        _text;

    public ReportsController(
        IReportQueryService reports,
        IReportExportService exports,
        ReportOptions options,
        IStringLocalizer<ReportResource> text)
    {
        _reports = reports;
        _exports = exports;
        _options = options;
        _text = text;
    }

    [HttpGet("")]
    [RequestTimeout(
        BackendResiliencePolicyNames.Report)]
    [EnableRateLimiting(
        BackendResiliencePolicyNames
            .ReportConcurrency)]
    public async Task<IActionResult> Index(
        ReportKind? kind,
        Guid? academicYearId,
        Guid? classGroupId,
        Guid? subjectId,
        Guid? studentProfileId,
        Guid? learningOutcomeId,
        CancellationToken cancellationToken)
    {
        if (!TryActor(
                out var actorUserId))
        {
            return Forbid();
        }

        var catalog =
            await _reports.GetCatalogAsync(
                actorUserId,
                cancellationToken);

        if (catalog.Value is null)
        {
            return Forbid();
        }

        var selectedKind =
            kind
            ?? catalog.Value.AllowedKinds
                .First();

        var request =
            new ReportRequest(
                selectedKind,
                academicYearId,
                classGroupId,
                subjectId,
                studentProfileId,
                learningOutcomeId);

        ReportDocument? document = null;

        if (ReportIndexViewModel
            .HasRequiredSelection(request))
        {
            var result =
                await _reports.BuildAsync(
                    actorUserId,
                    request,
                    _options.MaxHtmlRows,
                    cancellationToken);

            if (result.Value is null)
            {
                if (result.Error is
                    ReportErrorCode.AccessDenied or
                    ReportErrorCode.SchoolNotActive)
                {
                    return Forbid();
                }

                TempData["ReportError"] =
                    _text[
                        ErrorKey(
                            result.Error)]
                        .Value;
            }
            else
            {
                document =
                    result.Value;
            }
        }

        var jobs =
            await _exports.ListAsync(
                actorUserId,
                cancellationToken);

        return View(
            new ReportIndexViewModel
            {
                Catalog =
                    catalog.Value,
                Request =
                    request,
                Document =
                    document,
                Jobs =
                    jobs.Value
                    ?? []
            });
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor + "," + RoleNames.Teacher)]
    [HttpPost("export")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(
        BackendResiliencePolicyNames.Report)]
    [EnableRateLimiting(
        BackendResiliencePolicyNames
            .ReportExportRate)]
    public async Task<IActionResult>
        RequestExport(
            ReportKind kind,
            ReportExportFormat format,
            Guid? academicYearId,
            Guid? classGroupId,
            Guid? subjectId,
            Guid? studentProfileId,
            Guid? learningOutcomeId,
            CancellationToken cancellationToken)
    {
        if (!TryActor(
                out var actorUserId))
        {
            return Forbid();
        }

        var request =
            new ReportRequest(
                kind,
                academicYearId,
                classGroupId,
                subjectId,
                studentProfileId,
                learningOutcomeId);

        var result =
            await _exports.RequestAsync(
                actorUserId,
                request,
                format,
                CultureInfo
                    .CurrentUICulture.Name,
                cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error is
                ReportErrorCode.AccessDenied or
                ReportErrorCode.SchoolNotActive)
            {
                return Forbid();
            }

            TempData["ReportError"] =
                _text[
                    ErrorKey(
                        result.Error)]
                    .Value;
        }
        else
        {
            TempData["ReportSuccess"] =
                _text[
                    "ReportExportQueued"]
                    .Value;
        }

        return RedirectToAction(
            nameof(Index),
            new
            {
                kind,
                academicYearId,
                classGroupId,
                subjectId,
                studentProfileId,
                learningOutcomeId
            });
    }

    [HttpGet(
        "export/{exportJobId:guid}/download")]
    [RequestTimeout(
        BackendResiliencePolicyNames.Report)]
    [EnableRateLimiting(
        BackendResiliencePolicyNames
            .ReportConcurrency)]
    public async Task<IActionResult> Download(
        Guid exportJobId,
        CancellationToken cancellationToken)
    {
        if (!TryActor(
                out var actorUserId))
        {
            return Forbid();
        }

        var result =
            await _exports.DownloadAsync(
                actorUserId,
                exportJobId,
                cancellationToken);

        if (result.Value is null)
        {
            return result.Error ==
                ReportErrorCode.NotFound
                ? NotFound()
                : result.Error ==
                    ReportErrorCode.AccessDenied
                    ? Forbid()
                    : NotFound();
        }

        return File(
            result.Value.Content,
            result.Value.ContentType,
            result.Value.FileName);
    }

    private bool TryActor(
        out Guid actorUserId) =>
        Guid.TryParse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier),
            out actorUserId);

    private static string ErrorKey(
        ReportErrorCode? error) =>
        error switch
        {
            ReportErrorCode.InvalidFilter =>
                "ReportErrorInvalidFilter",

            ReportErrorCode.ReportTooLarge =>
                "ReportErrorTooLarge",

            ReportErrorCode.NotReady =>
                "ReportErrorNotReady",

            ReportErrorCode.Expired =>
                "ReportErrorExpired",

            ReportErrorCode.UnsupportedFormat =>
                "ReportErrorUnsupportedFormat",

            _ =>
                "ReportErrorPersistence"
        };
}
