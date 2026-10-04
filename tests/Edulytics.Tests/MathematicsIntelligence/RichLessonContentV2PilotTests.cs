using System.Text.Json;
using Edulytics.Core.Curriculum;
using Edulytics.Data.Seeding;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class RichLessonContentV2PilotTests
{
    [Fact]
    public void CambridgeStage6Pilot_CoversAll24SourceBackedLessons()
    {
        var richDocument = Assert.Single(
            RichLessonContentV2Registry.AllDocuments,
            x => x.ContentVersion ==
                 "rich-v2-cambridge-primary-stage6-pilot-2026-09-22");

        Assert.Equal(24, richDocument.Lessons.Count);

        var baseDocument = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .Single(x =>
                x.PackCode == "CAMBRIDGE-INTL-MATH" &&
                x.ContentVersion ==
                    "phase29-cambridge-primary-stage6-dfe-ogl-v1");

        var expectedCodes = baseDocument.Lessons
            .Select(x => x.LessonCode)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var actualCodes = richDocument.Lessons
            .Select(x => x.LessonCode)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedCodes, actualCodes);

        Assert.All(richDocument.Lessons, lesson =>
        {
            Assert.True(lesson.ExplanationParagraphs.Count >= 3);
            Assert.True(lesson.KeyConcepts.Count >= 3);
            Assert.True(lesson.WorkedExamples.Count >= 3);
            Assert.True(lesson.CommonMistakes.Count >= 2);
            Assert.True(lesson.SummaryPoints.Count >= 3);
            Assert.NotEmpty(lesson.Visuals);

            Assert.All(lesson.WorkedExamples, example =>
            {
                Assert.True(example.Steps.Count >= 3);
                Assert.False(string.IsNullOrWhiteSpace(example.Question));
                Assert.False(string.IsNullOrWhiteSpace(example.Answer));
                Assert.False(string.IsNullOrWhiteSpace(example.Check));
            });
        });
    }

    [Fact]
    public void CambridgeStage6Pilot_UsesTheExistingOglSourceDossier()
    {
        var baseDocument = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .Single(x =>
                x.PackCode == "CAMBRIDGE-INTL-MATH" &&
                x.ContentVersion ==
                    "phase29-cambridge-primary-stage6-dfe-ogl-v1");

        Assert.All(baseDocument.Lessons, lesson =>
        {
            var title = lesson.Translations
                .First(x => x.CultureCode == "en")
                .Title;
            var dossier = RichLessonSourceDossierFactory.Build(
                baseDocument,
                lesson,
                title);

            Assert.Equal(
                RichLessonSourceDossierStatus.ApprovedForAdaptation,
                dossier.Status);
            Assert.True(dossier.SourceAdaptationPermitted);
            Assert.Contains(
                "Open Government Licence",
                dossier.RightsNote,
                StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(dossier.SourceLocator));
        });
    }

    [Fact]
    public void SourceDossierPipeline_CoversTheWholeCatalogue_AndFailsClosed()
    {
        var dossiers = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .SelectMany(document =>
                document.Lessons.Select(lesson =>
                {
                    var title = lesson.Translations
                        .FirstOrDefault(x =>
                            string.Equals(
                                x.CultureCode,
                                document.AcademicLanguage,
                                StringComparison.OrdinalIgnoreCase))
                        ?.Title
                        ?? lesson.Translations.First().Title;

                    return RichLessonSourceDossierFactory.Build(
                        document,
                        lesson,
                        title);
                }))
            .OrderBy(x => x.PackCode, StringComparer.Ordinal)
            .ThenBy(x => x.LessonCode, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(5110, dossiers.Length);
        Assert.DoesNotContain(
            dossiers,
            dossier =>
                string.IsNullOrWhiteSpace(dossier.DecisionReason) ||
                string.IsNullOrWhiteSpace(dossier.ResearchQuery));

        var polish = dossiers
            .Where(x => x.PackCode == "PL-NATIONAL-MATH")
            .ToArray();

        Assert.Equal(1569, polish.Length);
        Assert.All(
            polish,
            dossier => Assert.Equal(
                RichLessonSourceDossierStatus.ResearchRequired,
                dossier.Status));

        var outputDirectory = Path.Combine(
            FindRoot(),
            "artifacts",
            "lesson-content-v2");
        Directory.CreateDirectory(outputDirectory);

        File.WriteAllText(
            Path.Combine(
                outputDirectory,
                "source-dossiers.json"),
            JsonSerializer.Serialize(
                new
                {
                    schemaVersion = 1,
                    generatedAtUtc = DateTimeOffset.UtcNow,
                    lessonCount = dossiers.Length,
                    statusCounts = dossiers
                        .GroupBy(x => x.Status)
                        .OrderBy(x => x.Key)
                        .ToDictionary(
                            x => x.Key.ToString(),
                            x => x.Count()),
                    dossiers
                },
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
    }

    [Fact]
    public void SharedRichRenderer_IsUsedByStudentAndStaff_AndUsesPrivacyEnhancedYouTube()
    {
        var root = FindRoot();
        var student = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Views/StudentPortal/Lesson.cshtml"));
        var staff = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Views/LessonContent/Detail.cshtml"));
        var richPartial = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Views/Shared/_RichLessonContentV2.cshtml"));

        Assert.Contains("_RichLessonContentV2", student, StringComparison.Ordinal);
        Assert.Contains("_RichLessonContentV2", staff, StringComparison.Ordinal);
        Assert.Contains(
            "youtube-nocookie.com/embed/",
            richPartial,
            StringComparison.Ordinal);

        var securityHeaders = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Middleware/SecurityHeadersMiddleware.cs"));
        Assert.Contains(
            "https://www.youtube-nocookie.com",
            securityHeaders,
            StringComparison.Ordinal);

        var styles = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/wwwroot/css/site.css"));
        Assert.DoesNotContain(
            ".rich-number-line {\n        overflow-x: auto;\n        min-width: 32rem;",
            styles.Replace("\r\n", "\n"),
            StringComparison.Ordinal);
        Assert.Contains(
            "RichLessonVideoReviewStatus.Approved",
            richPartial,
            StringComparison.Ordinal);
        Assert.Contains("WorkedExamples", richPartial, StringComparison.Ordinal);
        Assert.Contains("StepByStepSolutions", richPartial, StringComparison.Ordinal);
        Assert.Contains("CommonMistakes", richPartial, StringComparison.Ordinal);
        Assert.Contains("QuickSummary", richPartial, StringComparison.Ordinal);
    }

    [Fact]
    public void PilotSidecar_DoesNotRedefineCurriculumOutcomesOrLessonIdentity()
    {
        var root = FindRoot();
        var sidecar = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Curriculum/LessonContent/RichV2/cambridge-primary-stage6.rich-lesson-v2.json"));

        Assert.DoesNotContain(
            "\"OutcomeCodes\"",
            sidecar,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "\"FrameworkVersionId\"",
            sidecar,
            StringComparison.Ordinal);

        var richCodes = RichLessonContentV2Registry.AllDocuments
            .SelectMany(x => x.Lessons)
            .Select(x => x.LessonCode)
            .ToHashSet(StringComparer.Ordinal);

        var existingCodes = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .SelectMany(x => x.Lessons)
            .Select(x => x.LessonCode)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Subset(existingCodes, richCodes);
    }

    [Fact]
    public void ApprovedPilotVideos_AreCuratedAndLimitedToReviewedEntries()
    {
        var videos = RichLessonContentV2Registry.AllDocuments
            .SelectMany(x => x.Lessons)
            .SelectMany(x => x.Videos)
            .ToArray();

        Assert.NotEmpty(videos);
        Assert.All(videos, video =>
        {
            Assert.Equal(
                RichLessonVideoReviewStatus.Approved,
                video.ReviewStatus);
            Assert.Equal("YouTube", video.Provider);
            Assert.False(string.IsNullOrWhiteSpace(video.VideoId));
            Assert.False(string.IsNullOrWhiteSpace(video.WhyRecommended));
        });

        Assert.Contains(videos, x => x.VideoId == "HpdMJaKaXXc");
        Assert.Contains(videos, x => x.VideoId == "2dbasvm3iG0");
        Assert.Contains(videos, x => x.VideoId == "PcEwj5_v75g");
    }

    [Fact]
    public void UaeOfficialPilot_PreservesOfficialMappings_AndUsesIndependentAuthoring()
    {
        var richDocument = Assert.Single(
            RichLessonContentV2Registry.AllDocuments,
            x => x.ContentVersion ==
                 "rich-v2-uae-g9-advanced-official-pilot-2026-09-22");

        Assert.Equal(2, richDocument.Lessons.Count);

        var baseDocument = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .Single(x =>
                x.PackCode == "UAE-MOE-MATH" &&
                x.Lessons.Any(lesson =>
                    lesson.LessonCode == "PED:UAE:G9:ADV:T1:L2-1"));

        foreach (var richLesson in richDocument.Lessons)
        {
            var baseLesson = Assert.Single(
                baseDocument.Lessons,
                x => x.LessonCode == richLesson.LessonCode);

            Assert.NotEmpty(baseLesson.OutcomeCodes);

            var sourceDossier = RichLessonSourceDossierFactory.Build(
                baseDocument,
                baseLesson,
                richLesson.Title);

            Assert.Equal(
                RichLessonSourceDossierStatus.IndependentAuthoringReferenceOnly,
                sourceDossier.Status);
            Assert.False(sourceDossier.SourceAdaptationPermitted);

            Assert.True(richLesson.ExplanationParagraphs.Count >= 3);
            Assert.True(richLesson.KeyConcepts.Count >= 3);
            Assert.True(richLesson.WorkedExamples.Count >= 3);
            Assert.True(richLesson.CommonMistakes.Count >= 2);
            Assert.True(richLesson.SummaryPoints.Count >= 3);
            Assert.NotEmpty(richLesson.Visuals);
        }

        var root = FindRoot();
        var sidecar = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Curriculum/LessonContent/RichV2/uae-g9-advanced-official.rich-lesson-v2.json"));

        Assert.DoesNotContain(
            "\"OutcomeCodes\"",
            sidecar,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "\"FrameworkVersionId\"",
            sidecar,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PilotRegistry_Contains26UniqueLessonEntries()
    {
        var lessons = RichLessonContentV2Registry.AllDocuments
            .SelectMany(x => x.Lessons)
            .ToArray();

        Assert.Equal(26, lessons.Length);
        Assert.Equal(
            26,
            lessons.Select(x => x.LessonCode)
                .Distinct(StringComparer.Ordinal)
                .Count());
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Edulytics.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Edulytics solution root not found.");
    }
}
