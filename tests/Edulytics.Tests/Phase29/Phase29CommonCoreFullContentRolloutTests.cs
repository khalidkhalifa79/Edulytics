using Edulytics.Core.Curriculum;
using Edulytics.Core.Enums;
using Edulytics.Data.Seeding;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29CommonCoreFullContentRolloutTests
{
    [Fact]
    public void CommonCoreFullRolloutLocksAll1560SourceFaithfulEnglishTargets()
    {
        var blueprints = PedagogicalLessonBlueprintRegistry
            .LoadEmbeddedDocuments()
            .Where(x => x.PackCode == MathematicsCurriculumPackRegistry.CommonCoreCode)
            .ToArray();

        Assert.Equal(17, blueprints.Length);
        Assert.Equal(1560, blueprints.Sum(x => x.Lessons.Count));

        var expected = blueprints
            .SelectMany(document =>
                document.Lessons.Select(lesson =>
                {
                    var outcomes = document.SchemaVersion == 1
                        ? lesson.OutcomeCodes.ToArray()
                        : lesson.FormalTargets
                            .OrderBy(x => x.SortOrder)
                            .Select(x => x.OutcomeCode)
                            .ToArray();
                    return new { lesson.LessonCode, Outcomes = outcomes };
                }))
            .Where(x => x.Outcomes.Length > 0)
            .ToArray();

        Assert.Equal(1466, expected.Length);
        Assert.Equal(1466, expected.Select(x => x.LessonCode).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(94, 1560 - expected.Length);

        var documents = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .Where(x => x.PackCode == MathematicsCurriculumPackRegistry.CommonCoreCode)
            .ToArray();

        Assert.Equal(17, documents.Length);
        Assert.All(documents, document =>
        {
            Assert.Equal("CCSSM-2010", document.VersionCode);
            Assert.Equal(2, document.SourcePolicyVersion);
            Assert.Equal(PedagogicalSourceType.OpenEducationalResource, document.PedagogicalSourceType);
            Assert.Equal(CanonicalLessonContentStatus.Published, document.Status);
            Assert.Equal("en", document.AcademicLanguage);
            Assert.False(document.CurriculumTranslationRequired);
            Assert.False(string.IsNullOrWhiteSpace(document.ReviewedBy));
            Assert.False(string.IsNullOrWhiteSpace(document.ReviewEvidence));
            Assert.False(string.IsNullOrWhiteSpace(document.ReviewMethod));
        });

        var actualRows = documents.SelectMany(x => x.Lessons).ToArray();
        Assert.Equal(1560, actualRows.Length);
        Assert.Equal(1560, actualRows.Select(x => x.LessonCode).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(94, actualRows.Count(x => x.IsSupporting));
        Assert.All(actualRows.Where(x => x.IsSupporting), x => Assert.Empty(x.OutcomeCodes));
        var actual = actualRows.ToDictionary(x => x.LessonCode, StringComparer.Ordinal);

        foreach (var item in expected)
        {
            Assert.True(actual.TryGetValue(item.LessonCode, out var lesson), item.LessonCode);
            Assert.Equal(item.Outcomes, lesson!.OutcomeCodes);
            Assert.Equal(LessonTitleProvenance.PedagogicalSource, lesson.TitleProvenance);
            Assert.False(string.IsNullOrWhiteSpace(lesson.TitleSourceReference));

            Assert.Single(lesson.Translations);
            Assert.Equal("en", lesson.Translations[0].CultureCode);
            var en = lesson.Translations.Single(x => x.CultureCode == "en");

            foreach (var translation in lesson.Translations)
            {
                Assert.False(string.IsNullOrWhiteSpace(translation.Explanation));
                Assert.False(string.IsNullOrWhiteSpace(translation.KeyConceptsAndRules));
                Assert.False(string.IsNullOrWhiteSpace(translation.WorkedExamples));
                Assert.False(string.IsNullOrWhiteSpace(translation.StepByStepSolutions));
                Assert.False(string.IsNullOrWhiteSpace(translation.CommonMistakes));
                Assert.False(string.IsNullOrWhiteSpace(translation.QuickSummary));
                var body = string.Join(" ", translation.Explanation, translation.KeyConceptsAndRules, translation.WorkedExamples, translation.StepByStepSolutions, translation.CommonMistakes, translation.QuickSummary);
                Assert.DoesNotContain("TODO", body, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("TBD", body, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("LOREM", body, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("PLACEHOLDER", body, StringComparison.Ordinal);
                Assert.DoesNotContain("is an Edulytics lesson in the unit", body, StringComparison.OrdinalIgnoreCase);
                Assert.DoesNotContain("lesson-specific focus:", body, StringComparison.OrdinalIgnoreCase);
            }
        }

        Assert.DoesNotContain(actualRows.SelectMany(x => x.Translations), x => x.CultureCode == "pl");
    }
}
