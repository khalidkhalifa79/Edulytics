namespace Edulytics.Core.Entities;

public sealed class CurriculumPackNodeLink
{
    public Guid Id { get; set; }
    public Guid FrameworkVersionId { get; set; }
    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }
    public string LinkKind { get; set; } = string.Empty;
    public string AlignmentConfidence { get; set; } = string.Empty;
    public string EvidenceNote { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
