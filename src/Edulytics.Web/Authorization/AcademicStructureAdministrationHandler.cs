using System.Security.Claims;
using Edulytics.Core.Constants;
using Edulytics.Services.Users;
using Microsoft.AspNetCore.Authorization;

namespace Edulytics.Web.Authorization;

public sealed class AcademicStructureAdministrationHandler
    : AuthorizationHandler<AcademicStructureAdministrationRequirement>
{
    private readonly ISchoolUserManagementService _users;

    public AcademicStructureAdministrationHandler(
        ISchoolUserManagementService users)
    {
        _users = users;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        AcademicStructureAdministrationRequirement requirement)
    {
        var value = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(value, out var userId))
            return;

        var actor = await _users.GetActorContextAsync(userId);

        if (actor?.Role == RoleNames.SubjectSupervisor)
            context.Succeed(requirement);
    }
}
