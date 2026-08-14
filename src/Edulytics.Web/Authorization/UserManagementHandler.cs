using System.Security.Claims;
using Edulytics.Services.Users;
using Microsoft.AspNetCore.Authorization;

namespace Edulytics.Web.Authorization;

public sealed class UserManagementHandler
    : AuthorizationHandler<UserManagementRequirement>
{
    private readonly ISchoolUserManagementService _users;

    public UserManagementHandler(
        ISchoolUserManagementService users)
    {
        _users = users;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        UserManagementRequirement requirement)
    {
        var value =
            context.User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
        {
            context.Fail();
            return;
        }

        if (await _users.CanManageUsersAsync(userId))
        {
            context.Succeed(requirement);
        }
        else
        {
            context.Fail();
        }
    }
}
