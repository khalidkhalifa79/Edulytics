using Edulytics.Core.Entities;

namespace Edulytics.Core.Analytics;

public sealed record AnalyticsSourceSnapshot(
    IReadOnlyList<AcademicYear> AcademicYears,
    IReadOnlyList<ClassGroup> ClassGroups,
    IReadOnlyList<Subject> Subjects,
    IReadOnlyList<StudentProfile> StudentProfiles,
    IReadOnlyList<StudentEnrollment> StudentEnrollments,
    IReadOnlyList<TeacherAssignment> TeacherAssignments,
    IReadOnlyList<CurriculumTopic> CurriculumTopics,
    IReadOnlyList<LearningOutcome> LearningOutcomes,
    IReadOnlyList<Assessment> Assessments,
    IReadOnlyList<AssessmentQuestion> AssessmentQuestions,
    IReadOnlyList<QuestionLearningOutcome> OutcomeMappings,
    IReadOnlyList<AssessmentResult> AssessmentResults,
    IReadOnlyList<StudentAnswer> StudentAnswers);

public sealed record AnalyticsProjectionSnapshot(
    IReadOnlyList<AcademicYear> AcademicYears,
    IReadOnlyList<ClassGroup> ClassGroups,
    IReadOnlyList<Subject> Subjects,
    IReadOnlyList<StudentProfile> StudentProfiles,
    IReadOnlyList<TeacherAssignment> TeacherAssignments,
    IReadOnlyList<CurriculumTopic> CurriculumTopics,
    IReadOnlyList<LearningOutcome> LearningOutcomes,
    IReadOnlyList<StudentOutcomeMastery> StudentOutcomeMasteries,
    IReadOnlyList<ClassOutcomeSummary> ClassOutcomeSummaries,
    IReadOnlyList<ClassTopicSummary> ClassTopicSummaries,
    IReadOnlyList<ClassAssessmentTrend> ClassAssessmentTrends,
    IReadOnlyList<SchoolAnalyticsSnapshot> SchoolSnapshots);

public sealed record AnalyticsProjectionSet(
    IReadOnlyList<StudentOutcomeMastery> StudentOutcomeMasteries,
    IReadOnlyList<ClassOutcomeSummary> ClassOutcomeSummaries,
    IReadOnlyList<ClassTopicSummary> ClassTopicSummaries,
    IReadOnlyList<ClassAssessmentTrend> ClassAssessmentTrends,
    IReadOnlyList<SchoolAnalyticsSnapshot> SchoolSnapshots);

public enum AnalyticsPersistenceError
{
    None = 0,
    Constraint = 1,
    Unknown = 2
}

public sealed record AnalyticsPersistenceResult(
    bool Succeeded,
    AnalyticsPersistenceError Error)
{
    public static AnalyticsPersistenceResult Success() =>
        new(true, AnalyticsPersistenceError.None);

    public static AnalyticsPersistenceResult Failure(
        AnalyticsPersistenceError error) =>
        new(false, error);
}
