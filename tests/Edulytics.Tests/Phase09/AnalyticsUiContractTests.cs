using System.Reflection;
using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Tests.Phase09;

public sealed class AnalyticsUiContractTests
{
    [Fact]
    public void Controller_UsesAnalyticsReadPolicy()
    {
        var attribute =
            typeof(AnalyticsController)
                .GetCustomAttributes<
                    AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            "AnalyticsRead",
            attribute.Policy);
    }

    [Fact]
    public void RecalculatePost_UsesAntiForgery()
    {
        var method =
            typeof(AnalyticsController)
                .GetMethod(
                    nameof(
                        AnalyticsController
                            .Recalculate));

        Assert.NotNull(method);

        Assert.NotEmpty(
            method!
                .GetCustomAttributes<
                    HttpPostAttribute>());

        Assert.NotEmpty(
            method
                .GetCustomAttributes<
                    ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public void AnalyticsController_DoesNotUseDbContext()
    {
        var root = FindRoot();

        var text =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/Controllers/"
                    + "AnalyticsController.cs"));

        Assert.DoesNotContain(
            "DbContext",
            text);

        Assert.DoesNotContain(
            "EdulyticsDbContext",
            text);
    }

    [Fact]
    public void ResponsiveAnalyticsCss_Exists()
    {
        var root = FindRoot();

        var css =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/wwwroot/"
                    + "css/site.css"));

        Assert.Contains(
            ".analytics-page",
            css);

        Assert.Contains(
            ".analytics-metrics",
            css);

        Assert.Contains(
            ".analytics-heatmap",
            css);

        Assert.Contains(
            "@media (max-width: 767px)",
            css);

        Assert.Contains(
            "@media (max-width: 420px)",
            css);
    }

    private static string FindRoot()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "Edulytics.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException();
    }
}
