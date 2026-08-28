namespace Edulytics.Services.LessonContent;

public interface ILessonContentService
{
    Task<LessonContentQueryResult<LessonContentDashboard>> GetDashboardAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<LessonContentQueryResult<CanonicalLessonDetail>> GetStaffLessonAsync(
        Guid actorUserId,
        Guid lessonId,
        string cultureCode,
        CancellationToken cancellationToken = default);

    Task<LessonContentQueryResult<IReadOnlyList<StudentLessonSummary>>> ListPublishedForStudentAsync(
        Guid actorUserId,
        string cultureCode,
        CancellationToken cancellationToken = default);

    Task<LessonContentQueryResult<StudentLessonDetail>> GetPublishedForStudentAsync(
        Guid actorUserId,
        Guid lessonId,
        string cultureCode,
        CancellationToken cancellationToken = default);
}
