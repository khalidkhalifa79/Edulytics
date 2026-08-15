using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class ImportBatch : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }

    public ImportType ImportType { get; set; }
    public ImportBatchStatus Status { get; set; }

    public string OriginalFileName { get; set; } = string.Empty;
    public string FileHash { get; set; } = string.Empty;
    public string RowsJson { get; set; } = string.Empty;

    public int RowCount { get; set; }
    public int ValidRowCount { get; set; }
    public int ErrorCount { get; set; }

    public Guid UploadedByUserId { get; set; }
    public Guid? CompletedByUserId { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
