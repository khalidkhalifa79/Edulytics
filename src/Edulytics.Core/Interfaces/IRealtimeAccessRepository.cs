using Edulytics.Core.Entities;

namespace Edulytics.Core.Interfaces;

public interface IRealtimeAccessRepository
{
    Task<IReadOnlyList<TeacherAssignment>> GetTeacherAssignmentsAsync(
        Guid schoolId,
        Guid teacherUserId,
        CancellationToken cancellationToken = default);
}
