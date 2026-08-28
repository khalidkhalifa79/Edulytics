using Edulytics.Core.Enums;

namespace Edulytics.Core.Entities;

/// <summary>
/// Canonical Edulytics lesson body. Platform/curriculum scoped; never school scoped.
/// </summary>
public sealed class CurriculumLessonContent
{
    public Guid Id { get; set; }
    public Guid FrameworkVersionId { get; set; }

    /// <summary>
    /// Universal lesson identity is the Edulytics pedagogical lesson.
    /// </summary>
    public Guid PedagogicalLessonId { get; set; }

    public CanonicalLessonContentStatus Status { get; set; }
    public string ContentVersion { get; set; } = "1";
    public DateTime? VerifiedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
