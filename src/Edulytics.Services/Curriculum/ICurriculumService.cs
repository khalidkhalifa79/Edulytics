namespace Edulytics.Services.Curriculum;

public interface ICurriculumService
{
    Task<CurriculumQueryResult<CurriculumDashboard>> GetDashboardAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<CurriculumQueryResult<CurriculumTopicItem>> GetTopicAsync(
        Guid actorUserId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CurriculumQueryResult<LearningOutcomeItem>> GetOutcomeAsync(
        Guid actorUserId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CurriculumCommandResult> SelectFrameworkAsync(
        Guid actorUserId,
        SelectCurriculumFrameworkRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumCommandResult> CreateTopicAsync(
        Guid actorUserId,
        CreateCurriculumTopicRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumCommandResult> UpdateTopicAsync(
        Guid actorUserId,
        UpdateCurriculumTopicRequest request,
        CancellationToken cancellationToken = default);


    Task<CurriculumCommandResult> CreateOfficialOutcomeAsync(
        Guid actorUserId,
        CreateOfficialLearningOutcomeRequest request,
        CancellationToken cancellationToken = default);

    Task<CurriculumCommandResult> UpdateOutcomeAsync(
        Guid actorUserId,
        UpdateLearningOutcomeRequest request,
        CancellationToken cancellationToken = default);
}
