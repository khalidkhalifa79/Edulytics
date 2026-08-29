using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

/// <summary>
/// Controls whether a school offers a Program / curriculum stream
/// during one specific academic year.
///
/// AcademicProgram remains the stable school-level identity.
/// This entity controls annual availability without deleting history.
/// </summary>
public sealed class AcademicYearProgramOffering : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }

    public Guid AcademicYearId { get; set; }
    public Guid AcademicProgramId { get; set; }

    public bool IsOffered { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
