using Edulytics.Core.Enums;

namespace Edulytics.Core.Curriculum;

/// <summary>
/// Version-controlled Edulytics-authored/reviewed lesson content.
/// This is not an official curriculum pack and must never be represented as
/// official authority text.
/// </summary>
public sealed class CanonicalLessonContentPackDocument
{
    public string PackCode { get; set; } = string.Empty;
    public string VersionCode { get; set; } = string.Empty;
    public string ContentVersion { get; set; } = string.Empty;
    public CanonicalLessonContentStatus Status { get; set; } =
        CanonicalLessonContentStatus.Draft;
    public string ReviewedBy { get; set; } = string.Empty;
    public string ReviewEvidence { get; set; } = string.Empty;
    public List<CanonicalLessonContentPackLesson> Lessons { get; set; } = [];
}

public sealed class CanonicalLessonContentPackLesson
{
    public string LessonCode { get; set; } = string.Empty;
    public List<string> OutcomeCodes { get; set; } = [];
    public List<CanonicalLessonContentPackTranslation> Translations { get; set; } = [];
}

public sealed class CanonicalLessonContentPackTranslation
{
    public string CultureCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string KeyConceptsAndRules { get; set; } = string.Empty;
    public string WorkedExamples { get; set; } = string.Empty;
    public string StepByStepSolutions { get; set; } = string.Empty;
    public string CommonMistakes { get; set; } = string.Empty;
    public string QuickSummary { get; set; } = string.Empty;
}

public static class CanonicalLessonContentPackContract
{
    private static readonly HashSet<string> SupportedCultures =
        new(StringComparer.Ordinal)
        {
            "en",
            "pl"
        };

    public static void Validate(CanonicalLessonContentPackDocument document)
    {
        MathematicsCurriculumPackRegistry.Validate();

        if (!MathematicsCurriculumPackRegistry.All.Any(
                x => string.Equals(
                    x.Code,
                    document.PackCode,
                    StringComparison.Ordinal)))
        {
            throw new InvalidOperationException(
                $"Unsupported Mathematics pack: {document.PackCode}.");
        }

        Require(document.VersionCode, "VersionCode");
        Require(document.ContentVersion, "ContentVersion");

        if (document.ContentVersion.Length > 80)
            throw new InvalidOperationException("ContentVersion exceeds 80 characters.");

        if (!Enum.IsDefined(
                typeof(CanonicalLessonContentStatus),
                document.Status))
        {
            throw new InvalidOperationException(
                $"Unsupported canonical content status: {document.Status}.");
        }

        if (document.Lessons.Count == 0)
            throw new InvalidOperationException(
                $"Canonical content pack {document.PackCode} contains no lessons.");

        if (document.Status is
            CanonicalLessonContentStatus.Verified or
            CanonicalLessonContentStatus.Published)
        {
            Require(document.ReviewedBy, "ReviewedBy");
            Require(document.ReviewEvidence, "ReviewEvidence");
        }

        var lessonCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var lesson in document.Lessons)
        {
            Require(lesson.LessonCode, "LessonCode");

            if (lesson.LessonCode.Length > 600)
                throw new InvalidOperationException(
                    $"LessonCode exceeds 600 characters: {lesson.LessonCode}.");

            if (!lessonCodes.Add(lesson.LessonCode))
                throw new InvalidOperationException(
                    $"Duplicate LessonCode: {lesson.LessonCode}.");

            if (lesson.OutcomeCodes.Count == 0 ||
                lesson.OutcomeCodes.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException(
                    $"Lesson {lesson.LessonCode} requires at least one exact OutcomeCode.");
            }

            if (lesson.OutcomeCodes.Count !=
                lesson.OutcomeCodes
                    .Distinct(StringComparer.Ordinal)
                    .Count())
            {
                throw new InvalidOperationException(
                    $"Lesson {lesson.LessonCode} contains duplicate OutcomeCodes.");
            }

            if (lesson.Translations.Count == 0)
                throw new InvalidOperationException(
                    $"Lesson {lesson.LessonCode} contains no translations.");

            var cultures = new HashSet<string>(StringComparer.Ordinal);

            foreach (var translation in lesson.Translations)
            {
                Require(translation.CultureCode, "CultureCode");

                if (!SupportedCultures.Contains(translation.CultureCode))
                {
                    throw new InvalidOperationException(
                        $"Unsupported culture {translation.CultureCode} in {lesson.LessonCode}.");
                }

                if (!cultures.Add(translation.CultureCode))
                {
                    throw new InvalidOperationException(
                        $"Duplicate culture {translation.CultureCode} in {lesson.LessonCode}.");
                }

                RequireBody(lesson.LessonCode, translation);
            }

            if (document.Status == CanonicalLessonContentStatus.Published &&
                !cultures.SetEquals(SupportedCultures))
            {
                throw new InvalidOperationException(
                    $"Published lesson {lesson.LessonCode} requires complete English and Polish content.");
            }
        }
    }

    private static void RequireBody(
        string lessonCode,
        CanonicalLessonContentPackTranslation translation)
    {
        Require(translation.Title, $"{lessonCode}:{translation.CultureCode}:Title");

        if (translation.Title.Length > 600)
            throw new InvalidOperationException(
                $"Title exceeds 600 characters in {lessonCode}:{translation.CultureCode}.");

        Require(
            translation.Explanation,
            $"{lessonCode}:{translation.CultureCode}:Explanation");

        Require(
            translation.KeyConceptsAndRules,
            $"{lessonCode}:{translation.CultureCode}:KeyConceptsAndRules");

        Require(
            translation.WorkedExamples,
            $"{lessonCode}:{translation.CultureCode}:WorkedExamples");

        Require(
            translation.StepByStepSolutions,
            $"{lessonCode}:{translation.CultureCode}:StepByStepSolutions");

        Require(
            translation.CommonMistakes,
            $"{lessonCode}:{translation.CultureCode}:CommonMistakes");

        Require(
            translation.QuickSummary,
            $"{lessonCode}:{translation.CultureCode}:QuickSummary");
    }

    private static void Require(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"Canonical lesson content field is required: {field}.");
    }
}
