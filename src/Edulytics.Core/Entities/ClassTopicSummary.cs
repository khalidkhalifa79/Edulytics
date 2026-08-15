using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class ClassTopicSummary : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid ClassGroupId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid CurriculumTopicId { get; set; }
    public decimal MasteryPercentage { get; set; }
    public int OutcomeCount { get; set; }
    public int WeakOutcomeCount { get; set; }
    public int StudentCount { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
}
