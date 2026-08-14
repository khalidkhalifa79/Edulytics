using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class GradeLevel : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
}
