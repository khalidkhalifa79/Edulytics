using Edulytics.Core.Curriculum;

namespace Edulytics.Data.Seeding;

/// <summary>
/// Approved in-place Cambridge content upgrades for legacy lessons that were
/// replaced during the official mapping rebuild. Lesson identities remain
/// stable while learner-facing bodies move to reviewed Cambridge-aligned scope.
/// </summary>
public static class CambridgeOfficialMappingContentCorrections
{
    public const string CorrectionContentVersion =
        "cambridge-official-mapping-content-v1";

    private static readonly HashSet<string> TargetLessonCodes =
        new(StringComparer.Ordinal)
        {
            "PED:CAMBRIDGE-INTL-MATH:S4:4F-1:BUILD",
            "PED:CAMBRIDGE-INTL-MATH:S4:4F-1:APPLY",
            "PED:CAMBRIDGE-INTL-MATH:S4:4F-2:BUILD",
            "PED:CAMBRIDGE-INTL-MATH:S4:4F-2:APPLY",
            "PED:CAMBRIDGE-INTL-MATH:S5:5NPV-5:BUILD",
            "PED:CAMBRIDGE-INTL-MATH:S5:5NPV-5:APPLY",
            "PED:CAMBRIDGE-INTL-MATH:S6:6NPV-4:BUILD",
            "PED:CAMBRIDGE-INTL-MATH:S6:6NPV-4:APPLY",
            "PED:CAMBRIDGE-INTL-MATH:L7:SHARED:03:07:PYTHAGORAS-THEOREM",
            "PED:CAMBRIDGE-INTL-MATH:L8:SHARED:03:07:PYTHAGORAS-THEOREM",
            "PED:CAMBRIDGE-INTL-MATH:L8:SHARED:03:09:RIGHT-TRIANGLE-REASONING",
            "PED:CAMBRIDGE-INTL-MATH:L9:SHARED:03:09:TRIGONOMETRIC-RATIOS-FOUNDATIONS",
            "PED:CAMBRIDGE-INTL-MATH:L10:CORE:07:02:VECTOR-ARITHMETIC",
            "PED:CAMBRIDGE-INTL-MATH:L11:CORE:07:02:CONSOLIDATING-VECTOR-ARITHMETIC",
            "PED:CAMBRIDGE-INTL-MATH:L13:COMPONENT-ROUTE-STRUCTURE-PRESERVED-IN-REFERENCE-GRAPH:02:08:CORRELATION-AND-REGRESSION-REASONING",
            "PED:CAMBRIDGE-INTL-MATH:L13:COMPONENT-ROUTE-STRUCTURE-PRESERVED-IN-REFERENCE-GRAPH:03:01:PROJECTILES",
            "PED:CAMBRIDGE-INTL-MATH:L13:COMPONENT-ROUTE-STRUCTURE-PRESERVED-IN-REFERENCE-GRAPH:03:05:CIRCULAR-MOTION-FOUNDATIONS",
            "PED:CAMBRIDGE-INTL-MATH:L13:COMPONENT-ROUTE-STRUCTURE-PRESERVED-IN-REFERENCE-GRAPH:03:06:EQUILIBRIUM-OF-RIGID-BODIES"
        };

    public static bool IsTarget(
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson lesson) =>
        string.Equals(
            document.PackCode,
            CambridgePrimaryStage6LessonContentCorrections.PackCode,
            StringComparison.Ordinal) &&
        TargetLessonCodes.Contains(lesson.LessonCode);

    public static string GetExpectedContentVersion(
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson lesson,
        string priorExpectedVersion) =>
        IsTarget(document, lesson)
            ? CorrectionContentVersion
            : priorExpectedVersion;

    public static bool CanUpgradeExisting(
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson lesson,
        string existingContentVersion) =>
        IsTarget(document, lesson) &&
        (
            string.Equals(
                existingContentVersion,
                document.ContentVersion,
                StringComparison.Ordinal) ||
            string.Equals(
                existingContentVersion,
                CambridgePrimaryStage6LessonContentCorrections.CorrectionContentVersion,
                StringComparison.Ordinal) ||
            string.Equals(
                existingContentVersion,
                CorrectionContentVersion,
                StringComparison.Ordinal)
        );

    public static void ApplyApprovedCorrections(
        CanonicalLessonContentPackDocument document)
    {
        // The reviewed replacement bodies are stored directly in the canonical
        // content packs. This hook intentionally performs no textual mutation;
        // it exists to provide an explicit, versioned production upgrade path.
    }
}
