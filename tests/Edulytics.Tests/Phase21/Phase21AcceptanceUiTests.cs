namespace Edulytics.Tests.Phase21;

public sealed class Phase21AcceptanceUiTests
{
    [Fact]
    public void SchoolUserCreateButton_UsesUserSpecificLabel()
    {
        var root = FindRoot();

        var path =
            Path.Combine(
                root,
                "src",
                "Edulytics.Web",
                "Views",
                "SchoolUsers",
                "Create.cshtml");

        var text =
            File.ReadAllText(path);

        Assert.Contains(
            "@T[\"CreateSchoolUser\"]",
            text);

        Assert.DoesNotContain(
            "@T[\"Create\"]",
            text);
    }

    [Fact]
    public void DashboardReportsCard_UsesActualReportScope()
    {
        var root = FindRoot();

        var controller =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Controllers",
                    "SchoolHomeController.cs"));

        var model =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "ViewModels",
                    "SchoolUsers",
                    "SchoolUserViewModels.cs"));

        var view =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Views",
                    "SchoolHome",
                    "Dashboard.cshtml"));

        Assert.Contains(
            "IReportQueryService",
            controller);

        Assert.Contains(
            "_reports.GetCatalogAsync",
            controller);

        Assert.Contains(
            "CanViewReports =",
            controller);

        Assert.Contains(
            "reportCatalog.Value is not null",
            controller);

        Assert.Contains(
            "public bool CanViewReports",
            model);

        Assert.Contains(
            "@if (Model.CanViewReports)",
            view);

        Assert.Contains(
            "asp-controller=\"Reports\"",
            view);
    }

    private static string FindRoot()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (
            directory is not null &&
            !File.Exists(
                Path.Combine(
                    directory.FullName,
                    "Edulytics.sln")))
        {
            directory =
                directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                "Repository root not found.");
    }
}
