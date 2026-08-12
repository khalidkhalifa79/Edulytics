using Edulytics.Core.Constants;
using Edulytics.Data.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace Edulytics.Web.Authorization;

public sealed class PlatformAdministrationHandler
    : AuthorizationHandler<PlatformAdministrationRequirement>
{
    private readonly UserManager<ApplicationUser> _userManager;

    public PlatformAdministrationHandler(
        UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PlatformAdministrationRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var user = await _userManager.GetUserAsync(context.User);

        if (user is null)
        {
            return;
        }

        if (user.SchoolId is not null)
        {
            return;
        }

        if (!await _userManager.IsInRoleAsync(
                user,
                RoleNames.SuperAdmin))
        {
            return;
        }

        context.Succeed(requirement);
    }
}
