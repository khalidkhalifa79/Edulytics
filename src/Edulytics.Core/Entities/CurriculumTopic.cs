using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class CurriculumTopic : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid FrameworkVersionId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid GradeLevelId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}
