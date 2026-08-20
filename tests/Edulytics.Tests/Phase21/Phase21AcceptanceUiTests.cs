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
