using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class
    Phase29LessonContentVisualContractTests
{
    [Fact]
    public void
        StaffCanonicalLibraryIsReadOnlyStructuredAndSupportingAware()
    {
        var root = FindRoot();

        var index =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Views",
                    "LessonContent",
                    "Index.cshtml"));

        var detail =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Views",
                    "LessonContent",
                    "Detail.cshtml"));

        var css =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "wwwroot",
                    "css",
                    "site.css"));

        var english =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Resources",
                    "LessonContentResource.resx"));

        var polish =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Resources",
                    "LessonContentResource.pl.resx"));

        Assert.Contains(
            "standaloneCount",
            index);

        Assert.Contains(
            "group.Lessons.Count(x => x.HasOfficialAlignment)",
            index);

        Assert.Contains(
            "supportingCount",
            index);

        Assert.Contains(
            "incompleteStandalone",
            index);

        Assert.Contains(
            "isSupporting",
            index);

        Assert.Contains(
            "SupportingLesson",
            index);

        Assert.Contains(
            "lesson-content-row--supporting",
            index);

        Assert.DoesNotContain(
            "@group.ProductionReadyLessons / @group.TotalLessons",
            index);

        Assert.Contains(
            "SplitStructuredText",
            detail);

        Assert.Contains(
            "lesson-learning-section--examples",
            detail);

        Assert.Contains(
            "lesson-step-list",
            detail);

        Assert.Contains(
            "lesson-mistake-card",
            detail);

        Assert.Contains(
            "lesson-summary-box",
            detail);

        Assert.Contains(
            "Model.Lesson.Outcomes.Count == 0",
            detail);

        Assert.DoesNotContain(
            "<form",
            index,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "<form",
            detail,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "EDULYTICS PHASE 29 CANONICAL CONTENT ARCHITECTURE",
            css);

        Assert.Contains(
            "EDULYTICS PHASE 29 LESSON EXPERIENCE REDESIGN",
            css);

        Assert.Contains(
            "lesson-reader-layout",
            css);

        Assert.Contains(
            "lesson-content-metric--supporting",
            css);

        Assert.Contains(
            "name=\"SupportingLesson\"",
            english);

        Assert.Contains(
            "name=\"StandaloneLessons\"",
            english);

        Assert.Contains(
            "name=\"SupportingLesson\"",
            polish);

        Assert.Contains(
            "name=\"StandaloneLessons\"",
            polish);
    }

    private static string
        FindRoot()
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

            directory =
                directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root not found.");
    }
}
