using Edulytics.Web.Presentation;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29LessonReaderFinalPolishTests
{
    private static readonly string Root =
        FindRoot();

    [Fact]
    public void ExplanationSuppressesTeacherOnlyScaffolding()
    {
        var items =
            LessonPresentationParser.Parse(
                """
                Supports accessibility for: Memory; Organization
                Give each group of 2 students a set of pieces.
                Design Principle(s): Support sense-making
                Rearranging pieces does not change the total area.
                """,
                sectionKind: "explanation");

        var text =
            string.Join(
                "\n",
                items
                    .Where(x => !x.IsVisual)
                    .Select(x => x.Text));

        Assert.DoesNotContain(
            "Supports accessibility",
            text);

        Assert.DoesNotContain(
            "Give each group",
            text);

        Assert.DoesNotContain(
            "Design Principle",
            text);

        Assert.Contains(
            "Rearranging pieces does not change the total area.",
            text);
    }

    [Fact]
    public void ExampleScaffoldLabelsAreNotShown()
    {
        var items =
            LessonPresentationParser.Parse(
                """
                Example 1: Student Facing
                Find the area of the figure.
                Source reasoning / synthesis:
                The figure can be decomposed into rectangles.
                """,
                sectionKind: "examples");

        var text =
            string.Join(
                "\n",
                items.Select(x => x.Text));

        Assert.Contains(
            "Example 1:",
            text);

        Assert.DoesNotContain(
            "Student Facing",
            text);

        Assert.DoesNotContain(
            "Source reasoning / synthesis",
            text);

        Assert.Contains(
            "The figure can be decomposed into rectangles.",
            text);
    }

    [Fact]
    public void QuickSummaryIsActuallyCompact()
    {
        var items =
            LessonPresentationParser.Parse(
                """
                Area is measured in square units.
                Rearranging pieces does not change total area.
                Decomposing a figure can make its area easier to calculate.
                Equal-sized units must be used consistently.
                The total area is the sum of the non-overlapping parts.
                This sixth sentence should not make the quick summary longer.
                """,
                sectionKind: "summary");

        Assert.InRange(
            items.Count,
            1,
            5);
    }

    [Fact]
    public void VisualCaptionIsShorterThanAccessibilityDescription()
    {
        var item =
            LessonPresentationParser
                .Parse(
                    """
                    Description: <p>Four drawings that each show squares inside a shape. Shape A is broken up into large squares, Shape B is broken up into a combination of large and small squares, Shape C is broken up into a combination of large squares and white space, and Shape D is broken up into small squares.</p>
                    """,
                    sectionKind: "examples")
                .Single(x => x.IsVisual);

        Assert.NotNull(item.Visual);

        Assert.True(
            item.Visual!.Caption.Length <
            item.Visual.AccessibilityText.Length);

        Assert.True(
            item.Visual.Caption.Length < 140);
    }

    [Fact]
    public void BothReadersPassSemanticSectionKinds()
    {
        var staff =
            Read(
                "src/Edulytics.Web/Views/LessonContent/Detail.cshtml");

        var student =
            Read(
                "src/Edulytics.Web/Views/StudentPortal/Lesson.cshtml");

        foreach (var reader in new[] { staff, student })
        {
            Assert.Contains(
                "sectionKind: \"explanation\"",
                reader);

            Assert.Contains(
                "sectionKind: \"concepts\"",
                reader);

            Assert.Contains(
                "sectionKind: \"examples\"",
                reader);

            Assert.Contains(
                "sectionKind: \"steps\"",
                reader);

            Assert.Contains(
                "sectionKind: \"mistakes\"",
                reader);

            Assert.Contains(
                "sectionKind: \"summary\"",
                reader);
        }
    }

    [Fact]
    public void FinalCssHasSpacingAndSingleSurfaceContract()
    {
        var css =
            Read(
                "src/Edulytics.Web/wwwroot/css/site.css");

        Assert.Contains(
            "phase29-reader-final-v4",
            css);

        Assert.Contains(
            "padding: 1.65rem 1.75rem 1.85rem",
            css);

        Assert.Contains(
            ".lesson-reader-hero__main",
            css);

        Assert.Contains(
            "background: transparent !important",
            css);
    }

    [Fact]
    public void VisualLongDescriptionIsNotVisibleCaption()
    {
        var visual =
            Read(
                "src/Edulytics.Web/Views/Shared/" +
                "_LessonInstructionalVisual.cshtml");

        Assert.Contains(
            "@visual.AccessibilityText",
            visual);

        Assert.Contains(
            "@visual.Caption",
            visual);

        Assert.DoesNotContain(
            "<figcaption>\n        @visual.AccessibilityText",
            visual);
    }

    private static string Read(
        string relative)
    {
        return File.ReadAllText(
            Path.Combine(
                Root,
                relative));
    }

    private static string FindRoot()
    {
        for (
            var directory =
                new DirectoryInfo(
                    AppContext.BaseDirectory);

            directory is not null;

            directory =
                directory.Parent)
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
