namespace Edulytics.Tests.Phase28;

public sealed class Phase28StudentDashboardTitleTests
{
    [Fact]
    public void StudentDashboard_UsesRoleSpecificPageTitle()
    {
        var root = FindRoot();

        var dashboard =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Views",
                    "StudentPortal",
                    "Dashboard.cshtml"));

        var layout =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Views",
                    "Shared",
                    "_StudentLayout.cshtml"));

        var english =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Resources",
                    "StudentResource.resx"));

        var polish =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Resources",
                    "StudentResource.pl.resx"));

        Assert.Contains(
            "ViewData[\"Title\"] = S[\"StudentDashboard\"].Value;",
            dashboard);

        // The navigation item intentionally stays short: Dashboard.
        Assert.Contains(
            "<span>@S[\"Dashboard\"]</span>",
            layout);

        Assert.Contains(
            "name=\"StudentDashboard\"",
            english);

        Assert.Contains(
            "<value>Student dashboard</value>",
            english);

        Assert.Contains(
            "name=\"StudentDashboard\"",
            polish);

        Assert.Contains(
            "<value>Pulpit ucznia</value>",
            polish);
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

        throw new DirectoryNotFoundException(
            "Repository root not found.");
    }
}
