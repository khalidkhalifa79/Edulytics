using System.Text.Json;
using Edulytics.Core.Mathematics.Practice;
using Edulytics.Services.Practice;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class SupportingLessonPracticeR3Tests
{
    [Fact]
    public void ReviewDecisionRegistryCoversRemainingBaselineReviewRequiredLessons()
    {
        var root = FindRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Mathematics/Curriculum/supporting-review-decisions.r3.v1.json")));

        var rootElement = document.RootElement;
        Assert.Equal(68, rootElement.GetProperty("total").GetInt32());
        Assert.Equal(23, rootElement.GetProperty("approvedExistingSkill").GetInt32());
        Assert.Equal(45, rootElement.GetProperty("deferredToOntology").GetInt32());

        var decisions = rootElement.GetProperty("decisions").EnumerateArray().ToArray();
        Assert.Equal(68, decisions.Length);
        Assert.Equal(68, decisions.Select(x => x.GetProperty("lessonCode").GetString()).Distinct().Count());

        Assert.All(decisions, decision =>
        {
            var value = decision.GetProperty("decision").GetString();
            Assert.True(value is "APPROVED_EXISTING_SKILL" or "DEFER_TO_R5_ONTOLOGY");
            Assert.False(string.IsNullOrWhiteSpace(decision.GetProperty("reviewEvidence").GetString()));
            Assert.False(string.IsNullOrWhiteSpace(decision.GetProperty("nextAction").GetString()));
        });
    }

    [Fact]
    public void ApprovedR3LessonsHaveExactPracticeContracts()
    {
        var root = FindRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Mathematics/Curriculum/supporting-review-decisions.r3.v1.json")));

        var approvedCodes = document.RootElement
            .GetProperty("decisions")
            .EnumerateArray()
            .Where(x => x.GetProperty("decision").GetString() == "APPROVED_EXISTING_SKILL")
            .Select(x => x.GetProperty("lessonCode").GetString()!)
            .ToArray();

        Assert.Equal(23, approvedCodes.Length);
        foreach (var code in approvedCodes)
        {

            Assert.True(LessonPracticeContractRegistry.TryResolve(code, out var contract));
            Assert.NotNull(contract);
            Assert.Equal("READY_VERIFIED", contract!.Readiness);
            Assert.NotEmpty(contract.AllowedQuestionFamilies);
        }
    }

    [Fact]
    public void EveryApprovedR3ContractGeneratesSolverVerifiedItems()
    {
        var root = FindRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Mathematics/Curriculum/supporting-review-decisions.r3.v1.json")));

        var approvedCodes = document.RootElement
            .GetProperty("decisions")
            .EnumerateArray()
            .Where(x => x.GetProperty("decision").GetString() == "APPROVED_EXISTING_SKILL")
            .Select(x => x.GetProperty("lessonCode").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var engine = new Stage18SkillContractPracticeEngine();
        var seed = 12000;
        foreach (var contract in LessonPracticeContractRegistry.All.Where(x => approvedCodes.Contains(x.LessonCode)))
        {
            var legacy = contract.ToLegacyStage18Contract();
            var items = engine.Generate(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.NewGuid(),
                legacy,
                StudentPrivatePracticeDifficulty.Stretch,
                Math.Max(2, legacy.AllowedQuestionFamilies.Count),
                seed++,
                [],
                Guid.NewGuid());

            Assert.All(items, item =>
            {
                Assert.True(Stage18SkillContractPracticeEngine.VerifyPersistedItem(legacy, item));
                Assert.Contains(@"""solverVerified"":true", item.ValidationMetadataJson, StringComparison.Ordinal);
                Assert.Contains(@"""broadFallbackUsed"":false", item.ValidationMetadataJson, StringComparison.Ordinal);
            });
        }
    }

    [Theory]
    [InlineData("PED:CAMBRIDGE-INTL-MATH:S4:4NF-2:BUILD", "number.whole.divide.with_remainder.build")]
    [InlineData("PED:CAMBRIDGE-INTL-MATH:S5:5F-1:APPLY", "fractions.of_quantity.apply")]
    [InlineData("PED:US-CCSS-MATH:G8:U03:L12", "algebra.linear.ax_plus_b_equals_c")]
    [InlineData("PED:US-CCSS-MATH:G7:U06:L13", "algebra.linear.inequality.ax_plus_b_relation_c")]
    public void ReviewedLessonUsesNarrowExactFamily(string lessonCode, string family)
    {
        Assert.True(LessonPracticeContractRegistry.TryResolve(lessonCode, out var contract));
        Assert.NotNull(contract);
        Assert.Contains(contract!.AllowedQuestionFamilies, x => string.Equals(x, family, StringComparison.Ordinal));
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
