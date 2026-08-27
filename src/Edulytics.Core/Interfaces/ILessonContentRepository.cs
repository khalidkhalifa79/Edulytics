using Edulytics.Core.Entities;
using Edulytics.Core.Lessons;

namespace Edulytics.Core.Interfaces;

public interface ILessonContentRepository
{
    Task<IReadOnlyList<LessonTopicRecord>> ListTopicContextsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<LessonTopicRecord?> GetTopicContextAsync(
        Guid schoolId,
        Guid topicId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LessonAggregateRecord>> ListLessonAggregatesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<LessonAggregateRecord?> GetLessonAggregateAsync(
        Guid schoolId,
        Guid lessonId,
        CancellationToken cancellationToken = default);

    Task<LearningLesson?> GetLessonForUpdateAsync(
        Guid schoolId,
        Guid lessonId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LearningLessonTranslation>> GetTranslationsForUpdateAsync(
        Guid schoolId,
        Guid lessonId,
        CancellationToken cancellationToken = default);

    Task<bool> LessonOrderExistsAsync(
        Guid schoolId,
        Guid topicId,
        int order,
        Guid? excludeLessonId = null,
        CancellationToken cancellationToken = default);

    Task AddLessonAsync(
        LearningLesson lesson,
        CancellationToken cancellationToken = default);

    Task AddTranslationAsync(
        LearningLessonTranslation translation,
        CancellationToken cancellationToken = default);

    Task AddOutcomeLinkAsync(
        LearningLessonOutcome link,
        CancellationToken cancellationToken = default);

    Task ReplaceOutcomeLinksAsync(
        Guid schoolId,
        Guid lessonId,
        IReadOnlyCollection<Guid> outcomeIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudentPublishedLessonRecord>> ListPublishedForStudentAsync(
        Guid actorUserId,
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<StudentPublishedLessonRecord?> GetPublishedForStudentAsync(
        Guid actorUserId,
        Guid schoolId,
        Guid lessonId,
        CancellationToken cancellationToken = default);

    Task<LessonContentWriteResult> SaveAsync(
        CancellationToken cancellationToken = default);
}
