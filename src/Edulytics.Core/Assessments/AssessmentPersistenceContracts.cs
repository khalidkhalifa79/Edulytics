using Edulytics.Core.Entities;

namespace Edulytics.Core.Assessments;

public sealed record AssessmentSnapshot(
    IReadOnlyList<AcademicYear> AcademicYears,
    IReadOnlyList<Term> Terms,
    IReadOnlyList<GradeLevel> GradeLevels,
    IReadOnlyList<ClassGroup> ClassGroups,
    IReadOnlyList<Subject> Subjects,
    IReadOnlyList<StudentProfile> StudentProfiles,
    IReadOnlyList<TeacherAssignment> TeacherAssignments,
    IReadOnlyList<StudentEnrollment> StudentEnrollments,
    IReadOnlyList<CurriculumTopic> CurriculumTopics,
    IReadOnlyList<LearningOutcome> LearningOutcomes,
    IReadOnlyList<SchoolCurriculumAdoption> CurriculumAdoptions,
    IReadOnlyList<CurriculumFrameworkVersion> FrameworkVersions,
    IReadOnlyList<Assessment> Assessments,
    IReadOnlyList<AssessmentQuestion> Questions,
    IReadOnlyList<QuestionLearningOutcome> OutcomeMappings,
    IReadOnlyList<AssessmentResult> Results,
    IReadOnlyList<StudentAnswer> StudentAnswers);

public enum AssessmentPersistenceError
{
    None = 0,
    Conflict = 1,
    Constraint = 2
}

public sealed record AssessmentPersistenceResult(
    bool Succeeded,
    AssessmentPersistenceError Error)
{
    public static AssessmentPersistenceResult Success() =>
        new(true, AssessmentPersistenceError.None);

    public static AssessmentPersistenceResult Failure(
        AssessmentPersistenceError error) =>
        new(false, error);
}
