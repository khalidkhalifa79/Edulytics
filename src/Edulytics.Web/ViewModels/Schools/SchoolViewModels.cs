using Edulytics.Core.Enums;
using Edulytics.Services.Schools;

namespace Edulytics.Web.ViewModels.Schools;

public sealed class SchoolListViewModel
{
    public IReadOnlyList<SchoolListRowViewModel> Schools { get; init; } =
        Array.Empty<SchoolListRowViewModel>();
}

public sealed class SchoolListRowViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string SchoolCode { get; init; } = string.Empty;

    public SchoolStatus Status { get; init; }

    public string CountryCode { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public bool CanEdit { get; init; }

    public bool CanSuspend { get; init; }

    public bool CanReactivate { get; init; }

    public bool CanArchive { get; init; }

    public static SchoolListRowViewModel FromService(
        SchoolListItem school) =>
        new()
        {
            Id = school.Id,
            Name = school.Name,
            SchoolCode = school.SchoolCode,
            Status = school.Status,
            CountryCode = school.CountryCode,
            City = school.City,
            CreatedAtUtc = school.CreatedAtUtc,
            CanEdit = school.CanEdit,
            CanSuspend = school.CanSuspend,
            CanReactivate = school.CanReactivate,
            CanArchive = school.CanArchive
        };
}

public sealed class SchoolFormViewModel
{
    // Nullable by design. Required-field validation belongs to the
    // school service so user-facing errors come from localized resources
    // instead of MVC's implicit non-localized RequiredAttribute messages.
    public Guid? Id { get; set; }

    public string? Name { get; set; } = string.Empty;

    public string? SchoolCode { get; set; } = string.Empty;

    public string? CountryCode { get; set; } = string.Empty;

    public string? City { get; set; } = string.Empty;

    public string? ContactEmail { get; set; } = string.Empty;

    public string? DefaultCulture { get; set; } = string.Empty;

    public string? TimeZoneId { get; set; } = string.Empty;

    public string? RowVersionBase64 { get; set; } = string.Empty;
}

public sealed class SchoolDetailsViewModel
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string SchoolCode { get; init; } = string.Empty;

    public SchoolStatus Status { get; init; }

    public string CountryCode { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string ContactEmail { get; init; } = string.Empty;

    public string DefaultCulture { get; init; } = string.Empty;

    public string TimeZoneId { get; init; } = string.Empty;

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public DateTime? ArchivedAtUtc { get; init; }

    public string RowVersionBase64 { get; init; } = string.Empty;

    public bool CanEdit { get; init; }

    public bool CanSuspend { get; init; }

    public bool CanReactivate { get; init; }

    public bool CanArchive { get; init; }

    public static SchoolDetailsViewModel FromService(
        SchoolDetails school) =>
        new()
        {
            Id = school.Id,
            Name = school.Name,
            SchoolCode = school.SchoolCode,
            Status = school.Status,
            CountryCode = school.CountryCode,
            City = school.City,
            ContactEmail = school.ContactEmail,
            DefaultCulture = school.DefaultCulture,
            TimeZoneId = school.TimeZoneId,
            CreatedAtUtc = school.CreatedAtUtc,
            UpdatedAtUtc = school.UpdatedAtUtc,
            ArchivedAtUtc = school.ArchivedAtUtc,
            RowVersionBase64 =
                Convert.ToBase64String(school.RowVersion),
            CanEdit = school.CanEdit,
            CanSuspend = school.CanSuspend,
            CanReactivate = school.CanReactivate,
            CanArchive = school.CanArchive
        };
}
