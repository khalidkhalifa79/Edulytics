using Edulytics.Core.Analytics;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Realtime;
using Edulytics.Core.Users;
using Edulytics.Data.Contexts;
using Edulytics.Services.Analytics;
using Edulytics.Services.Auditing;
using Edulytics.Services.Realtime;
using Edulytics.Services.SubjectSupervisors;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase19;

public sealed class Phase19SubjectSupervisorTests
{
    [Fact]
    public void Model_HasRequiredUniqueAssignmentConstraint()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

        using var db =
            new EdulyticsDbContext(options);

        var entity =
            db.Model.FindEntityType(
                typeof(
                    SubjectSupervisorAssignment));

        Assert.NotNull(entity);

        var unique =
            entity!.GetIndexes()
                .Single(
                    x =>
                        x.IsUnique &&
                        x.Properties
                            .Select(p => p.Name)
                            .SequenceEqual(
                                new[]
                                {
                                    "SchoolId",
                                    "SupervisorUserId",
                                    "SubjectId"
                                }));

        Assert.True(unique.IsUnique);

        Assert.Contains(
            entity.GetForeignKeys(),
            fk =>
                fk.Properties
                    .Select(x => x.Name)
                    .SequenceEqual(
                        new[]
                        {
                            "SchoolId",
                            "SubjectId"
                        }));
    }

    [Fact]
    public async Task SchoolAdmin_CanCreateAssignment_AndAuditIt()
    {
        var fixture = NewFixture();

        var result =
            await fixture.Service.CreateAsync(
                fixture.Admin.Id,
                fixture.Supervisor.Id,
                fixture.SubjectA.Id);

        Assert.True(result.Succeeded);

        var assignment =
            Assert.Single(
                fixture.Assignments.Assignments);

        Assert.Equal(
            fixture.School.Id,
            assignment.SchoolId);

        Assert.Equal(
            fixture.Supervisor.Id,
            assignment.SupervisorUserId);

        Assert.Equal(
            fixture.SubjectA.Id,
            assignment.SubjectId);

        var audit =
            Assert.Single(fixture.Audit.Events);

        Assert.Equal(
            "SubjectSupervisorAssignment.Created",
            audit.Action);
    }

    [Fact]
    public async Task CrossSchoolSupervisor_IsNotAddressable()
    {
        var fixture = NewFixture();

        var foreignSchool = NewSchool();
        fixture.Schools.Seed(foreignSchool);

        var foreignSupervisor =
            NewUser(
                foreignSchool.Id,
                RoleNames.SubjectSupervisor);

        fixture.Users.Seed(
            foreignSupervisor);

        var result =
            await fixture.Service.CreateAsync(
                fixture.Admin.Id,
                foreignSupervisor.Id,
                fixture.SubjectA.Id);

        Assert.False(result.Succeeded);

        Assert.Equal(
            SubjectSupervisorErrorCode
                .SupervisorNotFound,
            result.Error);
    }

    [Fact]
    public async Task NonSchoolAdmin_CannotManageAssignments()
    {
        var fixture = NewFixture();

        var teacher =
            NewUser(
                fixture.School.Id,
                RoleNames.Teacher);

        fixture.Users.Seed(teacher);

        var result =
            await fixture.Service.CreateAsync(
                teacher.Id,
                fixture.Supervisor.Id,
                fixture.SubjectA.Id);

        Assert.False(result.Succeeded);

        Assert.Equal(
            SubjectSupervisorErrorCode.AccessDenied,
            result.Error);
    }

    [Fact]
    public async Task LockedSupervisor_IsNotEligible()
    {
        var fixture = NewFixture();

        fixture.Users.Seed(
            fixture.Supervisor with
            {
                IsLocked = true
            });

        var result =
            await fixture.Service.CreateAsync(
                fixture.Admin.Id,
                fixture.Supervisor.Id,
                fixture.SubjectA.Id);

        Assert.False(result.Succeeded);

        Assert.Equal(
            SubjectSupervisorErrorCode
                .SupervisorNotEligible,
            result.Error);
    }

    [Fact]
    public async Task DuplicateAssignment_IsRejected()
    {
        var fixture = NewFixture();

        fixture.Assignments.Assignments.Add(
            new SubjectSupervisorAssignment
            {
                Id = Guid.NewGuid(),
                SchoolId = fixture.School.Id,
                SupervisorUserId =
                    fixture.Supervisor.Id,
                SubjectId =
                    fixture.SubjectA.Id,
                CreatedAtUtc = DateTime.UtcNow
            });

        var result =
            await fixture.Service.CreateAsync(
                fixture.Admin.Id,
                fixture.Supervisor.Id,
                fixture.SubjectA.Id);

        Assert.False(result.Succeeded);

        Assert.Equal(
            SubjectSupervisorErrorCode
                .DuplicateAssignment,
            result.Error);
    }

    [Fact]
    public async Task OtherSchoolAssignment_CannotBeRemoved()
    {
        var fixture = NewFixture();

        var foreignSchool = NewSchool();

        var foreign =
            new SubjectSupervisorAssignment
            {
                Id = Guid.NewGuid(),
                SchoolId = foreignSchool.Id,
                SupervisorUserId = Guid.NewGuid(),
                SubjectId = Guid.NewGuid(),
                CreatedAtUtc = DateTime.UtcNow
            };

        fixture.Assignments.Assignments.Add(
            foreign);

        var result =
            await fixture.Service.RemoveAsync(
                fixture.Admin.Id,
                foreign.Id);

        Assert.False(result.Succeeded);

        Assert.Equal(
            SubjectSupervisorErrorCode
                .AssignmentNotFound,
            result.Error);

        Assert.Contains(
            foreign,
            fixture.Assignments.Assignments);
    }

    [Fact]
    public async Task SupervisorWithoutAssignment_CannotReadAnalytics()
    {
        var fixture = NewFixture();

        var analytics =
            NewAnalyticsService(
                fixture,
                BuildProjection(fixture));

        var result =
            await analytics.GetDashboardAsync(
                fixture.Supervisor.Id);

        Assert.Null(result.Value);

        Assert.Equal(
            AnalyticsErrorCode.AccessDenied,
            result.Error);
    }

    [Fact]
    public async Task Supervisor_SeesOnlyAssignedSubject_AndCannotRecalculate()
    {
        var fixture = NewFixture();

        fixture.Assignments.Assignments.Add(
            new SubjectSupervisorAssignment
            {
                Id = Guid.NewGuid(),
                SchoolId = fixture.School.Id,
                SupervisorUserId =
                    fixture.Supervisor.Id,
                SubjectId =
                    fixture.SubjectA.Id,
                CreatedAtUtc = DateTime.UtcNow
            });

        var analytics =
            NewAnalyticsService(
                fixture,
                BuildProjection(fixture));

        var result =
            await analytics.GetDashboardAsync(
                fixture.Supervisor.Id);

        var dashboard =
            Assert.IsType<AnalyticsDashboard>(
                result.Value);

        var subject =
            Assert.Single(dashboard.Subjects);

        Assert.Equal(
            fixture.SubjectA.Id,
            subject.Id);

        Assert.False(
            dashboard.CanRecalculate);

        var denied =
            await analytics.GetDashboardAsync(
                fixture.Supervisor.Id,
                subjectId:
                    fixture.SubjectB.Id);

        Assert.Equal(
            AnalyticsErrorCode.AccessDenied,
            denied.Error);

        var recalculate =
            await analytics.RecalculateAsync(
                fixture.Supervisor.Id);

        Assert.False(recalculate.Succeeded);

        Assert.Equal(
            AnalyticsErrorCode
                .RecalculationRequiresSchoolAdmin,
            recalculate.Error);
    }

    [Fact]
    public async Task SupervisorRealtimeMembership_IsSubjectScoped()
    {
        var fixture = NewFixture();

        fixture.Assignments.Assignments.Add(
            new SubjectSupervisorAssignment
            {
                Id = Guid.NewGuid(),
                SchoolId = fixture.School.Id,
                SupervisorUserId =
                    fixture.Supervisor.Id,
                SubjectId =
                    fixture.SubjectA.Id,
                CreatedAtUtc = DateTime.UtcNow
            });

        var service =
            new RealtimeGroupService(
                fixture.Users,
                fixture.Schools,
                new FakeRealtimeAccessRepository(),
                fixture.Assignments);

        var result =
            await service.ResolveGroupsAsync(
                fixture.Supervisor.Id);

        Assert.True(result.Succeeded);

        Assert.Contains(
            RealtimeGroupNames.SubjectSupervisors(
                fixture.School.Id,
                fixture.SubjectA.Id),
            result.Groups);

        Assert.Contains(
            RealtimeGroupNames.SchoolAnalytics(
                fixture.School.Id),
            result.Groups);

        Assert.DoesNotContain(
            result.Groups,
            x => x.EndsWith(
                ":teachers",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task SupervisorRealtimeWithoutAssignment_IsDenied()
    {
        var fixture = NewFixture();

        var service =
            new RealtimeGroupService(
                fixture.Users,
                fixture.Schools,
                new FakeRealtimeAccessRepository(),
                fixture.Assignments);

        var result =
            await service.ResolveGroupsAsync(
                fixture.Supervisor.Id);

        Assert.False(result.Succeeded);
    }

    private static AnalyticsService NewAnalyticsService(
        Fixture fixture,
        AnalyticsProjectionSnapshot projection) =>
        new(
            new FakeAnalyticsRepository(projection),
            fixture.Schools,
            fixture.Users,
            new AnalyticsProjectionBuilder(),
            fixture.Assignments);

    private static AnalyticsProjectionSnapshot
        BuildProjection(
            Fixture fixture)
    {
        var year =
            new AcademicYear
            {
                Id = Guid.NewGuid(),
                SchoolId = fixture.School.Id,
                Name = "2026/27",
                StartsOn =
                    new DateOnly(2026, 9, 1),
                EndsOn =
                    new DateOnly(2027, 6, 30),
                Status =
                    AcademicStructureStatus.Active
            };

        var classGroup =
            new ClassGroup
            {
                Id = Guid.NewGuid(),
                SchoolId = fixture.School.Id,
                AcademicYearId = year.Id,
                GradeLevelId = Guid.NewGuid(),
                Name = "A",
                Code = "A",
                NormalizedCode = "A",
                Status =
                    AcademicStructureStatus.Active
            };

        return new AnalyticsProjectionSnapshot(
            [year],
            [classGroup],
            [
                fixture.SubjectA,
                fixture.SubjectB
            ],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            [],
            []);
    }

    private static Fixture NewFixture()
    {
        var school = NewSchool();

        var admin =
            NewUser(
                school.Id,
                RoleNames.SchoolAdmin);

        var supervisor =
            NewUser(
                school.Id,
                RoleNames.SubjectSupervisor);

        var subjectA =
            NewSubject(
                school.Id,
                "MAT",
                "Mathematics");

        var subjectB =
            NewSubject(
                school.Id,
                "SCI",
                "Science");

        var users = new FakeUserRepository();
        users.Seed(admin);
        users.Seed(supervisor);

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        var assignments =
            new FakeAssignmentRepository();

        assignments.Subjects.Add(subjectA);
        assignments.Subjects.Add(subjectB);

        var audit = new FakeAuditService();

        var service =
            new SubjectSupervisorAssignmentService(
                assignments,
                users,
                schools,
                audit);

        return new Fixture(
            school,
            admin,
            supervisor,
            subjectA,
            subjectB,
            users,
            schools,
            assignments,
            audit,
            service);
    }

    private static School NewSchool() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Test School",
            SchoolCode = Guid.NewGuid()
                .ToString("N")[..8]
                .ToUpperInvariant(),
            NormalizedSchoolCode =
                Guid.NewGuid()
                    .ToString("N")[..8]
                    .ToUpperInvariant(),
            Status = SchoolStatus.Active,
            CountryCode = "PL",
            City = "Warsaw",
            ContactEmail =
                "school@example.com",
            DefaultCulture = "en",
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    private static Subject NewSubject(
        Guid schoolId,
        string code,
        string name) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Code = code,
            NormalizedCode =
                code.ToUpperInvariant(),
            Status =
                AcademicStructureStatus.Active
        };

    private static SchoolUserRecord NewUser(
        Guid schoolId,
        string role) =>
        new(
            Guid.NewGuid(),
            schoolId,
            $"{Guid.NewGuid():N}@example.com",
            true,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            [role]);

    private sealed record Fixture(
        School School,
        SchoolUserRecord Admin,
        SchoolUserRecord Supervisor,
        Subject SubjectA,
        Subject SubjectB,
        FakeUserRepository Users,
        FakeSchoolRepository Schools,
        FakeAssignmentRepository Assignments,
        FakeAuditService Audit,
        SubjectSupervisorAssignmentService Service);

    private sealed class FakeAssignmentRepository
        : ISubjectSupervisorAssignmentRepository
    {
        public List<SubjectSupervisorAssignment>
            Assignments { get; } = [];

        public List<Subject> Subjects { get; } = [];

        public Task<IReadOnlyList<SubjectSupervisorAssignment>>
            ListBySchoolAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<
                SubjectSupervisorAssignment>>(
                Assignments
                    .Where(x => x.SchoolId == schoolId)
                    .ToArray());

        public Task<IReadOnlyList<SubjectSupervisorAssignment>>
            ListActiveBySupervisorAsync(
                Guid schoolId,
                Guid supervisorUserId,
                CancellationToken cancellationToken = default)
        {
            var activeSubjectIds =
                Subjects
                    .Where(
                        x =>
                            x.SchoolId == schoolId &&
                            x.Status ==
                                AcademicStructureStatus.Active)
                    .Select(x => x.Id)
                    .ToHashSet();

            return Task.FromResult<IReadOnlyList<
                SubjectSupervisorAssignment>>(
                Assignments
                    .Where(
                        x =>
                            x.SchoolId == schoolId &&
                            x.SupervisorUserId ==
                                supervisorUserId &&
                            activeSubjectIds.Contains(
                                x.SubjectId))
                    .ToArray());
        }

        public Task<IReadOnlyList<Subject>>
            ListSubjectsAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Subject>>(
                Subjects
                    .Where(x => x.SchoolId == schoolId)
                    .ToArray());

        public Task<Subject?> GetSubjectAsync(
            Guid schoolId,
            Guid subjectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Subjects.SingleOrDefault(
                    x =>
                        x.SchoolId == schoolId &&
                        x.Id == subjectId));

        public Task<SubjectSupervisorAssignment?>
            GetBySchoolAndIdAsync(
                Guid schoolId,
                Guid assignmentId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Assignments.SingleOrDefault(
                    x =>
                        x.SchoolId == schoolId &&
                        x.Id == assignmentId));

        public Task<bool> ExistsAsync(
            Guid schoolId,
            Guid supervisorUserId,
            Guid subjectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Assignments.Any(
                    x =>
                        x.SchoolId == schoolId &&
                        x.SupervisorUserId ==
                            supervisorUserId &&
                        x.SubjectId == subjectId));

        public Task AddAsync(
            SubjectSupervisorAssignment assignment,
            CancellationToken cancellationToken = default)
        {
            Assignments.Add(assignment);
            return Task.CompletedTask;
        }

        public void Remove(
            SubjectSupervisorAssignment assignment) =>
            Assignments.Remove(assignment);

        public Task<bool> SaveAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class FakeUserRepository
        : ISchoolUserRepository
    {
        private readonly Dictionary<
            Guid,
            SchoolUserRecord> _users = [];

        public void Seed(
            SchoolUserRecord user) =>
            _users[user.Id] = user;

        public Task<SchoolUserRecord?> GetActorAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _users.GetValueOrDefault(userId));

        public Task<IReadOnlyList<SchoolUserRecord>>
            ListBySchoolAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchoolUserRecord>>(
                _users.Values
                    .Where(x => x.SchoolId == schoolId)
                    .ToArray());

        public Task<SchoolUserRecord?>
            GetBySchoolAndIdAsync(
                Guid schoolId,
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var user =
                _users.GetValueOrDefault(userId);

            return Task.FromResult(
                user?.SchoolId == schoolId
                    ? user
                    : null);
        }

        public Task<SchoolUserPersistenceResult> CreateAsync(
            Guid schoolId,
            string email,
            string role,
            CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult> SetActiveAsync(
            Guid schoolId,
            Guid userId,
            bool isActive,
            CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult> SetLockedAsync(
            Guid schoolId,
            Guid userId,
            bool isLocked,
            CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult> SetRoleAsync(
            Guid schoolId,
            Guid userId,
            string role,
            CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult>
            GeneratePasswordSetupAsync(
                Guid schoolId,
                Guid userId,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult>
            CompletePasswordSetupAsync(
                Guid userId,
                string token,
                string newPassword,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        private static Task<SchoolUserPersistenceResult>
            Unsupported() =>
            Task.FromResult(
                SchoolUserPersistenceResult.Failure(
                    SchoolUserPersistenceError.NotFound));
    }

    private sealed class FakeSchoolRepository
        : ISchoolRepository
    {
        private readonly Dictionary<Guid, School>
            _schools = [];

        public void Seed(School school) =>
            _schools[school.Id] = school;

        public Task<IReadOnlyList<School>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<School>>(
                _schools.Values.ToArray());

        public Task<School?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _schools.GetValueOrDefault(id));

        public Task<School?> GetForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            GetByIdAsync(id, cancellationToken);

        public Task<bool> ExistsByNormalizedCodeAsync(
            string normalizedSchoolCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(
            School school,
            CancellationToken cancellationToken = default)
        {
            Seed(school);
            return Task.CompletedTask;
        }

        public Task<SchoolRepositoryWriteResult> SaveAsync(
            School school,
            byte[]? expectedRowVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                SchoolRepositoryWriteResult.Success);
    }

    private sealed class FakeAuditService
        : IAuditService
    {
        public List<AuditEvent> Events { get; } = [];

        public Task QueueAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }

        public Task RecordAsync(
            AuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAnalyticsRepository
        : IAnalyticsRepository
    {
        private readonly AnalyticsProjectionSnapshot
            _projection;

        public FakeAnalyticsRepository(
            AnalyticsProjectionSnapshot projection)
        {
            _projection = projection;
        }

        public Task<AnalyticsSourceSnapshot>
            GetSourceSnapshotAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                new AnalyticsSourceSnapshot(
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    [],
                    []));

        public Task<AnalyticsProjectionSnapshot>
            GetProjectionSnapshotAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(_projection);

        public Task<DateTime?> GetLatestSourceUpdateAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<DateTime?>(null);

        public Task<AnalyticsPersistenceResult>
            ReplaceProjectionsAsync(
                Guid schoolId,
                AnalyticsProjectionSet projections,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                AnalyticsPersistenceResult.Success());
    }

    private sealed class FakeRealtimeAccessRepository
        : IRealtimeAccessRepository
    {
        public Task<IReadOnlyList<TeacherAssignment>>
            GetTeacherAssignmentsAsync(
                Guid schoolId,
                Guid teacherUserId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<
                TeacherAssignment>>([]);
    }
}
