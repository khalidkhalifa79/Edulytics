using System.Text.Json;
using Edulytics.Core.Entities;
using Edulytics.Core.Mathematics.Practice;
using Edulytics.Services.Practice;
using Edulytics.Web.Presentation;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class PracticeV2FullCatalogueCertificationTests
{
    private const int ExpectedLessonCount = 5110;
    private const string RunEnvironmentVariable =
        "EDULYTICS_RUN_FULL_PRACTICE_V2_CERTIFICATION";

    [Fact]
    public void EveryLearnerVisibleLessonProducesCertifiedPracticeV2Sessions()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(
                    RunEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            // The dedicated Mathematics Intelligence workflow executes the
            // exhaustive certification once. Ordinary/full regression runs
            // retain the test without paying the 4,453-lesson runtime cost.
            return;
        }

        var contracts = LessonPracticeContractRegistry.All
            .OrderBy(contract => contract.LessonCode, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(ExpectedLessonCount, contracts.Length);

        var engine = new Stage18SkillContractPracticeEngine();
        var validator = new PracticeSessionQualityValidator();
        var rows = new List<CertificationRow>(contracts.Length);
        var blockers = new List<string>();

        for (var index = 0; index < contracts.Length; index++)
        {
            var contract = contracts[index];
            var legacy = contract.ToLegacyStage18Contract();

            var standard = CertifySession(
                engine,
                validator,
                legacy,
                index,
                progressive: false);

            var progressive = CertifySession(
                engine,
                validator,
                legacy,
                index,
                progressive: true);

            rows.Add(new CertificationRow(
                contract.LessonCode,
                contract.SkillId,
                contract.AllowedQuestionFamilies.ToArray(),
                standard,
                progressive));

            if (!standard.IsReady)
            {
                blockers.Add(
                    $"{contract.LessonCode}: standard={standard.StatusCode} " +
                    $"[{string.Join(",", standard.ReasonCodes)}]" +
                    (string.IsNullOrWhiteSpace(standard.Exception)
                        ? string.Empty
                        : $" exception={standard.Exception}"));
            }

            if (!progressive.IsReady)
            {
                blockers.Add(
                    $"{contract.LessonCode}: progressive={progressive.StatusCode} " +
                    $"[{string.Join(",", progressive.ReasonCodes)}]" +
                    (string.IsNullOrWhiteSpace(progressive.Exception)
                        ? string.Empty
                        : $" exception={progressive.Exception}"));
            }
        }

        var summary = new CertificationSummary(
            ExpectedLessonCount,
            rows.Count,
            rows.Count(row =>
                row.Standard.StatusCode == "READY_BALANCED"),
            rows.Count(row =>
                row.Standard.StatusCode == "READY_NARROW"),
            rows.Count(row =>
                row.Progressive.StatusCode == "READY_BALANCED"),
            rows.Count(row =>
                row.Progressive.StatusCode == "READY_NARROW"),
            rows.Count(row =>
                !row.Standard.IsReady ||
                !row.Progressive.IsReady),
            rows.Sum(row =>
                row.Standard.ActualQuestionCount),
            rows.Sum(row =>
                row.Progressive.ActualQuestionCount),
            rows.Sum(row =>
                row.Standard.DistinctSemanticQuestions),
            rows.Sum(row =>
                row.Progressive.DistinctSemanticQuestions),
            rows.Sum(row =>
                row.Standard.VisualRenderedCount),
            rows.Sum(row =>
                row.Progressive.VisualRenderedCount),
            rows.Sum(row =>
                row.Standard.VisualLeakBlockedCount),
            rows.Sum(row =>
                row.Progressive.VisualLeakBlockedCount));

        WriteReport(summary, rows);

        Assert.True(
            blockers.Count == 0,
            "Practice V2 full catalogue certification blockers: " +
            string.Join(" | ", blockers.Take(100)) +
            (blockers.Count > 100
                ? $" (+{blockers.Count - 100} more)"
                : string.Empty));
    }

    private static SessionCertification CertifySession(
        Stage18SkillContractPracticeEngine engine,
        PracticeSessionQualityValidator validator,
        Stage18PracticeSkillContract contract,
        int index,
        bool progressive)
    {
        const int standardRequestedCount = 10;
        const int progressiveRequestedCount = 8;
        var requestedCount = progressive
            ? progressiveRequestedCount
            : standardRequestedCount;

        try
        {
            var schoolId = DeterministicGuid(index, progressive, 1);
            var adoptionId = DeterministicGuid(index, progressive, 2);
            var lessonId = DeterministicGuid(index, progressive, 3);
            var userId = DeterministicGuid(index, progressive, 4);
            var seed = unchecked(
                20260923 +
                (index * 37) +
                (progressive ? 1000000 : 0));

            IReadOnlyList<AssessmentItem> items = progressive
                ? engine.GenerateProgressiveLesson(
                    schoolId,
                    adoptionId,
                    lessonId,
                    contract,
                    seed,
                    [],
                    userId)
                : engine.GenerateComposed(
                    schoolId,
                    adoptionId,
                    lessonId,
                    contract,
                    StudentPrivatePracticeDifficulty.MyLevel,
                    requestedCount,
                    seed,
                    [],
                    userId);

            var quality = validator.Validate(
                contract,
                items,
                requestedCount);

            var visualRenderedCount = 0;
            var visualLeakBlockedCount = 0;
            var visualFailures = new List<string>();

            foreach (var item in items)
            {
                var svg = PracticeMathVisualRenderer.RenderSvg(
                    item.GenerationFamily,
                    item.GenerationParametersJson);

                if (string.IsNullOrWhiteSpace(svg))
                    continue;

                visualRenderedCount++;
                var visualCheck =
                    PracticeAnswerLeakValidator.ValidateVisual(
                        item.GenerationFamily,
                        item.CorrectAnswer,
                        svg);

                if (!visualCheck.IsSafe)
                {
                    visualLeakBlockedCount++;
                    visualFailures.Add(visualCheck.ReasonCode);
                }
            }

            var readiness = quality.IsReady &&
                            visualLeakBlockedCount == 0;

            var reasonCodes = quality.ReasonCodes
                .Concat(visualFailures)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(reason => reason, StringComparer.Ordinal)
                .ToArray();

            return new SessionCertification(
                readiness,
                quality.StatusCode,
                reasonCodes,
                requestedCount,
                items.Count,
                quality.DistinctSemanticQuestions,
                quality.DistinctFamilies,
                quality.DistinctQuestionForms,
                quality.DistinctCognitiveOperations,
                items
                    .Select(item => item.Difficulty.ToString())
                    .Distinct(StringComparer.Ordinal)
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray(),
                visualRenderedCount,
                visualLeakBlockedCount,
                null);
        }
        catch (Exception exception)
        {
            return new SessionCertification(
                false,
                "BLOCKED_GENERATION",
                ["GENERATION_EXCEPTION"],
                requestedCount,
                0,
                0,
                0,
                0,
                0,
                [],
                0,
                0,
                $"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static Guid DeterministicGuid(
        int index,
        bool progressive,
        byte discriminator)
    {
        Span<byte> bytes = stackalloc byte[16];
        BitConverter.TryWriteBytes(
            bytes[..4],
            index + 1);
        BitConverter.TryWriteBytes(
            bytes.Slice(4, 4),
            progressive ? 1 : 0);
        bytes[8] = discriminator;
        bytes[15] = 1;
        return new Guid(bytes);
    }

    private static void WriteReport(
        CertificationSummary summary,
        IReadOnlyList<CertificationRow> rows)
    {
        var root = FindRepositoryRoot();
        var outputDirectory = Path.Combine(
            root,
            "artifacts",
            "math-intelligence");
        Directory.CreateDirectory(outputDirectory);

        var path = Path.Combine(
            outputDirectory,
            "practice-v2-full-catalogue-certification.json");

        var payload = new
        {
            schemaVersion = 1,
            audit =
                "Practice Assessment Composition V2 full runtime certification",
            generatedAtUtc = DateTime.UtcNow,
            summary,
            lessons = rows
        };

        File.WriteAllText(
            path,
            JsonSerializer.Serialize(
                payload,
                new JsonSerializerOptions
                {
                    WriteIndented = true
                }) + Environment.NewLine);
    }

    private static string FindRepositoryRoot()
    {
        var workspace =
            Environment.GetEnvironmentVariable("GITHUB_WORKSPACE");
        if (!string.IsNullOrWhiteSpace(workspace) &&
            File.Exists(Path.Combine(workspace, "Edulytics.sln")))
        {
            return workspace;
        }

        var directory = new DirectoryInfo(
            Directory.GetCurrentDirectory());
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

        throw new InvalidOperationException(
            "Unable to locate Edulytics repository root for certification report.");
    }

    private sealed record CertificationSummary(
        int ExpectedLessonCount,
        int CertifiedLessonCount,
        int StandardReadyBalancedCount,
        int StandardReadyNarrowCount,
        int ProgressiveReadyBalancedCount,
        int ProgressiveReadyNarrowCount,
        int BlockedLessonCount,
        int StandardGeneratedQuestionCount,
        int ProgressiveGeneratedQuestionCount,
        int StandardDistinctSemanticQuestionCount,
        int ProgressiveDistinctSemanticQuestionCount,
        int StandardRenderedVisualCount,
        int ProgressiveRenderedVisualCount,
        int StandardVisualLeakBlockedCount,
        int ProgressiveVisualLeakBlockedCount);

    private sealed record CertificationRow(
        string LessonCode,
        string SkillId,
        IReadOnlyList<string> AllowedQuestionFamilies,
        SessionCertification Standard,
        SessionCertification Progressive);

    private sealed record SessionCertification(
        bool IsReady,
        string StatusCode,
        IReadOnlyList<string> ReasonCodes,
        int RequestedQuestionCount,
        int ActualQuestionCount,
        int DistinctSemanticQuestions,
        int DistinctFamilies,
        int DistinctQuestionForms,
        int DistinctCognitiveOperations,
        IReadOnlyList<string> DisplayDifficulties,
        int VisualRenderedCount,
        int VisualLeakBlockedCount,
        string? Exception);
}
