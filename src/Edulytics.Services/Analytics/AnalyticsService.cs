using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;

namespace Edulytics.Services.Analytics;

public sealed class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _analytics;
    private readonly ISchoolRepository _schools;
    private readonly ISchoolUserRepository _users;
    private readonly AnalyticsProjectionBuilder _builder;
    private readonly ISubjectSupervisorAssignmentRepository?
        _subjectSupervisors;

    public AnalyticsService(
        IAnalyticsRepository analytics,
        ISchoolRepository schools,
        ISchoolUserRepository users,
        AnalyticsProjectionBuilder builder,
        ISubjectSupervisorAssignmentRepository?
            subjectSupervisors = null)
    {
        _analytics = analytics;
        _schools = schools;
        _users = users;
        _builder = builder;
        _subjectSupervisors = subjectSupervisors;
    }

    public async Task<AnalyticsCommandResult> RecalculateAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (!scope.Succeeded)
        {
            return AnalyticsCommandResult.Failure(
                scope.Error!.Value);
        }

        if (scope.Role != RoleNames.SchoolAdmin)
        {
            return AnalyticsCommandResult.Failure(
                AnalyticsErrorCode
                    .RecalculationRequiresSchoolAdmin);
        }

        try
        {
            var source =
                await _analytics.GetSourceSnapshotAsync(
                    scope.School!.Id,
                    cancellationToken);

            var projections = _builder.Build(
                source,
                DateTime.UtcNow);

            var saved =
                await _analytics.ReplaceProjectionsAsync(
                    scope.School.Id,
                    projections,
                    cancellationToken);

            return saved.Succeeded
                ? AnalyticsCommandResult.Success()
                : AnalyticsCommandResult.Failure(
                    AnalyticsErrorCode.PersistenceError);
        }
        catch (InvalidOperationException)
        {
            return AnalyticsCommandResult.Failure(
                AnalyticsErrorCode.InvalidSourceData);
        }
    }

    public async Task<AnalyticsQueryResult<AnalyticsDashboard>>
        GetDashboardAsync(
            Guid actorUserId,
            Guid? academicYearId = null,
            Guid? classGroupId = null,
            Guid? subjectId = null,
            CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (!scope.Succeeded)
        {
            return AnalyticsQueryResult<AnalyticsDashboard>
                .Failure(scope.Error!.Value);
        }

        var schoolId = scope.School!.Id;

        var projection =
            await _analytics.GetProjectionSnapshotAsync(
                schoolId,
                cancellationToken);

        var supervisorSubjectIds =
            scope.SupervisedSubjectIds;

        var pairSet = scope.Role == RoleNames.Teacher
            ? projection.TeacherAssignments
                .Where(
                    x =>
                        x.TeacherUserId ==
                        actorUserId)
                .Select(
                    x =>
                        (
                            x.AcademicYearId,
                            x.ClassGroupId,
                            x.SubjectId
                        ))
                .ToHashSet()
            : [];

        bool PairAllowed(
            Guid yearId,
            Guid classId,
            Guid subject)
        {
            if (scope.Role == RoleNames.SchoolAdmin)
                return true;

            if (scope.Role ==
                RoleNames.SubjectSupervisor)
            {
                return supervisorSubjectIds.Contains(
                    subject);
            }

            return pairSet.Contains(
                (
                    yearId,
                    classId,
                    subject
                ));
        }

        var visibleClasses = projection.ClassGroups
            .Where(x => x.Status == AcademicStructureStatus.Active)
            .Where(
                x =>
                    scope.Role == RoleNames.SchoolAdmin ||
                    scope.Role == RoleNames.SubjectSupervisor ||
                    pairSet.Any(
                        pair =>
                            pair.ClassGroupId == x.Id &&
                            pair.AcademicYearId ==
                            x.AcademicYearId))
            .OrderBy(x => x.Name)
            .ToArray();

        var visibleSubjects = projection.Subjects
            .Where(x => x.Status == AcademicStructureStatus.Active)
            .Where(
                x =>
                    scope.Role == RoleNames.SchoolAdmin ||
                    (scope.Role ==
                        RoleNames.SubjectSupervisor &&
                     supervisorSubjectIds.Contains(x.Id)) ||
                    (scope.Role == RoleNames.Teacher &&
                     pairSet.Any(
                        pair =>
                            pair.SubjectId == x.Id)))
            .OrderBy(x => x.Name)
            .ToArray();

        var visibleYearIds =
            scope.Role == RoleNames.SchoolAdmin ||
            scope.Role == RoleNames.SubjectSupervisor
                ? projection.AcademicYears
                    .Select(x => x.Id)
                    .ToHashSet()
                : pairSet
                    .Select(x => x.AcademicYearId)
                    .ToHashSet();

        var visibleYears = projection.AcademicYears
            .Where(x => visibleYearIds.Contains(x.Id))
            .OrderByDescending(x => x.StartsOn)
            .ToArray();

        if (academicYearId.HasValue &&
            !visibleYearIds.Contains(
                academicYearId.Value))
        {
            return AnalyticsQueryResult<AnalyticsDashboard>
                .Failure(
                    AnalyticsErrorCode.AccessDenied);
        }

        if (classGroupId.HasValue &&
            !visibleClasses.Any(
                x => x.Id == classGroupId.Value))
        {
            return AnalyticsQueryResult<AnalyticsDashboard>
                .Failure(
                    AnalyticsErrorCode.AccessDenied);
        }

        if (subjectId.HasValue &&
            !visibleSubjects.Any(
                x => x.Id == subjectId.Value))
        {
            return AnalyticsQueryResult<AnalyticsDashboard>
                .Failure(
                    AnalyticsErrorCode.AccessDenied);
        }

        if (scope.Role == RoleNames.Teacher &&
            classGroupId.HasValue &&
            subjectId.HasValue)
        {
            var selectedClass =
                visibleClasses.First(
                    x =>
                        x.Id ==
                        classGroupId.Value);

            if (!pairSet.Contains(
                    (
                        selectedClass.AcademicYearId,
                        classGroupId.Value,
                        subjectId.Value
                    )))
            {
                return AnalyticsQueryResult<AnalyticsDashboard>
                    .Failure(
                        AnalyticsErrorCode.AccessDenied);
            }
        }

        bool Matches(
            Guid yearId,
            Guid classId,
            Guid subject)
        {
            if (!PairAllowed(
                    yearId,
                    classId,
                    subject))
            {
                return false;
            }

            if (academicYearId.HasValue &&
                yearId != academicYearId.Value)
            {
                return false;
            }

            if (classGroupId.HasValue &&
                classId != classGroupId.Value)
            {
                return false;
            }

            if (subjectId.HasValue &&
                subject != subjectId.Value)
            {
                return false;
            }

            return true;
        }

        var masteries =
            projection.StudentOutcomeMasteries
                .Where(
                    x =>
                        Matches(
                            x.AcademicYearId,
                            x.ClassGroupId,
                            x.SubjectId))
                .ToArray();

        var classOutcomes =
            projection.ClassOutcomeSummaries
                .Where(
                    x =>
                        Matches(
                            x.AcademicYearId,
                            x.ClassGroupId,
                            x.SubjectId))
                .ToArray();

        var classTopics =
            projection.ClassTopicSummaries
                .Where(
                    x =>
                        Matches(
                            x.AcademicYearId,
                            x.ClassGroupId,
                            x.SubjectId))
                .ToArray();

        var trends =
            projection.ClassAssessmentTrends
                .Where(
                    x =>
                        Matches(
                            x.AcademicYearId,
                            x.ClassGroupId,
                            x.SubjectId))
                .ToArray();

        var years = projection.AcademicYears
            .ToDictionary(x => x.Id);

        var classes = projection.ClassGroups
            .ToDictionary(x => x.Id);

        var subjects = projection.Subjects
            .ToDictionary(x => x.Id);

        var students = projection.StudentProfiles
            .ToDictionary(x => x.Id);

        var outcomes = projection.LearningOutcomes
            .ToDictionary(x => x.Id);

        var topics = projection.CurriculumTopics
            .ToDictionary(x => x.Id);

        string YearName(Guid id) =>
            years.TryGetValue(id, out var value)
                ? value.Name
                : string.Empty;

        string ClassName(Guid id) =>
            classes.TryGetValue(id, out var value)
                ? value.Name
                : string.Empty;

        string SubjectName(Guid id) =>
            subjects.TryGetValue(id, out var value)
                ? value.Name
                : string.Empty;

        var outcomeItems = classOutcomes
            .Where(
                x =>
                    outcomes.ContainsKey(
                        x.LearningOutcomeId))
            .Select(x =>
            {
                var outcome =
                    outcomes[x.LearningOutcomeId];

                return new AnalyticsOutcomeItem(
                    x.AcademicYearId,
                    YearName(x.AcademicYearId),
                    x.ClassGroupId,
                    ClassName(x.ClassGroupId),
                    x.SubjectId,
                    SubjectName(x.SubjectId),
                    x.LearningOutcomeId,
                    outcome.Code,
                    outcome.Description,
                    x.AverageMasteryPercentage,
                    x.StudentCount,
                    x.AtRiskStudentCount,
                    x.EvidenceCount,
                    AnalyticsProjectionBuilder.BandFor(
                        x.AverageMasteryPercentage));
            })
            .OrderBy(x => x.ClassName)
            .ThenBy(x => x.SubjectName)
            .ThenBy(x => x.OutcomeCode)
            .ToArray();

        var topicItems = classTopics
            .Where(
                x =>
                    topics.ContainsKey(
                        x.CurriculumTopicId))
            .Select(x =>
            {
                var topic =
                    topics[x.CurriculumTopicId];

                return new AnalyticsTopicItem(
                    x.AcademicYearId,
                    YearName(x.AcademicYearId),
                    x.ClassGroupId,
                    ClassName(x.ClassGroupId),
                    x.SubjectId,
                    SubjectName(x.SubjectId),
                    x.CurriculumTopicId,
                    topic.Name,
                    x.MasteryPercentage,
                    x.OutcomeCount,
                    x.WeakOutcomeCount,
                    x.StudentCount,
                    AnalyticsProjectionBuilder.BandFor(
                        x.MasteryPercentage));
            })
            .OrderBy(x => x.MasteryPercentage)
            .ThenBy(x => x.TopicName)
            .ToArray();

        var trendItems = trends
            .Select(
                x =>
                    new AnalyticsTrendItem(
                        x.AcademicYearId,
                        YearName(x.AcademicYearId),
                        x.ClassGroupId,
                        ClassName(x.ClassGroupId),
                        x.SubjectId,
                        SubjectName(x.SubjectId),
                        x.AssessmentId,
                        x.AssessmentTitle,
                        x.AssessmentDate,
                        x.AveragePercentage,
                        x.StudentCount,
                        x.AtRiskStudentCount,
                        AnalyticsProjectionBuilder.BandFor(
                            x.AveragePercentage)))
            .OrderBy(x => x.AssessmentDate)
            .ThenBy(x => x.AssessmentTitle)
            .ToArray();

        var riskItems = masteries
            .GroupBy(
                x => new
                {
                    x.StudentProfileId,
                    x.AcademicYearId,
                    x.ClassGroupId
                })
            .Select(group =>
            {
                var earned =
                    group.Sum(x => x.EarnedScore);

                var possible =
                    group.Sum(x => x.PossibleScore);

                var percentage =
                    possible <= 0m
                        ? 0m
                        : decimal.Round(
                            earned /
                            possible *
                            100m,
                            2,
                            MidpointRounding
                                .AwayFromZero);

                if (!students.TryGetValue(
                        group.Key.StudentProfileId,
                        out var student))
                {
                    return null;
                }

                return new AnalyticsRiskStudentItem(
                    student.Id,
                    student.StudentNumber,
                    student.DisplayName,
                    group.Key.AcademicYearId,
                    YearName(
                        group.Key.AcademicYearId),
                    group.Key.ClassGroupId,
                    ClassName(
                        group.Key.ClassGroupId),
                    percentage,
                    group.Count(
                        x =>
                            x.MasteryPercentage <
                            40m),
                    AnalyticsProjectionBuilder
                        .BandFor(percentage));
            })
            .Where(x => x is not null)
            .Cast<AnalyticsRiskStudentItem>()
            .Where(x => x.MasteryPercentage < 60m)
            .OrderBy(x => x.MasteryPercentage)
            .ThenBy(x => x.DisplayName)
            .ToArray();

        var earnedTotal =
            masteries.Sum(x => x.EarnedScore);

        var possibleTotal =
            masteries.Sum(x => x.PossibleScore);

        var overall =
            possibleTotal <= 0m
                ? 0m
                : decimal.Round(
                    earnedTotal /
                    possibleTotal *
                    100m,
                    2,
                    MidpointRounding
                        .AwayFromZero);

        var latestProjection =
            projection.SchoolSnapshots
                .Select(
                    x =>
                        (DateTime?)
                        x.CalculatedAtUtc)
                .Concat(
                    projection
                        .StudentOutcomeMasteries
                        .Select(
                            x =>
                                (DateTime?)
                                x.CalculatedAtUtc))
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .DefaultIfEmpty()
                .Max();

        DateTime? generatedAt =
            latestProjection == default
                ? null
                : latestProjection;

        var latestSource =
            await _analytics
                .GetLatestSourceUpdateAsync(
                    schoolId,
                    cancellationToken);

        var isStale =
            latestSource.HasValue &&
            (!generatedAt.HasValue ||
             latestSource.Value >
             generatedAt.Value);

        var dashboard =
            new AnalyticsDashboard(
                masteries.Length > 0,
                isStale,
                scope.Role ==
                RoleNames.SchoolAdmin,
                generatedAt,
                overall,
                masteries
                    .Select(
                        x =>
                            x.StudentProfileId)
                    .Distinct()
                    .Count(),
                riskItems
                    .Select(
                        x =>
                            x.StudentProfileId)
                    .Distinct()
                    .Count(),
                classOutcomes.Count(
                    x =>
                        x.AverageMasteryPercentage <
                        40m),
                classTopics.Count(
                    x =>
                        x.MasteryPercentage <
                        60m),
                academicYearId,
                classGroupId,
                subjectId,
                visibleYears
                    .Select(
                        x =>
                            new AnalyticsFilterItem(
                                x.Id,
                                x.Name))
                    .ToArray(),
                visibleClasses
                    .Select(
                        x =>
                            new AnalyticsFilterItem(
                                x.Id,
                                $"{x.Name} ({x.Code})"))
                    .ToArray(),
                visibleSubjects
                    .Select(
                        x =>
                            new AnalyticsFilterItem(
                                x.Id,
                                $"{x.Name} ({x.Code})"))
                    .ToArray(),
                outcomeItems,
                topicItems,
                trendItems,
                riskItems);

        return AnalyticsQueryResult<AnalyticsDashboard>
            .Success(dashboard);
    }

    private async Task<ScopeResult> ResolveScopeAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var actor =
            await _users.GetActorAsync(
                actorUserId,
                cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            !actor.SchoolId.HasValue)
        {
            return ScopeResult.Fail(
                AnalyticsErrorCode.AccessDenied);
        }

        var role =
            actor.Roles.Count == 1
                ? actor.Roles[0]
                : null;

        if (role != RoleNames.SchoolAdmin &&
            role != RoleNames.SubjectSupervisor &&
            role != RoleNames.Teacher)
        {
            return ScopeResult.Fail(
                AnalyticsErrorCode.AccessDenied);
        }

        var school =
            await _schools.GetByIdAsync(
                actor.SchoolId.Value,
                cancellationToken);

        if (school is null ||
            school.Status != SchoolStatus.Active)
        {
            return ScopeResult.Fail(
                AnalyticsErrorCode.SchoolNotActive);
        }

        IReadOnlySet<Guid> supervisedSubjectIds =
            new HashSet<Guid>();

        if (role == RoleNames.SubjectSupervisor)
        {
            if (_subjectSupervisors is null)
            {
                return ScopeResult.Fail(
                    AnalyticsErrorCode.AccessDenied);
            }

            var assignments =
                await _subjectSupervisors
                    .ListActiveBySupervisorAsync(
                        school.Id,
                        actorUserId,
                        cancellationToken);

            supervisedSubjectIds = assignments
                .Select(x => x.SubjectId)
                .ToHashSet();

            if (supervisedSubjectIds.Count == 0)
            {
                return ScopeResult.Fail(
                    AnalyticsErrorCode.AccessDenied);
            }
        }

        return ScopeResult.Ok(
            actor,
            school,
            role,
            supervisedSubjectIds);
    }

    private sealed record ScopeResult(
        bool Succeeded,
        SchoolUserRecord? Actor,
        School? School,
        string? Role,
        IReadOnlySet<Guid> SupervisedSubjectIds,
        AnalyticsErrorCode? Error)
    {
        public static ScopeResult Ok(
            SchoolUserRecord actor,
            School school,
            string role,
            IReadOnlySet<Guid> supervisedSubjectIds) =>
            new(
                true,
                actor,
                school,
                role,
                supervisedSubjectIds,
                null);

        public static ScopeResult Fail(
            AnalyticsErrorCode error) =>
            new(
                false,
                null,
                null,
                null,
                new HashSet<Guid>(),
                error);
    }
}
