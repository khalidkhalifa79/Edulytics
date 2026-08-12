using Edulytics.Core.Enums;

namespace Edulytics.Core.Entities;

public class School
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SchoolCode { get; set; } = string.Empty;
    public string NormalizedSchoolCode { get; set; } = string.Empty;
    public SchoolStatus Status { get; set; }
    public string CountryCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string DefaultCulture { get; set; } = "en";
    public string TimeZoneId { get; set; } = "UTC";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
