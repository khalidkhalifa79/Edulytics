using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Tests.Phase05;

public sealed class SchoolUserAuthorizationTests
{
    [Fact]
    public void SchoolUsersController_RequiresUserManagementPolicy()
    {
        var authorize =
            typeof(SchoolUsersController)
                .GetCustomAttributes(
                    typeof(AuthorizeAttribute),
                    inherit: true)
                .Cast<AuthorizeAttribute>()
                .ToArray();

        Assert.Contains(
            authorize,
            x =>
                x.Policy ==
                "UserManagement");
    }

    [Fact]
    public void SchoolHomeController_RequiresSchoolAccessPolicy()
    {
        var authorize =
            typeof(SchoolHomeController)
                .GetCustomAttributes(
                    typeof(AuthorizeAttribute),
                    inherit: true)
                .Cast<AuthorizeAttribute>()
                .ToArray();

        Assert.Contains(
            authorize,
            x =>
                x.Policy ==
                "SchoolAccess");
    }

    [Fact]
    public void EverySchoolUserPostAction_HasAntiForgery()
    {
        var postMethods =
            typeof(SchoolUsersController)
                .GetMethods()
                .Where(
                    method =>
                        method.GetCustomAttributes(
                                typeof(HttpPostAttribute),
                                inherit: true)
                            .Any())
                .ToArray();

        Assert.NotEmpty(postMethods);

        foreach (var method in postMethods)
        {
            Assert.True(
                method.GetCustomAttributes(
                        typeof(
                            ValidateAntiForgeryTokenAttribute),
                        inherit: true)
                    .Any(),
                $"{method.Name} lacks anti-forgery.");
        }
    }
}
