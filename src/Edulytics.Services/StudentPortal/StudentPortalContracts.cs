namespace Edulytics.Services.StudentPortal;

public enum StudentPortalErrorCode
{
    AccessDenied = 1,
    SchoolNotActive = 2,
    ProfileNotLinked = 3
}

public sealed record StudentPortalQueryResult<T>(
    T? Value,
    StudentPortalErrorCode? Error)
    where T : class
{
    public static StudentPortalQueryResult<T> Success(T value) =>
        new(value, null);

    public static StudentPortalQueryResult<T> Failure(
        StudentPortalErrorCode error) =>
        new(null, error);
}

public sealed record StudentEnrollmentItem(
    Guid ClassGroupId,
    Guid AcademicYearId,
    Guid GradeLevelId,
    string ClassName,
    string ClassCode,
    string AcademicYearName,
    string GradeName);

public sealed record StudentLearningNodeItem(
    Guid Id,
    Guid? ParentId,
    string Kind,
    string Code,
    string Title,
    string? Pathway,
    string? OfficialText,
    int SortOrder);

public sealed record StudentLearningSubjectItem(
    Guid SubjectId,
    string SubjectName,
    string SubjectCode,
    Guid FrameworkVersionId,
    string FrameworkName,
    string FrameworkVersionName,
    string AcademicYearName,
    string GradeName,
    IReadOnlyList<StudentLearningNodeItem> Nodes);

public sealed record StudentAssessmentItem(
    Guid AssessmentId,
    string Title,
    string SubjectName,
    string ClassName,
    DateOnly AssessmentDate,
    decimal MaxScore);

public sealed record StudentResultItem(
    Guid AssessmentId,
    string AssessmentTitle,
    string SubjectName,
    DateOnly AssessmentDate,
    decimal Score,
    decimal MaxScore,
    decimal Percentage);

public sealed record StudentPortalWorkspace(
    Guid SchoolId,
    string SchoolName,
    Guid StudentProfileId,
    string StudentNumber,
    string DisplayName,
    IReadOnlyList<StudentEnrollmentItem> Enrollments,
    IReadOnlyList<StudentLearningSubjectItem> Learning,
    IReadOnlyList<StudentAssessmentItem> Assessments,
    IReadOnlyList<StudentResultItem> Results);
