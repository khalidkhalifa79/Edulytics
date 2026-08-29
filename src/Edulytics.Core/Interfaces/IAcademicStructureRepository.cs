using Edulytics.Core.Academics;
using Edulytics.Core.Entities;

namespace Edulytics.Core.Interfaces;

public interface IAcademicStructureRepository
{
    Task<AcademicStructureSnapshot> GetSnapshotAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<AcademicYear?> GetAcademicYearAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AcademicProgram?> GetAcademicProgramAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AcademicProgram?>(null);

    Task<AcademicProgram?> GetDefaultAcademicProgramAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AcademicProgram?>(null);

    Task<AcademicProgram?> GetAcademicProgramByCodeAsync(
        Guid schoolId,
        string normalizedCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AcademicProgram?>(null);

    Task<AcademicYearProgramOffering?>
        GetAcademicYearProgramOfferingAsync(
            Guid schoolId,
            Guid academicYearId,
            Guid academicProgramId,
            CancellationToken cancellationToken = default) =>
        Task.FromResult<AcademicYearProgramOffering?>(null);

    Task<bool> AcademicYearProgramIsOfferedAsync(
        Guid schoolId,
        Guid academicYearId,
        Guid academicProgramId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    Task<bool> AcademicYearProgramHasUsageAsync(
        Guid schoolId,
        Guid academicYearId,
        Guid academicProgramId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    Task<GradeLevel?> GetGradeLevelAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ClassGroup?> GetClassGroupAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Subject?> GetSubjectAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<StudentProfile?> GetStudentProfileAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> AcademicYearNameExistsAsync(
        Guid schoolId,
        string normalizedName,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> TermNameExistsAsync(
        Guid schoolId,
        Guid academicYearId,
        string normalizedName,
        CancellationToken cancellationToken = default);

    Task<bool> GradeLevelNameExistsAsync(
        Guid schoolId,
        string normalizedName,
        CancellationToken cancellationToken = default);

    Task<bool> GradeLevelOrderExistsAsync(
        Guid schoolId,
        int order,
        CancellationToken cancellationToken = default);

    Task<bool> AcademicProgramCodeExistsAsync(
        Guid schoolId,
        string normalizedCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(false);

    Task<bool> ClassCodeExistsAsync(
        Guid schoolId,
        Guid academicYearId,
        string normalizedCode,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> ClassCodeExistsInProgramAsync(
        Guid schoolId,
        Guid academicYearId,
        Guid academicProgramId,
        string normalizedCode,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        ClassCodeExistsAsync(schoolId, academicYearId, normalizedCode, excludeId, cancellationToken);

    Task<bool> SubjectCodeExistsAsync(
        Guid schoolId,
        string normalizedCode,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> StudentNumberExistsAsync(
        Guid schoolId,
        string normalizedStudentNumber,
        CancellationToken cancellationToken = default);

    Task<bool> StudentUserLinkExistsAsync(
        Guid schoolId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> TeacherAssignmentExistsAsync(
        Guid schoolId,
        Guid teacherUserId,
        Guid classGroupId,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    Task<bool> StudentEnrollmentExistsAsync(
        Guid schoolId,
        Guid academicYearId,
        Guid studentProfileId,
        CancellationToken cancellationToken = default);

    Task<AcademicPersistenceResult>
        AddStudentProfileWithSeatGuardAsync(
            StudentProfile student,
            CancellationToken cancellationToken = default);

    Task<AcademicPersistenceResult>
        SaveStudentArchiveStateWithSeatGuardAsync(
            StudentProfile student,
            byte[] expectedRowVersion,
            bool restoring,
            CancellationToken cancellationToken = default);

    Task AddAsync<T>(
        T entity,
        CancellationToken cancellationToken = default)
        where T : class, ISchoolScoped;

    Task<AcademicPersistenceResult> SaveAsync(
        CancellationToken cancellationToken = default);

    Task<AcademicPersistenceResult> SaveWithRowVersionAsync<T>(
        T entity,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
        where T : class, ISchoolScoped;
}
