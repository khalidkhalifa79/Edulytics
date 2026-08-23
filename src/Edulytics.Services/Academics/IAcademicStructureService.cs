namespace Edulytics.Services.Academics;

public interface IAcademicStructureService
{
    Task<AcademicQueryResult<AcademicStructureDashboard>> GetDashboardAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<AcademicQueryResult<AcademicYearItem>> GetAcademicYearAsync(
        Guid actorUserId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AcademicQueryResult<ClassGroupItem>> GetClassGroupAsync(
        Guid actorUserId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AcademicQueryResult<SubjectItem>> GetSubjectAsync(
        Guid actorUserId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> CreateAcademicYearAsync(
        Guid actorUserId,
        CreateAcademicYearRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> UpdateAcademicYearAsync(
        Guid actorUserId,
        UpdateAcademicYearRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> CreateTermAsync(
        Guid actorUserId,
        CreateTermRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> CreateGradeLevelAsync(
        Guid actorUserId,
        CreateGradeLevelRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> CreateClassGroupAsync(
        Guid actorUserId,
        CreateClassGroupRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> UpdateClassGroupAsync(
        Guid actorUserId,
        UpdateClassGroupRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> CreateSubjectAsync(
        Guid actorUserId,
        CreateSubjectRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> UpdateSubjectAsync(
        Guid actorUserId,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> CreateTeacherAssignmentAsync(
        Guid actorUserId,
        CreateTeacherAssignmentRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> CreateStudentProfileAsync(
        Guid actorUserId,
        CreateStudentProfileRequest request,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> ArchiveStudentProfileAsync(
        Guid actorUserId,
        Guid studentProfileId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> RestoreStudentProfileAsync(
        Guid actorUserId,
        Guid studentProfileId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<AcademicCommandResult> CreateStudentEnrollmentAsync(
        Guid actorUserId,
        CreateStudentEnrollmentRequest request,
        CancellationToken cancellationToken = default);
}
