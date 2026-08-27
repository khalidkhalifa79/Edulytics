namespace Edulytics.Services.LessonContent;

public interface ILessonContentService
{
    Task<LessonContentQueryResult<LessonContentDashboard>> GetDashboardAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<LessonContentQueryResult<LessonContentEditor>> GetCreateEditorAsync(
        Guid actorUserId,
        Guid topicId,
        CancellationToken cancellationToken = default);

    Task<LessonContentQueryResult<LessonContentEditor>> GetEditEditorAsync(
        Guid actorUserId,
        Guid lessonId,
        CancellationToken cancellationToken = default);

    Task<LessonContentCommandResult> CreateAsync(
        Guid actorUserId,
        CreateLessonContentRequest request,
        CancellationToken cancellationToken = default);

    Task<LessonContentCommandResult> UpdateDraftAsync(
        Guid actorUserId,
        UpdateLessonContentRequest request,
        CancellationToken cancellationToken = default);

    Task<LessonContentCommandResult> SubmitForReviewAsync(
        Guid actorUserId,
        Guid lessonId,
        CancellationToken cancellationToken = default);

    Task<LessonContentCommandResult> ReturnToDraftAsync(
        Guid actorUserId,
        Guid lessonId,
        CancellationToken cancellationToken = default);

    Task<LessonContentCommandResult> PublishAsync(
        Guid actorUserId,
        Guid lessonId,
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
