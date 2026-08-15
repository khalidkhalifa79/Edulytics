using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class ClassAssessmentTrend : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid ClassGroupId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid AssessmentId { get; set; }
    public string AssessmentTitle { get; set; } = string.Empty;
    public DateOnly AssessmentDate { get; set; }
    public decimal AveragePercentage { get; set; }
    public int StudentCount { get; set; }
    public int AtRiskStudentCount { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
}
