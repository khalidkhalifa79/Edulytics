using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29LessonContentVisualContractTests
{
    [Fact]
    public void StaffLessonContentViewsHaveMatchingCssSelectors()
    {
        var root = FindRoot();

        var index = File.ReadAllText(
            Path.Combine(
                root,
                "src",
                "Edulytics.Web",
                "Views",
                "LessonContent",
                "Index.cshtml"));

        var editor = File.ReadAllText(
            Path.Combine(
                root,
                "src",
                "Edulytics.Web",
                "Views",
                "LessonContent",
                "Editor.cshtml"));

        var css = File.ReadAllText(
            Path.Combine(
                root,
                "src",
                "Edulytics.Web",
                "wwwroot",
                "css",
                "site.css"));

        Assert.Contains("lesson-content-topic-card", index);
        Assert.Contains("lesson-content-row", index);
        Assert.Contains("lesson-content-panel", editor);
        Assert.Contains("lesson-content-grid", editor);
        Assert.Contains("lesson-content-field", editor);
        Assert.Contains("lesson-content-outcome", editor);

        Assert.Contains(".lesson-content-topic-card {", css);
        Assert.Contains(".lesson-content-row {", css);
        Assert.Contains(".lesson-content-panel {", css);
        Assert.Contains(".lesson-content-grid {", css);
        Assert.Contains(".lesson-content-field input,", css);
        Assert.Contains(".lesson-content-outcome {", css);

        Assert.Contains(
            "EDULYTICS PHASE 29 BROWSER UI CORRECTIVE",
            css);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

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
