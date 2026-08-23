using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class StudentProfile : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid? UserId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string NormalizedStudentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public AcademicStructureStatus Status { get; set; }

    public bool IsArchived { get; set; }
    public DateTime? ArchivedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
