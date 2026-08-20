using System.Reflection;
using Edulytics.Web.Hubs;
using Microsoft.AspNetCore.Authorization;

namespace Edulytics.Tests.Phase10;

public sealed class RealtimeArchitectureTests
{
    [Fact]
    public void Hub_RequiresAnalyticsReadPolicy()
    {
        var attribute =
            typeof(AnalyticsHub)
                .GetCustomAttributes<AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            "AnalyticsRead",
            attribute.Policy);
    }

    [Fact]
    public void Hub_ExposesNoClientControlledGroupJoin()
    {
        var methods =
            typeof(AnalyticsHub)
                .GetMethods(
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.DeclaredOnly);

        var method = Assert.Single(methods);

        Assert.Equal(
            nameof(
                AnalyticsHub.OnConnectedAsync),
            method.Name);
    }

    [Fact]
    public void BrowserClient_DoesNotInvokeGroupMethods()
    {
        var root = FindRoot();

        var js =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/wwwroot/js/"
                    + "analytics-live.js"));

        Assert.False(
            js.Contains(
                ".invoke(",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(
            js.Contains(
                "school:",
                StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            "/hubs/analytics",
            js);

        Assert.Contains(
            "AnalyticsUpdated",
            js);
    }

    [Fact]
    public void ResultService_ProducesOutboxEvent()
    {
        var root = FindRoot();

        var serviceDirectory =
            Path.Combine(
                root,
                "src/Edulytics.Services/"
                + "Assessments");

        var text =
            string.Join(
                "\n",
                Directory
                    .GetFiles(
                        serviceDirectory,
                        "AssessmentService*.cs")
                    .OrderBy(
                        x => x,
                        StringComparer.Ordinal)
                    .Select(
                        File.ReadAllText));

        Assert.Contains(
            "AddOutboxAsync",
            text);

        Assert.Contains(
            "AssessmentResultChangedEvent",
            text);

        Assert.Contains(
            "AssessmentResultEntered",
            text);

        Assert.Contains(
            "AssessmentResultUpdated",
            text);
    }

    [Fact]
    public void AnalyticsPage_LoadsLocalRealtimeClient()
    {
        var root = FindRoot();

        var view =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/"
                    + "Views/Analytics/"
                    + "Index.cshtml"));

        Assert.Contains(
            "data-analytics-live",
            view);

        Assert.Contains(
            "~/lib/signalr/signalr.min.js",
            view);

        Assert.Contains(
            "~/js/analytics-live.js",
            view);
    }

    [Fact]
    public void ControllersAndHub_DoNotUseDbContext()
    {
        var root = FindRoot();

        var files =
            Directory.GetFiles(
                    Path.Combine(
                        root,
                        "src/Edulytics.Web/Controllers"),
                    "*Controller.cs")
                .Concat(
                    Directory.GetFiles(
                        Path.Combine(
                            root,
                            "src/Edulytics.Web/Hubs"),
                        "*.cs"));

        foreach (var file in files)
        {
            var text =
                File.ReadAllText(file);

            Assert.DoesNotContain(
                "EdulyticsDbContext",
                text);

            Assert.DoesNotContain(
                "DbContext",
                text);
        }
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
