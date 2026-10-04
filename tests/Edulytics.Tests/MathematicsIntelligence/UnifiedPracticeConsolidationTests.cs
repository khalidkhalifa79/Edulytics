using Edulytics.Core.AdaptivePractice;
using Edulytics.Core.Mathematics.Practice;
using Edulytics.Services.AdaptivePractice;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class UnifiedPracticeConsolidationTests
{
    private const string UaeGrade4Level =
        "UAE-MOE-MATH:L04:COMMON";

    private const string UaeRoundingLesson =
        "PED:UAE-MOE-MATH:L4:COMMON:02:04";

    [Fact]
    public void U1_UaeRoundingLesson_IsReadyVerified()
    {
        Assert.True(
            LessonPracticeCapabilityResolver.TryResolve(
                UaeRoundingLesson,
                out var contract));

        Assert.NotNull(contract);
        Assert.Equal(
            LessonPracticeCapabilityResolver.ReadyVerified,
            contract!.Readiness);
        Assert.Equal(
            "supporting.number.place_value_rounding",
            contract.SkillId);
        Assert.Contains(
            "supporting.number.rounding",
            contract.AllowedQuestionFamilies);
    }

    [Fact]
    public void U1_RouteAllReadyVerified_DoesNotRequirePerLessonAllowList()
    {
        var schoolId = Guid.NewGuid();
        var policy = new AdaptivePracticeV2Policy(
            Enabled: true,
            Mode: AdaptivePracticeV2Mode.Canary,
            AllowedCurriculumLevelKeys:
                new HashSet<string>(
                    [UaeGrade4Level],
                    StringComparer.OrdinalIgnoreCase),
            AllowedLessonCodes:
                new HashSet<string>(StringComparer.Ordinal),
            AllowedSchoolIds:
                new HashSet<Guid> { schoolId },
            ShadowSamplingPercentage: 100,
            MaxLessonQuestions: 8,
            EnableMisconceptionLoop: true,
            EnableDirectNextSteps: true,
            EnableQuestionLog: true,
            EnableLiveClassroom: true,
            EnableDiagnosticV2: true)
        {
            RouteAllReadyVerifiedLessons = true
        };

        var resolver =
            new AdaptivePracticeEligibilityResolver(policy);

        var result = resolver.Resolve(
            new AdaptivePracticeEligibilityRequest(
                schoolId,
                UaeGrade4Level,
                UaeRoundingLesson,
                IsMathematics: true));

        Assert.True(result.IsLearnerFacing);
        Assert.Equal(
            AdaptivePracticeEligibilityReasonCodes.Eligible,
            result.ReasonCode);
        Assert.Equal(
            "supporting.number.place_value_rounding",
            result.SkillId);
    }

    [Fact]
    public void U1_ReadyVerifiedScope_RejectsSecondaryLevels()
    {
        Assert.True(
            AdaptivePrimaryRolloutPlan.IsValidReadyVerifiedScope(
                [
                    "CAMBRIDGE-INTL-MATH:L02:SHARED",
                    UaeGrade4Level,
                    "PL-NATIONAL-MATH:L04:SHARED"
                ]));

        Assert.False(
            AdaptivePrimaryRolloutPlan.IsValidReadyVerifiedScope(
                ["CAMBRIDGE-INTL-MATH:L07:SHARED"]));
    }

    [Fact]
    public void U2_LessonStart_TriesUnifiedRuntimeBeforeLegacyPresentation()
    {
        var root = FindRoot();
        var controller = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Controllers/StudentPracticeController.cs"));

        var start = controller.IndexOf(
            "public async Task<IActionResult> StartLessonPractice",
            StringComparison.Ordinal);
        var end = controller.IndexOf(
            "[HttpPost(\"lesson-game/start\")",
            start,
            StringComparison.Ordinal);
        var block = controller[start..end];

        var adaptiveStart = block.IndexOf(
            "adaptivePractice.StartLessonAsync",
            StringComparison.Ordinal);
        var specializedFallback = block.IndexOf(
            "LessonPracticePresentationKind.SpecializedGame",
            StringComparison.Ordinal);
        var legacyGenerate = block.IndexOf(
            "privatePractice.GenerateAsync",
            StringComparison.Ordinal);

        Assert.True(adaptiveStart >= 0);
        Assert.True(specializedFallback > adaptiveStart);
        Assert.True(legacyGenerate > adaptiveStart);
        Assert.Contains(
            "GenerationFailed or",
            block,
            StringComparison.Ordinal);
        Assert.Contains(
            "never",
            block,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "silently",
            block,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void U2_AdaptiveRuntime_ExcludesPriorSemanticIdentityKeys()
    {
        var root = FindRoot();
        var service = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Services/AdaptivePractice/AdaptivePracticeV2Service.cs"));

        Assert.Contains(
            "replayTurns",
            service,
            StringComparison.Ordinal);
        Assert.Contains(
            "SemanticIdentityKey",
            service,
            StringComparison.Ordinal);
        Assert.Contains(
            ".Distinct(StringComparer.Ordinal)",
            service,
            StringComparison.Ordinal);
    }

    [Fact]
    public void U3_LearnerRuntime_UsesStudentPortalLayoutAndNoVersionBranding()
    {
        var root = FindRoot();
        var view = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Views/StudentAdaptivePractice/Attempt.cshtml"));

        Assert.Contains(
            "Layout = \"_StudentLayout\"",
            view,
            StringComparison.Ordinal);
        Assert.Contains(
            "Edulytics Practice",
            view,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "<html",
            view,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "EDULYTICS ADAPTIVE",
            view,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "Adaptive Practice V2",
            view,
            StringComparison.Ordinal);
    }

    [Fact]
    public void U4_CompletionLinksBackToEvidenceStream()
    {
        var root = FindRoot();
        var view = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Views/StudentAdaptivePractice/Attempt.cshtml"));

        Assert.Contains(
            "StudentAdaptiveIntelligence",
            view,
            StringComparison.Ordinal);
        Assert.Contains(
            "QuestionLog",
            view,
            StringComparison.Ordinal);
    }

    [Fact]
    public void U5_PrivateGeneratorAndLessonPractice_UseSeparateIntendedPaths()
    {
        var root = FindRoot();
        var controller = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Controllers/StudentPracticeController.cs"));

        var generateStart = controller.IndexOf(
            "public async Task<IActionResult> Generate(",
            StringComparison.Ordinal);
        var lessonStart = controller.IndexOf(
            "public async Task<IActionResult> StartLessonPractice(",
            StringComparison.Ordinal);
        var lessonGameStart = controller.IndexOf(
            "[HttpPost(\"lesson-game/start\")",
            lessonStart,
            StringComparison.Ordinal);

        var generateBlock = controller[generateStart..lessonStart];
        var lessonBlock = controller[lessonStart..lessonGameStart];

        Assert.Contains(
            "privatePractice.GenerateAsync",
            generateBlock,
            StringComparison.Ordinal);
        Assert.Contains(
            "mode = PersonalTestMode",
            generateBlock,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "return await StartLessonPractice(",
            generateBlock,
            StringComparison.Ordinal);

        Assert.Contains(
            "adaptivePractice.StartLessonAsync",
            lessonBlock,
            StringComparison.Ordinal);
        Assert.Contains(
            "Compatibility-only path",
            lessonBlock,
            StringComparison.Ordinal);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(
            AppContext.BaseDirectory);

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
