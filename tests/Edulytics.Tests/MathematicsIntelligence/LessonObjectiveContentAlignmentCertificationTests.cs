using System.Text.Json;
using Edulytics.Core.Curriculum;
using Edulytics.Core.Mathematics.Practice;
using Edulytics.Data.Seeding;
using Edulytics.Services.LessonContent;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class LessonObjectiveContentAlignmentCertificationTests
{
    private const int ExpectedLessonCount = 5110;
    private const string RunEnvironmentVariable =
        "EDULYTICS_RUN_LESSON_OBJECTIVE_CONTENT_CERTIFICATION";

    [Fact]
    public async Task EveryLessonHasExplicitObjectiveProvenanceAndTargetScopedHelp()
    {
        if (!string.Equals(
                Environment.GetEnvironmentVariable(
                    RunEnvironmentVariable),
                "1",
                StringComparison.Ordinal))
        {
            return;
        }

        var documents =
            MathematicsCanonicalLessonContentSeeder
                .LoadEmbeddedDocuments()
                .OrderBy(x => x.PackCode, StringComparer.Ordinal)
                .ThenBy(x => x.VersionCode, StringComparer.Ordinal)
                .ToArray();

        foreach (var document in documents)
            CanonicalLessonContentPackContract.Validate(document);

        RichLessonExternalHelpRegistry.Validate();

        var youtube =
            new YouTubeLessonDiscoveryService(
                new HttpClient(),
                new YouTubeLessonDiscoveryOptions
                {
                    Enabled = true,
                    ApiKey = string.Empty
                });

        var rows = new List<object>();
        var blockers = new List<string>();
        var warnings = new List<string>();

        foreach (var document in documents)
        {
            foreach (var lesson in document.Lessons)
            {
                try
                {
                    if (!LessonPracticeCapabilityResolver.TryResolve(
                            lesson.LessonCode,
                            out var contract) ||
                        contract is null)
                    {
                        throw new InvalidOperationException(
                            "No exact lesson Practice contract.");
                    }

                    if (!string.Equals(
                            contract.Readiness,
                            "READY_VERIFIED",
                            StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Practice contract is not READY_VERIFIED: {contract.Readiness}.");
                    }

                    var translation =
                        lesson.Translations.FirstOrDefault(x =>
                            string.Equals(
                                x.CultureCode,
                                document.AcademicLanguage,
                                StringComparison.OrdinalIgnoreCase))
                        ?? lesson.Translations.First();

                    if (lesson.IsSupporting &&
                        lesson.OutcomeCodes.Count != 0)
                    {
                        throw new InvalidOperationException(
                            "Supporting lesson exposes an official OutcomeCode.");
                    }

                    var objectiveKind =
                        lesson.IsSupporting
                            ? "EDULYTICS_SUPPORTING_SKILL"
                            : lesson.OutcomeCodes.Count > 0
                                ? "OFFICIAL_OUTCOME_MAPPED"
                                : "CURRICULUM_LESSON_WITHOUT_VERIFIED_OUTCOME_MAPPING";

                    var exactSkillLabel = string.Join(
                        " ",
                        contract.SkillId.Split(
                            ['.', '_', '-'],
                            StringSplitOptions.RemoveEmptyEntries));

                    var help =
                        RichLessonExternalHelpRegistry.Resolve(
                            lesson.LessonCode,
                            translation.Title,
                            translation.CultureCode);

                    if (help.SearchSuggestions.Count == 0 ||
                        help.SearchSuggestions.Any(x =>
                            !string.Equals(
                                x.Provider,
                                "YouTube",
                                StringComparison.OrdinalIgnoreCase) ||
                            !x.Url.StartsWith(
                                "https://www.youtube.com/",
                                StringComparison.OrdinalIgnoreCase) ||
                            x.Url.Contains(
                                "google",
                                StringComparison.OrdinalIgnoreCase) ||
                            !x.Query.Contains(
                                translation.Title,
                                StringComparison.OrdinalIgnoreCase) ||
                            !x.Query.Contains(
                                exactSkillLabel,
                                StringComparison.OrdinalIgnoreCase)))
                    {
                        throw new InvalidOperationException(
                            "External-help search is not scoped to the exact lesson title and SkillContract.");
                    }

                    if (help.ApprovedResources.Any(x =>
                            x.ReviewStatus !=
                                RichLessonExternalResourceReviewStatus.Approved) ||
                        help.ApprovedVideos.Any(x =>
                            x.ReviewStatus !=
                                RichLessonExternalResourceReviewStatus.Approved))
                    {
                        throw new InvalidOperationException(
                            "Unapproved external help reached the learner-facing registry.");
                    }

                    var youtubeResult =
                        await youtube.DiscoverAsync(
                            lesson.LessonCode,
                            translation.Title,
                            gradeLabel: string.Empty,
                            cultureCode: translation.CultureCode,
                            learnerQuery: null);

                    var youtubeTopic =
                        System.Text.RegularExpressions.Regex.Replace(
                            translation.Title,
                            @"\s*(?:—|–|-|:)\s*(?:advanced\s+reasoning|foundation(?:\s+explanation)?|worked\s+examples?)\s*$",
                            string.Empty,
                            System.Text.RegularExpressions.RegexOptions.IgnoreCase |
                            System.Text.RegularExpressions.RegexOptions.CultureInvariant)
                        .Trim();

                    if (!youtubeResult.SearchQuery.Contains(
                            youtubeTopic,
                            StringComparison.OrdinalIgnoreCase) ||
                        !youtubeResult.SearchQuery.Contains(
                            exactSkillLabel,
                            StringComparison.OrdinalIgnoreCase) ||
                        !youtubeResult.SearchUrl.StartsWith(
                            "https://www.youtube.com/results?search_query=",
                            StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidOperationException(
                            "Dynamic YouTube discovery is not anchored to the exact lesson topic and SkillContract.");
                    }

                    var quality =
                        RichLessonContentQualityAudit.Evaluate(
                            document,
                            lesson,
                            translation);

                    if (quality.OverallQuality !=
                        RichLessonOverallQuality.Good)
                    {
                        warnings.Add(
                            $"{lesson.LessonCode}: {quality.OverallQuality} - " +
                            string.Join(" ", quality.Findings));
                    }

                    rows.Add(new
                    {
                        lessonCode = lesson.LessonCode,
                        document.PackCode,
                        document.SourceAuthority,
                        document.SourcePolicyVersion,
                        title = translation.Title,
                        titleProvenance = lesson.TitleProvenance.ToString(),
                        objectiveKind,
                        officialOutcomeCodes = lesson.OutcomeCodes,
                        contract.SkillId,
                        questionFamilies = contract.AllowedQuestionFamilies,
                        contract.Readiness,
                        contentQuality = quality.OverallQuality.ToString(),
                        approvedResourceCount = help.ApprovedResources.Count,
                        approvedVideoCount = help.ApprovedVideos.Count,
                        youtubeQuery = youtubeResult.SearchQuery
                    });
                }
                catch (Exception exception)
                {
                    blockers.Add(
                        $"{document.PackCode} / {lesson.LessonCode}: " +
                        $"{exception.GetType().Name}: {exception.Message}");
                }
            }
        }

        WriteReport(
            rows,
            blockers,
            warnings);

        Assert.Equal(
            ExpectedLessonCount,
            documents.Sum(x => x.Lessons.Count));

        Assert.Equal(
            ExpectedLessonCount,
            rows.Count + blockers.Count);

        Assert.True(
            blockers.Count == 0,
            "Lesson objective/content alignment blockers: " +
            string.Join(" | ", blockers.Take(100)) +
            (blockers.Count > 100
                ? $" (+{blockers.Count - 100} more)"
                : string.Empty));
    }

    private static void WriteReport(
        IReadOnlyList<object> rows,
        IReadOnlyList<string> blockers,
        IReadOnlyList<string> warnings)
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(
            root,
            "artifacts",
            "math-intelligence");

        Directory.CreateDirectory(directory);

        var elements =
            rows.Select(row =>
                    JsonSerializer.SerializeToElement(row))
                .ToArray();

        var objectiveKinds =
            elements
                .GroupBy(
                    x => x.GetProperty("objectiveKind").GetString()
                         ?? string.Empty,
                    StringComparer.Ordinal)
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .ToDictionary(
                    x => x.Key,
                    x => x.Count(),
                    StringComparer.Ordinal);

        var quality =
            elements
                .GroupBy(
                    x => x.GetProperty("contentQuality").GetString()
                         ?? string.Empty,
                    StringComparer.Ordinal)
                .OrderBy(x => x.Key, StringComparer.Ordinal)
                .ToDictionary(
                    x => x.Key,
                    x => x.Count(),
                    StringComparer.Ordinal);

        var payload = new
        {
            schemaVersion = 1,
            audit =
                "Lesson objective, learner-content and external-help target alignment certification",
            generatedAtUtc = DateTime.UtcNow,
            summary = new
            {
                expectedLessonCount = ExpectedLessonCount,
                certifiedLessonCount = rows.Count,
                blockerCount = blockers.Count,
                warningCount = warnings.Count,
                objectiveKinds,
                quality,
                lessonsWithApprovedResources =
                    elements.Count(x =>
                        x.GetProperty("approvedResourceCount").GetInt32() > 0),
                lessonsWithApprovedVideos =
                    elements.Count(x =>
                        x.GetProperty("approvedVideoCount").GetInt32() > 0)
            },
            policy = new
            {
                supportingLessonsNeverInventOfficialOutcomeCodes = true,
                officialOutcomeTextRemainsAuthorityOwned = true,
                edulyticsSkillContractIsTheExactPracticeTarget = true,
                learnerSearchUsesLessonTitleAndVerifiedSkill = true,
                externalHelpIsSupplementalAndNeverAffectsMastery = true
            },
            blockers,
            warnings,
            lessons = rows
        };

        File.WriteAllText(
            Path.Combine(
                directory,
                "lesson-objective-content-alignment-certification.json"),
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

        var directory =
            new DirectoryInfo(Directory.GetCurrentDirectory());

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
            "Unable to locate Edulytics repository root.");
    }
}
