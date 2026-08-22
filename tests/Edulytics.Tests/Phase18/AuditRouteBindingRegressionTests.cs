using System.Reflection;
using Edulytics.Web.Controllers;

namespace Edulytics.Tests.Phase18;

public sealed class AuditRouteBindingRegressionTests
{
    [Fact]
    public void AuditIndex_FilterParameter_DoesNotReuseMvcActionRouteKey()
    {
        var method =
            typeof(AuditController).GetMethod(
                nameof(AuditController.Index),
                BindingFlags.Instance |
                BindingFlags.Public);

        Assert.NotNull(method);

        var names =
            method!
                .GetParameters()
                .Select(x => x.Name)
                .ToArray();

        Assert.Contains("auditAction", names);

        Assert.DoesNotContain(
            names,
            x => string.Equals(
                x,
                "action",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void AuditView_FilterAndPagination_UseAuditActionQueryKey()
    {
        var root = FindRepositoryRoot();

        var view =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Views",
                    "Audit",
                    "Index.cshtml"));

        Assert.Contains(
            "name=\"auditAction\"",
            view);

        Assert.Contains(
            "id=\"auditAction\"",
            view);

        Assert.Contains(
            "for=\"auditAction\"",
            view);

        Assert.Contains(
            "asp-route-auditAction=\"@Model.Query.Action\"",
            view);

        Assert.DoesNotContain(
            "name=\"action\"",
            view);

        Assert.DoesNotContain(
            "asp-route-action=\"@Model.Query.Action\"",
            view);
    }

    private static string FindRepositoryRoot()
    {
        var current =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        current.FullName,
                        "Edulytics.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root not found.");
    }
}
