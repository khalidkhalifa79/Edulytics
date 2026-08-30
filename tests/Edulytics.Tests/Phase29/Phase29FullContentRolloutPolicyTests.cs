using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29FullContentRolloutPolicyTests
{
    [Fact]
    public void RolloutPolicy_RequiresFourCurriculaAndFullContentClosure()
    {
        var root = FindRepositoryRoot();

        var rollout =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "docs/PHASE_29_FULL_CONTENT_ROLLOUT.md"));

        Assert.Contains(
            "UAE Ministry of Education Mathematics",
            rollout);

        Assert.Contains(
            "England Mathematics / National Curriculum",
            rollout);

        Assert.Contains(
            "US Common Core State Standards for Mathematics",
            rollout);

        Assert.Contains(
            "Poland national Mathematics curriculum",
            rollout);

        Assert.Contains(
            "Phase 29 is a LOCAL CLOSURE CANDIDATE",
            rollout);

        Assert.Contains(
            "Curriculum content stays in its official/source academic language",
            rollout);

        Assert.Contains(
            "Common Core canonical content is English",
            rollout);

        Assert.Contains(
            "Phase 30",
            rollout);

        Assert.Contains(
            "is NOT STARTED",
            rollout);

        Assert.Contains(
            "Generic `Lesson 01` shells are not Production Ready",
            rollout);

        Assert.Contains(
            "Phase 30 begins only after Phase 29 closure",
            rollout);
    }

    [Fact]
    public void SourcePolicy_UsesApprovedPedagogicalPriority()
    {
        var root = FindRepositoryRoot();

        var policy =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "docs/CURRICULUM_SOURCE_RESOLUTION_POLICY.md"));

        var school =
            policy.IndexOf(
                "Priority 1 — School-adopted textbook",
                StringComparison.Ordinal);

        var official =
            policy.IndexOf(
                "Priority 2 — Current official/ministry textbook",
                StringComparison.Ordinal);

        var publisher =
            policy.IndexOf(
                "Priority 3 — Widely-used publisher textbook",
                StringComparison.Ordinal);

        var framework =
            policy.IndexOf(
                "Priority 4 — Official framework only",
                StringComparison.Ordinal);

        Assert.True(
            school >= 0 &&
            official > school &&
            publisher > official &&
            framework > publisher);

        Assert.Contains(
            "\"Most widely used\" MUST NOT be asserted without recorded evidence.",
            policy);

        Assert.Contains(
            "official Standards / Learning Outcomes",
            policy,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void PolandRollout_UsesComplete2025_2026Baseline()
    {
        var root =
            FindRepositoryRoot();

        var policy =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "docs/CURRICULUM_SOURCE_RESOLUTION_POLICY.md"));

        var rollout =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "docs/PHASE_29_FULL_CONTENT_ROLLOUT.md"));

        Assert.Contains(
            "Poland 2026-2027 rollout baseline decision",
            policy);

        Assert.Contains(
            "2025-2026 Polish curriculum",
            policy);

        Assert.Contains(
            "PreviousOfficialFallback",
            policy);

        Assert.Contains(
            "the 2026 transitional curriculum is not mixed into this rollout",
            rollout,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "PL-MATH-2025-2026",
            rollout);
    }

    private static string FindRepositoryRoot()
    {
        var directory =
            new DirectoryInfo(AppContext.BaseDirectory);

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
            "Repository root not found.");
    }
}
