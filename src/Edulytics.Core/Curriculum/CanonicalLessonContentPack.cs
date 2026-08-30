using System.Text.RegularExpressions;
using Edulytics.Core.Enums;

namespace Edulytics.Core.Curriculum;

public enum CurriculumSourceResolutionStatus
{
    CurrentOfficial = 1,
    PreviousOfficialFallback = 2
}

public enum PedagogicalSourceType
{
    LegacyUnspecified = 0,
    SchoolAdoptedTextbook = 1,
    CurrentOfficialTextbook = 2,
    WidelyUsedPublisherTextbook = 3,
    OfficialFrameworkOnly = 4,
    OpenEducationalResource = 5
}

public enum LessonTitleProvenance
{
    LegacyUnspecified = 0,
    PedagogicalSource = 1,
    EdulyticsDerivedFromOfficialOutcome = 2
}

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
    /// <summary>
    /// BCP-47 language of the curriculum's canonical academic content. This
    /// is deliberately independent from the application's UI culture.
    /// </summary>
    public string AcademicLanguage { get; set; } = string.Empty;
    public bool CurriculumTranslationRequired { get; set; }

    // Curriculum authority/provenance.
    // TargetCurriculumPeriod is what Edulytics is serving.
    // SourceCurriculumPeriod is the official source actually reviewed.
    public string TargetCurriculumPeriod { get; set; } = string.Empty;
    public string SourceCurriculumPeriod { get; set; } = string.Empty;
    public string SourceVersionLabel { get; set; } = string.Empty;
    public string SourceAuthority { get; set; } = string.Empty;
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceCheckedAtUtc { get; set; } = string.Empty;
    public CurriculumSourceResolutionStatus SourceResolution { get; set; } =
        CurriculumSourceResolutionStatus.CurrentOfficial;
    public string FallbackReason { get; set; } = string.Empty;
    public string ReviewMethod { get; set; } = string.Empty;

    // Source Policy v2 separates official academic authority from the
    // pedagogical textbook/material used to structure and explain lessons.
    public int SourcePolicyVersion { get; set; } = 1;
    public PedagogicalSourceType PedagogicalSourceType { get; set; } =
        PedagogicalSourceType.LegacyUnspecified;
    public string PedagogicalSourceTitle { get; set; } = string.Empty;
    public string PedagogicalSourcePublisher { get; set; } = string.Empty;
    public string PedagogicalSourceEdition { get; set; } = string.Empty;
    public string PedagogicalSourceUrl { get; set; } = string.Empty;
    public string PedagogicalSourceCheckedAtUtc { get; set; } = string.Empty;
    public string PedagogicalSourceSelectionReason { get; set; } = string.Empty;
    public string PedagogicalSourceSelectionEvidence { get; set; } = string.Empty;
    public string PedagogicalSourceRightsNote { get; set; } = string.Empty;

    public CanonicalLessonContentStatus Status { get; set; } =
        CanonicalLessonContentStatus.Draft;
    public string ReviewedBy { get; set; } = string.Empty;
    public string ReviewEvidence { get; set; } = string.Empty;
    public List<CanonicalLessonContentPackLesson> Lessons { get; set; } = [];
}

public sealed class CanonicalLessonContentPackLesson
{
    public string LessonCode { get; set; } = string.Empty;
    public LessonTitleProvenance TitleProvenance { get; set; } =
        LessonTitleProvenance.LegacyUnspecified;
    public string TitleSourceReference { get; set; } = string.Empty;
    public List<string> OutcomeCodes { get; set; } = [];
    public bool IsSupporting { get; set; }
    public string SourceUrl { get; set; } = string.Empty;
    public string SourceLocator { get; set; } = string.Empty;
    public string SourceTitle { get; set; } = string.Empty;
    public string SourcePublisher { get; set; } = string.Empty;
    public string SourceEdition { get; set; } = string.Empty;
    public string SourceRights { get; set; } = string.Empty;
    public string SourceSha256 { get; set; } = string.Empty;
    public string CanonicalBodySha256 { get; set; } = string.Empty;
    public string SourceVerifiedAtUtc { get; set; } = string.Empty;
    public string RetrievalUrl { get; set; } = string.Empty;
    public string RetrievalChannel { get; set; } = string.Empty;
    public string RetrievalTimestamp { get; set; } = string.Empty;
    public string AdaptationStatus { get; set; } = string.Empty;
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
    private static readonly Regex GenericLessonTitlePattern =
        new(
            @"(?:^|\s[—-]\s)(?:Lesson|Lekcja)\s+\d+\s*$",
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant |
            RegexOptions.Compiled);

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
        Require(document.AcademicLanguage, "AcademicLanguage");

        if (!Regex.IsMatch(document.AcademicLanguage, @"^[a-z]{2,3}(?:-[A-Z]{2})?$"))
            throw new InvalidOperationException("AcademicLanguage must be a normalized BCP-47 language tag.");

        Require(
            document.TargetCurriculumPeriod,
            "TargetCurriculumPeriod");

        Require(
            document.SourceCurriculumPeriod,
            "SourceCurriculumPeriod");

        Require(
            document.SourceVersionLabel,
            "SourceVersionLabel");

        Require(
            document.SourceAuthority,
            "SourceAuthority");

        Require(
            document.SourceUrl,
            "SourceUrl");

        Require(
            document.SourceCheckedAtUtc,
            "SourceCheckedAtUtc");

        if (!Uri.TryCreate(
                document.SourceUrl,
                UriKind.Absolute,
                out var sourceUri) ||
            (sourceUri.Scheme != Uri.UriSchemeHttps &&
             sourceUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                $"Canonical lesson content SourceUrl must be an absolute HTTP/HTTPS URL: {document.SourceUrl}.");
        }

        if (!DateTimeOffset.TryParse(
                document.SourceCheckedAtUtc,
                out var sourceChecked))
        {
            throw new InvalidOperationException(
                $"Canonical lesson content SourceCheckedAtUtc is invalid: {document.SourceCheckedAtUtc}.");
        }

        if (sourceChecked.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "Canonical lesson content SourceCheckedAtUtc must be UTC.");
        }

        if (!Enum.IsDefined(
                typeof(CurriculumSourceResolutionStatus),
                document.SourceResolution))
        {
            throw new InvalidOperationException(
                $"Unsupported curriculum source resolution: {document.SourceResolution}.");
        }

        if (document.SourceResolution ==
            CurriculumSourceResolutionStatus.PreviousOfficialFallback)
        {
            Require(
                document.FallbackReason,
                "FallbackReason");
        }
        else if (!string.IsNullOrWhiteSpace(document.FallbackReason))
        {
            throw new InvalidOperationException(
                "FallbackReason is only valid for PreviousOfficialFallback.");
        }

        if (document.SourcePolicyVersion is < 1 or > 2)
        {
            throw new InvalidOperationException(
                $"Unsupported SourcePolicyVersion: {document.SourcePolicyVersion}.");
        }

        if (document.SourcePolicyVersion == 2)
        {
            ValidatePedagogicalSource(document);
        }

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
            Require(document.ReviewMethod, "ReviewMethod");
        }

        var lessonCodes = new HashSet<string>(StringComparer.Ordinal);

        foreach (var lesson in document.Lessons)
        {
            Require(lesson.LessonCode, "LessonCode");

            if (document.SourcePolicyVersion == 2)
            {
                if (!Enum.IsDefined(
                        typeof(LessonTitleProvenance),
                        lesson.TitleProvenance) ||
                    lesson.TitleProvenance ==
                        LessonTitleProvenance.LegacyUnspecified)
                {
                    throw new InvalidOperationException(
                        $"Lesson {lesson.LessonCode} requires explicit TitleProvenance under Source Policy v2.");
                }

                Require(
                    lesson.TitleSourceReference,
                    $"{lesson.LessonCode}:TitleSourceReference");
            }

            if (lesson.LessonCode.Length > 600)
                throw new InvalidOperationException(
                    $"LessonCode exceeds 600 characters: {lesson.LessonCode}.");

            if (!lessonCodes.Add(lesson.LessonCode))
                throw new InvalidOperationException(
                    $"Duplicate LessonCode: {lesson.LessonCode}.");

            if ((!lesson.IsSupporting && lesson.OutcomeCodes.Count == 0) ||
                (lesson.IsSupporting && lesson.OutcomeCodes.Count != 0) ||
                lesson.OutcomeCodes.Any(string.IsNullOrWhiteSpace))
            {
                throw new InvalidOperationException(
                    $"Lesson {lesson.LessonCode} must have exact OutcomeCodes when aligned and zero OutcomeCodes when Supporting.");
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

                if (!cultures.Add(translation.CultureCode))
                {
                    throw new InvalidOperationException(
                        $"Duplicate culture {translation.CultureCode} in {lesson.LessonCode}.");
                }

                RequireBody(lesson.LessonCode, translation);

                if (document.SourcePolicyVersion == 2 &&
                    document.Status ==
                        CanonicalLessonContentStatus.Published &&
                    GenericLessonTitlePattern.IsMatch(
                        translation.Title.Trim()))
                {
                    throw new InvalidOperationException(
                        $"Published Source Policy v2 lesson {lesson.LessonCode}:{translation.CultureCode} " +
                        $"cannot use a generic synthetic title: {translation.Title}.");
                }
            }

            if (document.Status == CanonicalLessonContentStatus.Published &&
                !cultures.Contains(document.AcademicLanguage))
            {
                throw new InvalidOperationException(
                    $"Published lesson {lesson.LessonCode} requires canonical academic content in {document.AcademicLanguage}.");
            }
        }
    }

    private static void ValidatePedagogicalSource(
        CanonicalLessonContentPackDocument document)
    {
        if (!Enum.IsDefined(
                typeof(PedagogicalSourceType),
                document.PedagogicalSourceType) ||
            document.PedagogicalSourceType ==
                PedagogicalSourceType.LegacyUnspecified)
        {
            throw new InvalidOperationException(
                "Source Policy v2 requires explicit PedagogicalSourceType.");
        }

        Require(
            document.PedagogicalSourceSelectionReason,
            "PedagogicalSourceSelectionReason");

        Require(
            document.PedagogicalSourceRightsNote,
            "PedagogicalSourceRightsNote");

        if (document.PedagogicalSourceType ==
            PedagogicalSourceType.OfficialFrameworkOnly)
        {
            return;
        }

        Require(
            document.PedagogicalSourceTitle,
            "PedagogicalSourceTitle");

        Require(
            document.PedagogicalSourcePublisher,
            "PedagogicalSourcePublisher");

        Require(
            document.PedagogicalSourceEdition,
            "PedagogicalSourceEdition");

        Require(
            document.PedagogicalSourceUrl,
            "PedagogicalSourceUrl");

        Require(
            document.PedagogicalSourceCheckedAtUtc,
            "PedagogicalSourceCheckedAtUtc");

        if (!Uri.TryCreate(
                document.PedagogicalSourceUrl,
                UriKind.Absolute,
                out var pedagogicalUri) ||
            (pedagogicalUri.Scheme != Uri.UriSchemeHttps &&
             pedagogicalUri.Scheme != Uri.UriSchemeHttp))
        {
            throw new InvalidOperationException(
                $"PedagogicalSourceUrl must be an absolute HTTP/HTTPS URL: " +
                $"{document.PedagogicalSourceUrl}.");
        }

        if (!DateTimeOffset.TryParse(
                document.PedagogicalSourceCheckedAtUtc,
                out var pedagogicalChecked) ||
            pedagogicalChecked.Offset != TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                "PedagogicalSourceCheckedAtUtc must be a valid UTC timestamp.");
        }

        if (document.PedagogicalSourceType is
            PedagogicalSourceType.SchoolAdoptedTextbook or
            PedagogicalSourceType.WidelyUsedPublisherTextbook or
            PedagogicalSourceType.OpenEducationalResource)
        {
            Require(
                document.PedagogicalSourceSelectionEvidence,
                "PedagogicalSourceSelectionEvidence");
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
