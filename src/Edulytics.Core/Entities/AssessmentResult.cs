using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class AssessmentResult : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid AssessmentId { get; set; }
    public Guid StudentProfileId { get; set; }
    public decimal Score { get; set; }
    public decimal Percentage { get; set; }
    public Guid EnteredByUserId { get; set; }
    public DateTime EnteredAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
