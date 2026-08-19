using Edulytics.Core.Entities;

namespace Edulytics.Core.Interfaces;

public interface ISubjectSupervisorAssignmentRepository
{
    Task<IReadOnlyList<SubjectSupervisorAssignment>>
        ListBySchoolAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SubjectSupervisorAssignment>>
        ListActiveBySupervisorAsync(
            Guid schoolId,
            Guid supervisorUserId,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Subject>> ListSubjectsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<Subject?> GetSubjectAsync(
        Guid schoolId,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    Task<SubjectSupervisorAssignment?>
        GetBySchoolAndIdAsync(
            Guid schoolId,
            Guid assignmentId,
            CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid schoolId,
        Guid supervisorUserId,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        SubjectSupervisorAssignment assignment,
        CancellationToken cancellationToken = default);

    void Remove(
        SubjectSupervisorAssignment assignment);

    Task<bool> SaveAsync(
        CancellationToken cancellationToken = default);
}
