namespace Edulytics.Services.StudentSetup;

public interface IStudentRoleProvisioningService
{
    Task<StudentRoleProvisioningContext?> GetContextAsync(
        Guid actorUserId,
        Guid schoolId,
        Guid targetUserId,
        CancellationToken cancellationToken = default);

    Task<StudentRoleProvisioningResult> ConvertToStudentAsync(
        Guid actorUserId,
        Guid schoolId,
        Guid targetUserId,
        StudentRoleProvisioningRequest request,
        CancellationToken cancellationToken = default);
}

public interface IStudentRoleProvisioningOperations
{
    Task<StudentRoleProvisioningContext?> ReadContextAsync(
        Guid actorUserId,
        Guid schoolId,
        Guid targetUserId,
        CancellationToken cancellationToken = default);

    Task<StudentRoleProvisioningOperationResult> ChangeRoleAsync(
        Guid actorUserId,
        Guid schoolId,
        Guid targetUserId,
        string role,
        CancellationToken cancellationToken = default);

    Task<StudentRoleProvisioningOperationResult> CreateProfileAsync(
        Guid actorUserId,
        Guid targetUserId,
        string studentNumber,
        string firstName,
        string lastName,
        CancellationToken cancellationToken = default);

    Task<StudentRoleProvisioningOperationResult> ArchiveProfileAsync(
        Guid actorUserId,
        Guid studentProfileId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<StudentRoleProvisioningOperationResult> RestoreProfileAsync(
        Guid actorUserId,
        Guid studentProfileId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<StudentRoleProvisioningOperationResult> CreateEnrollmentAsync(
        Guid actorUserId,
        Guid studentProfileId,
        Guid classGroupId,
        CancellationToken cancellationToken = default);
}
