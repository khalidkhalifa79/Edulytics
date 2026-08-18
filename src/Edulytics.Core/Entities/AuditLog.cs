namespace Edulytics.Core.Entities;

public sealed class AuditLog
{
    public Guid Id { get; set; }

    // Null only for platform-scoped events.
    public Guid? SchoolId { get; set; }

    // May be null for a genuine system/background event.
    public Guid? ActorUserId { get; set; }

    public string ActorRole { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public string EntityType { get; set; } = string.Empty;

    public string EntityId { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    public string CorrelationId { get; set; } = string.Empty;

    public string IpAddress { get; set; } = string.Empty;

    public string UserAgent { get; set; } = string.Empty;

    public string OldValuesJson { get; set; } = string.Empty;

    public string NewValuesJson { get; set; } = string.Empty;

    public string ResultSummary { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public string Feature { get; set; } = string.Empty;
}
