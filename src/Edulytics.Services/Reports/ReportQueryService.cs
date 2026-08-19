using Edulytics.Core.Analytics;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Reports;
using Edulytics.Core.Users;

namespace Edulytics.Services.Reports;

public sealed class ReportQueryService
    : IReportQueryService
{
    private readonly IAnalyticsRepository _analytics;
    private readonly ISchoolRepository _schools;
    private readonly ISchoolUserRepository _users;

    private readonly
        ISubjectSupervisorAssignmentRepository
        _subjectSupervisors;

    public ReportQueryService(
        IAnalyticsRepository analytics,
        ISchoolRepository schools,
        ISchoolUserRepository users,
        ISubjectSupervisorAssignmentRepository
            subjectSupervisors)
    {
        _analytics = analytics;
        _schools = schools;
        _users = users;
        _subjectSupervisors =
            subjectSupervisors;
    }

    public async Task<
        ReportQueryResult<ReportCatalog>>
        GetCatalogAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default)
    {
        var scope =
            await ResolveAsync(
                actorUserId,
                cancellationToken);

        if (scope.Value is null)
        {
            return ReportQueryResult<ReportCatalog>
                .Failure(scope.Error!.Value);
        }

        return ReportQueryResult<ReportCatalog>
            .Success(
                BuildCatalog(scope.Value));
    }

    public async Task<
        ReportQueryResult<ReportCatalog>>
        ValidateAsync(
            Guid actorUserId,
            ReportRequest request,
            CancellationToken cancellationToken = default)
    {
        var scope =
            await ResolveAsync(
                actorUserId,
                cancellationToken);

        if (scope.Value is null)
        {
            return ReportQueryResult<ReportCatalog>
                .Failure(scope.Error!.Value);
        }

        var catalog =
            BuildCatalog(scope.Value);

        var error =
            Validate(
                scope.Value,
                catalog,
                request);

        return error.HasValue
            ? ReportQueryResult<ReportCatalog>
                .Failure(error.Value)
            : ReportQueryResult<ReportCatalog>
                .Success(catalog);
    }

    public async Task<
        ReportQueryResult<ReportDocument>>
        BuildAsync(
            Guid actorUserId,
            ReportRequest request,
            int maxRows,
            CancellationToken cancellationToken = default)
    {
        if (maxRows <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxRows));
        }

        var scope =
            await ResolveAsync(
                actorUserId,
                cancellationToken);

        if (scope.Value is null)
        {
            return ReportQueryResult<ReportDocument>
                .Failure(scope.Error!.Value);
        }

        var catalog =
            BuildCatalog(scope.Value);

        var error =
            Validate(
                scope.Value,
                catalog,
                request);

        if (error.HasValue)
        {
            return ReportQueryResult<ReportDocument>
                .Failure(error.Value);
        }

        var document =
            BuildDocument(
                scope.Value,
                request,
                maxRows);

        return ReportQueryResult<ReportDocument>
            .Success(document);
    }

    private async Task<
        ReportQueryResult<Scope>>
        ResolveAsync(
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
            !actor.SchoolId.HasValue ||
            actor.Roles.Count != 1)
        {
            return ReportQueryResult<Scope>
                .Failure(
                    ReportErrorCode.AccessDenied);
        }

        var role = actor.Roles[0];

        if (role != RoleNames.SchoolAdmin &&
            role != RoleNames.Teacher &&
            role != RoleNames.SubjectSupervisor)
        {
            return ReportQueryResult<Scope>
                .Failure(
                    ReportErrorCode.AccessDenied);
        }

        var school =
            await _schools.GetByIdAsync(
                actor.SchoolId.Value,
                cancellationToken);

        if (school is null ||
            school.Status != SchoolStatus.Active)
        {
            return ReportQueryResult<Scope>
                .Failure(
                    ReportErrorCode.SchoolNotActive);
        }

        var projection =
            await _analytics
                .GetProjectionSnapshotAsync(
                    school.Id,
                    cancellationToken);

        IReadOnlySet<(
            Guid AcademicYearId,
            Guid ClassGroupId,
            Guid SubjectId)> teacherPairs =
            new HashSet<(
                Guid,
                Guid,
                Guid)>();

        if (role == RoleNames.Teacher)
        {
            teacherPairs =
                projection.TeacherAssignments
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
                    .ToHashSet();

            if (teacherPairs.Count == 0)
            {
                return ReportQueryResult<Scope>
                    .Failure(
                        ReportErrorCode.AccessDenied);
            }
        }

        IReadOnlySet<Guid>
            supervisedSubjectIds =
                new HashSet<Guid>();

        if (role ==
            RoleNames.SubjectSupervisor)
        {
            var assignments =
                await _subjectSupervisors
                    .ListActiveBySupervisorAsync(
                        school.Id,
                        actorUserId,
                        cancellationToken);

            supervisedSubjectIds =
                assignments
                    .Select(x => x.SubjectId)
                    .ToHashSet();

            if (supervisedSubjectIds.Count == 0)
            {
                return ReportQueryResult<Scope>
                    .Failure(
                        ReportErrorCode.AccessDenied);
            }
        }

        return ReportQueryResult<Scope>
            .Success(
                new Scope(
                    actor,
                    school,
                    role,
                    projection,
                    teacherPairs,
                    supervisedSubjectIds));
    }

    private static bool PairAllowed(
        Scope scope,
        Guid academicYearId,
        Guid classGroupId,
        Guid subjectId)
    {
        if (scope.Role ==
            RoleNames.SchoolAdmin)
        {
            return true;
        }

        if (scope.Role ==
            RoleNames.SubjectSupervisor)
        {
            return scope.SupervisedSubjectIds
                .Contains(subjectId);
        }

        return scope.TeacherPairs.Contains(
            (
                academicYearId,
                classGroupId,
                subjectId
            ));
    }

    private static ReportCatalog BuildCatalog(
        Scope scope)
    {
        var projection =
            scope.Projection;

        var visibleYears =
            scope.Role ==
                RoleNames.SchoolAdmin ||
            scope.Role ==
                RoleNames.SubjectSupervisor
                ? projection.AcademicYears
                    .Where(
                        x =>
                            x.Status ==
                            AcademicStructureStatus.Active)
                    .Select(x => x.Id)
                    .ToHashSet()
                : scope.TeacherPairs
                    .Select(x => x.AcademicYearId)
                    .ToHashSet();

        var visibleClasses =
            projection.ClassGroups
                .Where(
                    x =>
                        x.Status ==
                        AcademicStructureStatus.Active)
                .Where(
                    x =>
                        scope.Role ==
                            RoleNames.SchoolAdmin ||
                        scope.Role ==
                            RoleNames.SubjectSupervisor ||
                        scope.TeacherPairs.Any(
                            pair =>
                                pair.AcademicYearId ==
                                    x.AcademicYearId &&
                                pair.ClassGroupId ==
                                    x.Id))
                .ToArray();

        var visibleSubjects =
            projection.Subjects
                .Where(
                    x =>
                        x.Status ==
                        AcademicStructureStatus.Active)
                .Where(
                    x =>
                        scope.Role ==
                            RoleNames.SchoolAdmin ||
                        (scope.Role ==
                            RoleNames.SubjectSupervisor &&
                         scope.SupervisedSubjectIds
                            .Contains(x.Id)) ||
                        (scope.Role ==
                            RoleNames.Teacher &&
                         scope.TeacherPairs.Any(
                            pair =>
                                pair.SubjectId ==
                                    x.Id)))
                .ToArray();

        var visibleMasteries =
            projection.StudentOutcomeMasteries
                .Where(
                    x =>
                        PairAllowed(
                            scope,
                            x.AcademicYearId,
                            x.ClassGroupId,
                            x.SubjectId))
                .ToArray();

        var visibleStudentIds =
            visibleMasteries
                .Select(x => x.StudentProfileId)
                .ToHashSet();

        var visibleOutcomeIds =
            projection.ClassOutcomeSummaries
                .Where(
                    x =>
                        PairAllowed(
                            scope,
                            x.AcademicYearId,
                            x.ClassGroupId,
                            x.SubjectId))
                .Select(x => x.LearningOutcomeId)
                .ToHashSet();

        IReadOnlyList<ReportKind>
            allowedKinds =
                scope.Role ==
                    RoleNames.SchoolAdmin
                    ? new[]
                    {
                        ReportKind.School,
                        ReportKind.Class,
                        ReportKind.Subject,
                        ReportKind.Student,
                        ReportKind.LearningOutcome
                    }
                    : new[]
                    {
                        ReportKind.Class,
                        ReportKind.Subject,
                        ReportKind.Student,
                        ReportKind.LearningOutcome
                    };

        return new ReportCatalog(
            scope.School.Id,
            scope.School.Name,
            scope.Role,
            allowedKinds,
            projection.AcademicYears
                .Where(
                    x =>
                        visibleYears.Contains(x.Id))
                .OrderByDescending(x => x.StartsOn)
                .Select(
                    x =>
                        new ReportFilterItem(
                            x.Id,
                            x.Name))
                .ToArray(),
            visibleClasses
                .OrderBy(x => x.Name)
                .Select(
                    x =>
                        new ReportFilterItem(
                            x.Id,
                            $"{x.Name} ({x.Code})"))
                .ToArray(),
            visibleSubjects
                .OrderBy(x => x.Name)
                .Select(
                    x =>
                        new ReportFilterItem(
                            x.Id,
                            $"{x.Name} ({x.Code})"))
                .ToArray(),
            projection.StudentProfiles
                .Where(
                    x =>
                        visibleStudentIds.Contains(
                            x.Id) &&
                        x.Status ==
                            AcademicStructureStatus.Active)
                .OrderBy(x => x.DisplayName)
                .Select(
                    x =>
                        new ReportFilterItem(
                            x.Id,
                            $"{x.DisplayName} "
                            + $"({x.StudentNumber})"))
                .ToArray(),
            projection.LearningOutcomes
                .Where(
                    x =>
                        visibleOutcomeIds.Contains(
                            x.Id))
                .OrderBy(x => x.Code)
                .Select(
                    x =>
                        new ReportFilterItem(
                            x.Id,
                            $"{x.Code} — "
                            + x.Description))
                .ToArray());
    }

    private static ReportErrorCode?
        Validate(
            Scope scope,
            ReportCatalog catalog,
            ReportRequest request)
    {
        if (!catalog.AllowedKinds.Contains(
                request.Kind))
        {
            return ReportErrorCode.AccessDenied;
        }

        if (request.AcademicYearId.HasValue &&
            !catalog.AcademicYears.Any(
                x =>
                    x.Id ==
                    request.AcademicYearId.Value))
        {
            return ReportErrorCode.AccessDenied;
        }

        if (request.ClassGroupId.HasValue &&
            !catalog.ClassGroups.Any(
                x =>
                    x.Id ==
                    request.ClassGroupId.Value))
        {
            return ReportErrorCode.AccessDenied;
        }

        if (request.SubjectId.HasValue &&
            !catalog.Subjects.Any(
                x =>
                    x.Id ==
                    request.SubjectId.Value))
        {
            return ReportErrorCode.AccessDenied;
        }

        if (request.StudentProfileId.HasValue &&
            !catalog.Students.Any(
                x =>
                    x.Id ==
                    request.StudentProfileId.Value))
        {
            return ReportErrorCode.AccessDenied;
        }

        if (request.LearningOutcomeId.HasValue &&
            !catalog.LearningOutcomes.Any(
                x =>
                    x.Id ==
                    request.LearningOutcomeId.Value))
        {
            return ReportErrorCode.AccessDenied;
        }

        if (request.Kind == ReportKind.Class &&
            !request.ClassGroupId.HasValue)
        {
            return ReportErrorCode.InvalidFilter;
        }

        if (request.Kind == ReportKind.Subject &&
            !request.SubjectId.HasValue)
        {
            return ReportErrorCode.InvalidFilter;
        }

        if (request.Kind == ReportKind.Student &&
            !request.StudentProfileId.HasValue)
        {
            return ReportErrorCode.InvalidFilter;
        }

        if (request.Kind ==
                ReportKind.LearningOutcome &&
            !request.LearningOutcomeId.HasValue)
        {
            return ReportErrorCode.InvalidFilter;
        }

        if (scope.Role == RoleNames.Teacher &&
            request.ClassGroupId.HasValue &&
            request.SubjectId.HasValue)
        {
            var selectedClass =
                scope.Projection.ClassGroups
                    .Single(
                        x =>
                            x.Id ==
                            request.ClassGroupId.Value);

            if (!scope.TeacherPairs.Contains(
                    (
                        selectedClass.AcademicYearId,
                        request.ClassGroupId.Value,
                        request.SubjectId.Value
                    )))
            {
                return ReportErrorCode.AccessDenied;
            }
        }

        return null;
    }

    private static ReportDocument
        BuildDocument(
            Scope scope,
            ReportRequest request,
            int maxRows)
    {
        var projection =
            scope.Projection;

        var years =
            projection.AcademicYears
                .ToDictionary(x => x.Id);

        var classes =
            projection.ClassGroups
                .ToDictionary(x => x.Id);

        var subjects =
            projection.Subjects
                .ToDictionary(x => x.Id);

        var students =
            projection.StudentProfiles
                .ToDictionary(x => x.Id);

        var outcomes =
            projection.LearningOutcomes
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

        bool Matches(
            Guid academicYearId,
            Guid classGroupId,
            Guid subjectId)
        {
            if (!PairAllowed(
                    scope,
                    academicYearId,
                    classGroupId,
                    subjectId))
            {
                return false;
            }

            if (request.AcademicYearId.HasValue &&
                request.AcademicYearId.Value !=
                    academicYearId)
            {
                return false;
            }

            if (request.ClassGroupId.HasValue &&
                request.ClassGroupId.Value !=
                    classGroupId)
            {
                return false;
            }

            if (request.SubjectId.HasValue &&
                request.SubjectId.Value !=
                    subjectId)
            {
                return false;
            }

            return true;
        }

        IReadOnlyList<ReportColumn> columns;
        IEnumerable<ReportRow> source;
        string titleKey;

        switch (request.Kind)
        {
            case ReportKind.School:
                titleKey = "ReportTitleSchool";

                columns =
                [
                    new(
                        "ColumnAcademicYear",
                        ReportCellKind.Text),

                    new(
                        "ColumnOverallMastery",
                        ReportCellKind.Percentage),

                    new(
                        "ColumnStudentsWithEvidence",
                        ReportCellKind.Integer),

                    new(
                        "ColumnAtRiskStudents",
                        ReportCellKind.Integer),

                    new(
                        "ColumnCriticalOutcomes",
                        ReportCellKind.Integer),

                    new(
                        "ColumnWeakTopics",
                        ReportCellKind.Integer),

                    new(
                        "ColumnCalculatedAt",
                        ReportCellKind.DateTime)
                ];

                source =
                    projection.SchoolSnapshots
                        .Where(
                            x =>
                                !request.AcademicYearId
                                    .HasValue ||
                                x.AcademicYearId ==
                                request
                                    .AcademicYearId.Value)
                        .OrderByDescending(
                            x =>
                                years.TryGetValue(
                                    x.AcademicYearId,
                                    out var year)
                                    ? year.StartsOn
                                    : DateOnly.MinValue)
                        .Select(
                            x =>
                                new ReportRow(
                                [
                                    ReportCell.Text(
                                        YearName(
                                            x.AcademicYearId)),

                                    ReportCell.Percentage(
                                        x.OverallMasteryPercentage),

                                    ReportCell.Integer(
                                        x.StudentsWithEvidence),

                                    ReportCell.Integer(
                                        x.AtRiskStudents),

                                    ReportCell.Integer(
                                        x.CriticalOutcomeCount),

                                    ReportCell.Integer(
                                        x.WeakTopicCount),

                                    ReportCell.DateTime(
                                        x.CalculatedAtUtc)
                                ]));

                break;

            case ReportKind.Student:
                titleKey = "ReportTitleStudent";

                columns =
                    MasteryColumns(
                        includeStudent: true);

                source =
                    projection.StudentOutcomeMasteries
                        .Where(
                            x =>
                                x.StudentProfileId ==
                                request
                                    .StudentProfileId!.Value)
                        .Where(
                            x =>
                                Matches(
                                    x.AcademicYearId,
                                    x.ClassGroupId,
                                    x.SubjectId))
                        .Where(
                            x =>
                                outcomes.ContainsKey(
                                    x.LearningOutcomeId))
                        .Where(
                            x =>
                                students.ContainsKey(
                                    x.StudentProfileId))
                        .OrderBy(
                            x =>
                                x.AcademicYearId)
                        .ThenBy(
                            x =>
                                ClassName(
                                    x.ClassGroupId))
                        .ThenBy(
                            x =>
                                SubjectName(
                                    x.SubjectId))
                        .ThenBy(
                            x =>
                                outcomes[
                                    x.LearningOutcomeId]
                                    .Code)
                        .Select(
                            x =>
                            {
                                var student =
                                    students[
                                        x.StudentProfileId];

                                var outcome =
                                    outcomes[
                                        x.LearningOutcomeId];

                                return new ReportRow(
                                [
                                    ReportCell.Text(
                                        student.StudentNumber),

                                    ReportCell.Text(
                                        student.DisplayName),

                                    ReportCell.Text(
                                        YearName(
                                            x.AcademicYearId)),

                                    ReportCell.Text(
                                        ClassName(
                                            x.ClassGroupId)),

                                    ReportCell.Text(
                                        SubjectName(
                                            x.SubjectId)),

                                    ReportCell.Text(
                                        outcome.Code),

                                    ReportCell.Text(
                                        outcome.Description),

                                    ReportCell.Decimal(
                                        x.EarnedScore),

                                    ReportCell.Decimal(
                                        x.PossibleScore),

                                    ReportCell.Percentage(
                                        x.MasteryPercentage),

                                    ReportCell.Integer(
                                        x.EvidenceCount)
                                ]);
                            });

                break;

            case ReportKind.Class:
            case ReportKind.Subject:
            case ReportKind.LearningOutcome:
                titleKey =
                    request.Kind switch
                    {
                        ReportKind.Class =>
                            "ReportTitleClass",

                        ReportKind.Subject =>
                            "ReportTitleSubject",

                        _ =>
                            "ReportTitleLearningOutcome"
                    };

                columns =
                    OutcomeSummaryColumns();

                source =
                    projection.ClassOutcomeSummaries
                        .Where(
                            x =>
                                Matches(
                                    x.AcademicYearId,
                                    x.ClassGroupId,
                                    x.SubjectId))
                        .Where(
                            x =>
                                !request.LearningOutcomeId
                                    .HasValue ||
                                x.LearningOutcomeId ==
                                request
                                    .LearningOutcomeId.Value)
                        .Where(
                            x =>
                                outcomes.ContainsKey(
                                    x.LearningOutcomeId))
                        .OrderBy(
                            x =>
                                YearName(
                                    x.AcademicYearId))
                        .ThenBy(
                            x =>
                                ClassName(
                                    x.ClassGroupId))
                        .ThenBy(
                            x =>
                                SubjectName(
                                    x.SubjectId))
                        .ThenBy(
                            x =>
                                outcomes[
                                    x.LearningOutcomeId]
                                    .Code)
                        .Select(
                            x =>
                            {
                                var outcome =
                                    outcomes[
                                        x.LearningOutcomeId];

                                return new ReportRow(
                                [
                                    ReportCell.Text(
                                        YearName(
                                            x.AcademicYearId)),

                                    ReportCell.Text(
                                        ClassName(
                                            x.ClassGroupId)),

                                    ReportCell.Text(
                                        SubjectName(
                                            x.SubjectId)),

                                    ReportCell.Text(
                                        outcome.Code),

                                    ReportCell.Text(
                                        outcome.Description),

                                    ReportCell.Percentage(
                                        x.AverageMasteryPercentage),

                                    ReportCell.Integer(
                                        x.StudentCount),

                                    ReportCell.Integer(
                                        x.AtRiskStudentCount),

                                    ReportCell.Integer(
                                        x.EvidenceCount),

                                    ReportCell.DateTime(
                                        x.CalculatedAtUtc)
                                ]);
                            });

                break;

            default:
                throw new InvalidOperationException(
                    "Unsupported report kind.");
        }

        var allRows =
            source.Take(maxRows + 1).ToArray();

        var truncated =
            allRows.Length > maxRows;

        var rows =
            truncated
                ? allRows
                    .Take(maxRows)
                    .ToArray()
                : allRows;

        return new ReportDocument(
            request.Kind,
            titleKey,
            DateTime.UtcNow,
            columns,
            rows,
            allRows.Length,
            truncated);
    }

    private static IReadOnlyList<ReportColumn>
        OutcomeSummaryColumns() =>
        [
            new(
                "ColumnAcademicYear",
                ReportCellKind.Text),

            new(
                "ColumnClass",
                ReportCellKind.Text),

            new(
                "ColumnSubject",
                ReportCellKind.Text),

            new(
                "ColumnOutcomeCode",
                ReportCellKind.Text),

            new(
                "ColumnOutcomeDescription",
                ReportCellKind.Text),

            new(
                "ColumnMastery",
                ReportCellKind.Percentage),

            new(
                "ColumnStudents",
                ReportCellKind.Integer),

            new(
                "ColumnAtRisk",
                ReportCellKind.Integer),

            new(
                "ColumnEvidence",
                ReportCellKind.Integer),

            new(
                "ColumnCalculatedAt",
                ReportCellKind.DateTime)
        ];

    private static IReadOnlyList<ReportColumn>
        MasteryColumns(
            bool includeStudent)
    {
        var result =
            new List<ReportColumn>();

        if (includeStudent)
        {
            result.Add(
                new(
                    "ColumnStudentNumber",
                    ReportCellKind.Text));

            result.Add(
                new(
                    "ColumnStudentName",
                    ReportCellKind.Text));
        }

        result.AddRange(
        [
            new(
                "ColumnAcademicYear",
                ReportCellKind.Text),

            new(
                "ColumnClass",
                ReportCellKind.Text),

            new(
                "ColumnSubject",
                ReportCellKind.Text),

            new(
                "ColumnOutcomeCode",
                ReportCellKind.Text),

            new(
                "ColumnOutcomeDescription",
                ReportCellKind.Text),

            new(
                "ColumnEarned",
                ReportCellKind.Decimal),

            new(
                "ColumnPossible",
                ReportCellKind.Decimal),

            new(
                "ColumnMastery",
                ReportCellKind.Percentage),

            new(
                "ColumnEvidence",
                ReportCellKind.Integer)
        ]);

        return result;
    }

    private sealed record Scope(
        SchoolUserRecord Actor,
        School School,
        string Role,
        AnalyticsProjectionSnapshot Projection,
        IReadOnlySet<(
            Guid AcademicYearId,
            Guid ClassGroupId,
            Guid SubjectId)> TeacherPairs,
        IReadOnlySet<Guid> SupervisedSubjectIds);
}
