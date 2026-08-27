using Edulytics.Core.Enums;
using Edulytics.Services.Academics;
using Edulytics.Services.Users;

namespace Edulytics.Services.StudentSetup;

public sealed class StudentRoleProvisioningOperations
    : IStudentRoleProvisioningOperations
{
    private readonly ISchoolUserManagementService _users;
    private readonly IAcademicStructureService _academic;

    public StudentRoleProvisioningOperations(
        ISchoolUserManagementService users,
        IAcademicStructureService academic)
    {
        _users = users;
        _academic = academic;
    }

    public async Task<StudentRoleProvisioningContext?> ReadContextAsync(
        Guid actorUserId,
        Guid schoolId,
        Guid targetUserId,
        CancellationToken cancellationToken = default)
    {
        var userResult =
            await _users.GetAsync(
                actorUserId,
                schoolId,
                targetUserId,
                cancellationToken);

        if (userResult.Value is null)
            return null;

        var dashboardResult =
            await _academic.GetDashboardAsync(
                actorUserId,
                cancellationToken);

        if (dashboardResult.Value is null ||
            dashboardResult.Value.SchoolId != schoolId)
        {
            return null;
        }

        var user = userResult.Value;
        var dashboard = dashboardResult.Value;

        var profile =
            dashboard.StudentProfiles
                .FirstOrDefault(
                    x =>
                        string.Equals(
                            x.UserEmail,
                            user.Email,
                            StringComparison.OrdinalIgnoreCase));

        var classes =
            dashboard.ClassGroups
                .Where(
                    x =>
                        x.Status ==
                        AcademicStructureStatus.Active)
                .OrderByDescending(x => x.AcademicYearName)
                .ThenBy(x => x.GradeLevelName)
                .ThenBy(x => x.Name)
                .Select(
                    x =>
                        new StudentRoleClassOption(
                            x.Id,
                            x.AcademicYearId,
                            x.AcademicYearName,
                            x.GradeLevelName,
                            x.Name,
                            x.Code))
                .ToArray();

        IReadOnlyList<StudentRoleEnrollmentState> enrollments = [];

        if (profile is not null)
        {
            enrollments =
                dashboard.StudentEnrollments
                    .Where(
                        x =>
                            x.StudentProfileId ==
                            profile.Id)
                    .Select(
                        enrollment =>
                        {
                            var match =
                                classes.FirstOrDefault(
                                    x =>
                                        string.Equals(
                                            x.AcademicYearName,
                                            enrollment.AcademicYearName,
                                            StringComparison.Ordinal) &&
                                        string.Equals(
                                            x.Code,
                                            enrollment.ClassCode,
                                            StringComparison.OrdinalIgnoreCase));

                            return new StudentRoleEnrollmentState(
                                match?.Id,
                                match?.AcademicYearId,
                                enrollment.AcademicYearName,
                                enrollment.ClassName,
                                enrollment.ClassCode);
                        })
                    .ToArray();
        }

        return new StudentRoleProvisioningContext(
            schoolId,
            user.Id,
            user.Email,
            user.Role,
            user.IsActive,
            user.IsLocked,
            profile?.Id,
            profile?.StudentNumber,
            profile?.FirstName,
            profile?.LastName,
            profile?.Status ==
                AcademicStructureStatus.Active,
            profile?.IsArchived ?? false,
            profile?.RowVersion?.ToArray(),
            enrollments,
            classes);
    }

    public async Task<StudentRoleProvisioningOperationResult>
        ChangeRoleAsync(
            Guid actorUserId,
            Guid schoolId,
            Guid targetUserId,
            string role,
            CancellationToken cancellationToken = default)
    {
        var result =
            await _users.ChangeRoleAsync(
                actorUserId,
                schoolId,
                targetUserId,
                role,
                cancellationToken);

        return result.Succeeded
            ? StudentRoleProvisioningOperationResult.Success()
            : StudentRoleProvisioningOperationResult.Failure(
                result.Errors.FirstOrDefault()?.Code.ToString());
    }

    public async Task<StudentRoleProvisioningOperationResult>
        CreateProfileAsync(
            Guid actorUserId,
            Guid targetUserId,
            string studentNumber,
            string firstName,
            string lastName,
            CancellationToken cancellationToken = default)
    {
        var result =
            await _academic.CreateStudentProfileAsync(
                actorUserId,
                new CreateStudentProfileRequest(
                    studentNumber,
                    firstName,
                    lastName,
                    targetUserId,
                    AcademicStructureStatus.Active),
                cancellationToken);

        return Map(result);
    }

    public async Task<StudentRoleProvisioningOperationResult>
        ArchiveProfileAsync(
            Guid actorUserId,
            Guid studentProfileId,
            byte[] expectedRowVersion,
            CancellationToken cancellationToken = default)
    {
        var result =
            await _academic.ArchiveStudentProfileAsync(
                actorUserId,
                studentProfileId,
                expectedRowVersion,
                cancellationToken);

        return Map(result);
    }

    public async Task<StudentRoleProvisioningOperationResult>
        RestoreProfileAsync(
            Guid actorUserId,
            Guid studentProfileId,
            byte[] expectedRowVersion,
            CancellationToken cancellationToken = default)
    {
        var result =
            await _academic.RestoreStudentProfileAsync(
                actorUserId,
                studentProfileId,
                expectedRowVersion,
                cancellationToken);

        return Map(result);
    }

    public async Task<StudentRoleProvisioningOperationResult>
        CreateEnrollmentAsync(
            Guid actorUserId,
            Guid studentProfileId,
            Guid classGroupId,
            CancellationToken cancellationToken = default)
    {
        var result =
            await _academic.CreateStudentEnrollmentAsync(
                actorUserId,
                new CreateStudentEnrollmentRequest(
                    studentProfileId,
                    classGroupId),
                cancellationToken);

        return Map(result);
    }

    private static StudentRoleProvisioningOperationResult Map(
        AcademicCommandResult result)
    {
        if (result.Succeeded)
            return StudentRoleProvisioningOperationResult.Success();

        var first = result.Errors.FirstOrDefault();

        return StudentRoleProvisioningOperationResult.Failure(
            first?.Code.ToString(),
            first?.Code);
    }
}
