using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class StudentEnrollment : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid StudentProfileId { get; set; }
    public Guid ClassGroupId { get; set; }
    public Guid AcademicYearId { get; set; }
    public DateTime EnrolledAtUtc { get; set; }
}
