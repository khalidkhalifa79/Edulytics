using Edulytics.Core.Constants;
using System.Security.Claims;
using Edulytics.Services.Users;
using Edulytics.Web.ViewModels.SchoolUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "SchoolAccess")]
public sealed class SchoolHomeController : Controller
{
    private readonly ISchoolUserManagementService _users;

    public SchoolHomeController(
        ISchoolUserManagementService users)
    {
        _users = users;
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

        return View(
            new SchoolHomeViewModel
            {
                SchoolName =
                    context.SchoolName,
                Role =
                    context.Role,
                CanManageUsers =
                    context.CanManageUsers,
                CanViewAnalytics =
                    context.Role == RoleNames.SchoolAdmin ||
                    context.Role == RoleNames.SubjectSupervisor ||
                    context.Role == RoleNames.Teacher
            });
    }
}
