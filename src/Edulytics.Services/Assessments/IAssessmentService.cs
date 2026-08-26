namespace Edulytics.Services.Assessments;

public interface IAssessmentService
{
    Task<AssessmentQueryResult<AssessmentWorkspace>> GetWorkspaceAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<AssessmentQueryResult<AssessmentDetails>> GetDetailsAsync(
        Guid actorUserId,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<AssessmentQueryResult<AssessmentQuestionItem>> GetQuestionAsync(
        Guid actorUserId,
        Guid questionId,
        CancellationToken cancellationToken = default);

    Task<AssessmentQueryResult<AssessmentResultsWorkspace>> GetResultsAsync(
        Guid actorUserId,
        Guid assessmentId,
        CancellationToken cancellationToken = default);

    Task<AssessmentCommandResult> CreateAssessmentAsync(
        Guid actorUserId,
        CreateAssessmentRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentCommandResult> UpdateAssessmentAsync(
        Guid actorUserId,
        UpdateAssessmentRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentCommandResult> CreateQuestionAsync(
        Guid actorUserId,
        CreateAssessmentQuestionRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentCommandResult> UpdateQuestionAsync(
        Guid actorUserId,
        UpdateAssessmentQuestionRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentCommandResult> DeleteAssessmentAsync(
        Guid actorUserId,
        DeleteAssessmentRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentCommandResult> DeleteQuestionAsync(
        Guid actorUserId,
        DeleteAssessmentQuestionRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentCommandResult> MapOutcomeAsync(
        Guid actorUserId,
        MapQuestionOutcomeRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentCommandResult> UnmapOutcomeAsync(
        Guid actorUserId,
        UnmapQuestionOutcomeRequest request,
        CancellationToken cancellationToken = default);

    Task<AssessmentCommandResult> OpenAssessmentAsync(
        Guid actorUserId,
        Guid assessmentId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<AssessmentCommandResult> CloseAssessmentAsync(
        Guid actorUserId,
        Guid assessmentId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<AssessmentCommandResult> SaveStudentResultAsync(
        Guid actorUserId,
        SaveStudentAssessmentResultRequest request,
        CancellationToken cancellationToken = default);
}
