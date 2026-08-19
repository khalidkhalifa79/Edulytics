using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class SubjectSupervisorAssignmentRepository
    : ISubjectSupervisorAssignmentRepository
{
    private readonly EdulyticsDbContext _db;

    public SubjectSupervisorAssignmentRepository(
        EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SubjectSupervisorAssignment>>
        ListBySchoolAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
        await _db.SubjectSupervisorAssignments
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<SubjectSupervisorAssignment>>
        ListActiveBySupervisorAsync(
            Guid schoolId,
            Guid supervisorUserId,
            CancellationToken cancellationToken = default) =>
        await _db.SubjectSupervisorAssignments
            .AsNoTracking()
            .Where(
                x =>
                    x.SchoolId == schoolId &&
                    x.SupervisorUserId == supervisorUserId &&
                    _db.Subjects.Any(
                        subject =>
                            subject.SchoolId == schoolId &&
                            subject.Id == x.SubjectId &&
                            subject.Status ==
                                AcademicStructureStatus.Active))
            .OrderBy(x => x.SubjectId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Subject>> ListSubjectsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        await _db.Subjects
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

    public Task<Subject?> GetSubjectAsync(
        Guid schoolId,
        Guid subjectId,
        CancellationToken cancellationToken = default) =>
        _db.Subjects
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.Id == subjectId,
                cancellationToken);

    public Task<SubjectSupervisorAssignment?>
        GetBySchoolAndIdAsync(
            Guid schoolId,
            Guid assignmentId,
            CancellationToken cancellationToken = default) =>
        _db.SubjectSupervisorAssignments
            .SingleOrDefaultAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.Id == assignmentId,
                cancellationToken);

    public Task<bool> ExistsAsync(
        Guid schoolId,
        Guid supervisorUserId,
        Guid subjectId,
        CancellationToken cancellationToken = default) =>
        _db.SubjectSupervisorAssignments
            .AnyAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.SupervisorUserId == supervisorUserId &&
                    x.SubjectId == subjectId,
                cancellationToken);

    public Task AddAsync(
        SubjectSupervisorAssignment assignment,
        CancellationToken cancellationToken = default) =>
        _db.SubjectSupervisorAssignments.AddAsync(
                assignment,
                cancellationToken)
            .AsTask();

    public void Remove(
        SubjectSupervisorAssignment assignment) =>
        _db.SubjectSupervisorAssignments.Remove(
            assignment);

    public async Task<bool> SaveAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(
                cancellationToken);

            return true;
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return false;
        }
    }
}
