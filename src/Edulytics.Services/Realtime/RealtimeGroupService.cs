using Edulytics.Core.Constants;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Realtime;

namespace Edulytics.Services.Realtime;

public sealed class RealtimeGroupService
    : IRealtimeGroupService
{
    private readonly ISchoolUserRepository _users;
    private readonly ISchoolRepository _schools;
    private readonly IRealtimeAccessRepository _access;

    public RealtimeGroupService(
        ISchoolUserRepository users,
        ISchoolRepository schools,
        IRealtimeAccessRepository access)
    {
        _users = users;
        _schools = schools;
        _access = access;
    }

    public async Task<RealtimeGroupResolution> ResolveGroupsAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var actor = await _users.GetActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            !actor.SchoolId.HasValue ||
            actor.Roles.Count != 1)
        {
            return RealtimeGroupResolution.Denied();
        }

        var school = await _schools.GetByIdAsync(
            actor.SchoolId.Value,
            cancellationToken);

        if (school is null ||
            school.Status != SchoolStatus.Active)
        {
            return RealtimeGroupResolution.Denied();
        }

        if (actor.Roles[0] == RoleNames.SchoolAdmin)
        {
            return RealtimeGroupResolution.Success(
                [
                    RealtimeGroupNames.SchoolAdmins(
                        school.Id)
                ]);
        }

        if (actor.Roles[0] != RoleNames.Teacher)
            return RealtimeGroupResolution.Denied();

        var assignments = await _access.GetTeacherAssignmentsAsync(
            school.Id,
            actorUserId,
            cancellationToken);

        var groups = assignments
            .Select(x =>
                RealtimeGroupNames.Teachers(
                    school.Id,
                    x.ClassGroupId,
                    x.SubjectId))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        return RealtimeGroupResolution.Success(groups);
    }
}
