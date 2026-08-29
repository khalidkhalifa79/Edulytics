using System.Security.Claims;
using Edulytics.Core.Academics;
using Edulytics.Core.Constants;
using Edulytics.Core.Enums;
using Edulytics.Services.Academics;
using Edulytics.Web.ViewModels.Academics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "SchoolAccess")]
[Route("school/academic-structure")]
public sealed class AcademicStructureController : Controller
{
    private readonly IAcademicStructureService _academic;
    private readonly IStringLocalizer<AcademicResource> _text;

    public AcademicStructureController(
        IAcademicStructureService academic,
        IStringLocalizer<AcademicResource> text)
    {
        _academic = academic;
        _text = text;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
            return Forbid();

        var result = await _academic.GetDashboardAsync(actorUserId, cancellationToken);
        return result.Value is not null ? View(result.Value) : Forbid();
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("academic-years")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateAcademicYear(
        string name, DateOnly startsOn, DateOnly endsOn,
        AcademicStructureStatus status,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            id => _academic.CreateAcademicYearAsync(
                id,
                new CreateAcademicYearRequest(name, startsOn, endsOn, status),
                cancellationToken),
            "SuccessAcademicYearCreated");

    [HttpGet("academic-years/{id:guid}/edit")]
    public async Task<IActionResult> EditAcademicYear(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
            return Forbid();

        var result = await _academic.GetAcademicYearAsync(
            actorUserId, id, cancellationToken);

        return result.Value is null
            ? NotFound()
            : View(new AcademicYearEditViewModel { Year = result.Value });
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("academic-years/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> EditAcademicYear(
        Guid id, string name, DateOnly startsOn, DateOnly endsOn,
        AcademicStructureStatus status, string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(rowVersion, out var expected))
            return Task.FromResult(RedirectWithError("ErrorConcurrencyConflict"));

        return ExecuteAsync(
            actorId => _academic.UpdateAcademicYearAsync(
                actorId,
                new UpdateAcademicYearRequest(
                    id, name, startsOn, endsOn, status, expected),
                cancellationToken),
            "SuccessAcademicYearUpdated");
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("terms")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateTerm(
        Guid academicYearId, string name, DateOnly startsOn, DateOnly endsOn,
        AcademicStructureStatus status,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            id => _academic.CreateTermAsync(
                id,
                new CreateTermRequest(
                    academicYearId, name, startsOn, endsOn, status),
                cancellationToken),
            "SuccessTermCreated");

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("grade-levels")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateGradeLevel(
        string name, int order,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            id => _academic.CreateGradeLevelAsync(
                id, new CreateGradeLevelRequest(name, order), cancellationToken),
            "SuccessGradeCreated");

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("academic-programs")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateAcademicProgram(
        Guid academicYearId,
        string programChoice,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            id =>
                _academic.OfferAcademicProgramAsync(
                    id,
                    new OfferAcademicProgramRequest(
                        academicYearId,
                        programChoice),
                    cancellationToken),
            "SuccessAcademicProgramOffered");

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost(
        "academic-programs/{academicProgramId:guid}/years/" +
        "{academicYearId:guid}/stop")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> StopAcademicProgramForYear(
        Guid academicProgramId,
        Guid academicYearId,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                rowVersion,
                out var expected))
        {
            return Task.FromResult(
                RedirectWithError(
                    "ErrorConcurrencyConflict"));
        }

        return ExecuteAsync(
            id =>
                _academic
                    .StopAcademicProgramOfferingAsync(
                        id,
                        new StopAcademicProgramOfferingRequest(
                            academicYearId,
                            academicProgramId,
                            expected),
                        cancellationToken),
            "SuccessAcademicProgramStopped");
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("classes")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateClassGroup(
        Guid academicYearId,
        Guid academicProgramId,
        Guid gradeLevelId,
        string name,
        AcademicStructureStatus status,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            id => _academic.CreateClassGroupAsync(
                id,
                new CreateClassGroupRequest(
                    academicYearId,
                    gradeLevelId,
                    name,
                    string.Empty,
                    status,
                    academicProgramId),
                cancellationToken),
            "SuccessClassCreated");

    [HttpGet("classes/{id:guid}/edit")]
    public async Task<IActionResult> EditClassGroup(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
            return Forbid();

        var item = await _academic.GetClassGroupAsync(
            actorUserId, id, cancellationToken);

        var dashboard = await _academic.GetDashboardAsync(
            actorUserId, cancellationToken);

        if (item.Value is null || dashboard.Value is null)
            return NotFound();

        return View(new ClassGroupEditViewModel
        {
            ClassGroup = item.Value,
            GradeLevels = dashboard.Value.GradeLevels,
            AcademicPrograms =
                dashboard.Value.AcademicPrograms
                    .Where(
                        x =>
                            x.Id ==
                                item.Value.AcademicProgramId ||
                            dashboard.Value
                                .AcademicYearProgramOfferings
                                .Any(
                                    offering =>
                                        offering.AcademicYearId ==
                                            item.Value.AcademicYearId &&
                                        offering.AcademicProgramId ==
                                            x.Id &&
                                        offering.IsOffered))
                    .OrderBy(x => x.Name)
                    .ToArray()
        });
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("classes/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> EditClassGroup(
        Guid id,
        Guid academicProgramId,
        Guid gradeLevelId,
        string name,
        AcademicStructureStatus status,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(rowVersion, out var expected))
            return Task.FromResult(RedirectWithError("ErrorConcurrencyConflict"));

        return ExecuteAsync(
            actorId => _academic.UpdateClassGroupAsync(
                actorId,
                new UpdateClassGroupRequest(
                    id,
                    gradeLevelId,
                    name,
                    string.Empty,
                    status,
                    expected,
                    academicProgramId),
                cancellationToken),
            "SuccessClassUpdated");
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("subjects")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateSubject(
        string name, string code, AcademicStructureStatus status,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            id => _academic.CreateSubjectAsync(
                id, new CreateSubjectRequest(name, code, status), cancellationToken),
            "SuccessSubjectCreated");

    [HttpGet("subjects/{id:guid}/edit")]
    public async Task<IActionResult> EditSubject(
        Guid id,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
            return Forbid();

        var result = await _academic.GetSubjectAsync(
            actorUserId, id, cancellationToken);

        return result.Value is null
            ? NotFound()
            : View(new SubjectEditViewModel { Subject = result.Value });
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("subjects/{id:guid}/edit")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> EditSubject(
        Guid id, string name, string code, AcademicStructureStatus status,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(rowVersion, out var expected))
            return Task.FromResult(RedirectWithError("ErrorConcurrencyConflict"));

        return ExecuteAsync(
            actorId => _academic.UpdateSubjectAsync(
                actorId,
                new UpdateSubjectRequest(id, name, code, status, expected),
                cancellationToken),
            "SuccessSubjectUpdated");
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("teacher-assignments")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateTeacherAssignment(
        Guid teacherUserId, Guid classGroupId, Guid subjectId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            id => _academic.CreateTeacherAssignmentAsync(
                id,
                new CreateTeacherAssignmentRequest(
                    teacherUserId, classGroupId, subjectId),
                cancellationToken),
            "SuccessTeacherAssigned");

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("students")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateStudentProfile(
        string studentNumber, string firstName, string lastName,
        Guid? userId, AcademicStructureStatus status,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            id => _academic.CreateStudentProfileAsync(
                id,
                new CreateStudentProfileRequest(
                    studentNumber, firstName, lastName, userId, status),
                cancellationToken),
            "SuccessStudentCreated");

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("students/{id:guid}/archive")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ArchiveStudentProfile(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(rowVersion, out var expected))
            return Task.FromResult(RedirectWithError("ErrorConcurrencyConflict"));

        return ExecuteAsync(
            actorId => _academic.ArchiveStudentProfileAsync(
                actorId,
                id,
                expected,
                cancellationToken),
            "SuccessStudentArchived");
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("students/{id:guid}/restore")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RestoreStudentProfile(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(rowVersion, out var expected))
            return Task.FromResult(RedirectWithError("ErrorConcurrencyConflict"));

        return ExecuteAsync(
            actorId => _academic.RestoreStudentProfileAsync(
                actorId,
                id,
                expected,
                cancellationToken),
            "SuccessStudentRestored");
    }

    [Authorize(Roles = RoleNames.SubjectSupervisor)]
    [HttpPost("student-enrollments")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> CreateStudentEnrollment(
        Guid studentProfileId, Guid classGroupId,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            id => _academic.CreateStudentEnrollmentAsync(
                id,
                new CreateStudentEnrollmentRequest(
                    studentProfileId, classGroupId),
                cancellationToken),
            "SuccessStudentEnrolled");

    private async Task<IActionResult> ExecuteAsync(
        Func<Guid, Task<AcademicCommandResult>> action,
        string successKey)
    {
        if (!TryGetActorId(out var actorUserId))
            return Forbid();

        var result = await action(actorUserId);

        if (result.Succeeded)
        {
            TempData["AcademicSuccess"] = _text[successKey].Value;
        }
        else
        {
            var code = result.Errors.FirstOrDefault()?.Code ??
                AcademicStructureErrorCode.PersistenceError;
            TempData["AcademicError"] = _text[$"Error{code}"].Value;
        }

        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectWithError(string key)
    {
        TempData["AcademicError"] = _text[key].Value;
        return RedirectToAction(nameof(Index));
    }

    private bool TryGetActorId(out Guid userId) =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out userId);

    private static bool TryDecodeRowVersion(
        string? value,
        out byte[] rowVersion)
    {
        try
        {
            rowVersion = Convert.FromBase64String(value ?? string.Empty);
            return rowVersion.Length > 0;
        }
        catch (FormatException)
        {
            rowVersion = [];
            return false;
        }
    }
}
