using System.Data;
using Edulytics.Core.Academics;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class AcademicStructureRepository : IAcademicStructureRepository
{
    private readonly EdulyticsDbContext _db;

    public AcademicStructureRepository(EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<AcademicStructureSnapshot> GetSnapshotAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var years = await _db.AcademicYears.AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.StartsOn)
            .ThenBy(x => x.Name)
            .ToArrayAsync(cancellationToken);

        var terms = await _db.Terms.AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.StartsOn)
            .ThenBy(x => x.Name)
            .ToArrayAsync(cancellationToken);

        var grades = await _db.GradeLevels.AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name)
            .ToArrayAsync(cancellationToken);

        var classes = await _db.ClassGroups.AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Code)
            .ToArrayAsync(cancellationToken);

        var subjects = await _db.Subjects.AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Name)
            .ToArrayAsync(cancellationToken);

        var students = await _db.StudentProfiles.AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.DisplayName)
            .ToArrayAsync(cancellationToken);

        var assignments = await _db.TeacherAssignments.AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

        var enrollments = await _db.StudentEnrollments.AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.EnrolledAtUtc)
            .ToArrayAsync(cancellationToken);

        return new AcademicStructureSnapshot(
            years, terms, grades, classes, subjects, students, assignments, enrollments);
    }

    public Task<AcademicYear?> GetAcademicYearAsync(
        Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.AcademicYears.SingleOrDefaultAsync(
            x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<GradeLevel?> GetGradeLevelAsync(
        Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.GradeLevels.SingleOrDefaultAsync(
            x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<ClassGroup?> GetClassGroupAsync(
        Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.ClassGroups.SingleOrDefaultAsync(
            x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<Subject?> GetSubjectAsync(
        Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.Subjects.SingleOrDefaultAsync(
            x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<StudentProfile?> GetStudentProfileAsync(
        Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.StudentProfiles.SingleOrDefaultAsync(
            x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<bool> AcademicYearNameExistsAsync(
        Guid schoolId, string normalizedName, Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        _db.AcademicYears.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.Name.ToUpper() == normalizedName &&
                 (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task<bool> TermNameExistsAsync(
        Guid schoolId, Guid academicYearId, string normalizedName,
        CancellationToken cancellationToken = default) =>
        _db.Terms.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.AcademicYearId == academicYearId &&
                 x.Name.ToUpper() == normalizedName,
            cancellationToken);

    public Task<bool> GradeLevelNameExistsAsync(
        Guid schoolId, string normalizedName,
        CancellationToken cancellationToken = default) =>
        _db.GradeLevels.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.Name.ToUpper() == normalizedName,
            cancellationToken);

    public Task<bool> GradeLevelOrderExistsAsync(
        Guid schoolId, int order,
        CancellationToken cancellationToken = default) =>
        _db.GradeLevels.AnyAsync(
            x => x.SchoolId == schoolId && x.Order == order,
            cancellationToken);

    public Task<bool> ClassCodeExistsAsync(
        Guid schoolId, Guid academicYearId, string normalizedCode,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        _db.ClassGroups.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.AcademicYearId == academicYearId &&
                 x.NormalizedCode == normalizedCode &&
                 (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task<bool> SubjectCodeExistsAsync(
        Guid schoolId, string normalizedCode, Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        _db.Subjects.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.NormalizedCode == normalizedCode &&
                 (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task<bool> StudentNumberExistsAsync(
        Guid schoolId, string normalizedStudentNumber,
        CancellationToken cancellationToken = default) =>
        _db.StudentProfiles.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.NormalizedStudentNumber == normalizedStudentNumber,
            cancellationToken);

    public Task<bool> StudentUserLinkExistsAsync(
        Guid schoolId, Guid userId,
        CancellationToken cancellationToken = default) =>
        _db.StudentProfiles.AnyAsync(
            x => x.SchoolId == schoolId && x.UserId == userId,
            cancellationToken);

    public Task<bool> TeacherAssignmentExistsAsync(
        Guid schoolId, Guid teacherUserId, Guid classGroupId, Guid subjectId,
        CancellationToken cancellationToken = default) =>
        _db.TeacherAssignments.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.TeacherUserId == teacherUserId &&
                 x.ClassGroupId == classGroupId &&
                 x.SubjectId == subjectId,
            cancellationToken);

    public Task<bool> StudentEnrollmentExistsAsync(
        Guid schoolId, Guid academicYearId, Guid studentProfileId,
        CancellationToken cancellationToken = default) =>
        _db.StudentEnrollments.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.AcademicYearId == academicYearId &&
                 x.StudentProfileId == studentProfileId,
            cancellationToken);

    public async Task<AcademicPersistenceResult>
        AddStudentProfileWithSeatGuardAsync(
            StudentProfile student,
            CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational())
        {
            if (!await HasSeatCapacityAsync(
                    student.SchoolId,
                    student,
                    additionalSeats: 1,
                    lockSubscription: false,
                    cancellationToken))
            {
                _db.ChangeTracker.Clear();
                return AcademicPersistenceResult.Failure(
                    AcademicPersistenceError.SeatLimit);
            }

            _db.StudentProfiles.Add(student);
            return await SaveAsync(cancellationToken);
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            if (!await HasSeatCapacityAsync(
                    student.SchoolId,
                    student,
                    additionalSeats: 1,
                    lockSubscription: true,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                _db.ChangeTracker.Clear();
                return AcademicPersistenceResult.Failure(
                    AcademicPersistenceError.SeatLimit);
            }

            _db.StudentProfiles.Add(student);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AcademicPersistenceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            return AcademicPersistenceResult.Failure(
                AcademicPersistenceError.Conflict);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            return AcademicPersistenceResult.Failure(
                AcademicPersistenceError.Constraint);
        }
    }

    public async Task<AcademicPersistenceResult>
        SaveStudentArchiveStateWithSeatGuardAsync(
            StudentProfile student,
            byte[] expectedRowVersion,
            bool restoring,
            CancellationToken cancellationToken = default)
    {
        if (expectedRowVersion.Length == 0)
        {
            _db.ChangeTracker.Clear();
            return AcademicPersistenceResult.Failure(
                AcademicPersistenceError.Conflict);
        }

        if (!_db.Database.IsRelational())
        {
            if (restoring &&
                !await HasSeatCapacityAsync(
                    student.SchoolId,
                    student,
                    additionalSeats: 1,
                    lockSubscription: false,
                    cancellationToken))
            {
                _db.ChangeTracker.Clear();
                return AcademicPersistenceResult.Failure(
                    AcademicPersistenceError.SeatLimit);
            }

            _db.Entry(student)
                .Property(x => x.RowVersion)
                .OriginalValue = expectedRowVersion;

            return await SaveAsync(cancellationToken);
        }

        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            if (restoring &&
                !await HasSeatCapacityAsync(
                    student.SchoolId,
                    student,
                    additionalSeats: 1,
                    lockSubscription: true,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                _db.ChangeTracker.Clear();
                return AcademicPersistenceResult.Failure(
                    AcademicPersistenceError.SeatLimit);
            }

            _db.Entry(student)
                .Property(x => x.RowVersion)
                .OriginalValue = expectedRowVersion;

            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return AcademicPersistenceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            return AcademicPersistenceResult.Failure(
                AcademicPersistenceError.Conflict);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _db.ChangeTracker.Clear();
            return AcademicPersistenceResult.Failure(
                AcademicPersistenceError.Constraint);
        }
    }

    private async Task<bool> HasSeatCapacityAsync(
        Guid schoolId,
        StudentProfile student,
        int additionalSeats,
        bool lockSubscription,
        CancellationToken cancellationToken)
    {
        if (student.Status != AcademicStructureStatus.Active ||
            student.IsArchived)
        {
            return true;
        }

        SchoolSubscription? subscription;

        if (lockSubscription)
        {
            subscription =
                await _db.SchoolSubscriptions
                    .FromSqlInterpolated(
                        $@"SELECT *
FROM ""SchoolSubscriptions""
WHERE ""SchoolId"" = {schoolId}
FOR UPDATE")
                    .SingleOrDefaultAsync(cancellationToken);
        }
        else
        {
            subscription =
                await _db.SchoolSubscriptions
                    .SingleOrDefaultAsync(
                        x => x.SchoolId == schoolId,
                        cancellationToken);
        }

        if (subscription is null)
            return true;

        var now = DateTime.UtcNow;

        if (subscription.Status != SubscriptionStatus.Active ||
            !subscription.CurrentTermStartsAtUtc.HasValue ||
            !subscription.CurrentTermEndsAtUtc.HasValue ||
            subscription.CurrentTermStartsAtUtc.Value > now ||
            subscription.CurrentTermEndsAtUtc.Value <= now)
        {
            return false;
        }

        var activeSeats =
            await _db.StudentProfiles
                .AsNoTracking()
                .CountAsync(
                    x =>
                        x.SchoolId == schoolId &&
                        !x.IsArchived &&
                        x.Status == AcademicStructureStatus.Active,
                    cancellationToken);

        return activeSeats + additionalSeats <=
            subscription.CommittedSeats;
    }

    public async Task AddAsync<T>(
        T entity, CancellationToken cancellationToken = default)
        where T : class, ISchoolScoped
    {
        await _db.Set<T>().AddAsync(entity, cancellationToken);
    }

    public Task<AcademicPersistenceResult> SaveAsync(
        CancellationToken cancellationToken = default) =>
        SaveInternalAsync(cancellationToken);

    public Task<AcademicPersistenceResult> SaveWithRowVersionAsync<T>(
        T entity,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
        where T : class, ISchoolScoped
    {
        _db.Entry(entity).Property("RowVersion").OriginalValue = expectedRowVersion;
        return SaveInternalAsync(cancellationToken);
    }

    private async Task<AcademicPersistenceResult> SaveInternalAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return AcademicPersistenceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return AcademicPersistenceResult.Failure(AcademicPersistenceError.Conflict);
        }
        catch (DbUpdateException)
        {
            return AcademicPersistenceResult.Failure(AcademicPersistenceError.Constraint);
        }
    }
}
