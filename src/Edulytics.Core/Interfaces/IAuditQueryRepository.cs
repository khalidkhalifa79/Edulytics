using Edulytics.Core.Entities;

namespace Edulytics.Core.Interfaces;

public sealed record AuditLogQuerySpec(
    bool AllSchools,
    Guid? SchoolId,
    string? Action,
    string? EntityType,
    string? CorrelationId,
    Guid? ActorUserId,
    DateTime? FromUtc,
    DateTime? ToUtc,
    int Skip,
    int Take,
    string? ActorRole = null);

public sealed record AuditLogQueryPageData(
    int TotalCount,
    IReadOnlyList<AuditLog> Items);

public interface IAuditQueryRepository
{
    Task<AuditLogQueryPageData> QueryAsync(
        AuditLogQuerySpec spec,
        CancellationToken cancellationToken = default);
}
