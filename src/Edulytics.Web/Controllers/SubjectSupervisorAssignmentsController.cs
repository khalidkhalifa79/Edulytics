using System.Security.Claims;
using Edulytics.Core.Constants;
using Edulytics.Services.SubjectSupervisors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Roles = RoleNames.SchoolAdmin)]
[Route("school/subject-supervisors")]
public sealed class SubjectSupervisorAssignmentsController
    : Controller
{
    private readonly ISubjectSupervisorAssignmentService
        _service;

    private readonly IStringLocalizer<PlatformResource>
        _text;

    public SubjectSupervisorAssignmentsController(
        ISubjectSupervisorAssignmentService service,
        IStringLocalizer<PlatformResource> text)
    {
        _service = service;
        _text = text;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
            return Forbid();

        var result =
            await _service.GetManagementAsync(
                actorUserId,
                cancellationToken);

        if (result.Value is null)
            return Forbid();

        return View(result.Value);
    }

    [HttpPost("assign")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(
        Guid supervisorUserId,
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
            return Forbid();

        var result =
            await _service.CreateAsync(
                actorUserId,
                supervisorUserId,
                subjectId,
                cancellationToken);

        if (!result.Succeeded)
            return Failure(result.Error!.Value);

        TempData["SubjectSupervisorSuccess"] =
            _text[
                "SubjectSupervisorAssignmentCreated"]
                .Value;

        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{assignmentId:guid}/remove")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(
        Guid assignmentId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
            return Forbid();

        var result =
            await _service.RemoveAsync(
                actorUserId,
                assignmentId,
                cancellationToken);

        if (!result.Succeeded)
            return Failure(result.Error!.Value);

        TempData["SubjectSupervisorSuccess"] =
            _text[
                "SubjectSupervisorAssignmentRemoved"]
                .Value;

        return RedirectToAction(nameof(Index));
    }

    private IActionResult Failure(
        SubjectSupervisorErrorCode error)
    {
        if (error ==
            SubjectSupervisorErrorCode.AccessDenied)
        {
            return Forbid();
        }

        if (error is
            SubjectSupervisorErrorCode.SupervisorNotFound or
            SubjectSupervisorErrorCode.SubjectNotFound or
            SubjectSupervisorErrorCode.AssignmentNotFound)
        {
            return NotFound();
        }

        TempData["SubjectSupervisorError"] =
            _text[
                $"SubjectSupervisorError{error}"]
                .Value;

        return RedirectToAction(nameof(Index));
    }

    private bool TryGetActorId(
        out Guid actorUserId) =>
        Guid.TryParse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier),
            out actorUserId);
}
