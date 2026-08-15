using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class SchoolAnalyticsSnapshot : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid AcademicYearId { get; set; }
    public decimal OverallMasteryPercentage { get; set; }
    public int StudentsWithEvidence { get; set; }
    public int AtRiskStudents { get; set; }
    public int CriticalOutcomeCount { get; set; }
    public int WeakTopicCount { get; set; }
    public DateTime? LatestSourceUpdatedAtUtc { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
}
