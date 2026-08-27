using System.Reflection;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.StudentPortal;
using Edulytics.Core.Users;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Edulytics.Services.StudentPortal;
using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase28;

public sealed class Phase28StudentPortalTests
{
    [Fact]
    public async Task Repository_ResolvesLinkedStudentInsideExactSchoolOnly()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    $"phase28-repo-{Guid.NewGuid():N}")
                .Options;

        await using var db =
            new EdulyticsDbContext(options);

        var schoolA = NewSchool("A");
        var schoolB = NewSchool("B");
        var userId = Guid.NewGuid();

        db.Schools.AddRange(schoolA, schoolB);

        db.StudentProfiles.AddRange(
            NewProfile(schoolA.Id, userId, "A-001"),
            NewProfile(schoolB.Id, Guid.NewGuid(), "B-001"));

        await db.SaveChangesAsync();

        var repository =
            new StudentPortalRepository(db);

        var snapshot =
            await repository.GetSnapshotAsync(
                schoolA.Id,
                userId);

        Assert.NotNull(snapshot.Profile);
        Assert.Equal(
            schoolA.Id,
            snapshot.Profile!.SchoolId);
        Assert.Equal(
            userId,
            snapshot.Profile.UserId);
    }

    [Fact]
    public async Task Service_RequiresExactActiveStudentRole()
    {
        var school = NewSchool("ROLE");
        var teacher =
            NewUser(
                school.Id,
                RoleNames.Teacher);

        var service =
            NewService(
                school,
                teacher,
                EmptySnapshot());

        var result =
            await service.GetWorkspaceAsync(
                teacher.Id);

        Assert.Null(result.Value);
        Assert.Equal(
            StudentPortalErrorCode.AccessDenied,
            result.Error);
    }

    [Fact]
    public async Task Service_FailsClosedWhenStudentProfileIsNotLinked()
    {
        var school = NewSchool("LINK");
        var student =
            NewUser(
                school.Id,
                RoleNames.Student);

        var service =
            NewService(
                school,
                student,
                EmptySnapshot());

        var result =
            await service.GetWorkspaceAsync(
                student.Id);

        Assert.Null(result.Value);
        Assert.Equal(
            StudentPortalErrorCode.ProfileNotLinked,
            result.Error);
    }

    [Fact]
    public async Task Service_ShowsOnlyOpenAssessmentForEnrolledClass()
    {
        var fixture = BuildWorkspaceFixture();

        var otherClassId = Guid.NewGuid();

        fixture.Snapshot.Assessments
            .ToList();

        var assessments =
            fixture.Snapshot.Assessments
                .Concat(
                    [
                        NewAssessment(
                            fixture.School.Id,
                            otherClassId,
                            fixture.Year.Id,
                            fixture.Subject.Id,
                            AssessmentStatus.Open,
                            "Other class"),
                        NewAssessment(
                            fixture.School.Id,
                            fixture.ClassGroup.Id,
                            fixture.Year.Id,
                            fixture.Subject.Id,
                            AssessmentStatus.Draft,
                            "Draft hidden")
                    ])
                .ToArray();

        var snapshot =
            fixture.Snapshot with
            {
                Assessments = assessments
            };

        var service =
            NewService(
                fixture.School,
                fixture.StudentUser,
                snapshot);

        var result =
            await service.GetWorkspaceAsync(
                fixture.StudentUser.Id);

        Assert.NotNull(result.Value);

        var assessment =
            Assert.Single(
                result.Value!.Assessments);

        Assert.Equal(
            "Open enrolled",
            assessment.Title);
    }

    [Fact]
    public async Task Service_UsesYearSpecificCurriculumAdoptionBeforeDefault()
    {
        var fixture = BuildWorkspaceFixture();

        var result =
            await NewService(
                    fixture.School,
                    fixture.StudentUser,
                    fixture.Snapshot)
                .GetWorkspaceAsync(
                    fixture.StudentUser.Id);

        Assert.NotNull(result.Value);

        var learning =
            Assert.Single(
                result.Value!.Learning);

        Assert.Equal(
            fixture.YearSpecificVersion.Id,
            learning.FrameworkVersionId);

        Assert.DoesNotContain(
            result.Value.Learning,
            x =>
                x.FrameworkVersionId ==
                fixture.DefaultVersion.Id);
    }

    [Fact]
    public async Task Service_ReturnsOnlyOwnStudentResult()
    {
        var fixture = BuildWorkspaceFixture();

        var otherResult =
            new AssessmentResult
            {
                Id = Guid.NewGuid(),
                SchoolId = fixture.School.Id,
                AssessmentId =
                    fixture.Snapshot.Assessments[0].Id,
                StudentProfileId = Guid.NewGuid(),
                Score = 1m,
                Percentage = 10m,
                EnteredByUserId = Guid.NewGuid(),
                EnteredAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                RowVersion = []
            };

        var snapshot =
            fixture.Snapshot with
            {
                Results =
                    fixture.Snapshot.Results
                        .Concat([otherResult])
                        .ToArray()
            };

        var result =
            await NewService(
                    fixture.School,
                    fixture.StudentUser,
                    snapshot)
                .GetWorkspaceAsync(
                    fixture.StudentUser.Id);

        Assert.NotNull(result.Value);

        var own =
            Assert.Single(
                result.Value!.Results);

        Assert.Equal(
            80m,
            own.Percentage);
    }

    [Fact]
    public void Controller_IsProtectedByDedicatedStudentPortalPolicy()
    {
        var authorize =
            typeof(StudentPortalController)
                .GetCustomAttributes<
                    AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            "StudentPortal",
            authorize.Policy);

        var post =
            typeof(StudentPortalController)
                .GetMethod(
                    nameof(
                        StudentPortalController
                            .SetNotificationReadState));

        Assert.NotNull(post);

        Assert.NotNull(
            post!.GetCustomAttribute<
                Microsoft.AspNetCore.Mvc
                    .ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public void LoginAndLayout_UseDedicatedStudentSurface()
    {
        var root = FindRoot();

        var account =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Controllers",
                    "AccountController.cs"));

        var schoolHome =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Controllers",
                    "SchoolHomeController.cs"));

        var layout =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Views",
                    "Shared",
                    "_StudentLayout.cshtml"));

        Assert.Contains(
            "RoleNames.Student",
            account);

        Assert.Contains(
            "\"StudentPortal\"",
            account);

        Assert.Contains(
            "context.Role == RoleNames.Student",
            schoolHome);

        Assert.Contains(
            "asp-controller=\"StudentPortal\"",
            layout);

        Assert.DoesNotContain(
            "SchoolUsers",
            layout);

        Assert.DoesNotContain(
            "SupervisorAssignments",
            layout);

        Assert.DoesNotContain(
            "DataImport",
            layout);
    }

    private static StudentPortalService NewService(
        School school,
        SchoolUserRecord actor,
        StudentPortalSnapshot snapshot)
    {
        var users = new FakeUserRepository();
        users.Seed(actor);

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        return new StudentPortalService(
            new FakePortalRepository(snapshot),
            users,
            schools);
    }

    private static WorkspaceFixture BuildWorkspaceFixture()
    {
        var school = NewSchool("WORK");
        var studentUser =
            NewUser(
                school.Id,
                RoleNames.Student);

        var profile =
            NewProfile(
                school.Id,
                studentUser.Id,
                "ST-28");

        var year =
            new AcademicYear
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                Name = "2026/2027",
                StartsOn = new DateOnly(2026, 9, 1),
                EndsOn = new DateOnly(2027, 6, 30),
                Status = AcademicStructureStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                RowVersion = []
            };

        var grade =
            new GradeLevel
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                Name = "Grade 9",
                Order = 9
            };

        var classGroup =
            new ClassGroup
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                AcademicYearId = year.Id,
                GradeLevelId = grade.Id,
                Name = "9A",
                Code = "9A",
                NormalizedCode = "9A",
                Status = AcademicStructureStatus.Active,
                RowVersion = []
            };

        var enrollment =
            new StudentEnrollment
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                StudentProfileId = profile.Id,
                ClassGroupId = classGroup.Id,
                AcademicYearId = year.Id,
                EnrolledAtUtc = DateTime.UtcNow
            };

        var subject =
            new Subject
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                Name = "Mathematics",
                Code = "MATH",
                NormalizedCode = "MATH",
                Status = AcademicStructureStatus.Active,
                RowVersion = []
            };

        var framework =
            new CurriculumFramework
            {
                Id = Guid.NewGuid(),
                Code = "TEST-MATH",
                NormalizedCode = "TEST-MATH",
                Name = "Test Mathematics",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                RowVersion = []
            };

        var defaultVersion =
            NewVersion(
                framework.Id,
                "DEFAULT");

        var yearSpecificVersion =
            NewVersion(
                framework.Id,
                "2026");

        var defaultAdoption =
            NewAdoption(
                school.Id,
                grade.Id,
                subject.Id,
                defaultVersion.Id,
                null);

        var yearAdoption =
            NewAdoption(
                school.Id,
                grade.Id,
                subject.Id,
                yearSpecificVersion.Id,
                year.Id);

        var unit =
            NewNode(
                yearSpecificVersion.Id,
                "Unit",
                "U1",
                "Numbers",
                null,
                1);

        var lesson =
            NewNode(
                yearSpecificVersion.Id,
                "Lesson",
                "L1",
                "Number reasoning",
                unit.Id,
                2);

        var openAssessment =
            NewAssessment(
                school.Id,
                classGroup.Id,
                year.Id,
                subject.Id,
                AssessmentStatus.Open,
                "Open enrolled");

        var ownResult =
            new AssessmentResult
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                AssessmentId = openAssessment.Id,
                StudentProfileId = profile.Id,
                Score = 8m,
                Percentage = 80m,
                EnteredByUserId = Guid.NewGuid(),
                EnteredAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                RowVersion = []
            };

        var snapshot =
            new StudentPortalSnapshot(
                profile,
                [enrollment],
                [year],
                [classGroup],
                [grade],
                [subject],
                [defaultAdoption, yearAdoption],
                [framework],
                [defaultVersion, yearSpecificVersion],
                [unit, lesson],
                [openAssessment],
                [ownResult]);

        return new WorkspaceFixture(
            school,
            studentUser,
            year,
            classGroup,
            subject,
            defaultVersion,
            yearSpecificVersion,
            snapshot);
    }

    private static CurriculumFrameworkVersion NewVersion(
        Guid frameworkId,
        string code) =>
        new()
        {
            Id = Guid.NewGuid(),
            FrameworkId = frameworkId,
            VersionCode = code,
            NormalizedVersionCode = code,
            Name = $"Version {code}",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = []
        };

    private static SchoolCurriculumAdoption NewAdoption(
        Guid schoolId,
        Guid gradeId,
        Guid subjectId,
        Guid versionId,
        Guid? yearId) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            AcademicYearId = yearId,
            GradeLevelId = gradeId,
            SubjectId = subjectId,
            FrameworkVersionId = versionId,
            IsPrimary = true,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = []
        };

    private static CurriculumPackContentNode NewNode(
        Guid versionId,
        string kind,
        string code,
        string title,
        Guid? parentId,
        int order) =>
        new()
        {
            Id = Guid.NewGuid(),
            FrameworkVersionId = versionId,
            FrameworkCode = "TEST-MATH",
            VersionCode = "2026",
            NodeKind = kind,
            Code = code,
            ParentId = parentId,
            LogicalLevelFrom = 9,
            LogicalLevelTo = 9,
            NativeLevel = "Grade 9",
            Title = title,
            SourceAuthority = "Test",
            SourceUrl = "https://example.test",
            SourceLocator = "test",
            Attribution = "test",
            IsOfficial = false,
            IsActive = true,
            SortOrder = order,
            ContentHash = new string('a', 64),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = []
        };

    private static Assessment NewAssessment(
        Guid schoolId,
        Guid classId,
        Guid yearId,
        Guid subjectId,
        AssessmentStatus status,
        string title) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            SubjectId = subjectId,
            ClassGroupId = classId,
            AcademicYearId = yearId,
            TermId = Guid.NewGuid(),
            Title = title,
            AssessmentDate = new DateOnly(2026, 10, 1),
            MaxScore = 10m,
            Status = status,
            CreatedByUserId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = []
        };

    private static StudentProfile NewProfile(
        Guid schoolId,
        Guid userId,
        string studentNumber) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            UserId = userId,
            StudentNumber = studentNumber,
            NormalizedStudentNumber = studentNumber,
            FirstName = "Student",
            LastName = "Phase28",
            DisplayName = "Student Phase28",
            Status = AcademicStructureStatus.Active,
            IsArchived = false,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = []
        };

    private static School NewSchool(string suffix)
    {
        var code =
            $"P28-{suffix}-{Guid.NewGuid():N}"
                .ToUpperInvariant();

        code = code[..Math.Min(code.Length, 30)];

        return new School
        {
            Id = Guid.NewGuid(),
            Name = $"Phase 28 School {suffix}",
            SchoolCode = code,
            NormalizedSchoolCode = code,
            Status = SchoolStatus.Active,
            CountryCode = "PL",
            City = "Warsaw",
            ContactEmail =
                $"{Guid.NewGuid():N}@example.com",
            DefaultCulture = "en",
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = []
        };
    }

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

    private static StudentPortalSnapshot EmptySnapshot() =>
        new(
            null,
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
            []);

    private static string FindRoot()
    {
        var dir =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        dir.FullName,
                        "Edulytics.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException();
    }

    private sealed record WorkspaceFixture(
        School School,
        SchoolUserRecord StudentUser,
        AcademicYear Year,
        ClassGroup ClassGroup,
        Subject Subject,
        CurriculumFrameworkVersion DefaultVersion,
        CurriculumFrameworkVersion YearSpecificVersion,
        StudentPortalSnapshot Snapshot);

    private sealed class FakePortalRepository
        : IStudentPortalRepository
    {
        private readonly StudentPortalSnapshot _snapshot;

        public FakePortalRepository(
            StudentPortalSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<StudentPortalSnapshot>
            GetSnapshotAsync(
                Guid schoolId,
                Guid studentUserId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(_snapshot);
    }

    private sealed class FakeSchoolRepository
        : ISchoolRepository
    {
        private readonly Dictionary<Guid, School> _items = [];

        public void Seed(School school) =>
            _items[school.Id] = school;

        public Task<IReadOnlyList<School>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<School>>(
                _items.Values.ToArray());

        public Task<School?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _items.GetValueOrDefault(id));

        public Task<School?> GetForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            GetByIdAsync(id, cancellationToken);

        public Task<bool> ExistsByNormalizedCodeAsync(
            string normalizedSchoolCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _items.Values.Any(
                    x =>
                        x.NormalizedSchoolCode ==
                        normalizedSchoolCode));

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

    private sealed class FakeUserRepository
        : ISchoolUserRepository
    {
        private readonly Dictionary<
            Guid,
            SchoolUserRecord> _items = [];

        public void Seed(
            SchoolUserRecord user) =>
            _items[user.Id] = user;

        public Task<SchoolUserRecord?> GetActorAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _items.GetValueOrDefault(userId));

        public Task<IReadOnlyList<SchoolUserRecord>>
            ListBySchoolAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchoolUserRecord>>(
                _items.Values
                    .Where(
                        x =>
                            x.SchoolId ==
                            schoolId)
                    .ToArray());

        public Task<SchoolUserRecord?>
            GetBySchoolAndIdAsync(
                Guid schoolId,
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var user =
                _items.GetValueOrDefault(userId);

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

        public Task<SchoolUserPersistenceResult>
            GeneratePasswordSetupAsync(
                Guid schoolId,
                Guid userId,
                CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult>
            CompletePasswordSetupAsync(
                Guid userId,
                string token,
                string newPassword,
                CancellationToken cancellationToken = default) =>
            Failure();

        private static Task<SchoolUserPersistenceResult>
            Failure() =>
            Task.FromResult(
                SchoolUserPersistenceResult.Failure(
                    SchoolUserPersistenceError
                        .IdentityFailure));
    }
}
