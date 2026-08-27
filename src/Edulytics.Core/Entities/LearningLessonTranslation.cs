using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class LearningLessonTranslation : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid LessonId { get; set; }
    public string CultureCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string KeyConceptsAndRules { get; set; } = string.Empty;
    public string WorkedExamples { get; set; } = string.Empty;
    public string StepByStepSolutions { get; set; } = string.Empty;
    public string CommonMistakes { get; set; } = string.Empty;
    public string QuickSummary { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
