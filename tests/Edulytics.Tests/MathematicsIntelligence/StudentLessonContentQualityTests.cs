using Edulytics.Core.Mathematics.Practice;
using Edulytics.Data.Seeding;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class StudentLessonContentQualityTests
{
    private const string ScaleBuildCode =
        "PED:CAMBRIDGE-INTL-MATH:S6:6NPV-4:BUILD";

    [Fact]
    public void DecimalRoundingBuildLessonIsLearnerFacingAndTargetSpecific()
    {
        var lesson = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .SelectMany(document => document.Lessons)
            .Single(lesson => string.Equals(
                lesson.LessonCode,
                ScaleBuildCode,
                StringComparison.Ordinal));

        var english = Assert.Single(
            lesson.Translations,
            translation =>
                translation.CultureCode.StartsWith(
                    "en",
                    StringComparison.OrdinalIgnoreCase));

        Assert.Contains(
            "nearest target place value",
            english.Explanation,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "4.36",
            english.WorkedExamples,
            StringComparison.Ordinal);
        Assert.Contains(
            "4.4",
            english.WorkedExamples,
            StringComparison.Ordinal);
        Assert.Contains(
            "hundredths digit",
            english.KeyConceptsAndRules,
            StringComparison.OrdinalIgnoreCase);

        var combined = string.Join(
            " ",
            english.Explanation,
            english.KeyConceptsAndRules,
            english.WorkedExamples,
            english.StepByStepSolutions,
            english.CommonMistakes,
            english.QuickSummary);

        Assert.DoesNotContain(
            "A digit's value depends on its position",
            combined,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "6,203,405",
            combined,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Compose and decompose numbers with place-value units",
            combined,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "Always state what each number, unit, operation or geometric property represents before calculating",
            combined,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FractionOfQuantityLessonsAreLearnerFacingAndBuildApplyAreDistinct()
    {
        var lessons = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .SelectMany(document => document.Lessons)
            .Where(lesson =>
                StudentFacingLessonContentCorrections.AllLessonCodes
                    .Contains(lesson.LessonCode))
            .ToDictionary(
                lesson => lesson.LessonCode,
                lesson => lesson,
                StringComparer.Ordinal);

        Assert.Equal(4, lessons.Count);

        var s3Build = English(
            lessons[StudentFacingLessonContentCorrections.Stage3UnitFractionBuild]);
        var s3Apply = English(
            lessons[StudentFacingLessonContentCorrections.Stage3UnitFractionApply]);
        var s5Build = English(
            lessons[StudentFacingLessonContentCorrections.Stage5NonUnitFractionBuild]);
        var s5Apply = English(
            lessons[StudentFacingLessonContentCorrections.Stage5NonUnitFractionApply]);

        Assert.Contains("1/4 of 20", s3Build.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("20 ÷ 4 = 5", s3Build.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("24 pencils", s3Apply.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("24 ÷ 6 = 4", s3Apply.WorkedExamples, StringComparison.Ordinal);

        Assert.Contains("3/5 of 40", s5Build.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("40 ÷ 5 = 8", s5Build.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("56 books", s5Apply.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("56 ÷ 8 = 7", s5Apply.WorkedExamples, StringComparison.Ordinal);

        Assert.NotEqual(s3Build.WorkedExamples, s3Apply.WorkedExamples);
        Assert.NotEqual(s5Build.WorkedExamples, s5Apply.WorkedExamples);

        foreach (var translation in new[] { s3Build, s3Apply, s5Build, s5Apply })
        {
            var combined = string.Join(
                " ",
                translation.Explanation,
                translation.KeyConceptsAndRules,
                translation.WorkedExamples,
                translation.StepByStepSolutions,
                translation.CommonMistakes,
                translation.QuickSummary);

            Assert.DoesNotContain(
                "Always state what each number, unit, operation or geometric property represents before calculating",
                combined,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "Explain each step and name the mathematical relationship being used",
                combined,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "Read the problem and identify the quantities or properties",
                combined,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Theory]
    [InlineData(
        "PED:CAMBRIDGE-INTL-MATH:S3:3F-2:BUILD",
        "Find unit fractions of quantities: Build the Idea",
        "fractions.of_quantity.build")]
    [InlineData(
        "PED:CAMBRIDGE-INTL-MATH:S3:3F-2:APPLY",
        "Find unit fractions of quantities: Reason and Apply",
        "fractions.of_quantity.apply")]
    [InlineData(
        "PED:CAMBRIDGE-INTL-MATH:S5:5F-1:BUILD",
        "Find non-unit fractions of quantities: Build the Idea",
        "fractions.of_quantity.build")]
    [InlineData(
        "PED:CAMBRIDGE-INTL-MATH:S5:5F-1:APPLY",
        "Find non-unit fractions of quantities: Reason and Apply",
        "fractions.of_quantity.apply")]
    public void FractionOfQuantityLessonAndPracticeResolveToTheSameExactTarget(
        string lessonCode,
        string title,
        string expectedFamily)
    {
        Assert.True(
            SupportingPracticeTargetRuleRegistry.TryResolve(
                lessonCode,
                title,
                out var rule));
        Assert.NotNull(rule);
        Assert.Equal("fractions.of_quantity", rule!.SkillId);
        Assert.Contains(expectedFamily, rule.Families);

        Assert.True(
            LessonPracticeCapabilityResolver.TryResolve(
                lessonCode,
                out var contract));
        Assert.NotNull(contract);
        Assert.Equal(rule.SkillId, contract!.SkillId);
        Assert.Contains(expectedFamily, contract.AllowedQuestionFamilies);
    }

    [Fact]
    public void DecimalRoundingBuildLessonAndPracticeResolveToTheSameExactTarget()
    {
        Assert.True(
            LessonPracticeCapabilityResolver.TryResolve(
                ScaleBuildCode,
                out var contract));
        Assert.NotNull(contract);
        Assert.Equal(
            "supporting.number.place_value_rounding",
            contract!.SkillId);
        Assert.Contains(
            "supporting.number.rounding",
            contract.AllowedQuestionFamilies);
        Assert.Equal("READY_VERIFIED", contract.Readiness);
    }

    [Fact]
    public void CommonDenominationBuildLessonAndPracticeResolveToTheSameExactTarget()
    {
        const string lessonCode =
            "PED:CAMBRIDGE-INTL-MATH:S6:6F-2:BUILD";
        const string title =
            "Express fractions in a common denomination: Build the Idea";

        Assert.True(
            SupportingPracticeTargetRuleRegistry.TryResolve(
                lessonCode,
                title,
                out var rule));
        Assert.NotNull(rule);

        Assert.True(
            LessonPracticeCapabilityResolver.TryResolve(
                lessonCode,
                out var contract));
        Assert.NotNull(contract);

        Assert.Equal(
            "fractions.compare.unlike_denominators",
            rule!.SkillId);
        Assert.Equal(rule.SkillId, contract!.SkillId);
        Assert.Contains(
            "fractions.compare.unlike.common_denominator",
            contract.AllowedQuestionFamilies);
    }
    private static Edulytics.Core.Curriculum.CanonicalLessonContentPackTranslation English(
        Edulytics.Core.Curriculum.CanonicalLessonContentPackLesson lesson) =>
        Assert.Single(
            lesson.Translations,
            translation =>
                translation.CultureCode.StartsWith(
                    "en",
                    StringComparison.OrdinalIgnoreCase));

}
