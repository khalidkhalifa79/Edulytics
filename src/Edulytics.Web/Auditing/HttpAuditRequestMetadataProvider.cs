using System.Security.Claims;
using Edulytics.Services.Auditing;
using Edulytics.Web.Middleware;

namespace Edulytics.Web.Auditing;

public sealed class HttpAuditRequestMetadataProvider
    : IAuditRequestMetadataProvider
{
    private readonly IHttpContextAccessor
        _httpContextAccessor;

    public HttpAuditRequestMetadataProvider(
        IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor =
            httpContextAccessor;
    }

    public AuditRequestMetadata GetCurrent()
    {
        var context =
            _httpContextAccessor.HttpContext;

        if (context is null)
        {
            return new AuditRequestMetadata(
                null,
                "System",
                Guid.NewGuid()
                    .ToString("N"),
                string.Empty,
                string.Empty,
                "System");
        }

        Guid? actorUserId =
            null;

        var actorClaim =
            context.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (Guid.TryParse(
                actorClaim,
                out var parsedActor))
        {
            actorUserId =
                parsedActor;
        }

        var role =
            context.User
                .FindAll(
                    ClaimTypes.Role)
                .Select(x => x.Value)
                .Where(
                    x =>
                        !string.IsNullOrWhiteSpace(
                            x))
                .OrderBy(
                    x => x,
                    StringComparer.Ordinal)
                .FirstOrDefault()
            ?? string.Empty;

        var correlationId =
            CorrelationIdMiddleware
                .GetCorrelationId(
                    context);

        if (string.IsNullOrWhiteSpace(
                correlationId))
        {
            correlationId =
                context.TraceIdentifier;
        }

        return new AuditRequestMetadata(
            actorUserId,
            role,
            correlationId,
            context.Connection
                .RemoteIpAddress
                ?.ToString()
            ?? string.Empty,
            context.Request
                .Headers["User-Agent"]
                .ToString(),
            "HTTP");
    }
}
