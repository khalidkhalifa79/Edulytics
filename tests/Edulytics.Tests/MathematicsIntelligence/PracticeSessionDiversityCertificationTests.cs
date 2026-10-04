using System.Text.RegularExpressions;
using Edulytics.Core.Mathematics.Practice;
using Edulytics.Services.Mathematics;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class PracticeSessionDiversityCertificationTests
{
    [Fact]
    public void EveryLessonPracticeContractGeneratesVerifiedDiverseSession()
    {
        var engine = new ExactSkillContractQuestionEngine();
        var failures = new List<string>();
        var contracts = LessonPracticeContractRegistry.All
            .OrderBy(x => x.LessonCode, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(5110, contracts.Length);

        for (var index = 0; index < contracts.Length; index++)
        {
            var contract = contracts[index];
            var allowed = contract.AllowedQuestionFamilies
                .Distinct(StringComparer.Ordinal)
                .ToArray();

            if (allowed.Length == 0)
            {
                failures.Add($"{contract.LessonCode}: no allowed question families");
                continue;
            }

            const int questionCount = 10;

            try
            {
                var questions = engine.Generate(
                    "lesson-session-certification",
                    contract.LessonCode,
                    allowed,
                    ExactSkillQuestionDifficulty.Standard,
                    questionCount,
                    730000 + index,
                    []);

                if (questions.Count != questionCount)
                    failures.Add($"{contract.LessonCode}: generated {questions.Count}/{questionCount}");

                if (questions
                    .Select(x => x.ExposureFingerprint)
                    .Distinct(StringComparer.Ordinal)
                    .Count() != questions.Count)
                {
                    failures.Add($"{contract.LessonCode}: duplicate exposure fingerprint");
                }

                var observedFamilies = questions
                    .Select(x => x.Family)
                    .Distinct(StringComparer.Ordinal)
                    .ToHashSet(StringComparer.Ordinal);

                foreach (var expectedFamily in allowed.Take(questionCount))
                {
                    if (!observedFamilies.Contains(expectedFamily))
                        failures.Add($"{contract.LessonCode}: family not represented in session: {expectedFamily}");
                }

                foreach (var question in questions)
                {
                    if (!allowed.Contains(question.Family, StringComparer.Ordinal))
                        failures.Add($"{contract.LessonCode}: generated disallowed family {question.Family}");

                    if (string.IsNullOrWhiteSpace(question.Prompt))
                        failures.Add($"{contract.LessonCode}/{question.Family}: blank prompt");

                    if (string.IsNullOrWhiteSpace(question.Solution))
                        failures.Add($"{contract.LessonCode}/{question.Family}: blank solution");

                    if (!ExactSkillContractQuestionEngine.Verify(
                            question.Family,
                            question.Parameters,
                            question.CorrectAnswer))
                    {
                        failures.Add($"{contract.LessonCode}/{question.Family}: verifier rejected answer");
                    }
                }
            }
            catch (Exception ex)
            {
                failures.Add(
                    $"{contract.LessonCode} [{string.Join(",", allowed)}]: " +
                    $"{ex.GetType().Name}: {ex.Message}");
            }
        }

        Assert.True(
            failures.Count == 0,
            "Lesson Practice session certification failures: " +
            string.Join(" | ", failures.Take(100)) +
            (failures.Count > 100
                ? $" (+{failures.Count - 100} more)"
                : string.Empty));
    }

    [Fact]
    public void EveryLessonPracticeFamilyCanSustainItsCertifiedMeaningfulSessionCapacity()
    {
        var engine = new ExactSkillContractQuestionEngine();
        var families = LessonPracticeContractRegistry.All
            .SelectMany(contract => contract.AllowedQuestionFamilies)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var failures = new List<string>();

        for (var index = 0; index < families.Length; index++)
        {
            var family = families[index];

            try
            {
                const int questionCount = 10;

                var questions = engine.Generate(
                    "family-session-capacity-certification",
                    family,
                    [family],
                    ExactSkillQuestionDifficulty.Standard,
                    questionCount,
                    910000 + index,
                    []);

                if (questions.Count != questionCount)
                    failures.Add($"{family}: generated {questions.Count}/{questionCount}");

                if (questions
                    .Select(x => x.ExposureFingerprint)
                    .Distinct(StringComparer.Ordinal)
                    .Count() != questionCount)
                {
                    failures.Add($"{family}: duplicate exposure fingerprints");
                }

                foreach (var question in questions)
                {
                    if (!ExactSkillContractQuestionEngine.Verify(
                            question.Family,
                            question.Parameters,
                            question.CorrectAnswer))
                    {
                        failures.Add($"{family}: verifier rejected generated answer");
                    }
                }
            }
            catch (Exception ex)
            {
                failures.Add(
                    $"{family}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Assert.True(
            failures.Count == 0,
            "Question-family ten-item capacity failures: " +
            string.Join(" | ", failures));
    }

    [Fact]
    public void ShapeDimensionProvidesGenuineQuestionFormAndSemanticDiversity()
    {
        const string family = "supporting.geometry.shape_dimension";
        var engine = new ExactSkillContractQuestionEngine();

        var questions = engine.Generate(
            "shape-form-certification",
            family,
            [family],
            ExactSkillQuestionDifficulty.Standard,
            10,
            20260923,
            []);

        Assert.Equal(10, questions.Count);

        var forms = questions
            .Select(question => question.Parameters["form"])
            .Distinct()
            .OrderBy(value => value)
            .ToArray();

        Assert.True(
            forms.Length >= 3,
            $"Expected at least 3 genuine shape question forms, got {string.Join(",", forms)}.");

        var semanticKeys = questions
            .Select(question =>
                PracticeSemanticQuestionIdentityPolicy
                    .Create(
                        question.Family,
                        question.Parameters)
                    .Key)
            .ToArray();

        Assert.Equal(
            questions.Count,
            semanticKeys.Distinct(StringComparer.Ordinal).Count());

        Assert.All(
            questions,
            question => Assert.True(
                ExactSkillContractQuestionEngine.Verify(
                    question.Family,
                    question.Parameters,
                    question.CorrectAnswer)));
    }

    [Fact]
    public void QuantifiedRelationshipsMultistepFamilyHasMultipleReasoningForms()
    {
        const string family = "supporting.reasoning.multistep";

        var questions = new ExactSkillContractQuestionEngine().Generate(
            "multistep-diversity-certification",
            "PED:CAMBRIDGE-INTL-MATH:S6:6AS-MD-1:BUILD",
            [family],
            ExactSkillQuestionDifficulty.Standard,
            16,
            20260920,
            []);

        Assert.Equal(16, questions.Count);
        Assert.All(
            questions,
            question =>
                Assert.True(
                    ExactSkillContractQuestionEngine.Verify(
                        question.Family,
                        question.Parameters,
                        question.CorrectAnswer)));

        var modes = questions
            .Select(question => question.Parameters["mode"])
            .Distinct()
            .OrderBy(x => x)
            .ToArray();

        Assert.True(
            modes.Length >= 3,
            $"Expected at least 3 multistep reasoning modes, got {string.Join(",", modes)}.");

        var promptShapes = questions
            .Select(question => NormalizePromptShape(question.Prompt))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            promptShapes.Length >= 3,
            $"Expected at least 3 prompt structures, got {promptShapes.Length}.");
    }

    [Fact]
    public void PowersOf10LessonsResolveToFourDedicatedQuestionFamilies()
    {
        Assert.True(
            SupportingPracticeTargetRuleRegistry.TryResolve(
                "PED:CAMBRIDGE-INTL-MATH:S6:6NPV-1:APPLY",
                "Powers of 10: Reason and Apply",
                out var rule));

        Assert.NotNull(rule);
        Assert.Equal("powers-of-ten-primary", rule!.Id);
        Assert.Equal(
            new[]
            {
                "supporting.powers10.evaluate",
                "supporting.powers10.multiply",
                "supporting.powers10.divide",
                "supporting.powers10.missing_exponent"
            },
            rule.Families);
    }

    [Fact]
    public void PowersOf10PracticeRoutingDoesNotRewriteReviewedLessonBodyRecipe()
    {
        Assert.True(
            SupportingPracticeTargetRuleRegistry.TryGetById(
                "powers-of-ten-primary",
                out var powersOfTen));
        Assert.True(
            SupportingPracticeTargetRuleRegistry.TryGetById(
                "powers-roots",
                out var reviewedLegacyRecipe));

        Assert.NotNull(powersOfTen);
        Assert.NotNull(reviewedLegacyRecipe);
        Assert.Equal(
            reviewedLegacyRecipe!.Content,
            powersOfTen!.Content);
        Assert.NotEqual(
            reviewedLegacyRecipe.Families,
            powersOfTen.Families);
    }

    [Fact]
    public void QuantifiedRelationshipsSupportsAllEightReasoningTemplates()
    {
        const string family = "supporting.reasoning.multistep";
        var engine = new ExactSkillContractQuestionEngine();
        var questions = Enumerable.Range(0, 8)
            .Select(mode =>
                engine.Generate(
                    "multistep-template-certification",
                    "PED:CAMBRIDGE-INTL-MATH:S6:6AS-MD-1:BUILD",
                    [family],
                    ExactSkillQuestionDifficulty.Standard,
                    1,
                    81000 + mode,
                    [],
                    preferredVariant: mode)[0])
            .ToArray();

        Assert.Equal(
            Enumerable.Range(0, 8),
            questions.Select(question => question.Parameters["mode"]));

        Assert.Equal(
            8,
            questions
                .Select(question => NormalizePromptShape(question.Prompt))
                .Distinct(StringComparer.Ordinal)
                .Count());

        Assert.All(
            questions,
            question => Assert.True(
                ExactSkillContractQuestionEngine.Verify(
                    question.Family,
                    question.Parameters,
                    question.CorrectAnswer)));
    }

    private static string NormalizePromptShape(string prompt) =>
        Regex.Replace(
            Regex.Replace(
                prompt.ToLowerInvariant(),
                @"-?\d+(?:\.\d+)?",
                "#",
                RegexOptions.CultureInvariant),
            @"\s+",
            " ",
            RegexOptions.CultureInvariant)
        .Trim();
}