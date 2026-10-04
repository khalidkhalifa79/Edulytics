using System.Text.Json;
using Edulytics.Core.Mathematics.Practice;
using Edulytics.Services.Practice;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class SupportingLessonPracticeR4Tests
{
    [Fact]
    public void AmbiguityDecisionRegistryCoversRemainingBaselineLessons()
    {
        var root = FindRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Mathematics/Curriculum/supporting-ambiguity-decisions.r4.v1.json")));

        var rootElement = document.RootElement;
        Assert.Equal(7, rootElement.GetProperty("total").GetInt32());

        var decisions = rootElement.GetProperty("decisions").EnumerateArray().ToArray();
        Assert.Equal(7, decisions.Length);
        Assert.Equal(
            7,
            decisions.Select(x => x.GetProperty("lessonCode").GetString())
                .Distinct(StringComparer.Ordinal)
                .Count());

        Assert.All(decisions, decision =>
        {
            Assert.True(decision.GetProperty("primarySkills").GetArrayLength() >= 1);
            Assert.True(
                decision.GetProperty("terminalState").GetString() is
                    "READY_VERIFIED" or "EXPLICITLY_BLOCKED");
            Assert.False(string.IsNullOrWhiteSpace(
                decision.GetProperty("rationale").GetString()));
        });
    }

    [Fact]
    public void OnlyReadyAmbiguousLessonsHaveRuntimePracticeContracts()
    {
        var root = FindRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Mathematics/Curriculum/supporting-ambiguity-decisions.r4.v1.json")));

        foreach (var decision in document.RootElement.GetProperty("decisions").EnumerateArray())
        {
            var code = decision.GetProperty("lessonCode").GetString()!;
            var state = decision.GetProperty("terminalState").GetString();

            var resolved = LessonPracticeContractRegistry.TryResolve(code, out var contract);
            if (state == "READY_VERIFIED")
            {
                Assert.True(resolved);
                Assert.NotNull(contract);
                Assert.Equal("READY_VERIFIED", contract!.Readiness);
                Assert.NotEmpty(contract.AllowedQuestionFamilies);
            }
            else
            {
                Assert.False(resolved);
                Assert.Null(contract);
                Assert.False(string.IsNullOrWhiteSpace(
                    decision.GetProperty("blocker").GetString()));
            }
        }
    }

    [Fact]
    public void ReadyAmbiguousFractionContractsGenerateExactVerifiedPractice()
    {
        var root = FindRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Mathematics/Curriculum/supporting-ambiguity-decisions.r4.v1.json")));

        var ready = document.RootElement
            .GetProperty("decisions")
            .EnumerateArray()
            .Where(x => x.GetProperty("terminalState").GetString() == "READY_VERIFIED")
            .Select(x => x.GetProperty("lessonCode").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var engine = new Stage18SkillContractPracticeEngine();
        var seed = 16000;

        foreach (var contract in LessonPracticeContractRegistry.All.Where(x => ready.Contains(x.LessonCode)))
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
                Assert.Contains(
                    contract.AllowedQuestionFamilies,
                    family => string.Equals(family, item.GenerationFamily, StringComparison.Ordinal));
                Assert.True(Stage18SkillContractPracticeEngine.VerifyPersistedItem(legacy, item));
                Assert.Contains(@"""solverVerified"":true", item.ValidationMetadataJson, StringComparison.Ordinal);
                Assert.Contains(@"""broadFallbackUsed"":false", item.ValidationMetadataJson, StringComparison.Ordinal);
            });
        }
    }

    [Fact]
    public void AmbiguousFractionLessonsSeparatePrimarySecondaryAndPrerequisiteSkills()
    {
        var root = FindRoot();
        using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Mathematics/Curriculum/supporting-ambiguity-decisions.r4.v1.json")));

        var decision = document.RootElement.GetProperty("decisions")
            .EnumerateArray()
            .Single(x => x.GetProperty("lessonCode").GetString() ==
                "PED:CAMBRIDGE-INTL-MATH:S3:3F-4:BUILD");

        Assert.Equal(
            "fractions.add_subtract",
            decision.GetProperty("primarySkills")[0].GetString());
        Assert.Contains(
            decision.GetProperty("secondarySkills").EnumerateArray(),
            x => x.GetString() == "fractions.equivalent");
        Assert.Contains(
            decision.GetProperty("prerequisiteSkills").EnumerateArray(),
            x => x.GetString() == "number.whole.add_subtract");
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
