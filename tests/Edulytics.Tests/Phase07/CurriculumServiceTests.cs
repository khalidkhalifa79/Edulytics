using Edulytics.Core.Constants;
using Edulytics.Core.Curriculum;
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
    public async Task OfficialOutcome_IsSelectedFromActivePack_AndCodeIsAutomatic()
    {
        using var f = CreateFixture();
        var topic = await CreateTopic(f);
        var option = Assert.Single(topic.OfficialOutcomes);

        var result = await f.Service.CreateOfficialOutcomeAsync(
            f.Admin.Id,
            new CreateOfficialLearningOutcomeRequest(
                topic.Id,
                option.ContentNodeId,
                option.LessonNodeId,
                25m,
                1));

        Assert.True(result.Succeeded);

        var saved = Assert.Single(await f.Db.LearningOutcomes
            .AsNoTracking()
            .ToListAsync());
        Assert.Equal(f.OfficialNode.Id, saved.OfficialContentNodeId);
        Assert.Equal("PL:VI:ADDITION:001", saved.Code);
        Assert.Equal("Add whole numbers accurately.", saved.Description);

        var dashboard = await f.Service.GetDashboardAsync(f.Admin.Id);
        var savedTopic = Assert.Single(dashboard.Value!.Topics);
        Assert.Equal(
            "Polish National Curriculum Mathematics",
            savedTopic.FrameworkDisplayName);
        Assert.True(Assert.Single(savedTopic.Outcomes).IsOfficial);
    }

    [Fact]
    public async Task OfficialOutcome_CodeAndDescriptionCannotBeEdited()
    {
        using var f = CreateFixture();
        var topic = await CreateTopic(f);
        var option = Assert.Single(topic.OfficialOutcomes);

        Assert.True((await f.Service.CreateOfficialOutcomeAsync(
            f.Admin.Id,
            new CreateOfficialLearningOutcomeRequest(
                topic.Id,
                option.ContentNodeId,
                option.LessonNodeId,
                25m,
                1))).Succeeded);

        var outcome = Assert.Single(await f.Db.LearningOutcomes.ToListAsync());
        var update = await f.Service.UpdateOutcomeAsync(
            f.Admin.Id,
            new UpdateLearningOutcomeRequest(
                outcome.Id,
                "FAKE.CODE",
                "Changed",
                30m,
                2));

        Assert.False(update.Succeeded);
        Assert.Equal(
            CurriculumErrorCode.OfficialOutcomeReadOnly,
            update.Error);
    }


    [Fact]
    public async Task SchoolAdmin_CanSelectVerifiedFramework_ThenCreateTopic()
    {
        using var f = CreateFixture(withAdoption: false);

        var selected = await f.Service.SelectFrameworkAsync(
            f.Admin.Id,
            new SelectCurriculumFrameworkRequest(
                f.Subject.Id,
                f.Grade.Id,
                MathematicsCurriculumPackRegistry.PolandCode));

        Assert.True(selected.Succeeded);

        var created = await f.Service.CreateTopicAsync(
            f.Admin.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Numbers",
                1));

        Assert.True(created.Succeeded);

        var adoption = Assert.Single(
            await f.Db.SchoolCurriculumAdoptions
                .AsNoTracking()
                .ToListAsync());

        Assert.Equal(
            f.FrameworkVersion.Id,
            adoption.FrameworkVersionId);
    }

    [Fact]
    public async Task CreateTopic_RequiresExplicitCurriculumSelection()
    {
        using var f = CreateFixture(withAdoption: false);

        var result = await f.Service.CreateTopicAsync(
            f.Admin.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Numbers",
                1));

        Assert.False(result.Succeeded);
        Assert.Equal(
            CurriculumErrorCode.CurriculumNotSelected,
            result.Error);
    }

    [Fact]
    public async Task UnapprovedLegacyDefaultFramework_IsRejected()
    {
        using var f = CreateFixture(withAdoption: false);

        var result = await f.Service.SelectFrameworkAsync(
            f.Admin.Id,
            new SelectCurriculumFrameworkRequest(
                f.Subject.Id,
                f.Grade.Id,
                "EDULYTICS-DEFAULT"));

        Assert.False(result.Succeeded);
        Assert.Equal(
            CurriculumErrorCode.FrameworkNotFound,
            result.Error);
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

    private static Fixture CreateFixture(
        bool withAdoption = true)
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
            Code = MathematicsCurriculumPackRegistry.PolandCode,
            NormalizedCode = MathematicsCurriculumPackRegistry.PolandCode,
            Name = "Polish National Curriculum Mathematics",
            CountryCode = "PL",
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

        var officialNode = new CurriculumPackContentNode
        {
            Id = Guid.Parse("07500000-0000-0000-0000-000000000003"),
            FrameworkVersionId = version.Id,
            FrameworkCode = MathematicsCurriculumPackRegistry.PolandCode,
            VersionCode = "V1",
            NodeKind = "Outcome",
            Code = "PL:REQ:PL:VI:ADDITION:001",
            LogicalLevelFrom = 6,
            LogicalLevelTo = 6,
            NativeLevel = "Klasa VI",
            Title = "PL:VI:ADDITION:001",
            OfficialText = "Add whole numbers accurately.",
            SourceAuthority = "Official test authority",
            SourceUrl = "https://example.com/official",
            SourceLocator = "PL:VI:ADDITION:001",
            Attribution = "Official test content.",
            IsOfficial = true,
            IsActive = true,
            SortOrder = 1,
            ContentHash = new string('a', 64),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = BitConverter.GetBytes(1L)
        };
        db.CurriculumPackContentNodes.Add(officialNode);

        if (withAdoption)
        {
            db.SchoolCurriculumAdoptions.Add(
                new SchoolCurriculumAdoption
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school.Id,
                    AcademicYearId = null,
                    GradeLevelId = grade.Id,
                    SubjectId = subject.Id,
                    FrameworkVersionId = version.Id,
                    IsPrimary = true,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                    RowVersion = BitConverter.GetBytes(1L)
                });
        }

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
            service,
            version,
            officialNode);
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
        CurriculumService Service,
        CurriculumFrameworkVersion FrameworkVersion,
        CurriculumPackContentNode OfficialNode)
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
