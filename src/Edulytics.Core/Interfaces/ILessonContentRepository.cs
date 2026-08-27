using Edulytics.Core.Lessons;
namespace Edulytics.Core.Interfaces;

public interface ILessonContentRepository
{
    Task<IReadOnlyList<CanonicalCurriculumContextRecord>> ListStaffAdoptionsAsync(Guid schoolId,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<CanonicalCurriculumContextRecord>> ListStudentAdoptionsAsync(Guid actorUserId,Guid schoolId,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<CanonicalCurriculumNodeRecord>> ListCurriculumNodesAsync(IReadOnlyCollection<Guid> frameworkVersionIds,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<CanonicalLessonContentRecord>> ListCanonicalContentsAsync(IReadOnlyCollection<Guid> lessonNodeIds,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<LessonOutcomeRecord>> ListOfficialOutcomesAsync(Guid frameworkVersionId,Guid lessonNodeId,CancellationToken cancellationToken=default);
}
