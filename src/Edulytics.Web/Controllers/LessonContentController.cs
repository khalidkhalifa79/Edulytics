using System.Security.Claims;
using Edulytics.Core.Constants;
using Edulytics.Core.Enums;
using Edulytics.Services.LessonContent;
using Edulytics.Web.ViewModels.LessonContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Roles = RoleNames.SchoolAdmin + "," + RoleNames.SubjectSupervisor + "," + RoleNames.Teacher)]
[Route("lesson-content")]
public sealed class LessonContentController : Controller
{
    private readonly ILessonContentService _lessons;
    private readonly IStringLocalizer<LessonContentResource> _text;

    public LessonContentController(
        ILessonContentService lessons,
        IStringLocalizer<LessonContentResource> text)
    {
        _lessons = lessons;
        _text = text;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await _lessons.GetDashboardAsync(actorId, cancellationToken);
        if (result.Value is null)
            return HandleQueryError(result.Error);

        return View(new LessonContentIndexViewModel(
            result.Value,
            User.IsInRole(RoleNames.SubjectSupervisor)));
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(
        Guid topicId,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await _lessons.GetCreateEditorAsync(actorId, topicId, cancellationToken);
        if (result.Value is null)
            return HandleQueryError(result.Error);

        return View("Editor", LessonContentEditorViewModel.From(result.Value, true));
    }

    [HttpPost("create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        LessonContentEditorViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        if (!ModelState.IsValid)
            return await ReloadCreateAsync(actorId, model, cancellationToken);

        var result = await _lessons.CreateAsync(actorId, model.ToCreateRequest(), cancellationToken);
        if (!result.Succeeded)
        {
            AddCommandError(result);
            return await ReloadCreateAsync(actorId, model, cancellationToken);
        }

        TempData["LessonContentMessage"] = _text["SavedAsDraft"].Value;
        return RedirectToAction(nameof(Edit), new { id = result.LessonId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Edit(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await _lessons.GetEditEditorAsync(actorId, id, cancellationToken);
        if (result.Value is null)
            return HandleQueryError(result.Error);

        return View(
            "Editor",
            LessonContentEditorViewModel.From(
                result.Value,
                User.IsInRole(RoleNames.SubjectSupervisor)));
    }

    [HttpPost("{id:guid}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        LessonContentEditorViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        model.LessonId = id;
        var result = await _lessons.UpdateDraftAsync(actorId, model.ToUpdateRequest(), cancellationToken);
        if (!result.Succeeded)
        {
            AddCommandError(result);
            return await ReloadEditAsync(actorId, id, model, cancellationToken);
        }

        TempData["LessonContentMessage"] = _text["DraftUpdated"].Value;
        return RedirectToAction(nameof(Edit), new { id });
    }

    [HttpPost("{id:guid}/submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Submit(
        Guid id,
        CancellationToken cancellationToken) =>
        await TransitionAsync(
            id,
            _lessons.SubmitForReviewAsync,
            "SubmittedForReview",
            cancellationToken);

    [HttpPost("{id:guid}/return-draft")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ReturnDraft(
        Guid id,
        CancellationToken cancellationToken) =>
        await TransitionAsync(
            id,
            _lessons.ReturnToDraftAsync,
            "ReturnedToDraft",
            cancellationToken);

    [HttpPost("{id:guid}/publish")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Publish(
        Guid id,
        CancellationToken cancellationToken) =>
        await TransitionAsync(
            id,
            _lessons.PublishAsync,
            "Published",
            cancellationToken);

    private async Task<IActionResult> TransitionAsync(
        Guid id,
        Func<Guid, Guid, CancellationToken, Task<LessonContentCommandResult>> operation,
        string messageKey,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await operation(actorId, id, cancellationToken);
        if (!result.Succeeded)
        {
            TempData["LessonContentError"] = ErrorText(result.Error);
            return RedirectToAction(nameof(Edit), new { id });
        }

        TempData["LessonContentMessage"] = _text[messageKey].Value;
        return RedirectToAction(nameof(Edit), new { id });
    }

    private async Task<IActionResult> ReloadCreateAsync(
        Guid actorId,
        LessonContentEditorViewModel posted,
        CancellationToken cancellationToken)
    {
        var query = await _lessons.GetCreateEditorAsync(actorId, posted.TopicId, cancellationToken);
        if (query.Value is null)
            return HandleQueryError(query.Error);

        posted.AvailableOutcomes = query.Value.Topic.Outcomes;
        posted.FrameworkName = query.Value.Topic.FrameworkName;
        posted.FrameworkVersionName = query.Value.Topic.FrameworkVersionName;
        posted.SubjectName = query.Value.Topic.SubjectName;
        posted.SubjectCode = query.Value.Topic.SubjectCode;
        posted.GradeName = query.Value.Topic.GradeName;
        posted.TopicName = query.Value.Topic.TopicName;
        posted.IsNew = true;
        posted.CanAuthor = true;
        posted.Status = LearningLessonStatus.Draft;
        return View("Editor", posted);
    }

    private async Task<IActionResult> ReloadEditAsync(
        Guid actorId,
        Guid id,
        LessonContentEditorViewModel posted,
        CancellationToken cancellationToken)
    {
        var query = await _lessons.GetEditEditorAsync(actorId, id, cancellationToken);
        if (query.Value is null)
            return HandleQueryError(query.Error);

        posted.AvailableOutcomes = query.Value.Topic.Outcomes;
        posted.FrameworkName = query.Value.Topic.FrameworkName;
        posted.FrameworkVersionName = query.Value.Topic.FrameworkVersionName;
        posted.SubjectName = query.Value.Topic.SubjectName;
        posted.SubjectCode = query.Value.Topic.SubjectCode;
        posted.GradeName = query.Value.Topic.GradeName;
        posted.TopicName = query.Value.Topic.TopicName;
        posted.IsNew = false;
        posted.CanAuthor = User.IsInRole(RoleNames.SubjectSupervisor);
        posted.Status = query.Value.Status;
        return View("Editor", posted);
    }

    private void AddCommandError(LessonContentCommandResult result) =>
        ModelState.AddModelError(
            string.IsNullOrWhiteSpace(result.Field) ? string.Empty : result.Field,
            ErrorText(result.Error));

    private string ErrorText(LessonContentErrorCode? error) =>
        _text[error switch
        {
            LessonContentErrorCode.InvalidOrder => "ErrorInvalidOrder",
            LessonContentErrorCode.DuplicateOrder => "ErrorDuplicateOrder",
            LessonContentErrorCode.OutcomeRequired => "ErrorOutcomeRequired",
            LessonContentErrorCode.OutcomeNotInTopic => "ErrorOutcomeNotInTopic",
            LessonContentErrorCode.EnglishTitleRequired => "ErrorEnglishTitleRequired",
            LessonContentErrorCode.EnglishContentIncomplete => "ErrorEnglishContentIncomplete",
            LessonContentErrorCode.InvalidState => "ErrorInvalidState",
            LessonContentErrorCode.PublishedImmutable => "ErrorPublishedImmutable",
            LessonContentErrorCode.ConcurrencyConflict => "ErrorConcurrencyConflict",
            LessonContentErrorCode.ConstraintViolation => "ErrorConstraintViolation",
            _ => "ErrorGeneric"
        }].Value;

    private IActionResult HandleQueryError(LessonContentErrorCode? error) =>
        error is LessonContentErrorCode.AccessDenied or LessonContentErrorCode.SchoolNotActive
            ? Forbid()
            : NotFound();

    private bool TryActor(out Guid actorUserId) =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out actorUserId);
}
