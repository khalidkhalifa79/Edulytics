namespace Edulytics.Core.Entities;

public sealed class CurriculumFramework
{
    public Guid Id { get; set; }
    public Guid? OwnerSchoolId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NormalizedCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? CountryCode { get; set; }
    public string? ProviderName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
