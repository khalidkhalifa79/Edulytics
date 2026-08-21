using Edulytics.Core.Enums;
using Edulytics.Services.Onboarding;

namespace Edulytics.Web.ViewModels.Onboarding;

public sealed class RequestDemoViewModel
{
    public string? SchoolName { get; set; }
    public string? ContactName { get; set; }
    public string? WorkEmail { get; set; }
    public string? Phone { get; set; }
    public string? CountryCode { get; set; }
    public string? City { get; set; }
    public int EstimatedStudentCount { get; set; } = 500;
    public string? Message { get; set; }
    public bool PrivacyAccepted { get; set; }
}

public sealed record DemoLeadListViewModel(
    IReadOnlyList<DemoRequestListItem> Items);

public sealed class DemoLeadDetailsViewModel
{
    public required DemoRequestDetails Lead { get; init; }

    public string RequestRowVersionBase64 =>
        Convert.ToBase64String(Lead.RowVersion);

    public string? AccessRowVersionBase64 =>
        Lead.DemoAccess is null
            ? null
            : Convert.ToBase64String(Lead.DemoAccess.RowVersion);

    public string SuggestedCulture =>
        string.Equals(Lead.CountryCode, "PL", StringComparison.OrdinalIgnoreCase)
            ? "pl"
            : "en";

    public string SuggestedTimeZone =>
        Lead.CountryCode.ToUpperInvariant() switch
        {
            "PL" => "Europe/Warsaw",
            "AE" => "Asia/Dubai",
            _ => "UTC"
        };

    public string SuggestedSchoolCode =>
        $"SCH-{Lead.Id:N}"[..12].ToUpperInvariant();

    public IReadOnlyList<DemoRequestStatus> AllowedNextStatuses =>
        Lead.AllowedNextStatuses;
}
