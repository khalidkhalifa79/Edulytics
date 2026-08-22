using System.Security.Claims;
using Edulytics.Services.Auditing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Web.Controllers;

[Authorize(Roles = "SchoolAdmin,SuperAdmin")]
[Route("Audit")]
public sealed class AuditController
    : Controller
{
    private readonly IAuditQueryService _audit;

    public AuditController(
        IAuditQueryService audit)
    {
        _audit = audit;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        Guid? schoolId,
        string? auditAction,
        string? entityType,
        string? correlationId,
        Guid? actorUserId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page = 1,
        int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var actorValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                actorValue,
                out var actorUserIdValue))
        {
            return Forbid();
        }

        var result =
            await _audit.QueryAsync(
                actorUserIdValue,
                new AuditQueryRequest(
                    schoolId,
                    auditAction,
                    entityType,
                    correlationId,
                    actorUserId,
                    fromUtc,
                    toUtc,
                    page,
                    pageSize),
                cancellationToken);

        if (!result.Succeeded)
        {
            return result.Error ==
                   AuditQueryError.InvalidQuery
                ? BadRequest()
                : Forbid();
        }

        return View(
            result.Page!);
    }
}
