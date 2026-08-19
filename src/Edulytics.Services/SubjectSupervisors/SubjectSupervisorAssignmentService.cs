using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;
using Edulytics.Services.Auditing;

namespace Edulytics.Services.SubjectSupervisors;

public sealed class SubjectSupervisorAssignmentService
    : ISubjectSupervisorAssignmentService
{
    private readonly ISubjectSupervisorAssignmentRepository
        _assignments;

    private readonly ISchoolUserRepository _users;
    private readonly ISchoolRepository _schools;
    private readonly IAuditService _audit;

    public SubjectSupervisorAssignmentService(
        ISubjectSupervisorAssignmentRepository assignments,
        ISchoolUserRepository users,
        ISchoolRepository schools,
        IAuditService audit)
    {
        _assignments = assignments;
        _users = users;
        _schools = schools;
        _audit = audit;
    }

    public async Task<SubjectSupervisorManagementResult>
        GetManagementAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (scope.Error.HasValue)
        {
            return SubjectSupervisorManagementResult.Failure(
                scope.Error.Value);
        }

        var school = scope.School!;

        var assignments =
            await _assignments.ListBySchoolAsync(
                school.Id,
                cancellationToken);

        var users =
            await _users.ListBySchoolAsync(
                school.Id,
                cancellationToken);

        var subjects =
            await _assignments.ListSubjectsAsync(
                school.Id,
                cancellationToken);

        var usersById =
            users.ToDictionary(x => x.Id);

        var subjectsById =
            subjects.ToDictionary(x => x.Id);

        var rows = assignments
            .Select(
                assignment =>
                {
                    usersById.TryGetValue(
                        assignment.SupervisorUserId,
                        out var user);

                    subjectsById.TryGetValue(
                        assignment.SubjectId,
                        out var subject);

                    return new SubjectSupervisorAssignmentItem(
                        assignment.Id,
                        assignment.SupervisorUserId,
                        user?.Email ??
                            assignment.SupervisorUserId.ToString("D"),
                        assignment.SubjectId,
                        subject?.Name ??
                            assignment.SubjectId.ToString("D"),
                        subject?.Code ?? string.Empty,
                        assignment.CreatedAtUtc);
                })
            .OrderBy(x => x.SupervisorEmail)
            .ThenBy(x => x.SubjectName)
            .ToArray();

        var supervisorOptions = users
            .Where(
                x =>
                    x.SchoolId == school.Id &&
                    x.IsActive &&
                    !x.IsLocked &&
                    x.Roles.Count == 1 &&
                    x.Roles[0] ==
                        RoleNames.SubjectSupervisor)
            .OrderBy(x => x.Email)
            .Select(
                x =>
                    new SubjectSupervisorUserOption(
                        x.Id,
                        x.Email))
            .ToArray();

        var subjectOptions = subjects
            .Where(
                x =>
                    x.Status ==
                    AcademicStructureStatus.Active)
            .OrderBy(x => x.Name)
            .Select(
                x =>
                    new SubjectSupervisorSubjectOption(
                        x.Id,
                        x.Name,
                        x.Code))
            .ToArray();

        return SubjectSupervisorManagementResult.Success(
            new SubjectSupervisorManagementData(
                school.Id,
                school.Name,
                rows,
                supervisorOptions,
                subjectOptions));
    }

    public async Task<SubjectSupervisorCommandResult>
        CreateAsync(
            Guid actorUserId,
            Guid supervisorUserId,
            Guid subjectId,
            CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (scope.Error.HasValue)
        {
            return SubjectSupervisorCommandResult.Failure(
                scope.Error.Value);
        }

        var schoolId = scope.School!.Id;

        var supervisor =
            await _users.GetBySchoolAndIdAsync(
                schoolId,
                supervisorUserId,
                cancellationToken);

        if (supervisor is null)
        {
            return SubjectSupervisorCommandResult.Failure(
                SubjectSupervisorErrorCode.SupervisorNotFound);
        }

        if (!IsEligibleSupervisor(
                supervisor,
                schoolId))
        {
            return SubjectSupervisorCommandResult.Failure(
                SubjectSupervisorErrorCode.SupervisorNotEligible);
        }

        var subject =
            await _assignments.GetSubjectAsync(
                schoolId,
                subjectId,
                cancellationToken);

        if (subject is null)
        {
            return SubjectSupervisorCommandResult.Failure(
                SubjectSupervisorErrorCode.SubjectNotFound);
        }

        if (subject.Status !=
            AcademicStructureStatus.Active)
        {
            return SubjectSupervisorCommandResult.Failure(
                SubjectSupervisorErrorCode.SubjectInactive);
        }

        if (await _assignments.ExistsAsync(
                schoolId,
                supervisorUserId,
                subjectId,
                cancellationToken))
        {
            return SubjectSupervisorCommandResult.Failure(
                SubjectSupervisorErrorCode.DuplicateAssignment);
        }

        var assignment =
            new SubjectSupervisorAssignment
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                SupervisorUserId = supervisorUserId,
                SubjectId = subjectId,
                CreatedAtUtc = DateTime.UtcNow
            };

        try
        {
            await _assignments.AddAsync(
                assignment,
                cancellationToken);

            await _audit.QueueAsync(
                new AuditEvent(
                    schoolId,
                    "SubjectSupervisorAssignment.Created",
                    "SubjectSupervisorAssignment",
                    assignment.Id.ToString("D"),
                    "SubjectSupervisorManagement",
                    NewValues:
                        new Dictionary<string, object?>
                        {
                            ["SupervisorUserId"] =
                                supervisorUserId,
                            ["SubjectId"] =
                                subjectId
                        },
                    ResultSummary:
                        "Subject supervisor assignment created."),
                cancellationToken);

            if (!await _assignments.SaveAsync(
                    cancellationToken))
            {
                return SubjectSupervisorCommandResult.Failure(
                    SubjectSupervisorErrorCode.PersistenceError);
            }
        }
        catch
        {
            return SubjectSupervisorCommandResult.Failure(
                SubjectSupervisorErrorCode.PersistenceError);
        }

        return SubjectSupervisorCommandResult.Success();
    }

    public async Task<SubjectSupervisorCommandResult>
        RemoveAsync(
            Guid actorUserId,
            Guid assignmentId,
            CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (scope.Error.HasValue)
        {
            return SubjectSupervisorCommandResult.Failure(
                scope.Error.Value);
        }

        var schoolId = scope.School!.Id;

        var assignment =
            await _assignments.GetBySchoolAndIdAsync(
                schoolId,
                assignmentId,
                cancellationToken);

        if (assignment is null)
        {
            return SubjectSupervisorCommandResult.Failure(
                SubjectSupervisorErrorCode.AssignmentNotFound);
        }

        try
        {
            _assignments.Remove(assignment);

            await _audit.QueueAsync(
                new AuditEvent(
                    schoolId,
                    "SubjectSupervisorAssignment.Removed",
                    "SubjectSupervisorAssignment",
                    assignment.Id.ToString("D"),
                    "SubjectSupervisorManagement",
                    OldValues:
                        new Dictionary<string, object?>
                        {
                            ["SupervisorUserId"] =
                                assignment.SupervisorUserId,
                            ["SubjectId"] =
                                assignment.SubjectId
                        },
                    ResultSummary:
                        "Subject supervisor assignment removed."),
                cancellationToken);

            if (!await _assignments.SaveAsync(
                    cancellationToken))
            {
                return SubjectSupervisorCommandResult.Failure(
                    SubjectSupervisorErrorCode.PersistenceError);
            }
        }
        catch
        {
            return SubjectSupervisorCommandResult.Failure(
                SubjectSupervisorErrorCode.PersistenceError);
        }

        return SubjectSupervisorCommandResult.Success();
    }

    private async Task<ManagementScope> ResolveScopeAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var actor =
            await _users.GetActorAsync(
                actorUserId,
                cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            !actor.SchoolId.HasValue ||
            actor.Roles.Count != 1 ||
            actor.Roles[0] !=
                RoleNames.SchoolAdmin)
        {
            return ManagementScope.Fail(
                SubjectSupervisorErrorCode.AccessDenied);
        }

        var school =
            await _schools.GetByIdAsync(
                actor.SchoolId.Value,
                cancellationToken);

        if (school is null ||
            school.Status != SchoolStatus.Active)
        {
            return ManagementScope.Fail(
                SubjectSupervisorErrorCode.AccessDenied);
        }

        return ManagementScope.Ok(
            actor,
            school);
    }

    private static bool IsEligibleSupervisor(
        SchoolUserRecord user,
        Guid schoolId) =>
        user.SchoolId == schoolId &&
        user.IsActive &&
        !user.IsLocked &&
        user.Roles.Count == 1 &&
        user.Roles[0] ==
            RoleNames.SubjectSupervisor;

    private sealed record ManagementScope(
        SchoolUserRecord? Actor,
        School? School,
        SubjectSupervisorErrorCode? Error)
    {
        public static ManagementScope Ok(
            SchoolUserRecord actor,
            School school) =>
            new(actor, school, null);

        public static ManagementScope Fail(
            SubjectSupervisorErrorCode error) =>
            new(null, null, error);
    }
}
