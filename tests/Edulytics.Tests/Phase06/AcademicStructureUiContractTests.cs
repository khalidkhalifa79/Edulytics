using System.Reflection;
using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Tests.Phase06;

public sealed class AcademicStructureUiContractTests
{
    [Fact]
    public void Controller_UsesAcademicAdministrationPolicy()
    {
        var authorize =
            typeof(AcademicStructureController)
                .GetCustomAttributes<AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            "AcademicStructureAdministration",
            authorize.Policy);
    }

    [Fact]
    public void EveryStateChangingAction_UsesAntiForgery()
    {
        var posts =
            typeof(AcademicStructureController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.GetCustomAttributes<HttpPostAttribute>().Any())
                .ToArray();

        Assert.NotEmpty(posts);

        Assert.All(
            posts,
            method => Assert.True(
                method.GetCustomAttributes<ValidateAntiForgeryTokenAttribute>().Any(),
                method.Name));
    }

    [Fact]
    public void ResponsiveAcademicCssContractExists()
    {
        var root = FindRepositoryRoot();

        var css = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/wwwroot/css/site.css"));

        Assert.Contains(".academic-page", css);
        Assert.Contains("@media (max-width: 767px)", css);
        Assert.Contains("@media (max-width: 420px)", css);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Edulytics.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException();
    }
}
