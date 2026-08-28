using Edulytics.Core.Lessons;

namespace Edulytics.Core.Interfaces;

public interface ILessonContentRepository
{
    Task<IReadOnlyList<CanonicalCurriculumContextRecord>> ListStaffAdoptionsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CanonicalCurriculumContextRecord>> ListStudentAdoptionsAsync(
        Guid actorUserId,
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PedagogicalLessonRecord>> ListPedagogicalLessonsAsync(
        IReadOnlyCollection<Guid> frameworkVersionIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CanonicalLessonContentRecord>> ListCanonicalContentsAsync(
        IReadOnlyCollection<Guid> pedagogicalLessonIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LessonOutcomeRecord>> ListOfficialOutcomesAsync(
        Guid frameworkVersionId,
        Guid pedagogicalLessonId,
        CancellationToken cancellationToken = default);
}
