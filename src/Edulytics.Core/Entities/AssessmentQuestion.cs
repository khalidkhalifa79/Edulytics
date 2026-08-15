using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class AssessmentQuestion : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid AssessmentId { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public int Order { get; set; }
}
