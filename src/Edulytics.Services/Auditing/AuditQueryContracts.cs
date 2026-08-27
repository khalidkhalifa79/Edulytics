namespace Edulytics.Services.Auditing;

public sealed record AuditQueryRequest(
    Guid? SchoolId = null,
    string? Action = null,
    string? EntityType = null,
    string? CorrelationId = null,
    Guid? ActorUserId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Page = 1,
    int PageSize = 25,
    string? ActorRole = null);

public sealed record AuditLogItem(
    Guid Id,
    Guid? SchoolId,
    Guid? ActorUserId,
    string ActorRole,
    string Action,
    string EntityType,
    string EntityId,
    DateTime OccurredAtUtc,
    string CorrelationId,
    string? IpAddress,
    string? UserAgent,
    string? OldValuesJson,
    string? NewValuesJson,
    string ResultSummary,
    string Source,
    string Feature);

public sealed record AuditQueryPage(
    AuditQueryRequest Query,
    bool CanSelectSchool,
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<AuditLogItem> Items)
{
    public int TotalPages =>
        Math.Max(
            1,
            (int)Math.Ceiling(
                TotalCount /
                (double)PageSize));
}

public enum AuditQueryError
{
    AccessDenied = 1,
    InvalidQuery = 2
}

public sealed record AuditQueryResult(
    bool Succeeded,
    AuditQueryPage? Page,
    AuditQueryError? Error)
{
    public static AuditQueryResult Success(
        AuditQueryPage page) =>
        new(
            true,
            page,
            null);

    public static AuditQueryResult Failure(
        AuditQueryError error) =>
        new(
            false,
            null,
            error);
}

public sealed record AuditVisibilityDecision(
    bool Allowed,
    bool AllSchools,
    Guid? SchoolId)
{
    public static AuditVisibilityDecision Denied() =>
        new(
            false,
            false,
            null);
}

public static class AuditVisibilityPolicy
{
    public static AuditVisibilityDecision Resolve(
        string? role,
        Guid? actorSchoolId,
        Guid? requestedSchoolId)
    {
        if (string.Equals(
                role,
                "SchoolAdmin",
                StringComparison.Ordinal))
        {
            if (!actorSchoolId.HasValue)
            {
                return AuditVisibilityDecision
                    .Denied();
            }

            if (requestedSchoolId.HasValue &&
                requestedSchoolId.Value !=
                actorSchoolId.Value)
            {
                return AuditVisibilityDecision
                    .Denied();
            }

            return new AuditVisibilityDecision(
                true,
                false,
                actorSchoolId.Value);
        }

        if (string.Equals(
                role,
                "SuperAdmin",
                StringComparison.Ordinal))
        {
            return new AuditVisibilityDecision(
                true,
                !requestedSchoolId.HasValue,
                requestedSchoolId);
        }

        return AuditVisibilityDecision
            .Denied();
    }
}
