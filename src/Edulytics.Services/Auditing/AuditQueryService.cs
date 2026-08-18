using Edulytics.Core.Interfaces;

namespace Edulytics.Services.Auditing;

public sealed class AuditQueryService
    : IAuditQueryService
{
    private readonly IAuditQueryRepository _audit;
    private readonly ISchoolUserRepository _users;

    public AuditQueryService(
        IAuditQueryRepository audit,
        ISchoolUserRepository users)
    {
        _audit = audit;
        _users = users;
    }

    public async Task<AuditQueryResult> QueryAsync(
        Guid actorUserId,
        AuditQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor =
            await _users.GetActorAsync(
                actorUserId,
                cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            actor.Roles.Count != 1)
        {
            return AuditQueryResult.Failure(
                AuditQueryError.AccessDenied);
        }

        var role =
            actor.Roles[0];

        var visibility =
            AuditVisibilityPolicy.Resolve(
                role,
                actor.SchoolId,
                request.SchoolId);

        if (!visibility.Allowed)
        {
            return AuditQueryResult.Failure(
                AuditQueryError.AccessDenied);
        }

        if (request.FromUtc.HasValue &&
            request.ToUtc.HasValue &&
            request.FromUtc.Value >
            request.ToUtc.Value)
        {
            return AuditQueryResult.Failure(
                AuditQueryError.InvalidQuery);
        }

        var page =
            Math.Max(
                1,
                request.Page);

        var pageSize =
            Math.Clamp(
                request.PageSize,
                10,
                100);

        var normalized =
            request with
            {
                Action =
                    Clean(request.Action),
                EntityType =
                    Clean(request.EntityType),
                CorrelationId =
                    Clean(request.CorrelationId),
                Page = page,
                PageSize = pageSize
            };

        var data =
            await _audit.QueryAsync(
                new AuditLogQuerySpec(
                    visibility.AllSchools,
                    visibility.SchoolId,
                    normalized.Action,
                    normalized.EntityType,
                    normalized.CorrelationId,
                    normalized.ActorUserId,
                    normalized.FromUtc,
                    normalized.ToUtc,
                    (page - 1) * pageSize,
                    pageSize),
                cancellationToken);

        var items =
            data.Items
                .Select(
                    x => new AuditLogItem(
                        x.Id,
                        x.SchoolId,
                        x.ActorUserId,
                        x.ActorRole,
                        x.Action,
                        x.EntityType,
                        x.EntityId,
                        x.OccurredAtUtc,
                        x.CorrelationId,
                        x.IpAddress,
                        x.UserAgent,
                        x.OldValuesJson,
                        x.NewValuesJson,
                        x.ResultSummary,
                        x.Source,
                        x.Feature))
                .ToArray();

        return AuditQueryResult.Success(
            new AuditQueryPage(
                normalized,
                string.Equals(
                    role,
                    "SuperAdmin",
                    StringComparison.Ordinal),
                data.TotalCount,
                page,
                pageSize,
                items));
    }

    private static string? Clean(
        string? value)
    {
        value = value?.Trim();

        return string.IsNullOrEmpty(value)
            ? null
            : value;
    }
}
