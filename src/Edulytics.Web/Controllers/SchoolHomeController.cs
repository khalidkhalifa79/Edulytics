using Edulytics.Core.Constants;
using System.Security.Claims;
using Edulytics.Services.Users;
using Edulytics.Services.Reports;
using Edulytics.Web.ViewModels.SchoolUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "SchoolAccess")]
public sealed class SchoolHomeController : Controller
{
    private readonly ISchoolUserManagementService _users;
    private readonly IReportQueryService _reports;

    public SchoolHomeController(
        ISchoolUserManagementService users,
        IReportQueryService reports)
    {
        _users = users;
        _reports = reports;
    }

    [HttpGet("/school/dashboard")]
    public async Task<IActionResult> Dashboard(
        CancellationToken cancellationToken)
    {
        var value =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                value,
                out var userId))
        {
            return Forbid();
        }

        var context =
            await _users.GetActorContextAsync(
                userId,
                cancellationToken);

        if (context is null)
        {
            return Forbid();
        }

        var reportCatalog =
            await _reports.GetCatalogAsync(
                userId,
                cancellationToken);

        return View(
            new SchoolHomeViewModel
            {
                SchoolName =
                    context.SchoolName,
                Role =
                    context.Role,
                CanManageUsers =
                    context.CanManageUsers,
                CanViewReports =
                    reportCatalog.Value is not null,
                CanViewAnalytics =
                    context.Role == RoleNames.SchoolAdmin ||
                    context.Role == RoleNames.SubjectSupervisor ||
                    context.Role == RoleNames.Teacher
            });
    }
}
