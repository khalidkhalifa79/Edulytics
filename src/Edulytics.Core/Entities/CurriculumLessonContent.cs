using Edulytics.Core.Enums;
namespace Edulytics.Core.Entities;

/// <summary>Canonical platform/curriculum lesson body. Never school scoped.</summary>
public sealed class CurriculumLessonContent
{
    public Guid Id { get; set; }
    public Guid FrameworkVersionId { get; set; }
    public Guid LessonNodeId { get; set; }
    public CanonicalLessonContentStatus Status { get; set; }
    public string ContentVersion { get; set; } = "1";
    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
