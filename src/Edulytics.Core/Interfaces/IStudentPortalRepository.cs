using Edulytics.Core.StudentPortal;

namespace Edulytics.Core.Interfaces;

public interface IStudentPortalRepository
{
    Task<StudentPortalSnapshot> GetSnapshotAsync(
        Guid schoolId,
        Guid studentUserId,
        CancellationToken cancellationToken = default);
}
