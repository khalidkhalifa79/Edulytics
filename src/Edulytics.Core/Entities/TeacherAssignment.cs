using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class TeacherAssignment : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid TeacherUserId { get; set; }
    public Guid ClassGroupId { get; set; }
    public Guid SubjectId { get; set; }
    public Guid AcademicYearId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
