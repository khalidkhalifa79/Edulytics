using Edulytics.Core.Interfaces;
using Edulytics.Core.Reports;

namespace Edulytics.Core.Entities;

public sealed class ReportExportJob : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid RequestedByUserId { get; set; }

    public ReportKind ReportKind { get; set; }
    public ReportExportFormat ExportFormat { get; set; }

    public Guid? AcademicYearId { get; set; }
    public Guid? ClassGroupId { get; set; }
    public Guid? SubjectId { get; set; }
    public Guid? StudentProfileId { get; set; }
    public Guid? LearningOutcomeId { get; set; }

    public string Culture { get; set; } = "en";

    public ReportExportJobStatus Status { get; set; } =
        ReportExportJobStatus.Pending;

    public int? RowCount { get; set; }

    public string? FileName { get; set; }
    public string? ContentType { get; set; }
    public byte[]? FileContent { get; set; }

    public string? LastError { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
