using Edulytics.Core.Entities;
using Edulytics.Core.Enums;

namespace Edulytics.Core.Imports;

public sealed class ImportDataSnapshot
{
    public IReadOnlyList<AcademicYear> AcademicYears { get; init; } = [];
    public IReadOnlyList<GradeLevel> GradeLevels { get; init; } = [];
    public IReadOnlyList<ClassGroup> ClassGroups { get; init; } = [];
    public IReadOnlyList<Subject> Subjects { get; init; } = [];

    public IReadOnlyList<StudentProfile> StudentProfiles { get; init; } = [];
    public IReadOnlyList<StudentEnrollment> StudentEnrollments { get; init; } = [];
    public IReadOnlyList<TeacherAssignment> TeacherAssignments { get; init; } = [];

    public IReadOnlyList<LearningOutcome> LearningOutcomes { get; init; } = [];
    public IReadOnlyList<SchoolCurriculumAdoption> CurriculumAdoptions { get; init; } = [];
    public IReadOnlyList<CurriculumFrameworkVersion> FrameworkVersions { get; init; } = [];

    public IReadOnlyList<Assessment> Assessments { get; init; } = [];
    public IReadOnlyList<AssessmentQuestion> AssessmentQuestions { get; init; } = [];
    public IReadOnlyList<QuestionLearningOutcome> OutcomeMappings { get; init; } = [];
    public IReadOnlyList<AssessmentResult> AssessmentResults { get; init; } = [];
}

public sealed record ImportEntityGuard(
    Guid Id,
    byte[] RowVersion);

public sealed record ImportAssessmentGuard(
    Guid Id,
    byte[] RowVersion,
    AssessmentStatus RequiredStatus);

public sealed class ImportApplyPlan
{
    public List<Subject> Subjects { get; } = [];
    public List<ClassGroup> Classes { get; } = [];

    public List<StudentProfile> Students { get; } = [];
    public List<StudentEnrollment> Enrollments { get; } = [];

    public List<TeacherAssignment> TeacherAssignments { get; } = [];

    public List<AssessmentResult> AssessmentResults { get; } = [];
    public List<StudentAnswer> StudentAnswers { get; } = [];

    public List<QuestionLearningOutcome> CurriculumMappings { get; } = [];

    public List<OutboxMessage> OutboxMessages { get; } = [];

    public List<ImportEntityGuard> AcademicYearGuards { get; } = [];
    public List<ImportEntityGuard> ClassGroupGuards { get; } = [];
    public List<ImportEntityGuard> SubjectGuards { get; } = [];
    public List<ImportAssessmentGuard> AssessmentGuards { get; } = [];

    public List<Guid> AssessmentsToTouch { get; } = [];
}

public enum ImportPersistenceError
{
    None = 0,
    NotFound = 1,
    InvalidState = 2,
    Concurrency = 3,
    Constraint = 4,
    Unknown = 5,
    SeatLimit = 6
}

public sealed record ImportPersistenceResult(
    bool Succeeded,
    ImportPersistenceError Error)
{
    public static ImportPersistenceResult Success() =>
        new(true, ImportPersistenceError.None);

    public static ImportPersistenceResult Failure(
        ImportPersistenceError error) =>
        new(false, error);
}
