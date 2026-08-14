namespace Edulytics.Services.Users;

public interface ISchoolUserManagementService
{
    Task<SchoolUserQueryResult<SchoolUserListData>> ListAsync(
        Guid actorUserId,
        Guid? requestedSchoolId,
        CancellationToken cancellationToken = default);

    Task<SchoolUserQueryResult<SchoolUserManagementContext>>
        GetManagementContextAsync(
            Guid actorUserId,
            Guid? requestedSchoolId,
            CancellationToken cancellationToken = default);

    Task<SchoolUserQueryResult<SchoolUserDetails>> GetAsync(
        Guid actorUserId,
        Guid? requestedSchoolId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SchoolUserCreateResult> CreateAsync(
        Guid actorUserId,
        Guid? requestedSchoolId,
        CreateSchoolUserRequest request,
        CancellationToken cancellationToken = default);

    Task<SchoolUserCommandResult> SetActiveAsync(
        Guid actorUserId,
        Guid? requestedSchoolId,
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<SchoolUserCommandResult> SetLockedAsync(
        Guid actorUserId,
        Guid? requestedSchoolId,
        Guid userId,
        bool isLocked,
        CancellationToken cancellationToken = default);

    Task<SchoolUserCommandResult> ChangeRoleAsync(
        Guid actorUserId,
        Guid? requestedSchoolId,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default);

    Task<SchoolUserPasswordLinkResult>
        GeneratePasswordSetupAsync(
            Guid actorUserId,
            Guid? requestedSchoolId,
            Guid userId,
            CancellationToken cancellationToken = default);

    Task<SchoolUserCommandResult> CompletePasswordSetupAsync(
        Guid userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<SchoolUserSignInDecision> EvaluateSignInAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> CanManageUsersAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<SchoolUserActorContext?> GetActorContextAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
