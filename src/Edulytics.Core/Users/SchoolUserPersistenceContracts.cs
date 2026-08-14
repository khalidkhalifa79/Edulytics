namespace Edulytics.Core.Users;

public sealed record SchoolUserRecord(
    Guid Id,
    Guid? SchoolId,
    string Email,
    bool IsActive,
    bool IsLocked,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<string> Roles);

public enum SchoolUserPersistenceError
{
    None = 0,
    DuplicateEmail = 1,
    NotFound = 2,
    RoleFailure = 3,
    IdentityFailure = 4,
    InvalidToken = 5,
    PasswordPolicy = 6,
    Conflict = 7
}

public sealed record SchoolUserPersistenceResult(
    bool Succeeded,
    SchoolUserPersistenceError Error,
    SchoolUserRecord? User = null,
    string? PasswordSetupToken = null)
{
    public static SchoolUserPersistenceResult Success(
        SchoolUserRecord user,
        string? token = null) =>
        new(
            true,
            SchoolUserPersistenceError.None,
            user,
            token);

    public static SchoolUserPersistenceResult Failure(
        SchoolUserPersistenceError error) =>
        new(
            false,
            error);
}
