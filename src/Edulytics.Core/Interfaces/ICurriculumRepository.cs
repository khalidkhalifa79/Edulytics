using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;

namespace Edulytics.Core.Interfaces;

public interface ICurriculumRepository
{
    Task<CurriculumSnapshot> GetSnapshotAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<GradeLevel?> GetGradeLevelAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Subject?> GetSubjectAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CurriculumTopic?> GetTopicAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<LearningOutcome?> GetOutcomeAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetPrimaryDefaultFrameworkVersionIdAsync(
        Guid schoolId,
        Guid gradeLevelId,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetPlatformDefaultFrameworkVersionIdAsync(
        CancellationToken cancellationToken = default);

    Task<bool> TopicNameExistsAsync(
        Guid schoolId,
        Guid frameworkVersionId,
        Guid subjectId,
        Guid gradeLevelId,
        string normalizedName,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> TopicOrderExistsAsync(
        Guid schoolId,
        Guid frameworkVersionId,
        Guid subjectId,
        Guid gradeLevelId,
        int order,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> OutcomeCodeExistsAsync(
        Guid schoolId,
        Guid frameworkVersionId,
        Guid subjectId,
        Guid gradeLevelId,
        string normalizedCode,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> OutcomeOrderExistsAsync(
        Guid schoolId,
        Guid topicId,
        int order,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task AddDefaultAdoptionAsync(
        SchoolCurriculumAdoption adoption,
        CancellationToken cancellationToken = default);

    Task AddTopicAsync(
        CurriculumTopic topic,
        CancellationToken cancellationToken = default);

    Task AddOutcomeAsync(
        LearningOutcome outcome,
        CancellationToken cancellationToken = default);

    Task<CurriculumPersistenceResult> SaveAsync(
        CancellationToken cancellationToken = default);
}
