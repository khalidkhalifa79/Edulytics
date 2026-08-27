using Edulytics.Core.Constants;

namespace Edulytics.Services.StudentSetup;

public enum StudentRoleProvisioningErrorCode
{
    AccessDenied,
    InvalidTargetRole,
    MissingStudentNumber,
    MissingFirstName,
    MissingLastName,
    MissingClass,
    ClassNotFound,
    ProfileUnavailable,
    EnrollmentConflict,
    UnderlyingOperationFailed,
    RecoveryFailed
}

public sealed record StudentRoleClassOption(
    Guid Id,
    Guid AcademicYearId,
    string AcademicYearName,
    string GradeLevelName,
    string Name,
    string Code);

public sealed record StudentRoleEnrollmentState(
    Guid? ClassGroupId,
    Guid? AcademicYearId,
    string AcademicYearName,
    string ClassName,
    string ClassCode);

public sealed record StudentRoleProvisioningContext(
    Guid SchoolId,
    Guid UserId,
    string Email,
    string Role,
    bool IsActive,
    bool IsLocked,
    Guid? StudentProfileId,
    string? StudentNumber,
    string? FirstName,
    string? LastName,
    bool ProfileIsActive,
    bool ProfileArchived,
    byte[]? ProfileRowVersion,
    IReadOnlyList<StudentRoleEnrollmentState> Enrollments,
    IReadOnlyList<StudentRoleClassOption> Classes)
{
    public bool IsComplete =>
        Role == RoleNames.Student &&
        StudentProfileId.HasValue &&
        ProfileIsActive &&
        !ProfileArchived &&
        Enrollments.Count > 0;

    public Guid? CurrentClassGroupId =>
        Enrollments
            .Where(x => x.ClassGroupId.HasValue)
            .Select(x => x.ClassGroupId)
            .FirstOrDefault();

    public string? CurrentEnrollmentSummary
    {
        get
        {
            var item = Enrollments.FirstOrDefault();

            return item is null
                ? null
                : $"{item.AcademicYearName} · {item.ClassName}";
        }
    }
}

public sealed record StudentRoleProvisioningRequest(
    string StudentNumber,
    string FirstName,
    string LastName,
    Guid ClassGroupId);

public sealed record StudentRoleProvisioningResult(
    bool Succeeded,
    StudentRoleProvisioningErrorCode? Error)
{
    public static StudentRoleProvisioningResult Success() =>
        new(true, null);

    public static StudentRoleProvisioningResult Failure(
        StudentRoleProvisioningErrorCode error) =>
        new(false, error);
}

public sealed record StudentRoleProvisioningOperationResult(
    bool Succeeded,
    string? Error = null)
{
    public static StudentRoleProvisioningOperationResult Success() =>
        new(true);

    public static StudentRoleProvisioningOperationResult Failure(
        string? error = null) =>
        new(false, error);
}
