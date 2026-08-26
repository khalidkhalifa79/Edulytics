using System.Security.Claims;
using Edulytics.Core.Constants;
using Edulytics.Services.Assessments;
using Edulytics.Web.ViewModels.Assessments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using Edulytics.Web.Resilience;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "AssessmentManagement")]
[Route("school/assessments")]
public sealed class AssessmentsController : Controller
{
    private readonly IAssessmentService _service;
    private readonly IStringLocalizer<AssessmentResource> _text;

    public AssessmentsController(
        IAssessmentService service,
        IStringLocalizer<AssessmentResource> text)
    {
        _service = service;
        _text = text;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        var result = await _service.GetWorkspaceAsync(actorId, cancellationToken);
        return result.Value is null
            ? HandleQueryError(result.Error)
            : View(new AssessmentIndexViewModel(result.Value));
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpPost("")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    [EnableRateLimiting(BackendResiliencePolicyNames.HeavyWriteConcurrency)]
    public async Task<IActionResult> Create(
        Guid classGroupId,
        Guid subjectId,
        Guid termId,
        string title,
        DateOnly assessmentDate,
        decimal maxScore,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        var result = await _service.CreateAssessmentAsync(
            actorId,
            new CreateAssessmentRequest(
                classGroupId,
                subjectId,
                termId,
                title,
                assessmentDate,
                maxScore),
            cancellationToken);

        SetFeedback(result, "SuccessAssessmentCreated");

        return result.Succeeded && result.EntityId.HasValue
            ? RedirectToAction(nameof(Details), new { id = result.EntityId.Value })
            : RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(Guid id, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        var result = await _service.GetDetailsAsync(actorId, id, cancellationToken);
        return result.Value is null
            ? HandleQueryError(result.Error)
            : View(new AssessmentDetailsViewModel(result.Value));
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpGet("{id:guid}/edit")]
    public async Task<IActionResult> Edit(Guid id, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        var result = await _service.GetDetailsAsync(actorId, id, cancellationToken);
        return result.Value is null
            ? HandleQueryError(result.Error)
            : View(new AssessmentEditViewModel(result.Value));
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpPost("{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    [EnableRateLimiting(BackendResiliencePolicyNames.HeavyWriteConcurrency)]
    public async Task<IActionResult> Edit(
        Guid id,
        string title,
        DateOnly assessmentDate,
        decimal maxScore,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        if (!TryDecodeRowVersion(rowVersion, out var bytes))
        {
            TempData["Error"] = _text["ErrorConcurrencyConflict"].Value;
            return RedirectToAction(nameof(Edit), new { id });
        }

        var result = await _service.UpdateAssessmentAsync(
            actorId,
            new UpdateAssessmentRequest(id, title, assessmentDate, maxScore, bytes),
            cancellationToken);

        SetFeedback(result, "SuccessAssessmentUpdated");

        return result.Succeeded
            ? RedirectToAction(nameof(Details), new { id })
            : RedirectToAction(nameof(Edit), new { id });
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpPost("{id:guid}/questions")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    [EnableRateLimiting(BackendResiliencePolicyNames.HeavyWriteConcurrency)]
    public async Task<IActionResult> CreateQuestion(
        Guid id,
        string prompt,
        decimal maxScore,
        int order,
        Guid[]? outcomeIds,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        if (!TryDecodeRowVersion(rowVersion, out var bytes))
        {
            TempData["Error"] = _text["ErrorConcurrencyConflict"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _service.CreateQuestionAsync(
            actorId,
            new CreateAssessmentQuestionRequest(
                id,
                prompt,
                maxScore,
                order,
                outcomeIds ?? [],
                bytes),
            cancellationToken);

        SetFeedback(result, "SuccessQuestionCreated");
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpGet("questions/{questionId:guid}/edit")]
    public async Task<IActionResult> EditQuestion(
        Guid questionId,
        Guid assessmentId,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        var question = await _service.GetQuestionAsync(actorId, questionId, cancellationToken);
        if (question.Value is null) return HandleQueryError(question.Error);

        var assessment = await _service.GetDetailsAsync(actorId, assessmentId, cancellationToken);
        if (assessment.Value is null) return HandleQueryError(assessment.Error);

        return View(new AssessmentQuestionEditViewModel(
            assessmentId,
            question.Value,
            assessment.Value.EligibleOutcomes,
            assessment.Value.Assessment.RowVersion));
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpPost("questions/{questionId:guid}/edit")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    [EnableRateLimiting(BackendResiliencePolicyNames.HeavyWriteConcurrency)]
    public async Task<IActionResult> EditQuestion(
        Guid questionId,
        Guid assessmentId,
        string prompt,
        decimal maxScore,
        int order,
        Guid[]? outcomeIds,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        if (!TryDecodeRowVersion(rowVersion, out var bytes))
        {
            TempData["Error"] = _text["ErrorConcurrencyConflict"].Value;
            return RedirectToAction(nameof(EditQuestion), new { questionId, assessmentId });
        }

        var result = await _service.UpdateQuestionAsync(
            actorId,
            new UpdateAssessmentQuestionRequest(
                questionId,
                prompt,
                maxScore,
                order,
                outcomeIds ?? [],
                bytes),
            cancellationToken);

        SetFeedback(result, "SuccessQuestionUpdated");

        return result.Succeeded
            ? RedirectToAction(nameof(Details), new { id = assessmentId })
            : RedirectToAction(nameof(EditQuestion), new { questionId, assessmentId });
    }


    [Authorize(Roles = RoleNames.Teacher)]
    [HttpPost("{id:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    [EnableRateLimiting(BackendResiliencePolicyNames.HeavyWriteConcurrency)]
    public async Task<IActionResult> DeleteAssessment(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        if (!TryDecodeRowVersion(rowVersion, out var bytes))
        {
            TempData["Error"] =
                _text["ErrorConcurrencyConflict"].Value;

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var result = await _service.DeleteAssessmentAsync(
            actorId,
            new DeleteAssessmentRequest(
                id,
                bytes),
            cancellationToken);

        SetFeedback(
            result,
            "SuccessAssessmentDeleted");

        return result.Succeeded
            ? RedirectToAction(nameof(Index))
            : RedirectToAction(
                nameof(Details),
                new { id });
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpPost("questions/{questionId:guid}/delete")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    [EnableRateLimiting(BackendResiliencePolicyNames.HeavyWriteConcurrency)]
    public async Task<IActionResult> DeleteQuestion(
        Guid questionId,
        Guid assessmentId,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        if (!TryDecodeRowVersion(rowVersion, out var bytes))
        {
            TempData["Error"] =
                _text["ErrorConcurrencyConflict"].Value;

            return RedirectToAction(
                nameof(Details),
                new { id = assessmentId });
        }

        var result = await _service.DeleteQuestionAsync(
            actorId,
            new DeleteAssessmentQuestionRequest(
                questionId,
                bytes),
            cancellationToken);

        SetFeedback(
            result,
            "SuccessQuestionDeleted");

        return RedirectToAction(
            nameof(Details),
            new { id = assessmentId });
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpPost("questions/{questionId:guid}/outcomes")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    [EnableRateLimiting(BackendResiliencePolicyNames.HeavyWriteConcurrency)]
    public async Task<IActionResult> MapOutcome(
        Guid questionId,
        Guid assessmentId,
        Guid outcomeId,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        if (!TryDecodeRowVersion(rowVersion, out var bytes))
        {
            TempData["Error"] = _text["ErrorConcurrencyConflict"].Value;
            return RedirectToAction(nameof(Details), new { id = assessmentId });
        }

        var result = await _service.MapOutcomeAsync(
            actorId,
            new MapQuestionOutcomeRequest(questionId, outcomeId, bytes),
            cancellationToken);

        SetFeedback(result, "SuccessOutcomeMapped");
        return RedirectToAction(nameof(Details), new { id = assessmentId });
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpPost("questions/{questionId:guid}/outcomes/{outcomeId:guid}/remove")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    [EnableRateLimiting(BackendResiliencePolicyNames.HeavyWriteConcurrency)]
    public async Task<IActionResult> UnmapOutcome(
        Guid questionId,
        Guid outcomeId,
        Guid assessmentId,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        if (!TryDecodeRowVersion(rowVersion, out var bytes))
        {
            TempData["Error"] = _text["ErrorConcurrencyConflict"].Value;
            return RedirectToAction(nameof(Details), new { id = assessmentId });
        }

        var result = await _service.UnmapOutcomeAsync(
            actorId,
            new UnmapQuestionOutcomeRequest(questionId, outcomeId, bytes),
            cancellationToken);

        SetFeedback(result, "SuccessOutcomeUnmapped");
        return RedirectToAction(nameof(Details), new { id = assessmentId });
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpPost("{id:guid}/open")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    [EnableRateLimiting(BackendResiliencePolicyNames.HeavyWriteConcurrency)]
    public async Task<IActionResult> Open(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        if (!TryDecodeRowVersion(rowVersion, out var bytes))
        {
            TempData["Error"] = _text["ErrorConcurrencyConflict"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _service.OpenAssessmentAsync(actorId, id, bytes, cancellationToken);
        SetFeedback(result, "SuccessAssessmentOpened");
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpPost("{id:guid}/close")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    [EnableRateLimiting(BackendResiliencePolicyNames.HeavyWriteConcurrency)]
    public async Task<IActionResult> Close(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        if (!TryDecodeRowVersion(rowVersion, out var bytes))
        {
            TempData["Error"] = _text["ErrorConcurrencyConflict"].Value;
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _service.CloseAssessmentAsync(actorId, id, bytes, cancellationToken);
        SetFeedback(result, "SuccessAssessmentClosed");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpGet("{id:guid}/results")]
    public async Task<IActionResult> Results(Guid id, CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        var result = await _service.GetResultsAsync(actorId, id, cancellationToken);

        if (result.Value is null)
        {
            if (result.Error == AssessmentErrorCode.AssessmentNotOpen)
            {
                TempData["Error"] = _text["ErrorAssessmentNotOpen"].Value;
                return RedirectToAction(nameof(Details), new { id });
            }

            return HandleQueryError(result.Error);
        }

        return View(new AssessmentResultsViewModel(result.Value));
    }

    [Authorize(Roles = RoleNames.Teacher)]
    [HttpPost("{id:guid}/results/{studentProfileId:guid}")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    [EnableRateLimiting(BackendResiliencePolicyNames.HeavyWriteConcurrency)]
    public async Task<IActionResult> SaveResult(
        Guid id,
        Guid studentProfileId,
        Guid[] questionIds,
        decimal[] scores,
        string? rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId)) return Forbid();

        byte[]? bytes = null;

        if (!string.IsNullOrWhiteSpace(rowVersion) &&
            !TryDecodeRowVersion(rowVersion, out bytes))
        {
            TempData["Error"] = _text["ErrorConcurrencyConflict"].Value;
            return RedirectToAction(nameof(Results), new { id });
        }

        var result = await _service.SaveStudentResultAsync(
            actorId,
            new SaveStudentAssessmentResultRequest(
                id,
                studentProfileId,
                questionIds,
                scores,
                bytes),
            cancellationToken);

        SetFeedback(result, "SuccessResultSaved");
        return RedirectToAction(nameof(Results), new { id });
    }

    private bool TryActor(out Guid id) =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out id);

    private IActionResult HandleQueryError(AssessmentErrorCode? error) =>
        error == AssessmentErrorCode.AccessDenied ? Forbid() : NotFound();

    private void SetFeedback(AssessmentCommandResult result, string successKey)
    {
        TempData[result.Succeeded ? "Success" : "Error"] =
            result.Succeeded
                ? _text[successKey].Value
                : _text[ErrorKey(result.Error)].Value;
    }

    private static bool TryDecodeRowVersion(string? value, out byte[] bytes)
    {
        bytes = [];
        if (string.IsNullOrWhiteSpace(value)) return false;

        try
        {
            bytes = Convert.FromBase64String(value);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string ErrorKey(AssessmentErrorCode? code) =>
        code switch
        {
            AssessmentErrorCode.AccessDenied => "ErrorAccessDenied",
            AssessmentErrorCode.SchoolNotActive => "ErrorSchoolNotActive",
            AssessmentErrorCode.Required => "ErrorRequired",
            AssessmentErrorCode.InvalidText => "ErrorInvalidText",
            AssessmentErrorCode.InvalidDate => "ErrorInvalidDate",
            AssessmentErrorCode.InvalidMaxScore => "ErrorInvalidMaxScore",
            AssessmentErrorCode.InvalidQuestionScore => "ErrorInvalidQuestionScore",
            AssessmentErrorCode.InvalidOrder => "ErrorInvalidOrder",
            AssessmentErrorCode.TermNotFound => "ErrorTermNotFound",
            AssessmentErrorCode.ClassGroupNotFound => "ErrorClassGroupNotFound",
            AssessmentErrorCode.SubjectNotFound => "ErrorSubjectNotFound",
            AssessmentErrorCode.AssessmentNotFound => "ErrorAssessmentNotFound",
            AssessmentErrorCode.QuestionNotFound => "ErrorQuestionNotFound",
            AssessmentErrorCode.OutcomeNotFound => "ErrorOutcomeNotFound",
            AssessmentErrorCode.StudentNotFound => "ErrorStudentNotFound",
            AssessmentErrorCode.StudentNotEnrolled => "ErrorStudentNotEnrolled",
            AssessmentErrorCode.TeacherNotAssigned => "ErrorTeacherNotAssigned",
            AssessmentErrorCode.DuplicateAssessment => "ErrorDuplicateAssessment",
            AssessmentErrorCode.DuplicateQuestionOrder => "ErrorDuplicateQuestionOrder",
            AssessmentErrorCode.DuplicateOutcomeMapping => "ErrorDuplicateOutcomeMapping",
            AssessmentErrorCode.OutcomeDoesNotMatchAssessment => "ErrorOutcomeDoesNotMatchAssessment",
            AssessmentErrorCode.AssessmentNotDraft => "ErrorAssessmentNotDraft",
            AssessmentErrorCode.AssessmentNotOpen => "ErrorAssessmentNotOpen",
            AssessmentErrorCode.AssessmentAlreadyClosed => "ErrorAssessmentAlreadyClosed",
            AssessmentErrorCode.AssessmentHasNoQuestions => "ErrorAssessmentHasNoQuestions",
            AssessmentErrorCode.AssessmentScoreMismatch => "ErrorAssessmentScoreMismatch",
            AssessmentErrorCode.QuestionMissingOutcome => "ErrorQuestionMissingOutcome",
            AssessmentErrorCode.ResultQuestionMismatch => "ErrorResultQuestionMismatch",
            AssessmentErrorCode.ConcurrencyConflict => "ErrorConcurrencyConflict",
            _ => "ErrorPersistence"
        };
}
