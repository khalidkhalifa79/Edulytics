using Edulytics.Core.Constants;
using Edulytics.Services.Academics;

namespace Edulytics.Services.StudentSetup;

public sealed class StudentRoleProvisioningService
    : IStudentRoleProvisioningService
{
    private readonly IStudentRoleProvisioningOperations _operations;

    public StudentRoleProvisioningService(
        IStudentRoleProvisioningOperations operations)
    {
        _operations = operations;
    }

    public Task<StudentRoleProvisioningContext?> GetContextAsync(
        Guid actorUserId,
        Guid schoolId,
        Guid targetUserId,
        CancellationToken cancellationToken = default) =>
        _operations.ReadContextAsync(
            actorUserId,
            schoolId,
            targetUserId,
            cancellationToken);

    public async Task<StudentRoleProvisioningResult>
        ConvertToStudentAsync(
            Guid actorUserId,
            Guid schoolId,
            Guid targetUserId,
            StudentRoleProvisioningRequest request,
            CancellationToken cancellationToken = default)
    {
        var context =
            await _operations.ReadContextAsync(
                actorUserId,
                schoolId,
                targetUserId,
                cancellationToken);

        if (context is null ||
            context.SchoolId != schoolId ||
            context.UserId != targetUserId ||
            !context.IsActive ||
            context.IsLocked)
        {
            return Failure(
                StudentRoleProvisioningErrorCode.AccessDenied);
        }

        if (context.Role != RoleNames.Teacher &&
            context.Role != RoleNames.Student)
        {
            return Failure(
                StudentRoleProvisioningErrorCode.InvalidTargetRole);
        }

        var studentNumber = Clean(request.StudentNumber);
        var firstName = Clean(request.FirstName);
        var lastName = Clean(request.LastName);

        if (studentNumber.Length == 0)
        {
            return Failure(
                StudentRoleProvisioningErrorCode
                    .MissingStudentNumber);
        }

        if (firstName.Length == 0)
        {
            return Failure(
                StudentRoleProvisioningErrorCode
                    .MissingFirstName);
        }

        if (lastName.Length == 0)
        {
            return Failure(
                StudentRoleProvisioningErrorCode
                    .MissingLastName);
        }

        if (request.ClassGroupId == Guid.Empty)
        {
            return Failure(
                StudentRoleProvisioningErrorCode.MissingClass);
        }

        var selectedClass =
            context.Classes.FirstOrDefault(
                x => x.Id == request.ClassGroupId);

        if (selectedClass is null)
        {
            return Failure(
                StudentRoleProvisioningErrorCode.ClassNotFound);
        }

        var originalRole = context.Role;
        var roleChanged = false;
        var profileCreated = false;
        var profileRestored = false;

        if (context.Role != RoleNames.Student)
        {
            var roleWrite =
                await _operations.ChangeRoleAsync(
                    actorUserId,
                    schoolId,
                    targetUserId,
                    RoleNames.Student,
                    cancellationToken);

            if (!roleWrite.Succeeded)
            {
                return Failure(
                    StudentRoleProvisioningErrorCode
                        .UnderlyingOperationFailed);
            }

            roleChanged = true;

            context =
                await ReadRequiredContextAsync(
                    actorUserId,
                    schoolId,
                    targetUserId,
                    cancellationToken);

            if (context is null)
            {
                return await FailWithRecoveryAsync(
                    actorUserId,
                    schoolId,
                    targetUserId,
                    originalRole,
                    roleChanged,
                    profileCreated,
                    profileRestored,
                    StudentRoleProvisioningErrorCode
                        .UnderlyingOperationFailed,
                    cancellationToken);
            }
        }

        if (!context.StudentProfileId.HasValue)
        {
            var create =
                await _operations.CreateProfileAsync(
                    actorUserId,
                    targetUserId,
                    studentNumber,
                    firstName,
                    lastName,
                    cancellationToken);

            if (!create.Succeeded)
            {
                return await FailWithRecoveryAsync(
                    actorUserId,
                    schoolId,
                    targetUserId,
                    originalRole,
                    roleChanged,
                    profileCreated,
                    profileRestored,
                    StudentRoleProvisioningErrorCode
                        .UnderlyingOperationFailed,
                    cancellationToken,
                    create.AcademicError);
            }

            profileCreated = true;

            context =
                await ReadRequiredContextAsync(
                    actorUserId,
                    schoolId,
                    targetUserId,
                    cancellationToken);

            if (context is null ||
                !context.StudentProfileId.HasValue)
            {
                return await FailWithRecoveryAsync(
                    actorUserId,
                    schoolId,
                    targetUserId,
                    originalRole,
                    roleChanged,
                    profileCreated,
                    profileRestored,
                    StudentRoleProvisioningErrorCode
                        .ProfileUnavailable,
                    cancellationToken);
            }
        }

        if (context.ProfileArchived)
        {
            if (context.ProfileRowVersion is not
                { Length: > 0 })
            {
                return await FailWithRecoveryAsync(
                    actorUserId,
                    schoolId,
                    targetUserId,
                    originalRole,
                    roleChanged,
                    profileCreated,
                    profileRestored,
                    StudentRoleProvisioningErrorCode
                        .ProfileUnavailable,
                    cancellationToken);
            }

            var restore =
                await _operations.RestoreProfileAsync(
                    actorUserId,
                    context.StudentProfileId!.Value,
                    context.ProfileRowVersion,
                    cancellationToken);

            if (!restore.Succeeded)
            {
                return await FailWithRecoveryAsync(
                    actorUserId,
                    schoolId,
                    targetUserId,
                    originalRole,
                    roleChanged,
                    profileCreated,
                    profileRestored,
                    StudentRoleProvisioningErrorCode
                        .UnderlyingOperationFailed,
                    cancellationToken,
                    restore.AcademicError);
            }

            profileRestored = true;

            context =
                await ReadRequiredContextAsync(
                    actorUserId,
                    schoolId,
                    targetUserId,
                    cancellationToken);

            if (context is null)
            {
                return await FailWithRecoveryAsync(
                    actorUserId,
                    schoolId,
                    targetUserId,
                    originalRole,
                    roleChanged,
                    profileCreated,
                    profileRestored,
                    StudentRoleProvisioningErrorCode
                        .ProfileUnavailable,
                    cancellationToken);
            }
        }

        if (!context.ProfileIsActive ||
            context.ProfileArchived ||
            !context.StudentProfileId.HasValue)
        {
            return await FailWithRecoveryAsync(
                actorUserId,
                schoolId,
                targetUserId,
                originalRole,
                roleChanged,
                profileCreated,
                profileRestored,
                StudentRoleProvisioningErrorCode
                    .ProfileUnavailable,
                cancellationToken);
        }

        var sameAcademicYear =
            context.Enrollments
                .Where(
                    x =>
                        x.AcademicYearId ==
                            selectedClass.AcademicYearId ||
                        string.Equals(
                            x.AcademicYearName,
                            selectedClass.AcademicYearName,
                            StringComparison.Ordinal))
                .ToArray();

        if (sameAcademicYear.Length > 0)
        {
            var alreadySelected =
                sameAcademicYear.Any(
                    x =>
                        x.ClassGroupId == selectedClass.Id ||
                        (
                            string.Equals(
                                x.AcademicYearName,
                                selectedClass.AcademicYearName,
                                StringComparison.Ordinal) &&
                            string.Equals(
                                x.ClassCode,
                                selectedClass.Code,
                                StringComparison.OrdinalIgnoreCase)
                        ));

            if (alreadySelected)
                return StudentRoleProvisioningResult.Success();

            return await FailWithRecoveryAsync(
                actorUserId,
                schoolId,
                targetUserId,
                originalRole,
                roleChanged,
                profileCreated,
                profileRestored,
                StudentRoleProvisioningErrorCode
                    .EnrollmentConflict,
                cancellationToken);
        }

        var enrollment =
            await _operations.CreateEnrollmentAsync(
                actorUserId,
                context.StudentProfileId.Value,
                selectedClass.Id,
                cancellationToken);

        if (!enrollment.Succeeded)
        {
            return await FailWithRecoveryAsync(
                actorUserId,
                schoolId,
                targetUserId,
                originalRole,
                roleChanged,
                profileCreated,
                profileRestored,
                StudentRoleProvisioningErrorCode
                    .UnderlyingOperationFailed,
                cancellationToken,
                enrollment.AcademicError);
        }

        return StudentRoleProvisioningResult.Success();
    }

    private async Task<StudentRoleProvisioningContext?>
        ReadRequiredContextAsync(
            Guid actorUserId,
            Guid schoolId,
            Guid targetUserId,
            CancellationToken cancellationToken)
    {
        var context =
            await _operations.ReadContextAsync(
                actorUserId,
                schoolId,
                targetUserId,
                cancellationToken);

        return context is not null &&
               context.SchoolId == schoolId &&
               context.UserId == targetUserId
            ? context
            : null;
    }

    private async Task<StudentRoleProvisioningResult>
        FailWithRecoveryAsync(
            Guid actorUserId,
            Guid schoolId,
            Guid targetUserId,
            string originalRole,
            bool roleChanged,
            bool profileCreated,
            bool profileRestored,
            StudentRoleProvisioningErrorCode originalError,
            CancellationToken cancellationToken,
            AcademicStructureErrorCode? academicError = null)
    {
        var recovered = true;

        if (profileCreated || profileRestored)
        {
            var current =
                await ReadRequiredContextAsync(
                    actorUserId,
                    schoolId,
                    targetUserId,
                    cancellationToken);

            if (current is null ||
                !current.StudentProfileId.HasValue ||
                current.ProfileRowVersion is not
                    { Length: > 0 })
            {
                recovered = false;
            }
            else if (!current.ProfileArchived)
            {
                var archive =
                    await _operations.ArchiveProfileAsync(
                        actorUserId,
                        current.StudentProfileId.Value,
                        current.ProfileRowVersion,
                        cancellationToken);

                recovered &= archive.Succeeded;
            }
        }

        if (roleChanged)
        {
            var revertRole =
                await _operations.ChangeRoleAsync(
                    actorUserId,
                    schoolId,
                    targetUserId,
                    originalRole,
                    cancellationToken);

            recovered &= revertRole.Succeeded;
        }

        return recovered
            ? Failure(originalError, academicError)
            : Failure(
                StudentRoleProvisioningErrorCode
                    .RecoveryFailed);
    }

    private static string Clean(string? value) =>
        value?.Trim() ?? string.Empty;

    private static StudentRoleProvisioningResult Failure(
        StudentRoleProvisioningErrorCode error,
        AcademicStructureErrorCode? academicError = null) =>
        StudentRoleProvisioningResult.Failure(
            error,
            academicError);
}
