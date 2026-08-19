namespace Edulytics.Services.SubjectSupervisors;

public enum SubjectSupervisorErrorCode
{
    AccessDenied,
    SupervisorNotFound,
    SupervisorNotEligible,
    SubjectNotFound,
    SubjectInactive,
    DuplicateAssignment,
    AssignmentNotFound,
    PersistenceError
}

public sealed record SubjectSupervisorUserOption(
    Guid Id,
    string Email);

public sealed record SubjectSupervisorSubjectOption(
    Guid Id,
    string Name,
    string Code);

public sealed record SubjectSupervisorAssignmentItem(
    Guid Id,
    Guid SupervisorUserId,
    string SupervisorEmail,
    Guid SubjectId,
    string SubjectName,
    string SubjectCode,
    DateTime CreatedAtUtc);

public sealed record SubjectSupervisorManagementData(
    Guid SchoolId,
    string SchoolName,
    IReadOnlyList<SubjectSupervisorAssignmentItem> Assignments,
    IReadOnlyList<SubjectSupervisorUserOption> Supervisors,
    IReadOnlyList<SubjectSupervisorSubjectOption> Subjects);

public sealed record SubjectSupervisorManagementResult(
    SubjectSupervisorManagementData? Value,
    SubjectSupervisorErrorCode? Error)
{
    public static SubjectSupervisorManagementResult Success(
        SubjectSupervisorManagementData value) =>
        new(value, null);

    public static SubjectSupervisorManagementResult Failure(
        SubjectSupervisorErrorCode error) =>
        new(null, error);
}

public sealed record SubjectSupervisorCommandResult(
    bool Succeeded,
    SubjectSupervisorErrorCode? Error)
{
    public static SubjectSupervisorCommandResult Success() =>
        new(true, null);

    public static SubjectSupervisorCommandResult Failure(
        SubjectSupervisorErrorCode error) =>
        new(false, error);
}

public interface ISubjectSupervisorAssignmentService
{
    Task<SubjectSupervisorManagementResult> GetManagementAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SubjectSupervisorCommandResult> CreateAsync(
        Guid actorUserId,
        Guid supervisorUserId,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    Task<SubjectSupervisorCommandResult> RemoveAsync(
        Guid actorUserId,
        Guid assignmentId,
        CancellationToken cancellationToken = default);
}
