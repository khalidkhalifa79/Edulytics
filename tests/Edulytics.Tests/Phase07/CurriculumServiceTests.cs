using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Edulytics.Services.Curriculum;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase07;

public sealed class CurriculumServiceTests
{
    [Fact]
    public async Task SchoolAdmin_CanCreateTopicAndOutcome()
    {
        using var f = CreateFixture();

        var topic = await CreateTopic(f);

        var result = await f.Service.CreateOutcomeAsync(
            f.Admin.Id,
            new CreateLearningOutcomeRequest(
                topic.Id,
                "G6.N.1",
                "Uses place value to reason about whole numbers.",
                25m,
                1));

        Assert.True(result.Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Admin.Id);
        var savedTopic = Assert.Single(dashboard.Value!.Topics);
        var outcome = Assert.Single(savedTopic.Outcomes);

        Assert.Equal("G6.N.1", outcome.Code);
        Assert.Equal(25m, outcome.Weight);
    }

    [Fact]
    public async Task Teacher_CannotManageCurriculum()
    {
        using var f = CreateFixture();

        var teacher = NewUser(f.School.Id, RoleNames.Teacher);
        f.Users.Seed(teacher);

        var result = await f.Service.GetDashboardAsync(teacher.Id);

        Assert.Null(result.Value);
        Assert.Equal(CurriculumErrorCode.AccessDenied, result.Error);
    }

    [Fact]
    public async Task CrossSchoolReferences_AreRejected()
    {
        using var f = CreateFixture();

        var other = NewSchool();
        var otherSubject = NewSubject(other.Id, "OTHER");
        var otherGrade = NewGrade(other.Id, "Other Grade", 66);

        f.Db.Schools.Add(other);
        f.Db.Subjects.Add(otherSubject);
        f.Db.GradeLevels.Add(otherGrade);
        await f.Db.SaveChangesAsync();

        var result = await f.Service.CreateTopicAsync(
            f.Admin.Id,
            new CreateCurriculumTopicRequest(
                otherSubject.Id,
                otherGrade.Id,
                "Cross tenant",
                1));

        Assert.False(result.Succeeded);
        Assert.Equal(CurriculumErrorCode.SubjectNotFound, result.Error);
    }

    [Fact]
    public async Task DuplicateOutcomeCode_IsCaseInsensitive()
    {
        using var f = CreateFixture();
        var topic = await CreateTopic(f);

        Assert.True((await f.Service.CreateOutcomeAsync(
            f.Admin.Id,
            new CreateLearningOutcomeRequest(
                topic.Id,
                "G6.A.1",
                "First",
                20m,
                1))).Succeeded);

        var duplicate = await f.Service.CreateOutcomeAsync(
            f.Admin.Id,
            new CreateLearningOutcomeRequest(
                topic.Id,
                "g6.a.1",
                "Duplicate",
                20m,
                2));

        Assert.False(duplicate.Succeeded);
        Assert.Equal(
            CurriculumErrorCode.DuplicateOutcomeCode,
            duplicate.Error);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100.001)]
    public async Task InvalidWeight_IsRejected(decimal weight)
    {
        using var f = CreateFixture();
        var topic = await CreateTopic(f);

        var result = await f.Service.CreateOutcomeAsync(
            f.Admin.Id,
            new CreateLearningOutcomeRequest(
                topic.Id,
                $"W{Guid.NewGuid():N}"[..12],
                "Weight test",
                weight,
                1));

        Assert.False(result.Succeeded);
        Assert.Equal(CurriculumErrorCode.InvalidWeight, result.Error);
    }

    [Fact]
    public async Task Service_DoesNotInventSumToHundredRule()
    {
        using var f = CreateFixture();
        var topic = await CreateTopic(f);

        Assert.True((await f.Service.CreateOutcomeAsync(
            f.Admin.Id,
            new CreateLearningOutcomeRequest(
                topic.Id,
                "SUM.A",
                "Outcome A",
                80m,
                1))).Succeeded);

        Assert.True((await f.Service.CreateOutcomeAsync(
            f.Admin.Id,
            new CreateLearningOutcomeRequest(
                topic.Id,
                "SUM.B",
                "Outcome B",
                80m,
                2))).Succeeded);
    }

    [Theory]
    [InlineData(SchoolStatus.Suspended)]
    [InlineData(SchoolStatus.Archived)]
    public async Task NonActiveSchool_CannotMutate(
        SchoolStatus status)
    {
        using var f = CreateFixture();

        f.School.Status = status;
        await f.Db.SaveChangesAsync();

        var result = await f.Service.CreateTopicAsync(
            f.Admin.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Blocked",
                2));

        Assert.False(result.Succeeded);
        Assert.Equal(CurriculumErrorCode.SchoolNotActive, result.Error);
    }

    [Fact]
    public async Task DuplicateTopicOrder_IsRejected()
    {
        using var f = CreateFixture();

        Assert.True((await f.Service.CreateTopicAsync(
            f.Admin.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Numbers",
                1))).Succeeded);

        var duplicate = await f.Service.CreateTopicAsync(
            f.Admin.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Geometry",
                1));

        Assert.False(duplicate.Succeeded);
        Assert.Equal(
            CurriculumErrorCode.DuplicateTopicOrder,
            duplicate.Error);
    }

    private static async Task<CurriculumTopicItem> CreateTopic(Fixture f)
    {
        var result = await f.Service.CreateTopicAsync(
            f.Admin.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Numbers",
                1));

        Assert.True(result.Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Admin.Id);
        return Assert.Single(dashboard.Value!.Topics);
    }

    private static Fixture CreateFixture()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase($"phase07-{Guid.NewGuid():N}")
                .Options;

        var db = new EdulyticsDbContext(options);

        var school = NewSchool();
        var subject = NewSubject(school.Id, "MATH");
        var grade = NewGrade(school.Id, "Grade 6", 6);

        db.Schools.Add(school);
        db.Subjects.Add(subject);
        db.GradeLevels.Add(grade);

        var framework = new CurriculumFramework
        {
            Id = Guid.Parse("07500000-0000-0000-0000-000000000001"),
            OwnerSchoolId = null,
            Code = "EDULYTICS-DEFAULT",
            NormalizedCode = "EDULYTICS-DEFAULT",
            Name = "Edulytics Default Curriculum",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var version = new CurriculumFrameworkVersion
        {
            Id = Guid.Parse("07500000-0000-0000-0000-000000000002"),
            FrameworkId = framework.Id,
            VersionCode = "V1",
            NormalizedVersionCode = "V1",
            Name = "Version 1",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.CurriculumFrameworks.Add(framework);
        db.CurriculumFrameworkVersions.Add(version);
        db.SaveChanges();

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        var users = new FakeUserRepository();
        var admin = NewUser(school.Id, RoleNames.SchoolAdmin);
        users.Seed(admin);

        var service = new CurriculumService(
            new CurriculumRepository(db),
            schools,
            users);

        return new Fixture(
            db,
            school,
            subject,
            grade,
            admin,
            schools,
            users,
            service);
    }

    private static School NewSchool()
    {
        var code = $"P7-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        return new School
        {
            Id = Guid.NewGuid(),
            Name = "Phase 07 School",
            SchoolCode = code,
            NormalizedSchoolCode = code,
            Status = SchoolStatus.Active,
            CountryCode = "PL",
            City = "Warsaw",
            ContactEmail = $"{Guid.NewGuid():N}@example.com",
            DefaultCulture = "en",
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = BitConverter.GetBytes(1L)
        };
    }

    private static Subject NewSubject(Guid schoolId, string code) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = "Mathematics",
            Code = code,
            NormalizedCode = code,
            Status = AcademicStructureStatus.Active,
            RowVersion = BitConverter.GetBytes(1L)
        };

    private static GradeLevel NewGrade(
        Guid schoolId,
        string name,
        int order) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Order = order
        };

    private static SchoolUserRecord NewUser(Guid schoolId, string role) =>
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
        EdulyticsDbContext Db,
        School School,
        Subject Subject,
        GradeLevel Grade,
        SchoolUserRecord Admin,
        FakeSchoolRepository Schools,
        FakeUserRepository Users,
        CurriculumService Service)
        : IDisposable
    {
        public void Dispose() => Db.Dispose();
    }

    private sealed class FakeSchoolRepository : ISchoolRepository
    {
        private readonly List<School> _schools = [];

        public void Seed(School school) => _schools.Add(school);

        public Task<IReadOnlyList<School>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<School>>(_schools.ToArray());

        public Task<School?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _schools.SingleOrDefault(x => x.Id == id));

        public Task<School?> GetForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            GetByIdAsync(id, cancellationToken);

        public Task<bool> ExistsByNormalizedCodeAsync(
            string normalizedSchoolCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _schools.Any(
                    x => x.NormalizedSchoolCode == normalizedSchoolCode));

        public Task AddAsync(
            School school,
            CancellationToken cancellationToken = default)
        {
            _schools.Add(school);
            return Task.CompletedTask;
        }

        public Task<SchoolRepositoryWriteResult> SaveAsync(
            School school,
            byte[]? expectedRowVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SchoolRepositoryWriteResult.Success);
    }

    private sealed class FakeUserRepository : ISchoolUserRepository
    {
        private readonly Dictionary<Guid, SchoolUserRecord> _users = [];

        public void Seed(SchoolUserRecord user) => _users[user.Id] = user;

        public Task<SchoolUserRecord?> GetActorAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.GetValueOrDefault(userId));

        public Task<IReadOnlyList<SchoolUserRecord>> ListBySchoolAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchoolUserRecord>>(
                _users.Values
                    .Where(x => x.SchoolId == schoolId)
                    .ToArray());

        public Task<SchoolUserRecord?> GetBySchoolAndIdAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = _users.GetValueOrDefault(userId);
            return Task.FromResult(
                user?.SchoolId == schoolId ? user : null);
        }

        public Task<SchoolUserPersistenceResult> CreateAsync(
            Guid schoolId,
            string email,
            string role,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> SetActiveAsync(
            Guid schoolId,
            Guid userId,
            bool isActive,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> SetLockedAsync(
            Guid schoolId,
            Guid userId,
            bool isLocked,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> SetRoleAsync(
            Guid schoolId,
            Guid userId,
            string role,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> GeneratePasswordSetupAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> CompletePasswordSetupAsync(
            Guid userId,
            string token,
            string newPassword,
            CancellationToken cancellationToken = default) =>
            Failure();

        private static Task<SchoolUserPersistenceResult> Failure() =>
            Task.FromResult(
                SchoolUserPersistenceResult.Failure(
                    SchoolUserPersistenceError.IdentityFailure));
    }
}
