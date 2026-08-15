using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class Assessment : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid ClassGroupId { get; set; }
    public Guid AcademicYearId { get; set; }
    public Guid TermId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateOnly AssessmentDate { get; set; }
    public decimal MaxScore { get; set; }
    public AssessmentStatus Status { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
