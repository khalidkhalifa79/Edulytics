using Edulytics.Core.Enums;

namespace Edulytics.Services.Schools;

public enum SchoolErrorCode
{
    RequiredName,
    NameTooLong,
    RequiredSchoolCode,
    SchoolCodeTooLong,
    InvalidSchoolCode,
    DuplicateSchoolCode,
    RequiredCountryCode,
    CountryCodeTooLong,
    InvalidCountryCode,
    RequiredCity,
    CityTooLong,
    RequiredContactEmail,
    ContactEmailTooLong,
    InvalidContactEmail,
    RequiredDefaultCulture,
    InvalidDefaultCulture,
    RequiredTimeZoneId,
    TimeZoneIdTooLong,
    SchoolNotFound,
    ArchivedCannotEdit,
    InvalidStatusTransition,
    ConcurrencyConflict,
    PersistenceError
}

public sealed record SchoolValidationError(
    string Field,
    SchoolErrorCode Code);

public sealed record SchoolListItem(
    Guid Id,
    string Name,
    string SchoolCode,
    SchoolStatus Status,
    string CountryCode,
    string City,
    DateTime CreatedAtUtc,
    bool CanEdit,
    bool CanSuspend,
    bool CanReactivate,
    bool CanArchive);

public sealed record SchoolDetails(
    Guid Id,
    string Name,
    string SchoolCode,
    SchoolStatus Status,
    string CountryCode,
    string City,
    string ContactEmail,
    string DefaultCulture,
    string TimeZoneId,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? ArchivedAtUtc,
    byte[] RowVersion,
    bool CanEdit,
    bool CanSuspend,
    bool CanReactivate,
    bool CanArchive);

public sealed record CreateSchoolRequest(
    string Name,
    string SchoolCode,
    string CountryCode,
    string City,
    string ContactEmail,
    string DefaultCulture,
    string TimeZoneId);

public sealed record UpdateSchoolRequest(
    Guid Id,
    string Name,
    string CountryCode,
    string City,
    string ContactEmail,
    string DefaultCulture,
    string TimeZoneId,
    byte[] RowVersion);

public sealed record SchoolStatusChangeRequest(
    Guid Id,
    SchoolStatus TargetStatus,
    byte[] RowVersion);

public sealed class SchoolCommandResult
{
    private SchoolCommandResult(
        bool succeeded,
        Guid? schoolId,
        IReadOnlyList<SchoolValidationError> errors)
    {
        Succeeded = succeeded;
        SchoolId = schoolId;
        Errors = errors;
    }

    public bool Succeeded { get; }

    public Guid? SchoolId { get; }

    public IReadOnlyList<SchoolValidationError> Errors { get; }

    public static SchoolCommandResult Success(Guid schoolId) =>
        new(true, schoolId, Array.Empty<SchoolValidationError>());

    public static SchoolCommandResult Failure(
        params SchoolValidationError[] errors) =>
        new(false, null, errors);

    public static SchoolCommandResult Failure(
        IReadOnlyList<SchoolValidationError> errors) =>
        new(false, null, errors);
}
