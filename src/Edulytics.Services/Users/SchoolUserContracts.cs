using Edulytics.Core.Enums;

namespace Edulytics.Services.Users;

public enum SchoolUserErrorCode
{
    UserAccessDenied,
    SchoolNotFound,
    UserSchoolArchived,
    UserNotFound,
    UserRequiredEmail,
    UserInvalidEmail,
    UserEmailTooLong,
    UserDuplicateEmail,
    UserInvalidRole,
    UserCannotManageSelf,
    UserPersistenceError,
    UserInvalidPasswordSetup,
    UserPasswordPolicy,
    UserIdentityConflict
}

public sealed record SchoolUserError(
    string Field,
    SchoolUserErrorCode Code);

public sealed record SchoolUserCommandResult(
    bool Succeeded,
    IReadOnlyList<SchoolUserError> Errors)
{
    public static SchoolUserCommandResult Success() =>
        new(true, []);

    public static SchoolUserCommandResult Failure(
        string field,
        SchoolUserErrorCode code) =>
        new(
            false,
            [new SchoolUserError(field, code)]);
}

public sealed record SchoolUserQueryResult<T>(
    T? Value,
    SchoolUserErrorCode? Error)
    where T : class
{
    public static SchoolUserQueryResult<T> Success(
        T value) =>
        new(value, null);

    public static SchoolUserQueryResult<T> Failure(
        SchoolUserErrorCode error) =>
        new(null, error);
}

public sealed record CreateSchoolUserRequest(
    string Email,
    string Role);

public sealed record SchoolUserManagementContext(
    Guid SchoolId,
    string SchoolName,
    SchoolStatus SchoolStatus,
    bool CanMutate,
    bool IsPlatformActor);

public sealed record SchoolUserListItem(
    Guid Id,
    string Email,
    string Role,
    bool IsActive,
    bool IsLocked,
    bool IsSelf,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record SchoolUserListData(
    SchoolUserManagementContext Context,
    IReadOnlyList<SchoolUserListItem> Users);

public sealed record SchoolUserDetails(
    SchoolUserManagementContext Context,
    Guid Id,
    string Email,
    string Role,
    bool IsActive,
    bool IsLocked,
    bool IsSelf,
    bool CanModify,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);

public sealed record SchoolUserCreateResult(
    bool Succeeded,
    Guid? UserId,
    string? PasswordSetupToken,
    string? SchoolCulture,
    IReadOnlyList<SchoolUserError> Errors)
{
    public static SchoolUserCreateResult Success(
        Guid userId,
        string token,
        string schoolCulture) =>
        new(
            true,
            userId,
            token,
            schoolCulture,
            []);

    public static SchoolUserCreateResult Failure(
        string field,
        SchoolUserErrorCode code) =>
        new(
            false,
            null,
            null,
            null,
            [new SchoolUserError(field, code)]);
}

public sealed record SchoolUserPasswordLinkResult(
    bool Succeeded,
    Guid? UserId,
    string? PasswordSetupToken,
    string? SchoolCulture,
    IReadOnlyList<SchoolUserError> Errors)
{
    public static SchoolUserPasswordLinkResult Success(
        Guid userId,
        string token,
        string schoolCulture) =>
        new(
            true,
            userId,
            token,
            schoolCulture,
            []);

    public static SchoolUserPasswordLinkResult Failure(
        string field,
        SchoolUserErrorCode code) =>
        new(
            false,
            null,
            null,
            null,
            [new SchoolUserError(field, code)]);
}

public sealed record SchoolUserSignInDecision(
    bool Allowed,
    bool IsPlatformAdministrator,
    Guid? SchoolId,
    string? Role);

public sealed record SchoolUserActorContext(
    Guid UserId,
    Guid SchoolId,
    string SchoolName,
    string Role,
    bool CanManageUsers);
