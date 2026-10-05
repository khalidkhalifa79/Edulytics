using Edulytics.Core.Mathematics.Practice;
using Edulytics.Services.Practice;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class SupportingLessonPracticeR1Tests
{
    [Fact]
    public void RegistryContainsAllSevenBaselineReadyVerifiedSupportingLessons()
    {
        var expected = new[]
        {
            "PED:CAMBRIDGE-INTL-MATH:S5:5F-2:APPLY",
            "PED:CAMBRIDGE-INTL-MATH:S5:5F-2:BUILD",
            "PED:CAMBRIDGE-INTL-MATH:S6:6AS-MD-4:APPLY",
            "PED:CAMBRIDGE-INTL-MATH:S6:6F-3:APPLY",
            "PED:CAMBRIDGE-INTL-MATH:S6:6F-3:BUILD",
            "PED:CAMBRIDGE-INTL-MATH:S6:6NPV-4:APPLY",
            "PED:US-CCSS-MATH:G7:U06:L15"
        };

        Assert.True(LessonPracticeContractRegistry.All.Count >= 7);
        var actual = LessonPracticeContractRegistry.All
            .Select(x => x.LessonCode)
            .ToHashSet(StringComparer.Ordinal);
        Assert.All(expected, code => Assert.Contains(code, actual));

        var supportingContracts = LessonPracticeContractRegistry.All
            .Where(contract => string.Equals(
                contract.SourceType,
                "SupportingLesson",
                StringComparison.Ordinal))
            .ToArray();

        Assert.All(supportingContracts, contract =>
        {
            Assert.Equal("READY_VERIFIED", contract.Readiness);
            Assert.Contains(
                contract.ContractVersion,
                new[]
                {
                    LessonPracticeContractRegistry.Version,
                    "lesson-practice-projection-v1"
                });
            Assert.False(string.IsNullOrWhiteSpace(contract.SkillId));
            Assert.NotEmpty(contract.AllowedQuestionFamilies);
        });
    }

    [Fact]
    public void EveryR1ContractGeneratesAndIndependentlyVerifiesExactPractice()
    {
        var engine = new Stage18SkillContractPracticeEngine();
        var seed = 2100;

        foreach (var practiceContract in LessonPracticeContractRegistry.All)
        {
            var legacyContract = practiceContract.ToLegacyStage18Contract();
            var items = engine.Generate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                legacyContract,
                StudentPrivatePracticeDifficulty.Stretch,
                Math.Max(2, legacyContract.AllowedQuestionFamilies.Count),
                seed++,
                [],
                Guid.NewGuid());

            Assert.NotEmpty(items);
            Assert.All(items, item =>
            {
                Assert.Contains(
                    practiceContract.AllowedQuestionFamilies,
                    family => string.Equals(
                        family,
                        item.GenerationFamily,
                        StringComparison.Ordinal));
                Assert.True(
                    Stage18SkillContractPracticeEngine.VerifyPersistedItem(
                        legacyContract,
                        item));
                Assert.Contains(
                    "\"solverVerified\":true",
                    item.ValidationMetadataJson,
                    StringComparison.Ordinal);
                Assert.Contains(
                    "\"broadFallbackUsed\":false",
                    item.ValidationMetadataJson,
                    StringComparison.Ordinal);
            });
        }
    }

    [Fact]
    public void EfficientlySolvingInequalitiesUsesExactInequalityFamily()
    {
        Assert.True(
            LessonPracticeContractRegistry.TryResolve(
                "PED:US-CCSS-MATH:G7:U06:L15",
                out var practiceContract));
        Assert.NotNull(practiceContract);

        var legacyContract = practiceContract!.ToLegacyStage18Contract();
        var item = new Stage18SkillContractPracticeEngine()
            .Generate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                legacyContract,
                StudentPrivatePracticeDifficulty.Challenge,
                1,
                7401,
                [],
                Guid.NewGuid())
            .Single();

        Assert.Equal(
            "algebra.linear.inequality.ax_plus_b_relation_c",
            item.GenerationFamily);
        Assert.StartsWith("x", item.CorrectAnswer);
        Assert.True(
            item.CorrectAnswer.Contains('<') ||
            item.CorrectAnswer.Contains('>') ||
            item.CorrectAnswer.Contains('≤') ||
            item.CorrectAnswer.Contains('≥'));
        Assert.True(
            Stage18SkillContractPracticeEngine.VerifyPersistedItem(
                legacyContract,
                item));
    }

    [Fact]
    public void StudentLessonUiUsesExactPracticeReadinessIndependentlyOfGameRouting()
    {
        var root = FindRoot();
        var portal = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Controllers/StudentPortalController.cs"));
        var practice = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Controllers/StudentPracticeController.cs"));
        var view = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Views/StudentPortal/Lesson.cshtml"));

        Assert.Contains(
            "LessonPracticeCapabilityResolver.TryResolve",
            portal,
            StringComparison.Ordinal);
        Assert.Contains(
            "ExactPracticeAdoptionId",
            portal,
            StringComparison.Ordinal);
        Assert.Contains(
            "StartLessonPractice",
            practice,
            StringComparison.Ordinal);
        Assert.Contains(
            "ExactPracticeAdoptionId",
            view,
            StringComparison.Ordinal);
        Assert.Contains(
            "asp-action=\"StartLessonPractice\"",
            view,
            StringComparison.Ordinal);
    }


    [Fact]
    public void CommonDenominationBuildRuleResolvesButRuntimeContractMustAlsoResolve()
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
        Assert.Equal(
            "fractions.compare.unlike_denominators",
            rule!.SkillId);
        Assert.Contains(
            "fractions.compare.unlike.common_denominator",
            rule.Families);

        Assert.True(
            LessonPracticeContractRegistry.TryResolve(
                lessonCode,
                out var contract));
        Assert.NotNull(contract);
        Assert.Equal(
            "fractions.compare.unlike_denominators",
            contract!.SkillId);
        Assert.Contains(
            "fractions.compare.unlike.common_denominator",
            contract.AllowedQuestionFamilies);
        Assert.Equal("READY_VERIFIED", contract.Readiness);
    }

    [Fact]
    public void DecimalRoundingBuildRuntimeContractMatchesCurrentOfficialMappedLesson()
    {
        const string lessonCode =
            "PED:CAMBRIDGE-INTL-MATH:S6:6NPV-4:BUILD";

        Assert.True(
            LessonPracticeContractRegistry.TryResolve(
                lessonCode,
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
    public void SupportingRuleProjectionContributesRuntimeContracts()
    {
        Assert.Contains(
            LessonPracticeContractRegistry.All,
            contract => string.Equals(
                contract.SourceType,
                "SupportingRule",
                StringComparison.Ordinal));
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null &&
               !File.Exists(Path.Combine(directory.FullName, "Edulytics.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Repository root not found.");
    }
}
