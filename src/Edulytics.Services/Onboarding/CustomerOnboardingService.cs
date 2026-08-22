using System.Net.Mail;
using System.Text.RegularExpressions;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Onboarding;
using Edulytics.Services.Auditing;

namespace Edulytics.Services.Onboarding;

public sealed class CustomerOnboardingService : ICustomerOnboardingService
{
    private const int MinimumStudents = 500;
    private const int SchoolNameMax = 200;
    private const int ContactNameMax = 150;
    private const int EmailMax = 256;
    private const int PhoneMax = 50;
    private const int CountryMax = 10;
    private const int CityMax = 150;
    private const int MessageMax = 2000;
    private const int InternalNoteMax = 2000;
    private const int SchoolCodeMax = 50;
    private const int TimeZoneMax = 100;

    private static readonly Regex SchoolCodePattern = new(
        "^[A-Z0-9-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ICustomerOnboardingRepository _repository;
    private readonly IAuditService _audit;

    public CustomerOnboardingService(
        ICustomerOnboardingRepository repository,
        IAuditService audit)
    {
        _repository = repository;
        _audit = audit;
    }

    public async Task<OnboardingCommandResult> SubmitDemoRequestAsync(
        DemoRequestSubmission request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateSubmission(request);
        if (errors.Count > 0)
            return new OnboardingCommandResult(false, errors);

        var normalizedEmail = request.WorkEmail.Trim().ToUpperInvariant();

        if (await _repository.ExistsOpenByNormalizedEmailAsync(
                normalizedEmail,
                cancellationToken))
        {
            // Deliberately generic/idempotent: do not reveal whether a lead exists.
            return OnboardingCommandResult.Success();
        }

        var now = DateTime.UtcNow;
        var entity = new DemoRequest
        {
            Id = Guid.NewGuid(),
            SchoolName = request.SchoolName.Trim(),
            ContactName = request.ContactName.Trim(),
            WorkEmail = request.WorkEmail.Trim(),
            NormalizedWorkEmail = normalizedEmail,
            Phone = NullIfWhiteSpace(request.Phone),
            CountryCode = request.CountryCode.Trim().ToUpperInvariant(),
            City = request.City.Trim(),
            EstimatedStudentCount = request.EstimatedStudentCount,
            Message = NullIfWhiteSpace(request.Message),
            Status = DemoRequestStatus.New,
            PrivacyConsentAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            RowVersion = []
        };

        var write = await _repository.AddRequestAsync(entity, cancellationToken);
        if (!write.Succeeded)
            return Map(write.Error);

        await AuditAsync(
            null,
            "DemoRequest.Submitted",
            "DemoRequest",
            entity.Id,
            new Dictionary<string, object?>
            {
                ["status"] = entity.Status.ToString(),
                ["countryCode"] = entity.CountryCode,
                ["estimatedStudentCount"] = entity.EstimatedStudentCount
            },
            cancellationToken);

        return OnboardingCommandResult.Success();
    }

    public async Task<IReadOnlyList<DemoRequestListItem>> ListAsync(
        CancellationToken cancellationToken = default) =>
        (await _repository.ListRequestsAsync(cancellationToken))
            .Select(x => new DemoRequestListItem(
                x.Id,
                x.SchoolName,
                x.ContactName,
                x.WorkEmail,
                x.CountryCode,
                x.EstimatedStudentCount,
                x.Status,
                x.CreatedAtUtc))
            .ToArray();

    public async Task<DemoRequestDetails?> GetAsync(
        Guid requestId,
        CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetRequestAsync(requestId, cancellationToken);
        if (request is null)
            return null;

        var access = await _repository.GetDemoAccessByRequestAsync(
            requestId,
            cancellationToken);

        return MapDetails(request, access, DateTime.UtcNow);
    }

    public async Task<OnboardingCommandResult> UpdateLeadAsync(
        Guid requestId,
        DemoRequestStatus targetStatus,
        DateTime? demoScheduledAtUtc,
        string? internalNote,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetRequestAsync(requestId, cancellationToken);
        if (request is null)
            return Fail(OnboardingErrorCode.NotFound);

        if (expectedRowVersion.Length == 0 ||
            !request.RowVersion.SequenceEqual(expectedRowVersion))
            return Fail(OnboardingErrorCode.ConcurrencyConflict);

        if (!CanTransition(request.Status, targetStatus))
            return Fail(OnboardingErrorCode.InvalidTransition);

        if (targetStatus == DemoRequestStatus.DemoScheduled &&
            !demoScheduledAtUtc.HasValue)
            return OnboardingCommandResult.Failure(
                nameof(demoScheduledAtUtc),
                OnboardingErrorCode.DemoScheduleRequired);

        if ((internalNote?.Trim().Length ?? 0) > InternalNoteMax)
            return OnboardingCommandResult.Failure(
                nameof(internalNote),
                OnboardingErrorCode.InternalNoteTooLong);

        var oldStatus = request.Status;
        request.Status = targetStatus;
        if (targetStatus == DemoRequestStatus.DemoScheduled)
        {
            request.DemoScheduledAtUtc =
                DateTime.SpecifyKind(
                    demoScheduledAtUtc!.Value,
                    DateTimeKind.Utc);
        }

        request.InternalNote = NullIfWhiteSpace(internalNote);
        request.UpdatedAtUtc = DateTime.UtcNow;

        var write = await _repository.SaveRequestAsync(
            request,
            expectedRowVersion,
            cancellationToken);

        if (!write.Succeeded)
            return Map(write.Error);

        await AuditAsync(
            request.ProvisionedSchoolId ?? request.DemoSchoolId,
            "DemoRequest.StatusChanged",
            "DemoRequest",
            request.Id,
            new Dictionary<string, object?>
            {
                ["from"] = oldStatus.ToString(),
                ["to"] = targetStatus.ToString()
            },
            cancellationToken);

        return OnboardingCommandResult.Success();
    }

    public async Task<OnboardingCommandResult> GrantDemoAsync(
        Guid requestId,
        byte[] expectedRequestRowVersion,
        CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetRequestAsync(requestId, cancellationToken);
        if (request is null)
            return Fail(OnboardingErrorCode.NotFound);

        if (request.Status != DemoRequestStatus.Qualified)
            return Fail(OnboardingErrorCode.DemoNotQualified);

        if (request.DemoSchoolId.HasValue ||
            await _repository.GetDemoAccessByRequestAsync(requestId, cancellationToken)
                is not null)
            return Fail(OnboardingErrorCode.DemoAlreadyExists);

        var now = DateTime.UtcNow;
        var provision = await _repository.CreateDemoAsync(
            requestId,
            expectedRequestRowVersion,
            now,
            now.AddDays(7),
            cancellationToken);

        if (!provision.Succeeded)
            return Map(provision.Error);

        await AuditAsync(
            provision.SchoolId,
            "DemoAccess.Granted",
            "DemoAccess",
            request.Id,
            new Dictionary<string, object?> { ["durationDays"] = 7 },
            cancellationToken);

        return OnboardingCommandResult.Success(Invitation(provision));
    }

    public async Task<OnboardingCommandResult> ExtendDemoAsync(
        Guid requestId,
        byte[] expectedAccessRowVersion,
        CancellationToken cancellationToken = default)
    {
        var access = await _repository.GetDemoAccessByRequestAsync(
            requestId,
            cancellationToken);

        if (access is null)
            return Fail(OnboardingErrorCode.DemoNotFound);
        if (access.ConvertedAtUtc.HasValue)
            return Fail(OnboardingErrorCode.DemoAlreadyConverted);
        if (access.RevokedAtUtc.HasValue)
            return Fail(OnboardingErrorCode.DemoAlreadyRevoked);
        if (!access.RowVersion.SequenceEqual(expectedAccessRowVersion))
            return Fail(OnboardingErrorCode.ConcurrencyConflict);

        var now = DateTime.UtcNow;
        access.ExpiresAtUtc = (access.ExpiresAtUtc > now ? access.ExpiresAtUtc : now)
            .AddDays(7);
        access.UpdatedAtUtc = now;

        var write = await _repository.SaveDemoAccessAsync(
            access,
            expectedAccessRowVersion,
            cancellationToken);

        if (!write.Succeeded)
            return Map(write.Error);

        await AuditAsync(
            access.SchoolId,
            "DemoAccess.Extended",
            "DemoAccess",
            access.Id,
            new Dictionary<string, object?> { ["extensionDays"] = 7 },
            cancellationToken);

        return OnboardingCommandResult.Success();
    }

    public async Task<OnboardingCommandResult> ExpireDemoAsync(
        Guid requestId,
        byte[] expectedAccessRowVersion,
        CancellationToken cancellationToken = default)
    {
        var access = await _repository.GetDemoAccessByRequestAsync(
            requestId,
            cancellationToken);
        if (access is null)
            return Fail(OnboardingErrorCode.DemoNotFound);
        if (access.ConvertedAtUtc.HasValue)
            return Fail(OnboardingErrorCode.DemoAlreadyConverted);
        if (!access.RowVersion.SequenceEqual(expectedAccessRowVersion))
            return Fail(OnboardingErrorCode.ConcurrencyConflict);

        var now = DateTime.UtcNow;
        access.ExpiresAtUtc = now;
        access.UpdatedAtUtc = now;

        var write = await _repository.SaveDemoAccessAsync(
            access,
            expectedAccessRowVersion,
            cancellationToken);
        if (!write.Succeeded)
            return Map(write.Error);

        await AuditAsync(
            access.SchoolId,
            "DemoAccess.Expired",
            "DemoAccess",
            access.Id,
            null,
            cancellationToken);

        return OnboardingCommandResult.Success();
    }

    public async Task<OnboardingCommandResult> RevokeDemoAsync(
        Guid requestId,
        string reason,
        byte[] expectedAccessRowVersion,
        CancellationToken cancellationToken = default)
    {
        var cleanReason = reason?.Trim() ?? string.Empty;
        if (cleanReason.Length == 0)
            return OnboardingCommandResult.Failure(
                nameof(reason),
                OnboardingErrorCode.RevokeReasonRequired);
        if (cleanReason.Length > 500)
            return OnboardingCommandResult.Failure(
                nameof(reason),
                OnboardingErrorCode.RevokeReasonTooLong);

        var access = await _repository.GetDemoAccessByRequestAsync(
            requestId,
            cancellationToken);
        if (access is null)
            return Fail(OnboardingErrorCode.DemoNotFound);
        if (access.ConvertedAtUtc.HasValue)
            return Fail(OnboardingErrorCode.DemoAlreadyConverted);
        if (access.RevokedAtUtc.HasValue)
            return Fail(OnboardingErrorCode.DemoAlreadyRevoked);
        if (!access.RowVersion.SequenceEqual(expectedAccessRowVersion))
            return Fail(OnboardingErrorCode.ConcurrencyConflict);

        var now = DateTime.UtcNow;
        access.RevokedAtUtc = now;
        access.RevokedReason = cleanReason;
        access.UpdatedAtUtc = now;

        var write = await _repository.SaveDemoAccessAsync(
            access,
            expectedAccessRowVersion,
            cancellationToken);
        if (!write.Succeeded)
            return Map(write.Error);

        await AuditAsync(
            access.SchoolId,
            "DemoAccess.Revoked",
            "DemoAccess",
            access.Id,
            new Dictionary<string, object?> { ["revoked"] = true },
            cancellationToken);

        return OnboardingCommandResult.Success();
    }

    public async Task<OnboardingCommandResult> ProvisionCustomerAsync(
        Guid requestId,
        string schoolCode,
        string defaultCulture,
        string timeZoneId,
        byte[] expectedRequestRowVersion,
        CancellationToken cancellationToken = default)
    {
        var request = await _repository.GetRequestAsync(requestId, cancellationToken);
        if (request is null)
            return Fail(OnboardingErrorCode.NotFound);
        if (request.Status != DemoRequestStatus.Won)
            return Fail(OnboardingErrorCode.ProvisionRequiresWon);
        if (!request.RowVersion.SequenceEqual(expectedRequestRowVersion))
            return Fail(OnboardingErrorCode.ConcurrencyConflict);

        var cleanCode = (schoolCode ?? string.Empty).Trim().ToUpperInvariant();
        if (cleanCode.Length == 0)
            return OnboardingCommandResult.Failure(
                nameof(schoolCode),
                OnboardingErrorCode.RequiredSchoolCode);
        if (cleanCode.Length > SchoolCodeMax)
            return OnboardingCommandResult.Failure(
                nameof(schoolCode),
                OnboardingErrorCode.SchoolCodeTooLong);
        if (!SchoolCodePattern.IsMatch(cleanCode))
            return OnboardingCommandResult.Failure(
                nameof(schoolCode),
                OnboardingErrorCode.InvalidSchoolCode);

        var culture = (defaultCulture ?? string.Empty).Trim().ToLowerInvariant();
        if (culture is not ("en" or "pl"))
            return OnboardingCommandResult.Failure(
                nameof(defaultCulture),
                OnboardingErrorCode.InvalidCulture);

        var zone = (timeZoneId ?? string.Empty).Trim();
        if (zone.Length == 0)
            return OnboardingCommandResult.Failure(
                nameof(timeZoneId),
                OnboardingErrorCode.RequiredTimeZone);
        if (zone.Length > TimeZoneMax)
            return OnboardingCommandResult.Failure(
                nameof(timeZoneId),
                OnboardingErrorCode.TimeZoneTooLong);

        var provision = await _repository.ProvisionCustomerAsync(
            requestId,
            expectedRequestRowVersion,
            cleanCode,
            culture,
            zone,
            DateTime.UtcNow,
            cancellationToken);

        if (!provision.Succeeded)
            return Map(provision.Error);

        await AuditAsync(
            provision.SchoolId,
            "CustomerOnboarding.Provisioned",
            "School",
            provision.SchoolId,
            new Dictionary<string, object?>
            {
                ["status"] = SchoolStatus.Suspended.ToString(),
                ["activationPending"] = true
            },
            cancellationToken);

        return OnboardingCommandResult.Success(Invitation(provision));
    }

    private static List<OnboardingError> ValidateSubmission(DemoRequestSubmission request)
    {
        var errors = new List<OnboardingError>();
        ValidateRequired(request.SchoolName, nameof(request.SchoolName), SchoolNameMax,
            OnboardingErrorCode.RequiredSchoolName,
            OnboardingErrorCode.SchoolNameTooLong,
            errors);
        ValidateRequired(request.ContactName, nameof(request.ContactName), ContactNameMax,
            OnboardingErrorCode.RequiredContactName,
            OnboardingErrorCode.ContactNameTooLong,
            errors);

        var email = request.WorkEmail?.Trim() ?? string.Empty;
        if (email.Length == 0)
            errors.Add(new(nameof(request.WorkEmail), OnboardingErrorCode.RequiredEmail));
        else if (email.Length > EmailMax)
            errors.Add(new(nameof(request.WorkEmail), OnboardingErrorCode.EmailTooLong));
        else if (!IsValidEmail(email))
            errors.Add(new(nameof(request.WorkEmail), OnboardingErrorCode.InvalidEmail));

        if ((request.Phone?.Trim().Length ?? 0) > PhoneMax)
            errors.Add(new(nameof(request.Phone), OnboardingErrorCode.PhoneTooLong));

        ValidateRequired(request.CountryCode, nameof(request.CountryCode), CountryMax,
            OnboardingErrorCode.RequiredCountry,
            OnboardingErrorCode.CountryTooLong,
            errors);

        if (!string.IsNullOrWhiteSpace(request.CountryCode) &&
            request.CountryCode.Trim().Length <= CountryMax &&
            !SupportedCustomerCountries.IsSupported(request.CountryCode))
        {
            errors.Add(new(
                nameof(request.CountryCode),
                OnboardingErrorCode.UnsupportedCountry));
        }

        ValidateRequired(request.City, nameof(request.City), CityMax,
            OnboardingErrorCode.RequiredCity,
            OnboardingErrorCode.CityTooLong,
            errors);

        if (request.EstimatedStudentCount < MinimumStudents)
            errors.Add(new(nameof(request.EstimatedStudentCount),
                OnboardingErrorCode.MinimumStudentCount));
        if ((request.Message?.Trim().Length ?? 0) > MessageMax)
            errors.Add(new(nameof(request.Message), OnboardingErrorCode.MessageTooLong));
        if (!request.PrivacyAccepted)
            errors.Add(new(nameof(request.PrivacyAccepted),
                OnboardingErrorCode.PrivacyConsentRequired));

        return errors;
    }

    private static void ValidateRequired(
        string value,
        string field,
        int max,
        OnboardingErrorCode required,
        OnboardingErrorCode tooLong,
        ICollection<OnboardingError> errors)
    {
        var clean = value?.Trim() ?? string.Empty;
        if (clean.Length == 0)
            errors.Add(new(field, required));
        else if (clean.Length > max)
            errors.Add(new(field, tooLong));
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var parsed = new MailAddress(email);
            return string.Equals(parsed.Address, email, StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static bool CanTransition(DemoRequestStatus current, DemoRequestStatus target)
    {
        if (target == DemoRequestStatus.Lost &&
            current is not (DemoRequestStatus.Won or DemoRequestStatus.Lost))
            return true;

        return (current, target) switch
        {
            (DemoRequestStatus.New, DemoRequestStatus.Contacted) => true,
            (DemoRequestStatus.Contacted, DemoRequestStatus.DemoScheduled) => true,
            (DemoRequestStatus.DemoScheduled, DemoRequestStatus.DemoCompleted) => true,
            (DemoRequestStatus.DemoCompleted, DemoRequestStatus.Qualified) => true,
            (DemoRequestStatus.Qualified, DemoRequestStatus.Won) => true,
            _ => false
        };
    }

    private static IReadOnlyList<DemoRequestStatus> NextStatuses(DemoRequestStatus current)
    {
        IReadOnlyList<DemoRequestStatus> next = current switch
        {
            DemoRequestStatus.New => [DemoRequestStatus.Contacted],
            DemoRequestStatus.Contacted => [DemoRequestStatus.DemoScheduled],
            DemoRequestStatus.DemoScheduled => [DemoRequestStatus.DemoCompleted],
            DemoRequestStatus.DemoCompleted => [DemoRequestStatus.Qualified],
            DemoRequestStatus.Qualified => [DemoRequestStatus.Won],
            _ => Array.Empty<DemoRequestStatus>()
        };

        return current is not (DemoRequestStatus.Won or DemoRequestStatus.Lost)
            ? next.Concat([DemoRequestStatus.Lost]).ToArray()
            : next;
    }

    private static DemoRequestDetails MapDetails(
        DemoRequest request,
        DemoAccess? access,
        DateTime utcNow)
    {
        var accessDetails = access is null
            ? null
            : new DemoAccessDetails(
                access.Id,
                access.SchoolId,
                access.SchoolAdminUserId,
                access.StartsAtUtc,
                access.ExpiresAtUtc,
                access.RevokedAtUtc,
                access.RevokedReason,
                access.ConvertedAtUtc,
                access.RowVersion.ToArray(),
                access.ConvertedAtUtc is null &&
                access.RevokedAtUtc is null &&
                access.StartsAtUtc <= utcNow &&
                access.ExpiresAtUtc > utcNow);

        var canModifyDemo = access is not null && access.ConvertedAtUtc is null;

        return new DemoRequestDetails(
            request.Id,
            request.SchoolName,
            request.ContactName,
            request.WorkEmail,
            request.Phone,
            request.CountryCode,
            request.City,
            request.EstimatedStudentCount,
            request.Message,
            request.Status,
            request.DemoScheduledAtUtc,
            request.InternalNote,
            request.PrivacyConsentAtUtc,
            request.DemoSchoolId,
            request.ProvisionedSchoolId,
            request.ProvisionedSchoolAdminUserId,
            request.CreatedAtUtc,
            request.UpdatedAtUtc,
            request.RowVersion.ToArray(),
            accessDetails,
            NextStatuses(request.Status),
            request.Status == DemoRequestStatus.Qualified && access is null,
            canModifyDemo && access!.RevokedAtUtc is null,
            canModifyDemo,
            canModifyDemo && access!.RevokedAtUtc is null,
            request.Status == DemoRequestStatus.Won && request.ProvisionedSchoolId is null);
    }

    private static OnboardingInvitation Invitation(CustomerOnboardingProvisionResult result) =>
        new(
            result.SchoolAdminUserId!.Value,
            result.PasswordSetupToken!,
            result.RecipientEmail!,
            result.SchoolName!,
            result.Culture!);

    private static OnboardingCommandResult Map(CustomerOnboardingPersistenceError error) =>
        error switch
        {
            CustomerOnboardingPersistenceError.NotFound =>
                Fail(OnboardingErrorCode.NotFound),
            CustomerOnboardingPersistenceError.ConcurrencyConflict =>
                Fail(OnboardingErrorCode.ConcurrencyConflict),
            CustomerOnboardingPersistenceError.DuplicateEmail =>
                Fail(OnboardingErrorCode.DuplicateEmail),
            CustomerOnboardingPersistenceError.DuplicateSchoolCode =>
                Fail(OnboardingErrorCode.DuplicateSchoolCode),
            _ => Fail(OnboardingErrorCode.PersistenceError)
        };

    private static OnboardingCommandResult Fail(OnboardingErrorCode code) =>
        OnboardingCommandResult.Failure(string.Empty, code);

    private async Task AuditAsync(
        Guid? schoolId,
        string action,
        string entityType,
        Guid? entityId,
        IReadOnlyDictionary<string, object?>? newValues,
        CancellationToken cancellationToken)
    {
await _audit.RecordAsync(
            new AuditEvent(
                SchoolId: schoolId,
                Action: action,
                EntityType: entityType,
                EntityId: entityId?.ToString("D"),
                Feature: "CustomerOnboarding",
                NewValues: newValues,
                ResultSummary: "Phase25B onboarding action completed."),
            cancellationToken);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
