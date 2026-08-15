using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class ImportValidationError : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid ImportBatchId { get; set; }

    public int RowNumber { get; set; }

    public string ColumnName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? RawValue { get; set; }
}
