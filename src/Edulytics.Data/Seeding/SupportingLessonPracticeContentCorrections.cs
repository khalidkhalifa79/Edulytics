using Edulytics.Core.Curriculum;
using Edulytics.Core.Mathematics.Practice;

namespace Edulytics.Data.Seeding;

/// <summary>
/// Versioned learner-facing remediation for Supporting lessons whose reviewed
/// target rule supplies stronger target-specific English pedagogy. Official
/// curriculum identities and OutcomeCodes are never changed.
/// </summary>
public static class SupportingLessonPracticeContentCorrections
{
    public const string CorrectionContentVersion =
        "supporting-practice-remediation-v2";

    private const string PriorCorrectionContentVersion =
        "supporting-practice-remediation-v1";

    public static bool IsTarget(
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson lesson)
    {
        if ((lesson.OutcomeCodes.Count != 0 && !PreservesReviewedRehearsalBody(document, lesson)) ||
            CambridgePrimaryStage6LessonContentCorrections.IsTarget(document, lesson))
        {
            return false;
        }

        var english = lesson.Translations.FirstOrDefault(x =>
            x.CultureCode.StartsWith("en", StringComparison.OrdinalIgnoreCase));
        if (english is null)
            return false;

        return SupportingPracticeTargetRuleRegistry.TryResolve(
            lesson.LessonCode,
            english.Title,
            out _);
    }

    // These OGL lessons already had reviewed, materialized bodies before official
    // mapping. Adding an OutcomeCode must not restore their old generic raw prose
    // or silently replace their persisted correction version.
    private static bool PreservesReviewedRehearsalBody(
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson lesson) =>
        document.PackCode == MathematicsCurriculumPackRegistry.CambridgeCode ||
        (document.PackCode == MathematicsCurriculumPackRegistry.UaeCode &&
         new[] { "L3:COMMON:", "L4:COMMON:", "L7:ADVANCED:", "L8:ADVANCED:", "L11:ADVANCED:", "L12:ADVANCED:" }
             .Any(scope => lesson.LessonCode.StartsWith("PED:UAE-MOE-MATH:" + scope, StringComparison.Ordinal)));

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
        (string.Equals(
             existingContentVersion,
             document.ContentVersion,
             StringComparison.Ordinal) ||
         string.Equals(
             existingContentVersion,
             PriorCorrectionContentVersion,
             StringComparison.Ordinal) ||
         string.Equals(
             existingContentVersion,
             CorrectionContentVersion,
             StringComparison.Ordinal));

    public static void ApplyApprovedCorrections(
        CanonicalLessonContentPackDocument document)
    {
        foreach (var lesson in document.Lessons)
        {
            if (!IsTarget(document, lesson))
                continue;

            var english = lesson.Translations.Single(x =>
                x.CultureCode.StartsWith("en", StringComparison.OrdinalIgnoreCase));

            if (!SupportingPracticeTargetRuleRegistry.TryResolve(
                    lesson.LessonCode,
                    english.Title,
                    out var rule) ||
                rule is null)
            {
                continue;
            }

            english.Explanation =
                rule.Content.Concept + " " +
                (PreservesReviewedRehearsalBody(document, lesson)
                    ? "This reviewed learner lesson uses an exact Practice recipe without creating or altering curriculum mapping metadata."
                    : lesson.OutcomeCodes.Count == 0
                        ? "This Supporting lesson remains pedagogical content and does not create or imply an official curriculum OutcomeCode."
                        : "This learner lesson has a reviewed official curriculum mapping; this Practice recipe does not create or alter that mapping.");
            english.KeyConceptsAndRules =
                rule.Content.Concept;
            english.WorkedExamples =
                "Worked example: " + rule.Content.WorkedExample;
            english.StepByStepSolutions =
                "Solution method: " + rule.Content.Solution;
            english.CommonMistakes =
                rule.Content.CommonMistake;
            english.QuickSummary =
                rule.Content.Summary;
        }
    }
}
