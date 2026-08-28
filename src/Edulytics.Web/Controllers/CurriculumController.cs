using System.Security.Claims;
using Edulytics.Core.Constants;
using Edulytics.Services.Curriculum;
using Edulytics.Web.ViewModels.Curriculum;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "SchoolAccess")]
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
                result.Value.Topics)
            {
                AcademicPrograms = result.Value.AcademicPrograms,
                Frameworks = result.Value.Frameworks,
                Adoptions = result.Value.Adoptions
            });
    }


    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("framework")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SelectFramework(
        Guid subjectId,
        Guid gradeLevelId,
        Guid academicProgramId,
        string frameworkCode,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await _curriculum.SelectFrameworkAsync(
            actorId,
            new SelectCurriculumFrameworkRequest(
                subjectId,
                gradeLevelId,
                frameworkCode,
                academicProgramId),
            cancellationToken);

        SetFeedback(result, "SuccessFrameworkSelected");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("topics")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateTopic(
        Guid subjectId,
        Guid gradeLevelId,
        Guid academicProgramId,
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
                order,
                academicProgramId),
            cancellationToken);

        SetFeedback(result, "SuccessTopicCreated");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
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

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
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

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("outcomes/official")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOfficialOutcome(
        Guid topicId,
        string selectionKey,
        int order,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var selection = ParseOfficialSelection(selectionKey);
        var result = selection is null
            ? CurriculumCommandResult.Failure(
                "ContentNodeId",
                CurriculumErrorCode.OfficialOutcomeNotFound)
            : await _curriculum.CreateOfficialOutcomeAsync(
                actorId,
                new CreateOfficialLearningOutcomeRequest(
                    topicId,
                    selection.Value.ContentNodeId,
                    selection.Value.LessonNodeId,
                    order
),
                cancellationToken);

        SetFeedback(result, "SuccessOfficialOutcomeAdded");
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
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

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("outcomes/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOutcome(
        Guid id,
        string code,
        string description,
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
            CurriculumErrorCode.FrameworkNotFound => "ErrorFrameworkNotFound",
            CurriculumErrorCode.CurriculumNotSelected => "ErrorCurriculumNotSelected",
            CurriculumErrorCode.CurriculumFrameworkInUse => "ErrorCurriculumFrameworkInUse",
            CurriculumErrorCode.OfficialOutcomeNotFound => "ErrorOfficialOutcomeNotFound",
            CurriculumErrorCode.OfficialOutcomeReadOnly => "ErrorOfficialOutcomeReadOnly",
            CurriculumErrorCode.AcademicProgramNotFound => "ErrorAcademicProgramNotFound",
            _ => "ErrorPersistence"
        };

    private static (Guid ContentNodeId, Guid? LessonNodeId)?
        ParseOfficialSelection(string? value)
    {
        var parts = (value ?? string.Empty).Split('|');
        if (parts.Length is < 1 or > 2 ||
            !Guid.TryParse(parts[0], out var contentNodeId))
        {
            return null;
        }

        if (parts.Length == 1 || string.IsNullOrWhiteSpace(parts[1]))
            return (contentNodeId, null);

        return Guid.TryParse(parts[1], out var lessonNodeId)
            ? (contentNodeId, lessonNodeId)
            : null;
    }
}
