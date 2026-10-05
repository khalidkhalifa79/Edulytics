using System.Text.Json;
using Edulytics.Data.Seeding;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class EffectiveLessonContentSnapshotTests
{
    [Fact]
    public void WriteEffectiveLearnerContentSnapshot()
    {
        var rows = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .OrderBy(document => document.PackCode, StringComparer.Ordinal)
            .ThenBy(document => document.VersionCode, StringComparer.Ordinal)
            .SelectMany(document =>
                document.Lessons
                    .OrderBy(lesson => lesson.LessonCode, StringComparer.Ordinal)
                    .Select(lesson => new
                    {
                        lessonCode = lesson.LessonCode,
                        packCode = document.PackCode,
                        versionCode = document.VersionCode,
                        contentVersion =
                            CanonicalLessonContentMaterializer
                                .GetEffectiveContentVersion(document, lesson),
                        fingerprint =
                            CanonicalLessonContentMaterializer
                                .ComputeLessonFingerprint(document, lesson),
                        academicLanguage = document.AcademicLanguage,
                        documentStatus = document.Status.ToString(),
                        reviewedBy = document.ReviewedBy,
                        reviewEvidence = document.ReviewEvidence,
                        reviewMethod = document.ReviewMethod,
                        outcomeCodes = lesson.OutcomeCodes,
                        officialReferenceCode = lesson.OfficialReferenceCode,
                        isSupporting =
                            lesson.OutcomeCodes.Count == 0 &&
                            string.IsNullOrWhiteSpace(lesson.OfficialReferenceCode),
                        translations = lesson.Translations.Select(translation => new
                        {
                            cultureCode = translation.CultureCode,
                            title = translation.Title,
                            explanation = translation.Explanation,
                            keyConceptsAndRules = translation.KeyConceptsAndRules,
                            workedExamples = translation.WorkedExamples,
                            stepByStepSolutions = translation.StepByStepSolutions,
                            commonMistakes = translation.CommonMistakes,
                            quickSummary = translation.QuickSummary
                        }).ToArray()
                    }))
            .ToArray();

        Assert.Equal(5110, rows.Length);

        var quantified = Assert.Single(
            rows,
            row => string.Equals(
                row.lessonCode,
                "PED:CAMBRIDGE-INTL-MATH:S6:6AS-MD-1:BUILD",
                StringComparison.Ordinal));

        Assert.Equal(
            "supporting-practice-remediation-v2",
            quantified.contentVersion);

        var root = FindRoot();
        var outputDirectory = Path.Combine(
            root,
            "artifacts",
            "math-intelligence");
        Directory.CreateDirectory(outputDirectory);

        File.WriteAllText(
            Path.Combine(
                outputDirectory,
                "effective-lesson-content-snapshot.json"),
            JsonSerializer.Serialize(
                new
                {
                    schemaVersion = 1,
                    authority = "CanonicalLessonContentMaterializer",
                    lessonCount = rows.Length,
                    lessons = rows
                },
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }));
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
            "Edulytics solution root not found.");
    }
}