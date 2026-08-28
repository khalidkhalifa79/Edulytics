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
            f.Supervisor.Id,
            new CreateOfficialLearningOutcomeRequest(
                topic.Id,
                option.ContentNodeId,
                option.LessonNodeId,
                1
));

        Assert.True(result.Succeeded);

        var saved = Assert.Single(await f.Db.LearningOutcomes
            .AsNoTracking()
            .ToListAsync());
        Assert.Equal(f.OfficialNode.Id, saved.OfficialContentNodeId);
        Assert.Equal("PL:VI:ADDITION:001", saved.Code);
        Assert.Equal("Add whole numbers accurately.", saved.Description);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
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
            f.Supervisor.Id,
            new CreateOfficialLearningOutcomeRequest(
                topic.Id,
                option.ContentNodeId,
                option.LessonNodeId,
                1
))).Succeeded);

        var outcome = Assert.Single(await f.Db.LearningOutcomes.ToListAsync());
        var update = await f.Service.UpdateOutcomeAsync(
            f.Supervisor.Id,
            new UpdateLearningOutcomeRequest(
                outcome.Id,
                "FAKE.CODE",
                "Changed",
                2
));

        Assert.False(update.Succeeded);
        Assert.Equal(
            CurriculumErrorCode.OfficialOutcomeReadOnly,
            update.Error);
    }


    [Fact]
    public async Task SubjectSupervisor_CanSelectVerifiedFramework_ThenCreateTopic()
    {
        using var f = CreateFixture(withAdoption: false);

        var selected = await f.Service.SelectFrameworkAsync(
            f.Supervisor.Id,
            new SelectCurriculumFrameworkRequest(
                f.Subject.Id,
                f.Grade.Id,
                MathematicsCurriculumPackRegistry.PolandCode));

        Assert.True(selected.Succeeded);

        var created = await f.Service.CreateTopicAsync(
            f.Supervisor.Id,
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
            f.Supervisor.Id,
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
            f.Supervisor.Id,
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
    public async Task TeacherAndSchoolAdmin_CanRead_ButCannotManageCurriculum()
    {
        using var f = CreateFixture();

        var teacher =
            NewUser(
                f.School.Id,
                RoleNames.Teacher);

        f.Users.Seed(teacher);

        Assert.NotNull(
            (await f.Service.GetDashboardAsync(teacher.Id)).Value);

        Assert.NotNull(
            (await f.Service.GetDashboardAsync(f.Admin.Id)).Value);

        foreach (var actorId in new[] { teacher.Id, f.Admin.Id })
        {
            var denied =
                await f.Service.CreateTopicAsync(
                    actorId,
                    new CreateCurriculumTopicRequest(
                        f.Subject.Id,
                        f.Grade.Id,
                        "Blocked",
                        99));

            Assert.False(denied.Succeeded);
            Assert.Equal(
                CurriculumErrorCode.AccessDenied,
                denied.Error);
        }
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
            f.Supervisor.Id,
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
            f.Supervisor.Id,
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
            f.Supervisor.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Numbers",
                1))).Succeeded);

        var duplicate = await f.Service.CreateTopicAsync(
            f.Supervisor.Id,
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


    [Fact]
    public async Task SameGradeSubject_CanUseIndependentCurriculumScopes_PerProgram()
    {
        using var f = CreateFixture(withAdoption: false);

        var mainProgram = await f.Db.AcademicPrograms
            .SingleAsync(x => x.SchoolId == f.School.Id && x.IsDefault);

        var secondProgram = new AcademicProgram
        {
            Id = Guid.NewGuid(),
            SchoolId = f.School.Id,
            Name = "Second Stream",
            Code = "SECOND",
            NormalizedCode = "SECOND",
            Status = AcademicStructureStatus.Active,
            IsDefault = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = BitConverter.GetBytes(1L)
        };

        f.Db.AcademicPrograms.Add(secondProgram);
        await f.Db.SaveChangesAsync();

        var selectMain = await f.Service.SelectFrameworkAsync(
            f.Supervisor.Id,
            new SelectCurriculumFrameworkRequest(
                f.Subject.Id,
                f.Grade.Id,
                MathematicsCurriculumPackRegistry.PolandCode,
                mainProgram.Id));

        var selectSecond = await f.Service.SelectFrameworkAsync(
            f.Supervisor.Id,
            new SelectCurriculumFrameworkRequest(
                f.Subject.Id,
                f.Grade.Id,
                MathematicsCurriculumPackRegistry.PolandCode,
                secondProgram.Id));

        Assert.True(selectMain.Succeeded);
        Assert.True(selectSecond.Succeeded);

        var topicMain = await f.Service.CreateTopicAsync(
            f.Supervisor.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Numbers",
                1,
                mainProgram.Id));

        var topicSecond = await f.Service.CreateTopicAsync(
            f.Supervisor.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Numbers",
                1,
                secondProgram.Id));

        Assert.True(topicMain.Succeeded);
        Assert.True(topicSecond.Succeeded);

        var duplicateMain = await f.Service.CreateTopicAsync(
            f.Supervisor.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Another name",
                1,
                mainProgram.Id));

        Assert.False(duplicateMain.Succeeded);
        Assert.Equal(
            CurriculumErrorCode.DuplicateTopicOrder,
            duplicateMain.Error);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);

        Assert.Equal(2, dashboard.Value!.AcademicPrograms.Count);
        Assert.Equal(2, dashboard.Value.Adoptions.Count);
        Assert.Equal(2, dashboard.Value.Topics.Count);

        var mainTopic = dashboard.Value.Topics.Single(
            x => x.AcademicProgramId == mainProgram.Id);
        var secondTopic = dashboard.Value.Topics.Single(
            x => x.AcademicProgramId == secondProgram.Id);

        Assert.Equal("Phase 07 Default Program", mainTopic.AcademicProgramName);
        Assert.Equal("Second Stream", secondTopic.AcademicProgramName);

        var mainOption = Assert.Single(mainTopic.OfficialOutcomes);
        var secondOption = Assert.Single(secondTopic.OfficialOutcomes);

        var mainOutcome = await f.Service.CreateOfficialOutcomeAsync(
            f.Supervisor.Id,
            new CreateOfficialLearningOutcomeRequest(
                mainTopic.Id,
                mainOption.ContentNodeId,
                mainOption.LessonNodeId,
                1));

        var secondOutcome = await f.Service.CreateOfficialOutcomeAsync(
            f.Supervisor.Id,
            new CreateOfficialLearningOutcomeRequest(
                secondTopic.Id,
                secondOption.ContentNodeId,
                secondOption.LessonNodeId,
                1));

        Assert.True(mainOutcome.Succeeded);
        Assert.True(secondOutcome.Succeeded);

        var outcomes = await f.Db.LearningOutcomes
            .AsNoTracking()
            .OrderBy(x => x.AcademicProgramId)
            .ToArrayAsync();

        Assert.Equal(2, outcomes.Length);
        Assert.Equal(
            outcomes[0].Code,
            outcomes[1].Code);
        Assert.NotEqual(
            outcomes[0].AcademicProgramId,
            outcomes[1].AcademicProgramId);
    }

    [Fact]
    public async Task ForeignAcademicProgram_IsRejectedByCurriculumSelectionAndTopicCreation()
    {
        using var f = CreateFixture(withAdoption: false);

        var other = NewSchool();
        f.Db.Schools.Add(other);

        var foreignProgram = new AcademicProgram
        {
            Id = Guid.NewGuid(),
            SchoolId = other.Id,
            Name = "Foreign Stream",
            Code = "FOREIGN",
            NormalizedCode = "FOREIGN",
            Status = AcademicStructureStatus.Active,
            IsDefault = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = BitConverter.GetBytes(1L)
        };

        f.Db.AcademicPrograms.Add(foreignProgram);
        await f.Db.SaveChangesAsync();

        var select = await f.Service.SelectFrameworkAsync(
            f.Supervisor.Id,
            new SelectCurriculumFrameworkRequest(
                f.Subject.Id,
                f.Grade.Id,
                MathematicsCurriculumPackRegistry.PolandCode,
                foreignProgram.Id));

        Assert.False(select.Succeeded);
        Assert.Equal(
            CurriculumErrorCode.AcademicProgramNotFound,
            select.Error);

        var topic = await f.Service.CreateTopicAsync(
            f.Supervisor.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Blocked",
                1,
                foreignProgram.Id));

        Assert.False(topic.Succeeded);
        Assert.Equal(
            CurriculumErrorCode.AcademicProgramNotFound,
            topic.Error);
    }

    [Fact]
    public async Task CurriculumDashboard_ReportsAdoptionProgramMetadata()
    {
        using var f = CreateFixture();

        var program = await f.Db.AcademicPrograms
            .SingleAsync(x => x.SchoolId == f.School.Id && x.IsDefault);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);

        var adoption = Assert.Single(dashboard.Value!.Adoptions);

        Assert.Equal(program.Id, adoption.AcademicProgramId);
        Assert.Equal("Phase 07 Default Program", adoption.AcademicProgramName);
        Assert.Equal("MAIN", adoption.AcademicProgramCode);
    }


    [Fact]
    public async Task CurriculumReadUpdateAndRepositoryQueries_CoverProgramScopedPaths()
    {
        using var f = CreateFixture();

        var topic = await CreateTopic(f);

        var readTopic = await f.Service.GetTopicAsync(
            f.Supervisor.Id,
            topic.Id);

        Assert.NotNull(readTopic.Value);
        Assert.NotEqual(
            Guid.Empty,
            readTopic.Value!.AcademicProgramId);

        var updated = await f.Service.UpdateTopicAsync(
            f.Supervisor.Id,
            new UpdateCurriculumTopicRequest(
                topic.Id,
                "Numbers updated",
                2));

        Assert.True(updated.Succeeded);

        var refreshed = await f.Service.GetTopicAsync(
            f.Supervisor.Id,
            topic.Id);

        Assert.Equal("Numbers updated", refreshed.Value!.Name);
        Assert.Equal(2, refreshed.Value.Order);

        var option = Assert.Single(refreshed.Value.OfficialOutcomes);

        var outcomeCreate = await f.Service.CreateOfficialOutcomeAsync(
            f.Supervisor.Id,
            new CreateOfficialLearningOutcomeRequest(
                refreshed.Value.Id,
                option.ContentNodeId,
                option.LessonNodeId,
                1));

        Assert.True(outcomeCreate.Succeeded);

        var savedOutcome = Assert.Single(
            await f.Db.LearningOutcomes
                .AsNoTracking()
                .ToListAsync());

        var readOutcome = await f.Service.GetOutcomeAsync(
            f.Supervisor.Id,
            savedOutcome.Id);

        Assert.NotNull(readOutcome.Value);
        Assert.True(readOutcome.Value!.IsOfficial);

        var repo = new CurriculumRepository(f.Db);

        var program = await f.Db.AcademicPrograms
            .SingleAsync(
                x => x.SchoolId == f.School.Id && x.IsDefault);

        Assert.NotNull(
            await repo.GetAcademicProgramAsync(
                f.School.Id,
                program.Id));

        Assert.NotNull(
            await repo.GetDefaultAcademicProgramAsync(
                f.School.Id));

        Assert.NotNull(
            await repo.GetPrimaryAdoptionAsync(
                f.School.Id,
                program.Id,
                f.Grade.Id,
                f.Subject.Id));

        Assert.Equal(
            f.FrameworkVersion.Id,
            await repo.GetPrimaryFrameworkVersionIdAsync(
                f.School.Id,
                program.Id,
                f.Grade.Id,
                f.Subject.Id));

        Assert.True(
            await repo.TopicNameExistsInProgramAsync(
                f.School.Id,
                program.Id,
                f.FrameworkVersion.Id,
                f.Subject.Id,
                f.Grade.Id,
                "NUMBERS UPDATED"));

        Assert.True(
            await repo.TopicOrderExistsInProgramAsync(
                f.School.Id,
                program.Id,
                f.FrameworkVersion.Id,
                f.Subject.Id,
                f.Grade.Id,
                2));

        Assert.True(
            await repo.OutcomeCodeExistsInProgramAsync(
                f.School.Id,
                program.Id,
                f.FrameworkVersion.Id,
                f.Subject.Id,
                f.Grade.Id,
                savedOutcome.Code));

        Assert.False(
            await repo.TopicNameExistsInProgramAsync(
                f.School.Id,
                Guid.NewGuid(),
                f.FrameworkVersion.Id,
                f.Subject.Id,
                f.Grade.Id,
                "NUMBERS UPDATED"));
    }

    [Fact]
    public async Task CurriculumValidationBranches_CoverRequiredDuplicateAndMissingRows()
    {
        using var f = CreateFixture();

        var blank = await f.Service.CreateTopicAsync(
            f.Supervisor.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                " ",
                1));

        Assert.False(blank.Succeeded);

        var badOrder = await f.Service.CreateTopicAsync(
            f.Supervisor.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Bad order",
                0));

        Assert.False(badOrder.Succeeded);

        Assert.True((await f.Service.CreateTopicAsync(
            f.Supervisor.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Numbers",
                1))).Succeeded);

        var duplicateName = await f.Service.CreateTopicAsync(
            f.Supervisor.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Numbers",
                2));

        Assert.False(duplicateName.Succeeded);

        var missingTopic = await f.Service.GetTopicAsync(
            f.Supervisor.Id,
            Guid.NewGuid());

        Assert.Null(missingTopic.Value);

        var missingOutcome = await f.Service.GetOutcomeAsync(
            f.Supervisor.Id,
            Guid.NewGuid());

        Assert.Null(missingOutcome.Value);
    }

    private static async Task<CurriculumTopicItem> CreateTopic(Fixture f)
    {
        var result = await f.Service.CreateTopicAsync(
            f.Supervisor.Id,
            new CreateCurriculumTopicRequest(
                f.Subject.Id,
                f.Grade.Id,
                "Numbers",
                1));

        Assert.True(result.Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
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
        var program = new AcademicProgram
        {
            Id = Guid.NewGuid(),
            SchoolId = school.Id,
            Name = "Phase 07 Default Program",
            Code = "MAIN",
            NormalizedCode = "MAIN",
            Status = AcademicStructureStatus.Active,
            IsDefault = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = BitConverter.GetBytes(1L)
        };

        db.Schools.Add(school);
        db.Subjects.Add(subject);
        db.GradeLevels.Add(grade);
        db.AcademicPrograms.Add(program);

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
                    AcademicProgramId = program.Id,
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
        var supervisor = NewUser(
            school.Id,
            RoleNames.SubjectSupervisor);

        users.Seed(admin);
        users.Seed(supervisor);

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
            supervisor,
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
        SchoolUserRecord Supervisor,
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
