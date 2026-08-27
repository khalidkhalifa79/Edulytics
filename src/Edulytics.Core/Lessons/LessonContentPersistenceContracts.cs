using Edulytics.Core.Entities;
using Edulytics.Core.Enums;

namespace Edulytics.Core.Lessons;

public sealed record LessonOutcomeRecord(
    Guid Id,
    string Code,
    string Description,
    int Order);

public sealed record LessonTopicRecord(
    Guid TopicId,
    Guid FrameworkVersionId,
    string FrameworkName,
    string FrameworkVersionName,
    Guid SubjectId,
    string SubjectName,
    string SubjectCode,
    Guid GradeLevelId,
    string GradeName,
    string TopicName,
    int TopicOrder,
    IReadOnlyList<LessonOutcomeRecord> Outcomes);

public sealed record LessonTranslationRecord(
    string CultureCode,
    string Title,
    string Explanation,
    string KeyConceptsAndRules,
    string WorkedExamples,
    string StepByStepSolutions,
    string CommonMistakes,
    string QuickSummary);

public sealed record LessonAggregateRecord(
    Guid Id,
    Guid TopicId,
    int Order,
    LearningLessonStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? SubmittedAtUtc,
    DateTime? PublishedAtUtc,
    IReadOnlyList<Guid> OutcomeIds,
    IReadOnlyList<LessonTranslationRecord> Translations);

public sealed record StudentPublishedLessonRecord(
    Guid Id,
    Guid TopicId,
    string TopicName,
    string SubjectName,
    string SubjectCode,
    string GradeName,
    string FrameworkName,
    int Order,
    DateTime PublishedAtUtc,
    IReadOnlyList<LessonOutcomeRecord> Outcomes,
    IReadOnlyList<LessonTranslationRecord> Translations);

public enum LessonContentWriteResult
{
    Success = 1,
    ConcurrencyConflict = 2,
    ConstraintViolation = 3
}
