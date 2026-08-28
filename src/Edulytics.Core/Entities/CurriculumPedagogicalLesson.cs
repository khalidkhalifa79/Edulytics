namespace Edulytics.Core.Entities;

/// <summary>
/// Edulytics-owned pedagogical lesson identity for a curriculum version.
/// It is platform scoped. A verified official curriculum Lesson node is optional provenance only.
/// </summary>
public sealed class CurriculumPedagogicalLesson
{
    public Guid Id { get; set; }
    public Guid FrameworkVersionId { get; set; }
    public Guid? OfficialLessonNodeId { get; set; }

    public string Code { get; set; } = string.Empty;
    public string UnitKey { get; set; } = string.Empty;
    public string UnitTitle { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

    public int LogicalLevelFrom { get; set; }
    public int LogicalLevelTo { get; set; }
    public string NativeLevel { get; set; } = string.Empty;
    public string? Pathway { get; set; }
    public int SortOrder { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
