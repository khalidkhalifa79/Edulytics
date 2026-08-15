using Edulytics.Core.Assessments;
using Edulytics.Core.Entities;

namespace Edulytics.Core.Interfaces;

public interface IAssessmentRepository
{
    Task<AssessmentSnapshot> GetSnapshotAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<Assessment?> GetAssessmentAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<AssessmentQuestion?> GetQuestionAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<QuestionLearningOutcome?> GetMappingAsync(
        Guid schoolId,
        Guid questionId,
        Guid outcomeId,
        CancellationToken cancellationToken = default);

    Task<AssessmentResult?> GetResultAsync(
        Guid schoolId,
        Guid assessmentId,
        Guid studentProfileId,
        CancellationToken cancellationToken = default);

    Task<StudentAnswer?> GetAnswerAsync(
        Guid schoolId,
        Guid resultId,
        Guid questionId,
        CancellationToken cancellationToken = default);

    Task<Term?> GetTermAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ClassGroup?> GetClassGroupAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Subject?> GetSubjectAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<StudentProfile?> GetStudentProfileAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<LearningOutcome?> GetLearningOutcomeAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CurriculumTopic?> GetCurriculumTopicAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> IsTeacherAssignedAsync(
        Guid schoolId,
        Guid teacherUserId,
        Guid classGroupId,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    Task<bool> IsStudentEnrolledAsync(
        Guid schoolId,
        Guid academicYearId,
        Guid classGroupId,
        Guid studentProfileId,
        CancellationToken cancellationToken = default);

    Task<bool> AssessmentTitleExistsAsync(
        Guid schoolId,
        Guid classGroupId,
        Guid termId,
        string normalizedTitle,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> QuestionOrderExistsAsync(
        Guid schoolId,
        Guid assessmentId,
        int order,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> MappingExistsAsync(
        Guid schoolId,
        Guid questionId,
        Guid outcomeId,
        CancellationToken cancellationToken = default);

    Task AddAsync<T>(
        T entity,
        CancellationToken cancellationToken = default)
        where T : class, ISchoolScoped;

    Task AddOutboxAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default);

    void RemoveMapping(QuestionLearningOutcome mapping);

    Task<AssessmentPersistenceResult> SaveAsync(
        CancellationToken cancellationToken = default);

    Task<AssessmentPersistenceResult> SaveWithRowVersionAsync<T>(
        T entity,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
        where T : class, ISchoolScoped;
}
