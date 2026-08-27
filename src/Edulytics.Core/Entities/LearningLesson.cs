using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class LearningLesson : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid TopicId { get; set; }
    public int Order { get; set; }
    public LearningLessonStatus Status { get; set; } = LearningLessonStatus.Draft;
    public Guid CreatedByUserId { get; set; }
    public Guid? SubmittedByUserId { get; set; }
    public Guid? PublishedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public DateTime? PublishedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
