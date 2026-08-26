using System.Security.Claims;
using Edulytics.Core.Constants;
using Edulytics.Services.Analytics;
using Edulytics.Web.ViewModels.Analytics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using Edulytics.Web.Resilience;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "AnalyticsRead")]
[Route("school/analytics")]
public sealed class AnalyticsController : Controller
{
    private readonly IAnalyticsService _analytics;
    private readonly IStringLocalizer<AnalyticsResource> _text;

    public AnalyticsController(
        IAnalyticsService analytics,
        IStringLocalizer<AnalyticsResource> text)
    {
        _analytics = analytics;
        _text = text;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        Guid? academicYearId,
        Guid? classGroupId,
        Guid? subjectId,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result =
            await _analytics.GetDashboardAsync(
                actorId,
                academicYearId,
                classGroupId,
                subjectId,
                cancellationToken);

        if (result.Value is null)
            return HandleQueryError(result.Error);

        return View(
            new AnalyticsIndexViewModel(
                result.Value));
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("recalculate")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.Analytics)]
    [EnableRateLimiting(BackendResiliencePolicyNames.AnalyticsConcurrency)]
    public async Task<IActionResult> Recalculate(
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result =
            await _analytics.RecalculateAsync(
                actorId,
                cancellationToken);

        TempData[
            result.Succeeded
                ? "Success"
                : "Error"] =
            result.Succeeded
                ? _text[
                    "SuccessRecalculated"].Value
                : _text[
                    ErrorKey(
                        result.Error)].Value;

        return RedirectToAction(nameof(Index));
    }

    private bool TryActor(out Guid id) =>
        Guid.TryParse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier),
            out id);

    private IActionResult HandleQueryError(
        AnalyticsErrorCode? error) =>
        error is AnalyticsErrorCode.AccessDenied
            or AnalyticsErrorCode.SchoolNotActive
                ? Forbid()
                : NotFound();

    private static string ErrorKey(
        AnalyticsErrorCode? error) =>
        error switch
        {
            AnalyticsErrorCode.AccessDenied =>
                "ErrorAccessDenied",

            AnalyticsErrorCode.SchoolNotActive =>
                "ErrorSchoolNotActive",

            AnalyticsErrorCode
                .RecalculationRequiresSchoolAdmin =>
                "ErrorRecalculateAdminOnly",

            AnalyticsErrorCode.InvalidSourceData =>
                "ErrorInvalidSourceData",

            _ =>
                "ErrorPersistence"
        };
}
