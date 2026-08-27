using Edulytics.Core.Enums;
namespace Edulytics.Core.Lessons;

public sealed record CanonicalCurriculumContextRecord(
    Guid FrameworkVersionId,string FrameworkName,string FrameworkVersionName,
    Guid SubjectId,string SubjectName,string SubjectCode,
    Guid GradeLevelId,string GradeName,int GradeOrder);

public sealed record CanonicalCurriculumNodeRecord(
    Guid Id,Guid FrameworkVersionId,Guid? ParentId,string NodeKind,string Code,string Title,
    string? OfficialText,string? Pathway,int LogicalLevelFrom,int LogicalLevelTo,int SortOrder);

public sealed record LessonOutcomeRecord(Guid Id,string Code,string Description,int Order);

public sealed record CanonicalLessonTranslationRecord(
    string CultureCode,string Title,string Explanation,string KeyConceptsAndRules,
    string WorkedExamples,string StepByStepSolutions,string CommonMistakes,string QuickSummary);

public sealed record CanonicalLessonContentRecord(
    Guid Id,Guid FrameworkVersionId,Guid LessonNodeId,CanonicalLessonContentStatus Status,
    string ContentVersion,DateTime? VerifiedAtUtc,DateTime? PublishedAtUtc,DateTime UpdatedAtUtc,
    IReadOnlyList<CanonicalLessonTranslationRecord> Translations);
