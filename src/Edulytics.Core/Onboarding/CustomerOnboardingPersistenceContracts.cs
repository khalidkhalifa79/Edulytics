namespace Edulytics.Core.Onboarding;

public enum CustomerOnboardingPersistenceError
{
    None = 0,
    NotFound = 1,
    ConcurrencyConflict = 2,
    DuplicateEmail = 3,
    DuplicateSchoolCode = 4,
    RoleUnavailable = 5,
    PersistenceError = 6
}

public sealed record CustomerOnboardingWriteResult(
    bool Succeeded,
    CustomerOnboardingPersistenceError Error)
{
    public static CustomerOnboardingWriteResult Success() =>
        new(true, CustomerOnboardingPersistenceError.None);

    public static CustomerOnboardingWriteResult Failure(
        CustomerOnboardingPersistenceError error) =>
        new(false, error);
}

public sealed record CustomerOnboardingProvisionResult(
    bool Succeeded,
    CustomerOnboardingPersistenceError Error,
    Guid? SchoolId,
    Guid? SchoolAdminUserId,
    string? PasswordSetupToken,
    string? RecipientEmail,
    string? SchoolName,
    string? Culture)
{
    public static CustomerOnboardingProvisionResult Success(
        Guid schoolId,
        Guid userId,
        string token,
        string recipientEmail,
        string schoolName,
        string culture) =>
        new(true, CustomerOnboardingPersistenceError.None, schoolId, userId,
            token, recipientEmail, schoolName, culture);

    public static CustomerOnboardingProvisionResult Failure(
        CustomerOnboardingPersistenceError error) =>
        new(false, error, null, null, null, null, null, null);
}
