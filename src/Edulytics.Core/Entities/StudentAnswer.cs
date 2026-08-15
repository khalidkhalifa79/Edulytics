using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class StudentAnswer : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid AssessmentResultId { get; set; }
    public Guid AssessmentQuestionId { get; set; }
    public decimal Score { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
