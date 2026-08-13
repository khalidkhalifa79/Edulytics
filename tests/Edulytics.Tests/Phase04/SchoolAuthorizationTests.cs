using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace Edulytics.Tests.Phase04;

public sealed class SchoolAuthorizationTests
{
    [Fact]
    public void SchoolsController_RequiresPlatformAdministrationPolicy()
    {
        var attributes = typeof(SchoolsController)
            .GetCustomAttributes(
                typeof(AuthorizeAttribute),
                inherit: true)
            .Cast<AuthorizeAttribute>()
            .ToArray();

        Assert.Contains(
            attributes,
            attribute =>
                attribute.Policy == "PlatformAdministration");
    }
}
