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

    private readonly
        ISubjectSupervisorAssignmentRepository?
        _subjectSupervisors;

    public RealtimeGroupService(
        ISchoolUserRepository users,
        ISchoolRepository schools,
        IRealtimeAccessRepository access,
        ISubjectSupervisorAssignmentRepository?
            subjectSupervisors = null)
    {
        _users = users;
        _schools = schools;
        _access = access;
        _subjectSupervisors = subjectSupervisors;
    }

    public async Task<RealtimeGroupResolution>
        ResolveGroupsAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default)
    {
        var actor =
            await _users.GetActorAsync(
                actorUserId,
                cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            !actor.SchoolId.HasValue ||
            actor.Roles.Count != 1)
        {
            return RealtimeGroupResolution
                .Denied();
        }

        var school =
            await _schools.GetByIdAsync(
                actor.SchoolId.Value,
                cancellationToken);

        if (school is null ||
            school.Status != SchoolStatus.Active)
        {
            return RealtimeGroupResolution
                .Denied();
        }

        var role = actor.Roles[0];

        var schoolAnalytics =
            RealtimeGroupNames
                .SchoolAnalytics(
                    school.Id);

        if (role == RoleNames.SchoolAdmin)
        {
            return RealtimeGroupResolution
                .Success(
                    [
                        schoolAnalytics,
                        RealtimeGroupNames
                            .SchoolAdmins(
                                school.Id)
                    ]);
        }

        if (role == RoleNames.SubjectSupervisor)
        {
            if (_subjectSupervisors is null)
            {
                return RealtimeGroupResolution
                    .Denied();
            }

            var assignments =
                await _subjectSupervisors
                    .ListActiveBySupervisorAsync(
                        school.Id,
                        actorUserId,
                        cancellationToken);

            if (assignments.Count == 0)
            {
                return RealtimeGroupResolution
                    .Denied();
            }

            var groups =
                assignments
                    .Select(
                        x =>
                            RealtimeGroupNames
                                .SubjectSupervisors(
                                    school.Id,
                                    x.SubjectId))
                    .Append(
                        schoolAnalytics)
                    .Distinct(
                        StringComparer.Ordinal)
                    .OrderBy(
                        x => x,
                        StringComparer.Ordinal)
                    .ToArray();

            return RealtimeGroupResolution
                .Success(groups);
        }

        if (role != RoleNames.Teacher)
        {
            return RealtimeGroupResolution
                .Denied();
        }

        var teacherAssignments =
            await _access
                .GetTeacherAssignmentsAsync(
                    school.Id,
                    actorUserId,
                    cancellationToken);

        var teacherGroups =
            teacherAssignments
                .Select(
                    x =>
                        RealtimeGroupNames
                            .Teachers(
                                school.Id,
                                x.ClassGroupId,
                                x.SubjectId))
                .Append(
                    schoolAnalytics)
                .Distinct(
                    StringComparer.Ordinal)
                .OrderBy(
                    x => x,
                    StringComparer.Ordinal)
                .ToArray();

        return RealtimeGroupResolution
            .Success(teacherGroups);
    }
}
