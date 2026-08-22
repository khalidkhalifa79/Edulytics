using Edulytics.Core.Enums;

namespace Edulytics.Services.Onboarding;

public enum OnboardingErrorCode
{
    RequiredSchoolName = 1,
    SchoolNameTooLong = 2,
    RequiredContactName = 3,
    ContactNameTooLong = 4,
    RequiredEmail = 5,
    InvalidEmail = 6,
    EmailTooLong = 7,
    PhoneTooLong = 8,
    RequiredCountry = 9,
    CountryTooLong = 10,
    RequiredCity = 11,
    CityTooLong = 12,
    MinimumStudentCount = 13,
    MessageTooLong = 14,
    PrivacyConsentRequired = 15,
    NotFound = 16,
    InvalidTransition = 17,
    DemoScheduleRequired = 18,
    DemoNotQualified = 19,
    DemoAlreadyExists = 20,
    DemoNotFound = 21,
    DemoAlreadyRevoked = 22,
    DemoAlreadyConverted = 23,
    RevokeReasonRequired = 24,
    RevokeReasonTooLong = 25,
    InternalNoteTooLong = 26,
    ProvisionRequiresWon = 27,
    RequiredSchoolCode = 28,
    InvalidSchoolCode = 29,
    SchoolCodeTooLong = 30,
    DuplicateSchoolCode = 31,
    InvalidCulture = 32,
    RequiredTimeZone = 33,
    TimeZoneTooLong = 34,
    DuplicateEmail = 35,
    ConcurrencyConflict = 36,
    PersistenceError = 37,
    UnsupportedCountry = 38
}

public sealed record OnboardingError(string Field, OnboardingErrorCode Code);

public sealed record DemoRequestSubmission(
    string SchoolName,
    string ContactName,
    string WorkEmail,
    string? Phone,
    string CountryCode,
    string City,
    int EstimatedStudentCount,
    string? Message,
    bool PrivacyAccepted);

public sealed record DemoRequestListItem(
    Guid Id,
    string SchoolName,
    string ContactName,
    string WorkEmail,
    string CountryCode,
    int EstimatedStudentCount,
    DemoRequestStatus Status,
    DateTime CreatedAtUtc);

public sealed record DemoAccessDetails(
    Guid Id,
    Guid SchoolId,
    Guid SchoolAdminUserId,
    DateTime StartsAtUtc,
    DateTime ExpiresAtUtc,
    DateTime? RevokedAtUtc,
    string? RevokedReason,
    DateTime? ConvertedAtUtc,
    byte[] RowVersion,
    bool IsCurrentlyUsable);

public sealed record DemoRequestDetails(
    Guid Id,
    string SchoolName,
    string ContactName,
    string WorkEmail,
    string? Phone,
    string CountryCode,
    string City,
    int EstimatedStudentCount,
    string? Message,
    DemoRequestStatus Status,
    DateTime? DemoScheduledAtUtc,
    string? InternalNote,
    DateTime PrivacyConsentAtUtc,
    Guid? DemoSchoolId,
    Guid? ProvisionedSchoolId,
    Guid? ProvisionedSchoolAdminUserId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    byte[] RowVersion,
    DemoAccessDetails? DemoAccess,
    IReadOnlyList<DemoRequestStatus> AllowedNextStatuses,
    bool CanGrantDemo,
    bool CanExtendDemo,
    bool CanExpireDemo,
    bool CanRevokeDemo,
    bool CanProvision);

public sealed record OnboardingInvitation(
    Guid UserId,
    string Token,
    string RecipientEmail,
    string SchoolName,
    string Culture);

public sealed record OnboardingCommandResult(
    bool Succeeded,
    IReadOnlyList<OnboardingError> Errors,
    OnboardingInvitation? Invitation = null)
{
    public static OnboardingCommandResult Success(OnboardingInvitation? invitation = null) =>
        new(true, [], invitation);

    public static OnboardingCommandResult Failure(string field, OnboardingErrorCode code) =>
        new(false, [new OnboardingError(field, code)]);
}
