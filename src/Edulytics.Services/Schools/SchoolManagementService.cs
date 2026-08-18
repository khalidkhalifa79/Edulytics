using System.Net.Mail;
using System.Text.RegularExpressions;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Services.Auditing;

namespace Edulytics.Services.Schools;

public sealed class SchoolManagementService : ISchoolManagementService
{
    private const int NameMaxLength = 200;
    private const int SchoolCodeMaxLength = 50;
    private const int CountryCodeMaxLength = 10;
    private const int CityMaxLength = 150;
    private const int ContactEmailMaxLength = 255;
    private const int TimeZoneIdMaxLength = 100;

    private static readonly Regex SchoolCodePattern = new(
        "^[A-Z0-9-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly HashSet<string> SupportedCultures =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "en",
            "pl"
        };

    private readonly ISchoolRepository _repository;
    private readonly IAuditService? _audit;

    public SchoolManagementService(
        ISchoolRepository repository,
        IAuditService? audit = null)
    {
        _repository = repository;
        _audit = audit;
    }

    public async Task<IReadOnlyList<SchoolListItem>> ListAsync(
        CancellationToken cancellationToken = default)
    {
        var schools = await _repository.ListAsync(cancellationToken);

        return schools
            .Select(MapListItem)
            .ToArray();
    }

    public async Task<SchoolDetails?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var school = await _repository.GetByIdAsync(id, cancellationToken);

        return school is null ? null : MapDetails(school);
    }

    public async Task<SchoolCommandResult> CreateAsync(
        CreateSchoolRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateCreate(request);

        if (errors.Count > 0)
        {
            return SchoolCommandResult.Failure(errors);
        }

        var normalizedCode = NormalizeSchoolCode(request.SchoolCode);

        if (await _repository.ExistsByNormalizedCodeAsync(
                normalizedCode,
                cancellationToken))
        {
            return SchoolCommandResult.Failure(
                new SchoolValidationError(
                    nameof(CreateSchoolRequest.SchoolCode),
                    SchoolErrorCode.DuplicateSchoolCode));
        }

        var now = DateTime.UtcNow;

        var school = new School
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            SchoolCode = normalizedCode,
            NormalizedSchoolCode = normalizedCode,
            Status = SchoolStatus.Active,
            CountryCode = NormalizeCountryCode(request.CountryCode),
            City = request.City.Trim(),
            ContactEmail = request.ContactEmail.Trim(),
            DefaultCulture = request.DefaultCulture.Trim().ToLowerInvariant(),
            TimeZoneId = request.TimeZoneId.Trim(),
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            ArchivedAtUtc = null,
            RowVersion = Array.Empty<byte>()
        };

        await _repository.AddAsync(school, cancellationToken);

        if (_audit is not null)
        {
            await _audit.QueueAsync(
                new AuditEvent(
                    SchoolId: school.Id,
                    Action: "School.Created",
                    EntityType: "School",
                    EntityId: school.Id.ToString("D"),
                    Feature: "SchoolManagement",
                    NewValues: new Dictionary<string, object?>
                    {
                        ["name"] = school.Name,
                        ["schoolCode"] = school.SchoolCode,
                        ["status"] = school.Status.ToString(),
                        ["countryCode"] = school.CountryCode,
                        ["city"] = school.City,
                        ["contactEmail"] = school.ContactEmail,
                        ["defaultCulture"] = school.DefaultCulture,
                        ["timeZoneId"] = school.TimeZoneId
                    },
                    ResultSummary: "School created."),
                cancellationToken);
        }

        var saveResult = await _repository.SaveAsync(
            school,
            expectedRowVersion: null,
            cancellationToken);

        return saveResult switch
        {
            SchoolRepositoryWriteResult.Success =>
                SchoolCommandResult.Success(school.Id),

            SchoolRepositoryWriteResult.ConstraintViolation =>
                SchoolCommandResult.Failure(
                    new SchoolValidationError(
                        nameof(CreateSchoolRequest.SchoolCode),
                        SchoolErrorCode.DuplicateSchoolCode)),

            SchoolRepositoryWriteResult.ConcurrencyConflict =>
                SchoolCommandResult.Failure(
                    new SchoolValidationError(
                        string.Empty,
                        SchoolErrorCode.ConcurrencyConflict)),

            _ =>
                SchoolCommandResult.Failure(
                    new SchoolValidationError(
                        string.Empty,
                        SchoolErrorCode.PersistenceError))
        };
    }

    public async Task<SchoolCommandResult> UpdateAsync(
        UpdateSchoolRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateUpdate(request);

        if (errors.Count > 0)
        {
            return SchoolCommandResult.Failure(errors);
        }

        var school = await _repository.GetForUpdateAsync(
            request.Id,
            cancellationToken);

        if (school is null)
        {
            return SchoolCommandResult.Failure(
                new SchoolValidationError(
                    string.Empty,
                    SchoolErrorCode.SchoolNotFound));
        }

        if (school.Status == SchoolStatus.Archived)
        {
            return SchoolCommandResult.Failure(
                new SchoolValidationError(
                    string.Empty,
                    SchoolErrorCode.ArchivedCannotEdit));
        }

        var oldValues = new Dictionary<string, object?>
        {
            ["name"] = school.Name,
            ["countryCode"] = school.CountryCode,
            ["city"] = school.City,
            ["contactEmail"] = school.ContactEmail,
            ["defaultCulture"] = school.DefaultCulture,
            ["timeZoneId"] = school.TimeZoneId
        };

        school.Name = request.Name.Trim();
        school.CountryCode = NormalizeCountryCode(request.CountryCode);
        school.City = request.City.Trim();
        school.ContactEmail = request.ContactEmail.Trim();
        school.DefaultCulture =
            request.DefaultCulture.Trim().ToLowerInvariant();
        school.TimeZoneId = request.TimeZoneId.Trim();
        school.UpdatedAtUtc = DateTime.UtcNow;

        if (_audit is not null)
        {
            await _audit.QueueAsync(
                new AuditEvent(
                    SchoolId: school.Id,
                    Action: "School.Updated",
                    EntityType: "School",
                    EntityId: school.Id.ToString("D"),
                    Feature: "SchoolManagement",
                    OldValues: oldValues,
                    NewValues: new Dictionary<string, object?>
                    {
                        ["name"] = school.Name,
                        ["countryCode"] = school.CountryCode,
                        ["city"] = school.City,
                        ["contactEmail"] = school.ContactEmail,
                        ["defaultCulture"] = school.DefaultCulture,
                        ["timeZoneId"] = school.TimeZoneId
                    },
                    ResultSummary: "School details updated."),
                cancellationToken);
        }

        var saveResult = await _repository.SaveAsync(
            school,
            request.RowVersion,
            cancellationToken);

        return MapWriteResult(saveResult, school.Id);
    }

    public async Task<SchoolCommandResult> ChangeStatusAsync(
        SchoolStatusChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        var school = await _repository.GetForUpdateAsync(
            request.Id,
            cancellationToken);

        if (school is null)
        {
            return SchoolCommandResult.Failure(
                new SchoolValidationError(
                    string.Empty,
                    SchoolErrorCode.SchoolNotFound));
        }

        if (!IsAllowedTransition(school.Status, request.TargetStatus))
        {
            return SchoolCommandResult.Failure(
                new SchoolValidationError(
                    string.Empty,
                    SchoolErrorCode.InvalidStatusTransition));
        }

        var previousStatus = school.Status;

        school.Status = request.TargetStatus;
        school.UpdatedAtUtc = DateTime.UtcNow;

        if (request.TargetStatus == SchoolStatus.Archived)
        {
            school.ArchivedAtUtc = school.UpdatedAtUtc;
        }

        if (_audit is not null)
        {
            await _audit.QueueAsync(
                new AuditEvent(
                    SchoolId: school.Id,
                    Action: "School.StatusChanged",
                    EntityType: "School",
                    EntityId: school.Id.ToString("D"),
                    Feature: "SchoolManagement",
                    OldValues: new Dictionary<string, object?>
                    {
                        ["status"] = previousStatus.ToString()
                    },
                    NewValues: new Dictionary<string, object?>
                    {
                        ["status"] = school.Status.ToString()
                    },
                    ResultSummary:
                        $"School status changed from {previousStatus} to {school.Status}."),
                cancellationToken);
        }

        var saveResult = await _repository.SaveAsync(
            school,
            request.RowVersion,
            cancellationToken);

        return MapWriteResult(saveResult, school.Id);
    }

    public static string NormalizeSchoolCode(string value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();

    private static string NormalizeCountryCode(string value) =>
        (value ?? string.Empty).Trim().ToUpperInvariant();

    private static bool IsAllowedTransition(
        SchoolStatus current,
        SchoolStatus target) =>
        (current, target) switch
        {
            (SchoolStatus.Active, SchoolStatus.Suspended) => true,
            (SchoolStatus.Active, SchoolStatus.Archived) => true,
            (SchoolStatus.Suspended, SchoolStatus.Active) => true,
            (SchoolStatus.Suspended, SchoolStatus.Archived) => true,
            _ => false
        };

    private static List<SchoolValidationError> ValidateCreate(
        CreateSchoolRequest request)
    {
        var errors = ValidateCommon(
            request.Name,
            request.CountryCode,
            request.City,
            request.ContactEmail,
            request.DefaultCulture,
            request.TimeZoneId);

        var code = NormalizeSchoolCode(request.SchoolCode);

        if (string.IsNullOrWhiteSpace(code))
        {
            errors.Add(new(
                nameof(CreateSchoolRequest.SchoolCode),
                SchoolErrorCode.RequiredSchoolCode));
        }
        else
        {
            if (code.Length > SchoolCodeMaxLength)
            {
                errors.Add(new(
                    nameof(CreateSchoolRequest.SchoolCode),
                    SchoolErrorCode.SchoolCodeTooLong));
            }

            if (!SchoolCodePattern.IsMatch(code))
            {
                errors.Add(new(
                    nameof(CreateSchoolRequest.SchoolCode),
                    SchoolErrorCode.InvalidSchoolCode));
            }
        }

        return errors;
    }

    private static List<SchoolValidationError> ValidateUpdate(
        UpdateSchoolRequest request)
    {
        var errors = ValidateCommon(
            request.Name,
            request.CountryCode,
            request.City,
            request.ContactEmail,
            request.DefaultCulture,
            request.TimeZoneId);

        if (request.RowVersion is not { Length: > 0 })
        {
            errors.Add(new(
                string.Empty,
                SchoolErrorCode.ConcurrencyConflict));
        }

        return errors;
    }

    private static List<SchoolValidationError> ValidateCommon(
        string name,
        string countryCode,
        string city,
        string contactEmail,
        string defaultCulture,
        string timeZoneId)
    {
        var errors = new List<SchoolValidationError>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(new(
                "Name",
                SchoolErrorCode.RequiredName));
        }
        else if (name.Trim().Length > NameMaxLength)
        {
            errors.Add(new(
                "Name",
                SchoolErrorCode.NameTooLong));
        }

        if (string.IsNullOrWhiteSpace(countryCode))
        {
            errors.Add(new(
                "CountryCode",
                SchoolErrorCode.RequiredCountryCode));
        }
        else if (countryCode.Trim().Length > CountryCodeMaxLength)
        {
            errors.Add(new(
                "CountryCode",
                SchoolErrorCode.CountryCodeTooLong));
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            errors.Add(new(
                "City",
                SchoolErrorCode.RequiredCity));
        }
        else if (city.Trim().Length > CityMaxLength)
        {
            errors.Add(new(
                "City",
                SchoolErrorCode.CityTooLong));
        }

        if (string.IsNullOrWhiteSpace(contactEmail))
        {
            errors.Add(new(
                "ContactEmail",
                SchoolErrorCode.RequiredContactEmail));
        }
        else
        {
            var email = contactEmail.Trim();

            if (email.Length > ContactEmailMaxLength)
            {
                errors.Add(new(
                    "ContactEmail",
                    SchoolErrorCode.ContactEmailTooLong));
            }
            else if (!IsValidEmail(email))
            {
                errors.Add(new(
                    "ContactEmail",
                    SchoolErrorCode.InvalidContactEmail));
            }
        }

        if (string.IsNullOrWhiteSpace(defaultCulture))
        {
            errors.Add(new(
                "DefaultCulture",
                SchoolErrorCode.RequiredDefaultCulture));
        }
        else if (!SupportedCultures.Contains(defaultCulture.Trim()))
        {
            errors.Add(new(
                "DefaultCulture",
                SchoolErrorCode.InvalidDefaultCulture));
        }

        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            errors.Add(new(
                "TimeZoneId",
                SchoolErrorCode.RequiredTimeZoneId));
        }
        else if (timeZoneId.Trim().Length > TimeZoneIdMaxLength)
        {
            errors.Add(new(
                "TimeZoneId",
                SchoolErrorCode.TimeZoneIdTooLong));
        }

        return errors;
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var parsed = new MailAddress(email);
            return string.Equals(
                parsed.Address,
                email,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static SchoolCommandResult MapWriteResult(
        SchoolRepositoryWriteResult result,
        Guid schoolId) =>
        result switch
        {
            SchoolRepositoryWriteResult.Success =>
                SchoolCommandResult.Success(schoolId),

            SchoolRepositoryWriteResult.ConcurrencyConflict =>
                SchoolCommandResult.Failure(
                    new SchoolValidationError(
                        string.Empty,
                        SchoolErrorCode.ConcurrencyConflict)),

            SchoolRepositoryWriteResult.ConstraintViolation =>
                SchoolCommandResult.Failure(
                    new SchoolValidationError(
                        string.Empty,
                        SchoolErrorCode.PersistenceError)),

            _ =>
                SchoolCommandResult.Failure(
                    new SchoolValidationError(
                        string.Empty,
                        SchoolErrorCode.PersistenceError))
        };

    private static SchoolListItem MapListItem(School school) =>
        new(
            school.Id,
            school.Name,
            school.SchoolCode,
            school.Status,
            school.CountryCode,
            school.City,
            school.CreatedAtUtc,
            CanEdit(school.Status),
            school.Status == SchoolStatus.Active,
            school.Status == SchoolStatus.Suspended,
            school.Status is SchoolStatus.Active or SchoolStatus.Suspended);

    private static SchoolDetails MapDetails(School school) =>
        new(
            school.Id,
            school.Name,
            school.SchoolCode,
            school.Status,
            school.CountryCode,
            school.City,
            school.ContactEmail,
            school.DefaultCulture,
            school.TimeZoneId,
            school.CreatedAtUtc,
            school.UpdatedAtUtc,
            school.ArchivedAtUtc,
            school.RowVersion.ToArray(),
            CanEdit(school.Status),
            school.Status == SchoolStatus.Active,
            school.Status == SchoolStatus.Suspended,
            school.Status is SchoolStatus.Active or SchoolStatus.Suspended);

    private static bool CanEdit(SchoolStatus status) =>
        status != SchoolStatus.Archived;
}
