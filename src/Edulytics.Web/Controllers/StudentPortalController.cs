using System.Security.Claims;
using Edulytics.Services.Notifications;
using Edulytics.Services.StudentPortal;
using Edulytics.Web.ViewModels.StudentPortal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "StudentPortal")]
[Route("student")]
public sealed class StudentPortalController : Controller
{
    private readonly IStudentPortalService _portal;
    private readonly INotificationService _notifications;

    public StudentPortalController(
        IStudentPortalService portal,
        INotificationService notifications)
    {
        _portal = portal;
        _notifications = notifications;
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var workspace =
            await _portal.GetWorkspaceAsync(
                actorId,
                cancellationToken);

        if (workspace.Value is null)
            return HandlePortalError(workspace.Error);

        var notifications =
            await _notifications.ListInboxAsync(
                actorId,
                cancellationToken);

        return View(
            new StudentDashboardViewModel(
                workspace.Value,
                notifications.Value ?? []));
    }

    [HttpGet("learning")]
    public async Task<IActionResult> Learning(
        CancellationToken cancellationToken)
    {
        var workspace =
            await WorkspaceAsync(cancellationToken);

        return workspace.Result ??
            View(
                nameof(Learning),
                new StudentLearningViewModel(
                    workspace.Workspace!));
    }

    [HttpGet("assessments")]
    public async Task<IActionResult> Assessments(
        CancellationToken cancellationToken)
    {
        var workspace =
            await WorkspaceAsync(cancellationToken);

        return workspace.Result ??
            View(
                nameof(Assessments),
                new StudentAssessmentsViewModel(
                    workspace.Workspace!));
    }

    [HttpGet("results")]
    public async Task<IActionResult> Results(
        CancellationToken cancellationToken)
    {
        var workspace =
            await WorkspaceAsync(cancellationToken);

        return workspace.Result ??
            View(
                nameof(Results),
                new StudentResultsViewModel(
                    workspace.Workspace!));
    }

    [HttpGet("notifications")]
    public async Task<IActionResult> Notifications(
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var workspace =
            await _portal.GetWorkspaceAsync(
                actorId,
                cancellationToken);

        if (workspace.Value is null)
            return HandlePortalError(workspace.Error);

        var notifications =
            await _notifications.ListInboxAsync(
                actorId,
                cancellationToken);

        if (notifications.Value is null)
            return Forbid();

        return View(
            new StudentNotificationsViewModel(
                workspace.Value,
                notifications.Value));
    }

    [HttpPost("notifications/{id:guid}/read")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetNotificationReadState(
        Guid id,
        bool isRead,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var workspace =
            await _portal.GetWorkspaceAsync(
                actorId,
                cancellationToken);

        if (workspace.Value is null)
            return HandlePortalError(workspace.Error);

        var result =
            await _notifications.SetReadStateAsync(
                actorId,
                id,
                isRead,
                cancellationToken);

        if (result.Value is null)
            return Forbid();

        return RedirectToAction(nameof(Notifications));
    }

    private async Task<(
        StudentPortalWorkspace? Workspace,
        IActionResult? Result)> WorkspaceAsync(
            CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return (null, Forbid());

        var workspace =
            await _portal.GetWorkspaceAsync(
                actorId,
                cancellationToken);

        return workspace.Value is null
            ? (null, HandlePortalError(workspace.Error))
            : (workspace.Value, null);
    }

    private IActionResult HandlePortalError(
        StudentPortalErrorCode? error) =>
        error switch
        {
            StudentPortalErrorCode.AccessDenied =>
                Forbid(),

            StudentPortalErrorCode.ProfileNotLinked =>
                Forbid(),

            StudentPortalErrorCode.SchoolNotActive =>
                Forbid(),

            _ =>
                NotFound()
        };

    private bool TryActor(out Guid actorUserId) =>
        Guid.TryParse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier),
            out actorUserId);
}
