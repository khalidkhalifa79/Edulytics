namespace Edulytics.Core.Entities;

public sealed class CurriculumPackContentNode
{
    public Guid Id { get; set; }
    public Guid FrameworkVersionId { get; set; }
    public string FrameworkCode { get; set; } = string.Empty;
    public string VersionCode { get; set; } = string.Empty;
    public string NodeKind { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    public int LogicalLevelFrom { get; set; }
    public int LogicalLevelTo { get; set; }
    public string NativeLevel { get; set; } = string.Empty;
    public string? Pathway { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OfficialText { get; set; }
    public string? AuthorDescription { get; set; }
    public string SourceAuthority { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceLocator { get; set; } = string.Empty;
    public string Attribution { get; set; } = string.Empty;
    public bool IsOfficial { get; set; }
    public bool IsActive { get; set; }
    public int SortOrder { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
