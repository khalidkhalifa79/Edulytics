using System.Text.Json;
using Edulytics.Core.Mathematics.Planning;
using Edulytics.Core.Mathematics.Solving;
using Edulytics.Services.Mathematics.Contracts;
using Edulytics.Services.Mathematics.Difficulty;
using Edulytics.Services.Mathematics.Generation;
using Edulytics.Services.Mathematics.Planning;
using Edulytics.Services.Mathematics.Solving;
using Edulytics.Services.Mathematics.Verification;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class Stage24AsALevel9709GateTests
{
    [Fact]
    public void GateInventoriesAll9709LessonsAndReportsCoveragePerPaperRoute()
    {
        var root = FindRoot();
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Mathematics/Curriculum/stage24-as-a-level-9709-gate-manifest.v1.json")));

        var lessons = manifest.RootElement.GetProperty("lessons").EnumerateArray().ToArray();
        Assert.Equal(57, lessons.Length);
        Assert.Equal(57, lessons.Select(x => x.GetProperty("lessonCode").GetString()).Distinct().Count());

        var routes = manifest.RootElement.GetProperty("coverageByPaper").EnumerateArray().ToArray();
        Assert.Equal(6, routes.Length);
        Assert.Equal(
            new[] { "A-MECHANICS", "A-PROBSTAT", "A-PURE", "AS-MECHANICS", "AS-PROBSTAT", "AS-PURE" },
            routes.Select(x => x.GetProperty("paperRoute").GetString()).OrderBy(x => x).ToArray());

        Assert.All(routes, route =>
        {
            Assert.False(route.GetProperty("productRoutingEnabled").GetBoolean());
            Assert.Equal("GATED", route.GetProperty("capabilityClaim").GetString());
            Assert.Equal(0, route.GetProperty("verified").GetInt32());
            Assert.Equal(
                lessons.Count(lesson => lesson.GetProperty("paperRoute").GetString() == route.GetProperty("paperRoute").GetString()
                    && lesson.GetProperty("formalOutcomeMapped").GetBoolean()),
                route.GetProperty("formalOutcomeMapped").GetInt32());
        });

        var summary = manifest.RootElement.GetProperty("summary");
        Assert.Equal(0, summary.GetProperty("verified").GetInt32());
        Assert.Equal(25, summary.GetProperty("contextual").GetInt32());
        Assert.Equal(32, summary.GetProperty("unsupported").GetInt32());
        Assert.Equal(57, summary.GetProperty("formalOutcomeMapped").GetInt32());
        Assert.Equal(6, summary.GetProperty("paperRouteCount").GetInt32());

        var gate = manifest.RootElement.GetProperty("gatePolicy");
        Assert.Equal("domain-and-paper-route", gate.GetProperty("coverageClaimGranularity").GetString());
        Assert.False(gate.GetProperty("global9709CapabilityClaimAllowed").GetBoolean());
        Assert.False(gate.GetProperty("productRoutingEnabled").GetBoolean());
        Assert.True(gate.GetProperty("titleSimilarityAloneNeverPromotes").GetBoolean());
    }

    [Fact]
    public void EveryContextual9709LessonReferencesEngineBenchmarkAndRequiresAcademicReview()
    {
        var root = FindRoot();
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Mathematics/Curriculum/stage24-as-a-level-9709-gate-manifest.v1.json")));
        using var corpus = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Mathematics/Curriculum/stage24-as-a-level-9709-benchmark-corpus.v1.json")));

        var benchmarkIds = corpus.RootElement
            .GetProperty("benchmarks")
            .EnumerateArray()
            .Select(x => x.GetProperty("benchmarkId").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        var sourceCodes = new Dictionary<string, string[]>(StringComparer.Ordinal);
        foreach (var packName in new[] { "cambridge-as-level-9709-ogl-v1", "cambridge-a-level-9709-ogl-v1" })
        {
            using var pack = JsonDocument.Parse(File.ReadAllText(Path.Combine(root,
                "src/Edulytics.Core/Curriculum/LessonBlueprints/Packs", packName + ".lesson-blueprint.json")));
            foreach (var row in pack.RootElement.GetProperty("Lessons").EnumerateArray())
                sourceCodes.Add(row.GetProperty("LessonCode").GetString()!,
                    row.GetProperty("OutcomeCodes").EnumerateArray().Select(x => x.GetString()!).ToArray());
        }

        foreach (var lesson in manifest.RootElement.GetProperty("lessons").EnumerateArray())
        {
            var expectedCodes = sourceCodes[lesson.GetProperty("lessonCode").GetString()!];
            Assert.Equal(expectedCodes.Length > 0, lesson.GetProperty("formalOutcomeMapped").GetBoolean());
            Assert.Equal(expectedCodes,
                lesson.GetProperty("formalOutcomeCodes").EnumerateArray().Select(x => x.GetString()!).ToArray());

            var status = lesson.GetProperty("status").GetString();
            var ids = lesson.GetProperty("benchmarkIds")
                .EnumerateArray()
                .Select(x => x.GetString()!)
                .ToArray();

            if (status == "CONTEXTUAL")
            {
                Assert.NotEmpty(ids);
                Assert.Equal("REQUIRED", lesson.GetProperty("academicReviewStatus").GetString());
                Assert.All(ids, id => Assert.Contains(id, benchmarkIds));
            }
            else
            {
                Assert.Equal("UNSUPPORTED", status);
                Assert.Empty(ids);
            }
        }
    }

    [Fact]
    public void BenchmarkCorpusCoversPureMechanicsAndStatisticsAndExecutesFullChain()
    {
        var root = FindRoot();
        using var corpus = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Mathematics/Curriculum/stage24-as-a-level-9709-benchmark-corpus.v1.json")));

        var corpusRows = corpus.RootElement.GetProperty("benchmarks").EnumerateArray().ToArray();
        var cases = BenchmarkCases().ToDictionary(x => x.BenchmarkId, StringComparer.Ordinal);

        Assert.Equal(25, corpusRows.Length);
        Assert.Equal(25, cases.Count);
        Assert.Equal(
            new[] { "Mechanics", "Probability & Statistics", "Pure Mathematics" },
            corpusRows.Select(x => x.GetProperty("domain").GetString()).Distinct().OrderBy(x => x).ToArray());

        var planner = new DeterministicMathematicsStrategyPlanner();
        var difficulty = new MathematicsDifficultyEngine();
        var grader = new MathematicsAnswerEquivalenceV2();

        foreach (var row in corpusRows)
        {
            var id = row.GetProperty("benchmarkId").GetString()!;
            Assert.True(cases.TryGetValue(id, out var benchmark), $"Missing executable benchmark {id}.");

            for (var band = 1; band <= 3; band++)
            {
                var generated = benchmark!.Generate(24000 + band * 131, band);

                Assert.Equal(row.GetProperty("skillId").GetString(), generated.Skill.Value);
                Assert.Equal(row.GetProperty("questionFamilyId").GetString(), generated.QuestionFamilyId);
                Assert.Equal(MathematicsSolveStatus.Solved, generated.SolveResult.Status);
                Assert.True(generated.Verification.IsVerified);

                var grade = grader.Evaluate(
                    generated.ExpectedAnswer,
                    generated.ExpectedAnswer,
                    new MathematicsAnswerEvaluationPolicy());
                Assert.True(grade.IsEquivalent);

                var plan = planner.Plan(new MathematicsPlanningRequest(generated.Problem, []));
                Assert.Equal(MathematicsPlanningStatus.Planned, plan.Status);

                var calibrated = difficulty.Assess(plan);
                Assert.InRange(calibrated.ComplexityScore, 0, 200);
                Assert.NotEmpty(calibrated.Reasons);
            }
        }
    }

    [Fact]
    public void AcademicReviewAndFormalMappingAreMandatoryForAnyVerified9709Claim()
    {
        var root = FindRoot();
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Core/Mathematics/Curriculum/stage24-as-a-level-9709-gate-manifest.v1.json")));

        var academic = manifest.RootElement.GetProperty("academicReview");
        Assert.Equal("REQUIRED", academic.GetProperty("currentState").GetString());
        Assert.Equal(0, academic.GetProperty("approvedVerifiedLessonCount").GetInt32());

        Assert.DoesNotContain(
            manifest.RootElement.GetProperty("lessons").EnumerateArray(),
            x => string.Equals(x.GetProperty("status").GetString(), "VERIFIED", StringComparison.Ordinal));
    }

    private static IReadOnlyList<BenchmarkCase> BenchmarkCases()
    {
        var mechanicsSolver = new ExactMechanicsModelSolver();
        var mechanicsVerifier = new ExactMechanicsModelVerifier();

        return
        [
            new("9709-quadratic", (s,b) => new ExactQuadraticQuestionFactory(new ExactQuadraticEquationSolver(), new ExactQuadraticEquationVerifier()).Generate(s,b)),
            new("9709-function-eval", (s,b) => new ExactFunctionEvaluationQuestionFactory(new ExactFunctionEvaluationSolver(), new ExactFunctionEvaluationVerifier()).Generate(s,b)),
            new("9709-arithmetic-sequence", (s,b) => new ExactArithmeticSequenceQuestionFactory(new ExactSequenceTermSolver(), new ExactSequenceTermVerifier()).Generate(s,b)),
            new("9709-geometric-sequence", (s,b) => new ExactGeometricSequenceQuestionFactory(new ExactSequenceTermSolver(), new ExactSequenceTermVerifier()).Generate(s,b)),
            new("9709-exponential-same-base", (s,b) => new ExactExponentialSameBaseQuestionFactory(new ExactExponentialSameBaseSolver(), new ExactExponentialSameBaseVerifier()).Generate(s,b)),
            new("9709-logarithm", (s,b) => new ExactLogarithmQuestionFactory(new ExactLogarithmSolver(), new ExactLogarithmVerifier()).Generate(s,b)),
            new("9709-special-angle-trig", (s,b) => new ExactSpecialAngleTrigonometryQuestionFactory(new ExactSpecialAngleTrigonometrySolver(), new ExactSpecialAngleTrigonometryVerifier()).Generate(s,b)),
            new("9709-polynomial-derivative", (s,b) => new ExactPolynomialDerivativeQuestionFactory(new ExactPolynomialDerivativeSolver(), new ExactPolynomialDerivativeVerifier()).Generate(s,b)),
            new("9709-polynomial-definite-integral", (s,b) => new ExactPolynomialDefiniteIntegralQuestionFactory(new ExactPolynomialDefiniteIntegralSolver(), new ExactPolynomialDefiniteIntegralVerifier()).Generate(s,b)),
            new("9709-bisection", (s,b) => new ExactBisectionIterationQuestionFactory(new ExactBisectionIterationSolver(), new ExactBisectionIterationVerifier()).Generate(s,b)),
            new("9709-newton", (s,b) => new ExactNewtonIterationQuestionFactory(new ExactNewtonIterationSolver(), new ExactNewtonIterationVerifier()).Generate(s,b)),
            new("9709-vector-add", (s,b) => new ExactVectorAddQuestionFactory(new ExactVectorAddSolver(), new ExactVectorAddVerifier()).Generate(s,b)),
            new("9709-vector-dot", (s,b) => new ExactVectorDotProductQuestionFactory(new ExactVectorDotProductSolver(), new ExactVectorDotProductVerifier()).Generate(s,b)),
            new("9709-simple-probability", (s,b) => new ExactSimpleProbabilityQuestionFactory(new ExactSimpleProbabilitySolver(), new ExactSimpleProbabilityVerifier()).Generate(s,b)),
            new("9709-probability-complement", (s,b) => new ExactProbabilityComplementQuestionFactory(new ExactProbabilityComplementSolver(), new ExactProbabilityComplementVerifier()).Generate(s,b)),
            new("9709-arithmetic-mean", (s,b) => new ExactArithmeticMeanQuestionFactory(new ExactArithmeticMeanSolver(), new ExactArithmeticMeanVerifier()).Generate(s,b)),
            new("9709-frequency-mean", (s,b) => new ExactFrequencyMeanQuestionFactory(new ExactFrequencyMeanSolver(), new ExactFrequencyMeanVerifier()).Generate(s,b)),
            new("9709-mech-velocity", (s,b) => new ExactMechanicsQuestionFactory(mechanicsSolver, mechanicsVerifier).Generate(ExactMechanicsQuestionFactory.VelocityFamily,s,b)),
            new("9709-mech-displacement", (s,b) => new ExactMechanicsQuestionFactory(mechanicsSolver, mechanicsVerifier).Generate(ExactMechanicsQuestionFactory.DisplacementFamily,s,b)),
            new("9709-mech-newton2", (s,b) => new ExactMechanicsQuestionFactory(mechanicsSolver, mechanicsVerifier).Generate(ExactMechanicsQuestionFactory.NewtonSecondFamily,s,b)),
            new("9709-mech-newton3", (s,b) => new ExactMechanicsQuestionFactory(mechanicsSolver, mechanicsVerifier).Generate(ExactMechanicsQuestionFactory.NewtonThirdFamily,s,b)),
            new("9709-mech-equilibrium", (s,b) => new ExactMechanicsQuestionFactory(mechanicsSolver, mechanicsVerifier).Generate(ExactMechanicsQuestionFactory.EquilibriumFamily,s,b)),
            new("9709-mech-momentum", (s,b) => new ExactMechanicsQuestionFactory(mechanicsSolver, mechanicsVerifier).Generate(ExactMechanicsQuestionFactory.MomentumFamily,s,b)),
            new("9709-mech-work-energy", (s,b) => new ExactMechanicsQuestionFactory(mechanicsSolver, mechanicsVerifier).Generate(ExactMechanicsQuestionFactory.WorkEnergyFamily,s,b)),
            new("9709-mech-power", (s,b) => new ExactMechanicsQuestionFactory(mechanicsSolver, mechanicsVerifier).Generate(ExactMechanicsQuestionFactory.PowerFamily,s,b))
        ];
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Edulytics.sln")))
                return directory.FullName;
            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Edulytics solution root not found.");
    }

    private sealed record BenchmarkCase(
        string BenchmarkId,
        Func<int,int,VerifiedGeneratedMathematicsProblem> Generate);
}
