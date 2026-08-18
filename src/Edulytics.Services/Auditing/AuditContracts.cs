namespace Edulytics.Services.Auditing;

public sealed record AuditRequestMetadata(
    Guid? ActorUserId,
    string ActorRole,
    string CorrelationId,
    string IpAddress,
    string UserAgent,
    string Source);

public interface IAuditRequestMetadataProvider
{
    AuditRequestMetadata GetCurrent();
}

public sealed record AuditEvent(
    Guid? SchoolId,
    string Action,
    string EntityType,
    string? EntityId,
    string Feature,
    IReadOnlyDictionary<string, object?>? OldValues = null,
    IReadOnlyDictionary<string, object?>? NewValues = null,
    string? ResultSummary = null,
    Guid? ActorUserIdOverride = null,
    string? ActorRoleOverride = null,
    string? CorrelationIdOverride = null,
    string? SourceOverride = null);
