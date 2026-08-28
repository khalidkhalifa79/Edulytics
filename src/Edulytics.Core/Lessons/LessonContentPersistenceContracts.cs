using Edulytics.Core.Enums;

namespace Edulytics.Core.Lessons;

public sealed record CanonicalCurriculumContextRecord(
    Guid FrameworkVersionId,
    string FrameworkCode,
    string FrameworkName,
    string FrameworkVersionName,
    Guid SubjectId,
    string SubjectName,
    string SubjectCode,
    Guid GradeLevelId,
    string GradeName,
    int GradeOrder)
{
    public Guid AcademicProgramId { get; init; }
    public string AcademicProgramName { get; init; } = string.Empty;
    public string AcademicProgramCode { get; init; } = string.Empty;
}

public sealed record PedagogicalLessonRecord(
    Guid Id,
    Guid FrameworkVersionId,
    Guid? OfficialLessonNodeId,
    string Code,
    string UnitKey,
    string UnitTitle,
    string Title,
    string? Pathway,
    int LogicalLevelFrom,
    int LogicalLevelTo,
    int SortOrder,
    int OfficialOutcomeCount);

public sealed record LessonOutcomeRecord(
    Guid Id,
    string Code,
    string Description,
    int Order);

public sealed record CanonicalLessonTranslationRecord(
    string CultureCode,
    string Title,
    string Explanation,
    string KeyConceptsAndRules,
    string WorkedExamples,
    string StepByStepSolutions,
    string CommonMistakes,
    string QuickSummary);

public sealed record CanonicalLessonContentRecord(
    Guid Id,
    Guid FrameworkVersionId,
    Guid PedagogicalLessonId,
    CanonicalLessonContentStatus Status,
    string ContentVersion,
    DateTime? VerifiedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime UpdatedAtUtc,
    IReadOnlyList<CanonicalLessonTranslationRecord> Translations);
