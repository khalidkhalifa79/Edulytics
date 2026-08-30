namespace Edulytics.Tests.Phase29;

public sealed class Phase29StepNumberLayoutTests
{
    private static readonly string Root =
        FindRoot();

    [Fact]
    public void StepNumberHasDedicatedGridColumn()
    {
        var css =
            File.ReadAllText(
                Path.Combine(
                    Root,
                    "src/Edulytics.Web/wwwroot/css/site.css"));

        Assert.Contains(
            "phase29-step-number-layout-v5",
            css);

        Assert.Contains(
            "grid-template-columns:",
            css);

        Assert.Contains(
            "counter-reset: edulytics-step",
            css);

        Assert.Contains(
            "counter(edulytics-step)",
            css);

        Assert.Contains(
            "grid-column: 2 !important",
            css);

        Assert.Contains(
            "position: static !important",
            css);
    }

    [Fact]
    public void NativeListMarkerIsDisabled()
    {
        var css =
            File.ReadAllText(
                Path.Combine(
                    Root,
                    "src/Edulytics.Web/wwwroot/css/site.css"));

        Assert.Contains(
            "list-style: none !important",
            css);

        Assert.Contains(
            ".lesson-step-list > li::marker",
            css);
    }

    private static string FindRoot()
    {
        for (
            var directory =
                new DirectoryInfo(AppContext.BaseDirectory);

            directory is not null;

            directory = directory.Parent)
        {
            if (
                File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "Edulytics.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            "Repository root not found.");
    }
}
