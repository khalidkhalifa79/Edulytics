using System.Security.Cryptography;
using System.Text;
using Edulytics.Core.Curriculum;

namespace Edulytics.Data.Seeding;

/// <summary>
/// Single authority for the effective learner-facing canonical lesson body.
/// Audits, persistence and Production parity checks must materialize content
/// through this class so reviewed corrections cannot exist only in memory in
/// one path while a different body is served to students.
/// </summary>
public static class CanonicalLessonContentMaterializer
{
    public static void Materialize(
        CanonicalLessonContentPackDocument document)
    {
        CambridgePrimaryStage6LessonContentCorrections
            .ApplyApprovedCorrections(document);
        CambridgeOfficialMappingContentCorrections
            .ApplyApprovedCorrections(document);
        SupportingLessonPracticeContentCorrections
            .ApplyApprovedCorrections(document);
        OfficialLessonPracticeContentCorrections
            .ApplyApprovedCorrections(document);
        PolishLessonPracticeContentCorrections
            .ApplyApprovedCorrections(document);

        ValidateUnicode(document);
        CanonicalLessonContentPackContract.Validate(document);
    }

    private static void ValidateUnicode(
        CanonicalLessonContentPackDocument document)
    {
        foreach (var lesson in document.Lessons)
        {
            foreach (var translation in lesson.Translations)
            {
                ValidateField(
                    lesson.LessonCode,
                    translation.CultureCode,
                    "Title",
                    translation.Title);
                ValidateField(
                    lesson.LessonCode,
                    translation.CultureCode,
                    "Explanation",
                    translation.Explanation);
                ValidateField(
                    lesson.LessonCode,
                    translation.CultureCode,
                    "KeyConceptsAndRules",
                    translation.KeyConceptsAndRules);
                ValidateField(
                    lesson.LessonCode,
                    translation.CultureCode,
                    "WorkedExamples",
                    translation.WorkedExamples);
                ValidateField(
                    lesson.LessonCode,
                    translation.CultureCode,
                    "StepByStepSolutions",
                    translation.StepByStepSolutions);
                ValidateField(
                    lesson.LessonCode,
                    translation.CultureCode,
                    "CommonMistakes",
                    translation.CommonMistakes);
                ValidateField(
                    lesson.LessonCode,
                    translation.CultureCode,
                    "QuickSummary",
                    translation.QuickSummary);
            }
        }
    }

    private static void ValidateField(
        string lessonCode,
        string cultureCode,
        string field,
        string value)
    {
        for (var index = 0; index < value.Length; index++)
        {
            var current = value[index];

            if (char.IsHighSurrogate(current))
            {
                if (index + 1 < value.Length &&
                    char.IsLowSurrogate(value[index + 1]))
                {
                    index++;
                    continue;
                }

                throw InvalidUnicode(
                    lessonCode,
                    cultureCode,
                    field,
                    index,
                    current);
            }

            if (char.IsLowSurrogate(current))
            {
                throw InvalidUnicode(
                    lessonCode,
                    cultureCode,
                    field,
                    index,
                    current);
            }
        }
    }

    private static InvalidOperationException InvalidUnicode(
        string lessonCode,
        string cultureCode,
        string field,
        int index,
        char value) =>
        new(
            $"Invalid UTF-16 learner content: {lessonCode}/{cultureCode}/" +
            $"{field}@{index}:U+{(int)value:X4}.");

    public static string GetEffectiveContentVersion(
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson lesson)
    {
        var stage6 =
            CambridgePrimaryStage6LessonContentCorrections
                .GetExpectedContentVersion(document, lesson);

        var officialMapping =
            CambridgeOfficialMappingContentCorrections
                .GetExpectedContentVersion(
                    document,
                    lesson,
                    stage6);

        var supporting =
            SupportingLessonPracticeContentCorrections
                .GetExpectedContentVersion(
                    document,
                    lesson,
                    CambridgeReviewedExampleContentCorrections
                        .GetExpectedContentVersion(
                            document,
                            lesson,
                            officialMapping));

        var official =
            OfficialLessonPracticeContentCorrections
                .GetExpectedContentVersion(
                    document,
                    lesson,
                    supporting);

        return PolishLessonPracticeContentCorrections
            .GetExpectedContentVersion(
                document,
                lesson,
                official);
    }

    public static bool IsReviewedCorrectionTarget(
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson lesson) =>
        CambridgePrimaryStage6LessonContentCorrections
            .IsTarget(document, lesson) ||
        CambridgeOfficialMappingContentCorrections
            .IsTarget(document, lesson) ||
        CambridgeReviewedExampleContentCorrections
            .IsTarget(document, lesson) ||
        SupportingLessonPracticeContentCorrections
            .IsTarget(document, lesson) ||
        OfficialLessonPracticeContentCorrections
            .IsTarget(document, lesson) ||
        PolishLessonPracticeContentCorrections
            .IsTarget(document, lesson);

    public static bool CanUpgradeExisting(
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson lesson,
        string existingContentVersion) =>
        CambridgePrimaryStage6LessonContentCorrections
            .CanUpgradeExisting(
                document,
                lesson,
                existingContentVersion) ||
        CambridgeOfficialMappingContentCorrections
            .CanUpgradeExisting(
                document,
                lesson,
                existingContentVersion) ||
        CambridgeReviewedExampleContentCorrections
            .CanUpgradeExisting(document, lesson, existingContentVersion) ||
        SupportingLessonPracticeContentCorrections
            .CanUpgradeExisting(
                document,
                lesson,
                existingContentVersion) ||
        OfficialLessonPracticeContentCorrections
            .CanUpgradeExisting(
                document,
                lesson,
                existingContentVersion) ||
        PolishLessonPracticeContentCorrections
            .CanUpgradeExisting(
                document,
                lesson,
                existingContentVersion);

    public static string ComputeLessonFingerprint(
        CanonicalLessonContentPackDocument document,
        CanonicalLessonContentPackLesson lesson)
    {
        var builder = new StringBuilder();
        builder.Append(document.PackCode).Append('\n');
        builder.Append(document.VersionCode).Append('\n');
        builder.Append(lesson.LessonCode).Append('\n');
        builder.Append(GetEffectiveContentVersion(document, lesson)).Append('\n');

        foreach (var translation in lesson.Translations
                     .OrderBy(x => x.CultureCode, StringComparer.Ordinal))
        {
            builder
                .Append(translation.CultureCode).Append('\n')
                .Append(translation.Title).Append('\n')
                .Append(translation.Explanation).Append('\n')
                .Append(translation.KeyConceptsAndRules).Append('\n')
                .Append(translation.WorkedExamples).Append('\n')
                .Append(translation.StepByStepSolutions).Append('\n')
                .Append(translation.CommonMistakes).Append('\n')
                .Append(translation.QuickSummary).Append('\n');
        }

        return Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(builder.ToString())))
            .ToLowerInvariant();
    }
}
