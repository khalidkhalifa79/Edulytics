namespace Edulytics.Services.Curriculum;

public enum CurriculumErrorCode
{
    AccessDenied = 1,
    SchoolNotActive = 2,
    Required = 3,
    InvalidName = 4,
    InvalidOrder = 5,
    InvalidCode = 6,
    InvalidWeight = 7,
    SubjectNotFound = 8,
    GradeLevelNotFound = 9,
    TopicNotFound = 10,
    OutcomeNotFound = 11,
    DuplicateTopicName = 12,
    DuplicateTopicOrder = 13,
    DuplicateOutcomeCode = 14,
    DuplicateOutcomeOrder = 15,
    PersistenceError = 16
}

public sealed record CurriculumCommandResult(
    bool Succeeded,
    string Field,
    CurriculumErrorCode? Error)
{
    public static CurriculumCommandResult Success() =>
        new(true, string.Empty, null);

    public static CurriculumCommandResult Failure(
        string field,
        CurriculumErrorCode error) =>
        new(false, field, error);
}

public sealed record CurriculumQueryResult<T>(
    T? Value,
    CurriculumErrorCode? Error)
{
    public static CurriculumQueryResult<T> Success(T value) =>
        new(value, null);

    public static CurriculumQueryResult<T> Failure(
        CurriculumErrorCode error) =>
        new(default, error);
}

public sealed record CurriculumGradeItem(
    Guid Id,
    string Name,
    int Order);

public sealed record CurriculumSubjectItem(
    Guid Id,
    string Name,
    string Code);

public sealed record LearningOutcomeItem(
    Guid Id,
    Guid TopicId,
    string Code,
    string Description,
    decimal Weight,
    int Order);

public sealed record CurriculumTopicItem(
    Guid Id,
    Guid SubjectId,
    Guid GradeLevelId,
    string Name,
    int Order,
    IReadOnlyList<LearningOutcomeItem> Outcomes);

public sealed record CurriculumDashboard(
    Guid SchoolId,
    IReadOnlyList<CurriculumGradeItem> GradeLevels,
    IReadOnlyList<CurriculumSubjectItem> Subjects,
    IReadOnlyList<CurriculumTopicItem> Topics);

public sealed record CreateCurriculumTopicRequest(
    Guid SubjectId,
    Guid GradeLevelId,
    string Name,
    int Order);

public sealed record UpdateCurriculumTopicRequest(
    Guid Id,
    string Name,
    int Order);

public sealed record CreateLearningOutcomeRequest(
    Guid TopicId,
    string Code,
    string Description,
    decimal Weight,
    int Order);

public sealed record UpdateLearningOutcomeRequest(
    Guid Id,
    string Code,
    string Description,
    decimal Weight,
    int Order);
