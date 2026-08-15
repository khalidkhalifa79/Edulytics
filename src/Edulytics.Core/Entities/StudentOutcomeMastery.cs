using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class StudentOutcomeMastery : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid ClassGroupId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid StudentProfileId { get; set; }
    public Guid LearningOutcomeId { get; set; }
    public decimal EarnedScore { get; set; }
    public decimal PossibleScore { get; set; }
    public decimal MasteryPercentage { get; set; }
    public int EvidenceCount { get; set; }
    public MasteryBand Band { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
}
