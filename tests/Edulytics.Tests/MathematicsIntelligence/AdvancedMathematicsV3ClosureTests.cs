using System.Numerics;
using System.Text.Json;
using Edulytics.Core.Mathematics.Ast;
using Edulytics.Core.Mathematics.Rollout;
using Edulytics.Core.Mathematics.Solving;
using Edulytics.Services.Mathematics.Generation;
using Edulytics.Services.Mathematics.Rollout;
using Edulytics.Services.Mathematics.Solving;
using Edulytics.Services.Mathematics.Verification;
using Edulytics.Services.Mathematics.Visuals;
using Edulytics.Web.Presentation;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class AdvancedMathematicsV3ClosureTests
{
    [Fact]
    public void RegistryContainsBroadAdvancedEngineBaselineWithoutProductionAutoRouting()
    {
        var rows = AdvancedMathematicsCertificationRegistry.All;

        Assert.True(rows.Count >= 100);
        Assert.Contains(rows, x => x.FamilyId.StartsWith("functions.", StringComparison.Ordinal));
        Assert.Contains(rows, x => x.FamilyId.StartsWith("calculus.", StringComparison.Ordinal));
        Assert.Contains(rows, x => x.FamilyId.StartsWith("trigonometry.", StringComparison.Ordinal));
        Assert.Contains(rows, x => x.FamilyId.StartsWith("vectors.", StringComparison.Ordinal));
        Assert.Contains(rows, x => x.FamilyId.StartsWith("matrices.", StringComparison.Ordinal));
        Assert.Contains(rows, x => x.FamilyId.StartsWith("complex.", StringComparison.Ordinal));
        Assert.Contains(rows, x => x.FamilyId.StartsWith("probability.", StringComparison.Ordinal));
        Assert.Contains(rows, x => x.FamilyId.StartsWith("statistics.", StringComparison.Ordinal));
        Assert.Contains(rows, x => x.FamilyId.StartsWith("numerical.", StringComparison.Ordinal));
        Assert.Contains(rows, x => x.FamilyId.StartsWith("mechanics.", StringComparison.Ordinal));

        var area = Assert.Single(
            rows,
            x => x.FamilyId ==
                 ExactAreaBetweenCurvesQuestionFactory.FamilyId);

        Assert.Equal(
            AdvancedMathematicsReadiness.EngineVerified,
            area.HighestReadiness);
        Assert.False(area.AcademicReviewApproved);
        Assert.False(area.AdaptiveApproved);
        Assert.False(
            AdvancedMathematicsCertificationRegistry.IsReadyFor(
                area.FamilyId,
                AdvancedMathematicsSurface.Assessment));
        Assert.False(
            AdvancedMathematicsCertificationRegistry.IsReadyFor(
                area.FamilyId,
                AdvancedMathematicsSurface.Exam));
    }

    [Fact]
    public void V3PolicyIsFailClosedAcrossPracticeAssessmentExamAndAdaptive()
    {
        var off = new AdvancedMathematicsV3Policy(
            false,
            false,
            false,
            false,
            false,
            new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            new HashSet<Guid>());

        var family = "supporting.calculus.derivative_value";

        Assert.False(
            AdvancedMathematicsCertificationRegistry.CanRoute(
                family,
                AdvancedMathematicsSurface.Practice,
                off,
                "L12",
                Guid.NewGuid()));

        var allFlags = off with
        {
            Enabled = true,
            PracticeEnabled = true,
            AssessmentEnabled = true,
            ExamEnabled = true,
            AdaptiveEnabled = true
        };

        var exam = AdvancedMathematicsExamEligibility.Evaluate(
            new AdvancedMathematicsMarkSchemeContract(
                ExactAreaBetweenCurvesQuestionFactory.FamilyId,
                "exact_scalar",
                3,
                1,
                true),
            allFlags,
            "L12",
            Guid.NewGuid());

        Assert.False(exam.Allowed);
        Assert.Equal(
            "ADVANCED_EXAM_BLOCKED_ACADEMIC_OR_CERTIFICATION",
            exam.ReasonCode);

        var primary = AdvancedMathematicsAdaptiveCompatibility.Evaluate(
            family,
            allFlags,
            "L6",
            Guid.NewGuid(),
            6);

        Assert.False(primary.Allowed);
        Assert.Equal(
            "PRIMARY_BOUNDARY_HARD_BLOCK",
            primary.ReasonCode);
    }

    [Fact]
    public void AreaSolutionTracePassesM10StructuredTraceGate()
    {
        var problem = new AreaBetweenCurvesNode(
            new AddNode([
                I(4),
                new NegateNode(
                    new PowerNode(
                        new SymbolNode("x"),
                        I(2)))
            ]),
            I(0),
            new SymbolNode("x"),
            I(-3),
            I(3));

        var result = new ExactAreaBetweenCurvesSolver().Solve(
            new MathematicsSolveRequest(problem, [], []));

        var validation =
            AdvancedMathematicsSolutionTraceValidator.Validate(
                result.Trace);

        Assert.True(validation.IsValid);
        Assert.Empty(validation.Diagnostics);
    }

    [Fact]
    public void FormalAdvancedCurriculumGatesRemainBlockedUntilAcademicReview()
    {
        var igcse = LoadRepositoryJson(
            "src", "Edulytics.Core", "Mathematics", "Curriculum",
            "stage23-igcse-extended-gate-manifest.v1.json");
        var aLevel = LoadRepositoryJson(
            "src", "Edulytics.Core", "Mathematics", "Curriculum",
            "stage24-as-a-level-9709-gate-manifest.v1.json");
        var ib = LoadRepositoryJson(
            "src", "Edulytics.Core", "Mathematics", "Curriculum",
            "stage25-ib-aa-hl-style-gate-manifest.v1.json");

        Assert.False(
            igcse.RootElement
                .GetProperty("gatePolicy")
                .GetProperty("globalIgcseExtendedCapabilityClaimAllowed")
                .GetBoolean());
        Assert.False(
            igcse.RootElement
                .GetProperty("gatePolicy")
                .GetProperty("productRoutingEnabled")
                .GetBoolean());
        Assert.Equal(
            0,
            igcse.RootElement
                .GetProperty("summary")
                .GetProperty("formalOutcomeMapped")
                .GetInt32());

        Assert.False(
            aLevel.RootElement
                .GetProperty("gatePolicy")
                .GetProperty("global9709CapabilityClaimAllowed")
                .GetBoolean());
        Assert.False(
            aLevel.RootElement
                .GetProperty("gatePolicy")
                .GetProperty("productRoutingEnabled")
                .GetBoolean());
        Assert.Equal(
            57,
            aLevel.RootElement
                .GetProperty("summary")
                .GetProperty("formalOutcomeMapped")
                .GetInt32());

        Assert.False(
            ib.RootElement
                .GetProperty("gatePolicy")
                .GetProperty("globalIbAaHlCapabilityClaimAllowed")
                .GetBoolean());
        Assert.False(
            ib.RootElement
                .GetProperty("gatePolicy")
                .GetProperty("productRoutingEnabled")
                .GetBoolean());
        Assert.Equal(
            0,
            ib.RootElement
                .GetProperty("summary")
                .GetProperty("formalIbMappings")
                .GetInt32());
    }

    [Fact]
    public void M15FuzzAreaGeneratorVerifierAndVisualRemainDeterministicAndBounded()
    {
        var factory = new ExactAreaBetweenCurvesQuestionFactory(
            new ExactAreaBetweenCurvesSolver(),
            new ExactAreaBetweenCurvesVerifier());

        for (var seed = 1; seed <= 100; seed++)
        {
            var difficulty = 1 + seed % 3;
            var first = factory.Generate(seed, difficulty);
            var second = factory.Generate(seed, difficulty);

            Assert.Equal(
                JsonSerializer.Serialize(first.Problem),
                JsonSerializer.Serialize(second.Problem));
            Assert.Equal(
                JsonSerializer.Serialize(first.ExpectedAnswer),
                JsonSerializer.Serialize(second.ExpectedAnswer));
            Assert.True(first.Verification.IsVerified);

            var problem = Assert.IsType<AreaBetweenCurvesNode>(
                first.Problem);

            Assert.True(
                AdvancedMathematicsVisualProjector
                    .TryProjectAreaBetweenCurves(
                        problem,
                        first.SolveResult,
                        out var visual));

            var svg =
                AdvancedMathematicsVisualRenderer.RenderSvg(
                    visual!);

            Assert.True(svg.Length < 120_000);
            Assert.DoesNotContain(
                "<script",
                svg,
                StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(
                "foreignObject",
                svg,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ProductionConfigurationKeepsV3DisabledByDefault()
    {
        var appsettings = ReadRepositoryFile(
            "src", "Edulytics.Web", "appsettings.json");

        using var json = JsonDocument.Parse(appsettings);
        var v3 = json.RootElement
            .GetProperty("Edulytics")
            .GetProperty("AdvancedMathematicsV3");

        Assert.False(v3.GetProperty("Enabled").GetBoolean());
        Assert.False(v3.GetProperty("PracticeEnabled").GetBoolean());
        Assert.False(v3.GetProperty("AssessmentEnabled").GetBoolean());
        Assert.False(v3.GetProperty("ExamEnabled").GetBoolean());
        Assert.False(v3.GetProperty("AdaptiveEnabled").GetBoolean());
    }

    private static JsonDocument LoadRepositoryJson(
        params string[] segments) =>
        JsonDocument.Parse(
            ReadRepositoryFile(segments));

    private static string ReadRepositoryFile(
        params string[] relativeSegments)
    {
        var directory = new DirectoryInfo(
            AppContext.BaseDirectory);

        while (directory is not null &&
               !File.Exists(
                   Path.Combine(
                       directory.FullName,
                       "Edulytics.sln")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName
            ?? throw new InvalidOperationException(
                "Repository root not found.");

        return File.ReadAllText(
            Path.Combine(
                [root, .. relativeSegments]));
    }

    private static IntegerNode I(int value) =>
        new(new BigInteger(value));
}
