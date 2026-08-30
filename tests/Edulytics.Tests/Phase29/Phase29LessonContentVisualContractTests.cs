using System.Text.Json;
using Edulytics.Web.Presentation;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29LessonContentVisualContractTests
{
    private static readonly string Root =
        FindRoot();

    [Fact]
    public void LibraryRemovesRejectedReadinessAndIncompleteUi()
    {
        var index =
            Read(
                "src/Edulytics.Web/Views/LessonContent/Index.cshtml");

        Assert.DoesNotContain(
            "IncompleteStandalone",
            index);

        Assert.DoesNotContain(
            "incompleteStandalone",
            index);

        Assert.DoesNotContain(
            "lesson-content-coverage-badge",
            index);

        Assert.DoesNotContain(
            "@group.ProductionReadyLessons /",
            index);

        Assert.Contains(
            "group.TotalLessons",
            index);

        Assert.Contains(
            "officiallyAlignedCount",
            index);

        Assert.Contains(
            "supportingCount",
            index);
    }

    [Theory]
    [InlineData(
        "Description: <p>A diagram of a line.</p>",
        "Description:\nA diagram of a line.")]
    [InlineData(
        "A&lt;br /&gt;B &amp; C",
        "A\nB & C")]
    [InlineData(
        "<script>alert(1)</script><p>Safe</p><img src=x onerror=alert(2)>",
        "Safe")]
    public void SanitizerNeverLeaksSourceMarkup(
        string input,
        string expected)
    {
        var text =
            LessonPresentationParser.ToPlainText(
                input);

        Assert.Equal(
            expected,
            text);

        Assert.DoesNotContain(
            "<",
            text);

        Assert.DoesNotContain(
            ">",
            text);
    }

    [Fact]
    public void OrderedStepsUseOnlyUiNumbering()
    {
        var items =
            LessonPresentationParser.Parse(
                "Step 2: Compare.\nStep 3: Solve.",
                orderedSteps: true);

        Assert.Equal(
            new[]
            {
                "Compare.",
                "Solve."
            },
            items.Select(
                x => x.Text));
    }

    [Fact]
    public void MeasuredPathUsesActualDescriptionValues()
    {
        const string description =
            """
            Description: <p>A diagram of a line with three markings.
            The first mark is labeled "Warm-up Mark",
            the second mark is labeled "Start",
            and the third mark is labeled "Finish".
            The distance between the first and second mark is labeled 1m.
            The distance between the second and third mark is labeled 10m.</p>
            """;

        var item =
            LessonPresentationParser
                .Parse(description)
                .Single();

        Assert.Equal(
            LessonVisualType.MeasuredPath,
            item.VisualType);

        Assert.Equal(
            new[]
            {
                "Warm-up Mark",
                "Start",
                "Finish"
            },
            item.Visual!.Labels);

        Assert.Equal(
            new[]
            {
                "1m",
                "10m"
            },
            item.Visual.Measures);
    }

    [Fact]
    public void DoubleNumberLineUsesSourceLabelsAndValues()
    {
        const string description =
            """
            Description: <p>Double number line titled, Moving Slowly.
            2 evenly spaced tick marks.
            Top line, distance traveled, meters.
            Beginning at first tick mark, labels: 0, 10.
            Bottom line, elapsed time, seconds.
            Beginning at first tick mark, labels: 0, blank.</p>
            """;

        var item =
            LessonPresentationParser
                .Parse(description)
                .Single();

        Assert.Equal(
            LessonVisualType.DoubleNumberLine,
            item.VisualType);

        Assert.Contains(
            "distance traveled",
            item.Visual!.PrimaryLabel,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "elapsed time",
            item.Visual.SecondaryLabel,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            new[] { "0", "10" },
            item.Visual.PrimaryValues);

        Assert.Equal(
            new[] { "0", "" },
            item.Visual.SecondaryValues);
    }

    [Fact]
    public void RequiredGrade6RegressionLessonsHaveSafeVisualSupport()
    {
        using var pack =
            JsonDocument.Parse(
                Read(
                    "src/Edulytics.Core/Curriculum/LessonContent/Packs/" +
                    "us-ccss-math-g6-phase29-v1.lesson-content-pack.json"));

        var lessons =
            pack.RootElement
                .GetProperty("lessons")
                .EnumerateArray();

        var area =
            lessons.Single(
                x =>
                    x.GetProperty("lessonCode")
                        .GetString() ==
                    "PED:US-CCSS-MATH:G6:U01:L02");

        var speed =
            pack.RootElement
                .GetProperty("lessons")
                .EnumerateArray()
                .Single(
                    x =>
                        x.GetProperty("lessonCode")
                            .GetString() ==
                        "PED:US-CCSS-MATH:G6:U02:L09");

        var areaItems =
            ParseField(
                area,
                "workedExamples");

        var speedExplanation =
            ParseField(
                speed,
                "explanation");

        var speedExamples =
            ParseField(
                speed,
                "workedExamples");

        Assert.Contains(
            areaItems,
            x =>
                x.VisualType ==
                LessonVisualType.AreaDecomposition);

        Assert.Contains(
            speedExplanation.Concat(
                speedExamples),
            x =>
                x.VisualType ==
                LessonVisualType.MeasuredPath);

        Assert.True(
            speedExplanation
                .Concat(speedExamples)
                .Count(
                    x =>
                        x.VisualType ==
                        LessonVisualType.DoubleNumberLine)
            >= 2);

        Assert.All(
            areaItems
                .Concat(speedExplanation)
                .Concat(speedExamples),
            item =>
            {
                var value =
                    item.Text ??
                    item.AccessibilityText ??
                    string.Empty;

                Assert.False(
                    value.Contains(
                        "<p>",
                        StringComparison.OrdinalIgnoreCase));

                Assert.False(
                    value.Contains(
                        "</p>",
                        StringComparison.OrdinalIgnoreCase));
            });
    }

    [Fact]
    public void UnknownDescriptionsDoNotProduceFakeGenericDrawings()
    {
        var item =
            LessonPresentationParser
                .Parse(
                    "Description: <p>An unusual visual that cannot be safely reconstructed.</p>")
                .Single();

        Assert.False(
            item.IsVisual);

        Assert.Contains(
            "unusual visual",
            item.Text!,
            StringComparison.OrdinalIgnoreCase);

        var visualPartial =
            Read(
                "src/Edulytics.Web/Views/Shared/" +
                "_LessonInstructionalVisual.cshtml");

        Assert.False(
            visualPartial.Contains(
                "default:",
                StringComparison.OrdinalIgnoreCase));

        Assert.False(
            visualPartial.Contains(
                "visual-generic",
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void BothReadersUseSafeSharedPresentation()
    {
        var staff =
            Read(
                "src/Edulytics.Web/Views/LessonContent/Detail.cshtml");

        var student =
            Read(
                "src/Edulytics.Web/Views/StudentPortal/Lesson.cshtml");

        foreach (var reader in
            new[]
            {
                staff,
                student
            })
        {
            Assert.Contains(
                "LessonPresentationParser.Parse",
                reader);

            Assert.Contains(
                "_LessonReaderSection",
                reader);

            Assert.DoesNotContain(
                "Html.Raw",
                reader);
        }

        var visual =
            Read(
                "src/Edulytics.Web/Views/Shared/" +
                "_LessonInstructionalVisual.cshtml");

        Assert.Contains(
            "<figure",
            visual);

        Assert.Contains(
            "role=\"img\"",
            visual);

        Assert.Contains(
            "aria-label=\"@visual.AccessibilityText\"",
            visual);
    }

    [Fact]
    public void DeploymentMarkerIsV3()
    {
        var css =
            Read(
                "src/Edulytics.Web/wwwroot/css/site.css");

        Assert.Contains(
            "phase29-visual-reader-v3",
            css);
    }

    private static IReadOnlyList<LessonPresentationItem>
        ParseField(
            JsonElement lesson,
            string field)
    {
        var translation =
            lesson
                .GetProperty("translations")[0];

        return LessonPresentationParser.Parse(
            translation
                .GetProperty(field)
                .GetString());
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
