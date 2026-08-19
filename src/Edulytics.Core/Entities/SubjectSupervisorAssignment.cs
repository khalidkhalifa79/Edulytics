using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class SubjectSupervisorAssignment : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid SupervisorUserId { get; set; }
    public Guid SubjectId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
