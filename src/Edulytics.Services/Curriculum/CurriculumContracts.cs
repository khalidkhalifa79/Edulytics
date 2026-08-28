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
    PersistenceError = 16,
    FrameworkNotFound = 17,
    CurriculumNotSelected = 18,
    CurriculumFrameworkInUse = 19,
    OfficialOutcomeNotFound = 20,
    OfficialOutcomeReadOnly = 21,
    AcademicProgramNotFound = 22
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

public sealed record CurriculumProgramItem(Guid Id, string Name, string Code);

public sealed record CurriculumSubjectItem(
    Guid Id,
    string Name,
    string Code);

public sealed record CurriculumFrameworkItem(
    string Code,
    string DisplayName);

public sealed record LearningOutcomeItem(
    Guid Id,
    Guid TopicId,
    string Code,
    string Description,
    decimal Weight,
    int Order)
{
    public bool IsOfficial { get; init; }
}

public sealed record OfficialCurriculumOutcomeOption(
    Guid ContentNodeId,
    Guid? LessonNodeId,
    string Code,
    string Description,
    string SelectionLabel,
    string? GroupLabel,
    int SortOrder);

public sealed record CurriculumAdoptionItem(
    Guid GradeLevelId,
    Guid SubjectId,
    string FrameworkCode,
    string FrameworkDisplayName)
{
    public Guid AcademicProgramId { get; init; }
    public string AcademicProgramName { get; init; } = string.Empty;
    public string AcademicProgramCode { get; init; } = string.Empty;
}

public sealed record CurriculumTopicItem(
    Guid Id,
    Guid SubjectId,
    Guid GradeLevelId,
    string Name,
    int Order,
    IReadOnlyList<LearningOutcomeItem> Outcomes)
{
    public Guid AcademicProgramId { get; init; }
    public string AcademicProgramName { get; init; } = string.Empty;
    public string FrameworkCode { get; init; } = string.Empty;
    public string FrameworkDisplayName { get; init; } = string.Empty;
    public IReadOnlyList<OfficialCurriculumOutcomeOption> OfficialOutcomes
    {
        get;
        init;
    } = [];
}

public sealed record CurriculumDashboard(
    Guid SchoolId,
    IReadOnlyList<CurriculumGradeItem> GradeLevels,
    IReadOnlyList<CurriculumSubjectItem> Subjects,
    IReadOnlyList<CurriculumTopicItem> Topics)
{
    public IReadOnlyList<CurriculumProgramItem> AcademicPrograms { get; init; } = [];

    public IReadOnlyList<CurriculumFrameworkItem> Frameworks
    {
        get;
        init;
    } = [];

    public IReadOnlyList<CurriculumAdoptionItem> Adoptions
    {
        get;
        init;
    } = [];
}

public sealed record SelectCurriculumFrameworkRequest(
    Guid SubjectId,
    Guid GradeLevelId,
    string FrameworkCode,
    Guid AcademicProgramId = default);

public sealed record CreateCurriculumTopicRequest(
    Guid SubjectId,
    Guid GradeLevelId,
    string Name,
    int Order,
    Guid AcademicProgramId = default);

public sealed record UpdateCurriculumTopicRequest(
    Guid Id,
    string Name,
    int Order);


public sealed record CreateOfficialLearningOutcomeRequest(
    Guid TopicId,
    Guid ContentNodeId,
    Guid? LessonNodeId,
    int Order
);

public sealed record UpdateLearningOutcomeRequest(
    Guid Id,
    string Code,
    string Description,
    int Order);
