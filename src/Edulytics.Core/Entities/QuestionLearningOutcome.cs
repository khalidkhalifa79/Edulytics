using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class QuestionLearningOutcome : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid AssessmentQuestionId { get; set; }
    public Guid LearningOutcomeId { get; set; }
}
