using System.Reflection;
using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Tests.Phase08;

public sealed class AssessmentUiContractTests
{
    [Fact]
    public void Controller_UsesPolicy_AndAllPostsUseAntiForgery()
    {
        var auth = typeof(AssessmentsController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();

        Assert.Equal("AssessmentManagement", auth.Policy);

        var posts = typeof(AssessmentsController)
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
    public void ResponsiveCssContractExists()
    {
        var root = FindRoot();
        var css = File.ReadAllText(
            Path.Combine(root, "src/Edulytics.Web/wwwroot/css/site.css"));

        Assert.Contains(".assessment-page", css);
        Assert.Contains(".assessment-results-grid", css);
        Assert.Contains(".assessment-score-row", css);
        Assert.Contains("@media (max-width: 767px)", css);
        Assert.Contains("@media (max-width: 420px)", css);
    }

    private static string FindRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null)
        {
            if (File.Exists(Path.Combine(d.FullName, "Edulytics.sln")))
                return d.FullName;
            d = d.Parent;
        }
        throw new DirectoryNotFoundException();
    }
}
