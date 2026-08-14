using System.Security.Claims;
using Edulytics.Services.Users;
using Microsoft.AspNetCore.Authorization;

namespace Edulytics.Web.Authorization;

public sealed class SchoolAccessHandler
    : AuthorizationHandler<SchoolAccessRequirement>
{
    private readonly ISchoolUserManagementService _users;

    public SchoolAccessHandler(
        ISchoolUserManagementService users)
    {
        _users = users;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SchoolAccessRequirement requirement)
    {
        var value =
            context.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
        {
            context.Fail();
            return;
        }

        var decision =
            await _users.EvaluateSignInAsync(userId);

        if (decision.Allowed &&
            !decision.IsPlatformAdministrator)
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
