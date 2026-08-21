using Edulytics.Core.Enums;

namespace Edulytics.Core.Entities;

public sealed class DemoRequest
{
    public Guid Id { get; set; }
    public string SchoolName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string WorkEmail { get; set; } = string.Empty;
    public string NormalizedWorkEmail { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int EstimatedStudentCount { get; set; }
    public string? Message { get; set; }
    public DemoRequestStatus Status { get; set; } = DemoRequestStatus.New;
    public DateTime? DemoScheduledAtUtc { get; set; }
    public string? InternalNote { get; set; }
    public DateTime PrivacyConsentAtUtc { get; set; }
    public Guid? DemoSchoolId { get; set; }
    public Guid? ProvisionedSchoolId { get; set; }
    public Guid? ProvisionedSchoolAdminUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
