namespace Edulytics.Core.Entities;

public sealed class CurriculumPackImportState
{
    public Guid Id { get; set; }
    public Guid FrameworkVersionId { get; set; }
    public string FrameworkCode { get; set; } = string.Empty;
    public string VersionCode { get; set; } = string.Empty;
    public string SourceDigest { get; set; } = string.Empty;
    public string ContentDigest { get; set; } = string.Empty;
    public int NodeCount { get; set; }
    public int OfficialNodeCount { get; set; }
    public int UnitCount { get; set; }
    public int LessonCount { get; set; }
    public int LinkCount { get; set; }
    public bool IsComplete { get; set; }
    public DateTime ImportedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
