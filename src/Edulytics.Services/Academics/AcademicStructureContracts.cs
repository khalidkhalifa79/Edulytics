using Edulytics.Core.Enums;

namespace Edulytics.Services.Academics;

public enum AcademicStructureErrorCode
{
    AccessDenied,
    SchoolNotActive,
    Required,
    InvalidName,
    InvalidCode,
    InvalidDateRange,
    TermOutsideAcademicYear,
    InvalidOrder,
    DuplicateAcademicYear,
    DuplicateTerm,
    DuplicateGradeLevel,
    DuplicateGradeOrder,
    DuplicateClassCode,
    DuplicateSubjectCode,
    DuplicateStudentNumber,
    DuplicateStudentUserLink,
    DuplicateTeacherAssignment,
    DuplicateEnrollment,
    AcademicYearNotFound,
    GradeLevelNotFound,
    ClassGroupNotFound,
    SubjectNotFound,
    StudentProfileNotFound,
    InvalidTeacher,
    InvalidStudentAccount,
    StudentSeatLimitReached,
    StudentAlreadyArchived,
    StudentNotArchived,
    ConcurrencyConflict,
    PersistenceError
}

public sealed record AcademicStructureError(
    string Field,
    AcademicStructureErrorCode Code);

public sealed record AcademicCommandResult(
    bool Succeeded,
    IReadOnlyList<AcademicStructureError> Errors)
{
    public static AcademicCommandResult Success() => new(true, []);

    public static AcademicCommandResult Failure(
        string field,
        AcademicStructureErrorCode code) =>
        new(false, [new AcademicStructureError(field, code)]);
}

public sealed record AcademicQueryResult<T>(
    T? Value,
    AcademicStructureErrorCode? Error)
    where T : class
{
    public static AcademicQueryResult<T> Success(T value) => new(value, null);
    public static AcademicQueryResult<T> Failure(AcademicStructureErrorCode error) =>
        new(null, error);
}

public sealed record AcademicYearItem(
    Guid Id,
    string Name,
    DateOnly StartsOn,
    DateOnly EndsOn,
    AcademicStructureStatus Status,
    byte[] RowVersion);

public sealed record TermItem(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    string Name,
    DateOnly StartsOn,
    DateOnly EndsOn,
    AcademicStructureStatus Status);

public sealed record GradeLevelItem(Guid Id, string Name, int Order);

public sealed record ClassGroupItem(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    Guid GradeLevelId,
    string GradeLevelName,
    string Name,
    string Code,
    AcademicStructureStatus Status,
    byte[] RowVersion);

public sealed record SubjectItem(
    Guid Id,
    string Name,
    string Code,
    AcademicStructureStatus Status,
    byte[] RowVersion);

public sealed record TeacherAssignmentItem(
    Guid Id,
    string TeacherEmail,
    string ClassName,
    string ClassCode,
    string SubjectName,
    string AcademicYearName);

public sealed record StudentProfileItem(
    Guid Id,
    string StudentNumber,
    string FirstName,
    string LastName,
    string DisplayName,
    string? UserEmail,
    AcademicStructureStatus Status,
    bool IsArchived = false,
    DateTime? ArchivedAtUtc = null,
    byte[]? RowVersion = null);

public sealed record StudentEnrollmentItem(
    Guid Id,
    Guid StudentProfileId,
    string StudentDisplayName,
    string ClassName,
    string ClassCode,
    string AcademicYearName);

public sealed record UserCandidate(Guid Id, string Email);

public sealed record AcademicStructureDashboard(
    Guid SchoolId,
    string SchoolName,
    IReadOnlyList<AcademicYearItem> AcademicYears,
    IReadOnlyList<TermItem> Terms,
    IReadOnlyList<GradeLevelItem> GradeLevels,
    IReadOnlyList<ClassGroupItem> ClassGroups,
    IReadOnlyList<SubjectItem> Subjects,
    IReadOnlyList<TeacherAssignmentItem> TeacherAssignments,
    IReadOnlyList<StudentProfileItem> StudentProfiles,
    IReadOnlyList<StudentEnrollmentItem> StudentEnrollments,
    IReadOnlyList<UserCandidate> TeacherCandidates,
    IReadOnlyList<UserCandidate> StudentAccountCandidates);

public sealed record CreateAcademicYearRequest(
    string Name,
    DateOnly StartsOn,
    DateOnly EndsOn,
    AcademicStructureStatus Status);

public sealed record UpdateAcademicYearRequest(
    Guid Id,
    string Name,
    DateOnly StartsOn,
    DateOnly EndsOn,
    AcademicStructureStatus Status,
    byte[] ExpectedRowVersion);

public sealed record CreateTermRequest(
    Guid AcademicYearId,
    string Name,
    DateOnly StartsOn,
    DateOnly EndsOn,
    AcademicStructureStatus Status);

public sealed record CreateGradeLevelRequest(string Name, int Order);

public sealed record CreateClassGroupRequest(
    Guid AcademicYearId,
    Guid GradeLevelId,
    string Name,
    string Code,
    AcademicStructureStatus Status);

public sealed record UpdateClassGroupRequest(
    Guid Id,
    Guid GradeLevelId,
    string Name,
    string Code,
    AcademicStructureStatus Status,
    byte[] ExpectedRowVersion);

public sealed record CreateSubjectRequest(
    string Name,
    string Code,
    AcademicStructureStatus Status);

public sealed record UpdateSubjectRequest(
    Guid Id,
    string Name,
    string Code,
    AcademicStructureStatus Status,
    byte[] ExpectedRowVersion);

public sealed record CreateTeacherAssignmentRequest(
    Guid TeacherUserId,
    Guid ClassGroupId,
    Guid SubjectId);

public sealed record CreateStudentProfileRequest(
    string StudentNumber,
    string FirstName,
    string LastName,
    Guid? UserId,
    AcademicStructureStatus Status);

public sealed record CreateStudentEnrollmentRequest(
    Guid StudentProfileId,
    Guid ClassGroupId);
