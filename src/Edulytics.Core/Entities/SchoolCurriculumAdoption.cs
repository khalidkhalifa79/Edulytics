using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class SchoolCurriculumAdoption : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid? AcademicYearId { get; set; }
    public Guid GradeLevelId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid FrameworkVersionId { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
