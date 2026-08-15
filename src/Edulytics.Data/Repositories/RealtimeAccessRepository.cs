using Edulytics.Core.Entities;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class RealtimeAccessRepository
    : IRealtimeAccessRepository
{
    private readonly EdulyticsDbContext _db;

    public RealtimeAccessRepository(EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TeacherAssignment>>
        GetTeacherAssignmentsAsync(
            Guid schoolId,
            Guid teacherUserId,
            CancellationToken cancellationToken = default) =>
        await _db.TeacherAssignments
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId &&
                x.TeacherUserId == teacherUserId)
            .ToListAsync(cancellationToken);
}
