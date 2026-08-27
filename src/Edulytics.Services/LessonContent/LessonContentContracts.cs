using Edulytics.Core.Enums;
using Edulytics.Core.Lessons;

namespace Edulytics.Services.LessonContent;

public enum LessonContentErrorCode
{
    AccessDenied = 1,
    SchoolNotActive = 2,
    TopicNotFound = 3,
    LessonNotFound = 4,
    InvalidOrder = 5,
    DuplicateOrder = 6,
    OutcomeRequired = 7,
    OutcomeNotInTopic = 8,
    EnglishTitleRequired = 9,
    EnglishContentIncomplete = 10,
    InvalidState = 11,
    PublishedImmutable = 12,
    ConcurrencyConflict = 13,
    ConstraintViolation = 14
}

public sealed record LessonContentQueryResult<T>(
    T? Value,
    LessonContentErrorCode? Error)
    where T : class
{
    public static LessonContentQueryResult<T> Success(T value) => new(value, null);
    public static LessonContentQueryResult<T> Failure(LessonContentErrorCode error) => new(null, error);
}

public sealed record LessonContentCommandResult(
    bool Succeeded,
    LessonContentErrorCode? Error = null,
    string? Field = null,
    Guid? LessonId = null)
{
    public static LessonContentCommandResult Success(Guid? lessonId = null) =>
        new(true, null, null, lessonId);

    public static LessonContentCommandResult Failure(
        LessonContentErrorCode error,
        string? field = null) =>
        new(false, error, field, null);
}

public sealed record LessonTranslationInput(
    string Title,
    string Explanation,
    string KeyConceptsAndRules,
    string WorkedExamples,
    string StepByStepSolutions,
    string CommonMistakes,
    string QuickSummary);

public sealed record CreateLessonContentRequest(
    Guid TopicId,
    int Order,
    IReadOnlyList<Guid> OutcomeIds,
    LessonTranslationInput English,
    LessonTranslationInput? Polish);

public sealed record UpdateLessonContentRequest(
    Guid LessonId,
    int Order,
    IReadOnlyList<Guid> OutcomeIds,
    LessonTranslationInput English,
    LessonTranslationInput? Polish);

public sealed record LessonContentSummary(
    Guid Id,
    int Order,
    LearningLessonStatus Status,
    string Title,
    DateTime? PublishedAtUtc,
    IReadOnlyList<Guid> OutcomeIds);

public sealed record LessonContentTopicGroup(
    LessonTopicRecord Topic,
    IReadOnlyList<LessonContentSummary> Lessons);

public sealed record LessonContentDashboard(
    Guid SchoolId,
    IReadOnlyList<LessonContentTopicGroup> Topics);

public sealed record LessonContentEditor(
    LessonTopicRecord Topic,
    Guid? LessonId,
    int Order,
    LearningLessonStatus Status,
    IReadOnlyList<Guid> SelectedOutcomeIds,
    LessonTranslationInput English,
    LessonTranslationInput? Polish,
    bool IsNew);

public sealed record StudentLessonSummary(
    Guid Id,
    string Title,
    string TopicName,
    string SubjectName,
    string SubjectCode,
    string GradeName,
    string FrameworkName,
    int Order);

public sealed record StudentLessonDetail(
    Guid Id,
    string Title,
    string TopicName,
    string SubjectName,
    string SubjectCode,
    string GradeName,
    string FrameworkName,
    string Explanation,
    string KeyConceptsAndRules,
    string WorkedExamples,
    string StepByStepSolutions,
    string CommonMistakes,
    string QuickSummary,
    IReadOnlyList<LessonOutcomeRecord> Outcomes,
    DateTime PublishedAtUtc);
