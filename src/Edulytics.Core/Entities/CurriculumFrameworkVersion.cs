namespace Edulytics.Core.Entities;

public sealed class CurriculumFrameworkVersion
{
    public Guid Id { get; set; }
    public Guid FrameworkId { get; set; }
    public string VersionCode { get; set; } = string.Empty;
    public string NormalizedVersionCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateOnly? EffectiveFrom { get; set; }
    public DateOnly? EffectiveTo { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
