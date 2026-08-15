using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class ClassOutcomeSummary : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid ClassGroupId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid LearningOutcomeId { get; set; }
    public decimal EarnedScore { get; set; }
    public decimal PossibleScore { get; set; }
    public decimal AverageMasteryPercentage { get; set; }
    public int StudentCount { get; set; }
    public int AtRiskStudentCount { get; set; }
    public int EvidenceCount { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
}
