namespace Edulytics.Services.StudentPortal;

public interface IStudentPortalService
{
    Task<StudentPortalQueryResult<StudentPortalWorkspace>> GetWorkspaceAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);
}
