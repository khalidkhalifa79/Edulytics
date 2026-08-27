using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class LearningLessonOutcome : ISchoolScoped
{
    public Guid SchoolId { get; set; }
    public Guid LessonId { get; set; }
    public Guid LearningOutcomeId { get; set; }
}
