namespace Edulytics.Services.Auditing;

public interface IAuditQueryService
{
    Task<AuditQueryResult> QueryAsync(
        Guid actorUserId,
        AuditQueryRequest request,
        CancellationToken cancellationToken = default);
}
