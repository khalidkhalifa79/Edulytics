using Edulytics.Data.Seeding;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class CanonicalLessonContentMaterializerTests
{
    [Fact]
    public void Materializer_IsDeterministicAndIdempotentAcrossCatalogue()
    {
        var documents = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .OrderBy(x => x.PackCode, StringComparer.Ordinal)
            .ThenBy(x => x.VersionCode, StringComparer.Ordinal)
            .ToArray();

        var before = documents
            .SelectMany(document => document.Lessons.Select(lesson => new
            {
                lesson.LessonCode,
                Fingerprint =
                    CanonicalLessonContentMaterializer
                        .ComputeLessonFingerprint(
                            document,
                            lesson),
                Version =
                    CanonicalLessonContentMaterializer
                        .GetEffectiveContentVersion(
                            document,
                            lesson)
            }))
            .ToDictionary(
                x => x.LessonCode,
                x => (x.Fingerprint, x.Version),
                StringComparer.Ordinal);

        foreach (var document in documents)
            CanonicalLessonContentMaterializer.Materialize(document);

        var after = documents
            .SelectMany(document => document.Lessons.Select(lesson => new
            {
                lesson.LessonCode,
                Fingerprint =
                    CanonicalLessonContentMaterializer
                        .ComputeLessonFingerprint(
                            document,
                            lesson),
                Version =
                    CanonicalLessonContentMaterializer
                        .GetEffectiveContentVersion(
                            document,
                            lesson)
            }))
            .ToDictionary(
                x => x.LessonCode,
                x => (x.Fingerprint, x.Version),
                StringComparer.Ordinal);

        Assert.Equal(5110, before.Count);
        Assert.Equal(before, after);
    }

    [Fact]
    public void QuantifiedRelationships_UsesReviewedEffectiveLearnerBody()
    {
        const string lessonCode =
            "PED:CAMBRIDGE-INTL-MATH:S6:6AS-MD-1:BUILD";

        var pair = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .SelectMany(document =>
                document.Lessons.Select(lesson => (document, lesson)))
            .Single(x =>
                string.Equals(
                    x.lesson.LessonCode,
                    lessonCode,
                    StringComparison.Ordinal));

        var english = Assert.Single(
            pair.lesson.Translations,
            translation =>
                translation.CultureCode.StartsWith(
                    "en",
                    StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            "supporting-practice-remediation-v2",
            CanonicalLessonContentMaterializer
                .GetEffectiveContentVersion(
                    pair.document,
                    pair.lesson));

        Assert.Contains(
            "Quantifying a relationship means expressing how quantities are connected by exact operations",
            english.Explanation,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "more than",
            english.CommonMistakes,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "Always state what each number, unit, operation or geometric property represents before calculating",
            english.KeyConceptsAndRules,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "Read the problem and identify the quantities or properties",
            english.StepByStepSolutions,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReviewedCorrectionTargetSet_IsResolvedByOneAuthority()
    {
        var targets = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .SelectMany(document =>
                document.Lessons
                    .Where(lesson =>
                        CanonicalLessonContentMaterializer
                            .IsReviewedCorrectionTarget(
                                document,
                                lesson))
                    .Select(lesson => lesson.LessonCode))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(targets);
        Assert.Contains(
            "PED:CAMBRIDGE-INTL-MATH:S6:6AS-MD-1:BUILD",
            targets);
        Assert.Contains(
            "PED:CAMBRIDGE-INTL-MATH:S6:6F-3:BUILD",
            targets);
    }
}
