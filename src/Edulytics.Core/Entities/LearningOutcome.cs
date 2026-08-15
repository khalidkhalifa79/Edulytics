using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class LearningOutcome : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid FrameworkVersionId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid GradeLevelId { get; set; }
    public Guid TopicId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public int Order { get; set; }
}
