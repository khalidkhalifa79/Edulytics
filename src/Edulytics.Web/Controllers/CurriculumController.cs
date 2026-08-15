using System.Security.Claims;
using Edulytics.Services.Curriculum;
using Edulytics.Web.ViewModels.Curriculum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "AcademicStructureAdministration")]
[Route("school/curriculum")]
public sealed class CurriculumController : Controller
{
    private readonly ICurriculumService _curriculum;
    private readonly IStringLocalizer<CurriculumResource> _text;

    public CurriculumController(
        ICurriculumService curriculum,
        IStringLocalizer<CurriculumResource> text)
    {
        _curriculum = curriculum;
        _text = text;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await _curriculum.GetDashboardAsync(
            actorId,
            cancellationToken);

        if (result.Value is null)
            return HandleQueryError(result.Error);

        return View(
            new CurriculumIndexViewModel(
                result.Value.GradeLevels,
                result.Value.Subjects,
                result.Value.Topics));
    }

    [HttpPost("topics")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTopic(
        Guid subjectId,
        Guid gradeLevelId,
        string name,
        int order,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await _curriculum.CreateTopicAsync(
            actorId,
            new CreateCurriculumTopicRequest(
                subjectId,
                gradeLevelId,
                name,
                order),
            cancellationToken);

        SetFeedback(result, "SuccessTopicCreated");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("topics/{id:guid}/edit")]
    public async Task<IActionResult> EditTopic(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await _curriculum.GetTopicAsync(
            actorId,
            id,
            cancellationToken);

        if (result.Value is null)
            return HandleQueryError(result.Error);

        return View(
            new CurriculumTopicEditViewModel(result.Value));
    }

    [HttpPost("topics/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditTopic(
        Guid id,
        string name,
        int order,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await _curriculum.UpdateTopicAsync(
            actorId,
            new UpdateCurriculumTopicRequest(id, name, order),
            cancellationToken);

        SetFeedback(result, "SuccessTopicUpdated");

        return result.Succeeded
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(EditTopic), new { id });
    }

    [HttpPost("outcomes")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOutcome(
        Guid topicId,
        string code,
        string description,
        decimal weight,
        int order,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await _curriculum.CreateOutcomeAsync(
            actorId,
            new CreateLearningOutcomeRequest(
                topicId,
                code,
                description,
                weight,
                order),
            cancellationToken);

        SetFeedback(result, "SuccessOutcomeCreated");
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("outcomes/{id:guid}/edit")]
    public async Task<IActionResult> EditOutcome(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await _curriculum.GetOutcomeAsync(
            actorId,
            id,
            cancellationToken);

        if (result.Value is null)
            return HandleQueryError(result.Error);

        return View(
            new LearningOutcomeEditViewModel(result.Value));
    }

    [HttpPost("outcomes/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOutcome(
        Guid id,
        string code,
        string description,
        decimal weight,
        int order,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await _curriculum.UpdateOutcomeAsync(
            actorId,
            new UpdateLearningOutcomeRequest(
                id,
                code,
                description,
                weight,
                order),
            cancellationToken);

        SetFeedback(result, "SuccessOutcomeUpdated");

        return result.Succeeded
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(nameof(EditOutcome), new { id });
    }

    private bool TryActor(out Guid id) =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out id);

    private IActionResult HandleQueryError(
        CurriculumErrorCode? error) =>
        error == CurriculumErrorCode.AccessDenied
            ? Forbid()
            : NotFound();

    private void SetFeedback(
        CurriculumCommandResult result,
        string successKey)
    {
        if (result.Succeeded)
        {
            TempData["Success"] = _text[successKey].Value;
            return;
        }

        TempData["Error"] = _text[ErrorKey(result.Error)].Value;
    }

    private static string ErrorKey(CurriculumErrorCode? code) =>
        code switch
        {
            CurriculumErrorCode.AccessDenied => "ErrorAccessDenied",
            CurriculumErrorCode.SchoolNotActive => "ErrorSchoolNotActive",
            CurriculumErrorCode.Required => "ErrorRequired",
            CurriculumErrorCode.InvalidName => "ErrorInvalidName",
            CurriculumErrorCode.InvalidOrder => "ErrorInvalidOrder",
            CurriculumErrorCode.InvalidCode => "ErrorInvalidCode",
            CurriculumErrorCode.InvalidWeight => "ErrorInvalidWeight",
            CurriculumErrorCode.SubjectNotFound => "ErrorSubjectNotFound",
            CurriculumErrorCode.GradeLevelNotFound => "ErrorGradeLevelNotFound",
            CurriculumErrorCode.TopicNotFound => "ErrorTopicNotFound",
            CurriculumErrorCode.OutcomeNotFound => "ErrorOutcomeNotFound",
            CurriculumErrorCode.DuplicateTopicName => "ErrorDuplicateTopicName",
            CurriculumErrorCode.DuplicateTopicOrder => "ErrorDuplicateTopicOrder",
            CurriculumErrorCode.DuplicateOutcomeCode => "ErrorDuplicateOutcomeCode",
            CurriculumErrorCode.DuplicateOutcomeOrder => "ErrorDuplicateOutcomeOrder",
            _ => "ErrorPersistence"
        };
}
