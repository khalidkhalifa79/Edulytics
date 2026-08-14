using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Edulytics.Services.Academics;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase06;

public sealed class AcademicStructureServiceTests
{
    [Fact]
    public async Task SchoolAdmin_CanCreateAcademicYear()
    {
        using var f = CreateFixture();

        var result = await f.Service.CreateAcademicYearAsync(
            f.Admin.Id,
            new CreateAcademicYearRequest(
                "2026/2027",
                new DateOnly(2026, 9, 1),
                new DateOnly(2027, 6, 30),
                AcademicStructureStatus.Active));

        Assert.True(result.Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Admin.Id);
        Assert.Single(dashboard.Value!.AcademicYears);
    }

    [Fact]
    public async Task Teacher_CannotAdministerAcademicStructure()
    {
        using var f = CreateFixture();

        var teacher = NewUser(f.School.Id, RoleNames.Teacher);
        f.Users.Seed(teacher);

        var result = await f.Service.GetDashboardAsync(teacher.Id);

        Assert.Null(result.Value);
        Assert.Equal(AcademicStructureErrorCode.AccessDenied, result.Error);
    }

    [Fact]
    public async Task TermMustStayInsideAcademicYear()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);

        var result = await f.Service.CreateTermAsync(
            f.Admin.Id,
            new CreateTermRequest(
                year.Id,
                "Bad term",
                year.StartsOn.AddDays(-1),
                year.EndsOn,
                AcademicStructureStatus.Active));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            x => x.Code == AcademicStructureErrorCode.TermOutsideAcademicYear);
    }

    [Fact]
    public async Task TeacherAssignment_RequiresTeacherRole()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);
        var grade = await CreateGrade(f);
        var classGroup = await CreateClass(f, year.Id, grade.Id);
        var subject = await CreateSubject(f);

        var supervisor = NewUser(f.School.Id, RoleNames.SubjectSupervisor);
        f.Users.Seed(supervisor);

        var result = await f.Service.CreateTeacherAssignmentAsync(
            f.Admin.Id,
            new CreateTeacherAssignmentRequest(
                supervisor.Id,
                classGroup.Id,
                subject.Id));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            x => x.Code == AcademicStructureErrorCode.InvalidTeacher);
    }

    [Fact]
    public async Task StudentProfile_CanExistWithoutLoginAccount()
    {
        using var f = CreateFixture();

        var result = await f.Service.CreateStudentProfileAsync(
            f.Admin.Id,
            new CreateStudentProfileRequest(
                "ST-001",
                "Jan",
                "Kowalski",
                null,
                AcademicStructureStatus.Active));

        Assert.True(result.Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Admin.Id);
        var student = Assert.Single(dashboard.Value!.StudentProfiles);
        Assert.Null(student.UserEmail);
    }

    [Fact]
    public async Task Enrollment_IsUniquePerStudentAndYear()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);
        var grade = await CreateGrade(f);
        var classGroup = await CreateClass(f, year.Id, grade.Id);

        Assert.True((await f.Service.CreateStudentProfileAsync(
            f.Admin.Id,
            new CreateStudentProfileRequest(
                "ST-002",
                "Anna",
                "Nowak",
                null,
                AcademicStructureStatus.Active))).Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Admin.Id);
        var student = Assert.Single(dashboard.Value!.StudentProfiles);

        var first = await f.Service.CreateStudentEnrollmentAsync(
            f.Admin.Id,
            new CreateStudentEnrollmentRequest(student.Id, classGroup.Id));

        Assert.True(first.Succeeded);

        var second = await f.Service.CreateStudentEnrollmentAsync(
            f.Admin.Id,
            new CreateStudentEnrollmentRequest(student.Id, classGroup.Id));

        Assert.False(second.Succeeded);
        Assert.Contains(
            second.Errors,
            x => x.Code == AcademicStructureErrorCode.DuplicateEnrollment);
    }

    [Fact]
    public async Task CrossSchoolGrade_IsRejected()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);
        var other = NewSchool();
        f.Schools.Seed(other);

        var foreignGrade = new GradeLevel
        {
            Id = Guid.NewGuid(),
            SchoolId = other.Id,
            Name = "Foreign grade",
            Order = 1
        };

        await f.Academic.AddAsync(foreignGrade);
        Assert.True((await f.Academic.SaveAsync()).Succeeded);

        var result = await f.Service.CreateClassGroupAsync(
            f.Admin.Id,
            new CreateClassGroupRequest(
                year.Id,
                foreignGrade.Id,
                "6A",
                "6A",
                AcademicStructureStatus.Active));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            x => x.Code == AcademicStructureErrorCode.GradeLevelNotFound);
    }

    private static async Task<AcademicYearItem> CreateYear(Fixture f)
    {
        Assert.True((await f.Service.CreateAcademicYearAsync(
            f.Admin.Id,
            new CreateAcademicYearRequest(
                "2026/2027",
                new DateOnly(2026, 9, 1),
                new DateOnly(2027, 6, 30),
                AcademicStructureStatus.Active))).Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Admin.Id);
        return Assert.Single(dashboard.Value!.AcademicYears);
    }

    private static async Task<GradeLevelItem> CreateGrade(Fixture f)
    {
        Assert.True((await f.Service.CreateGradeLevelAsync(
            f.Admin.Id,
            new CreateGradeLevelRequest("Grade 6", 6))).Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Admin.Id);
        return Assert.Single(dashboard.Value!.GradeLevels);
    }

    private static async Task<ClassGroupItem> CreateClass(
        Fixture f,
        Guid yearId,
        Guid gradeId)
    {
        Assert.True((await f.Service.CreateClassGroupAsync(
            f.Admin.Id,
            new CreateClassGroupRequest(
                yearId,
                gradeId,
                "6A",
                "6A",
                AcademicStructureStatus.Active))).Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Admin.Id);
        return Assert.Single(dashboard.Value!.ClassGroups);
    }

    private static async Task<SubjectItem> CreateSubject(Fixture f)
    {
        Assert.True((await f.Service.CreateSubjectAsync(
            f.Admin.Id,
            new CreateSubjectRequest(
                "Mathematics",
                "MATH",
                AcademicStructureStatus.Active))).Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Admin.Id);
        return Assert.Single(dashboard.Value!.Subjects);
    }

    private static Fixture CreateFixture()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase($"phase06-{Guid.NewGuid():N}")
                .Options;

        var db = new EdulyticsDbContext(options);
        var school = NewSchool();

        db.Schools.Add(school);
        db.SaveChanges();

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        var users = new FakeUserRepository();
        var admin = NewUser(school.Id, RoleNames.SchoolAdmin);
        users.Seed(admin);

        var academic = new AcademicStructureRepository(db);
        var service = new AcademicStructureService(academic, schools, users);

        return new Fixture(db, school, admin, schools, users, academic, service);
    }

    private static School NewSchool()
    {
        var code = $"P6-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        return new School
        {
            Id = Guid.NewGuid(),
            Name = "Phase 06 School",
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
        SchoolUserRecord Admin,
        FakeSchoolRepository Schools,
        FakeUserRepository Users,
        AcademicStructureRepository Academic,
        AcademicStructureService Service)
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
            Task.FromResult(_schools.SingleOrDefault(x => x.Id == id));

        public Task<School?> GetForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            GetByIdAsync(id, cancellationToken);

        public Task<bool> ExistsByNormalizedCodeAsync(
            string normalizedSchoolCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _schools.Any(x => x.NormalizedSchoolCode == normalizedSchoolCode));

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
                _users.Values.Where(x => x.SchoolId == schoolId).ToArray());

        public Task<SchoolUserRecord?> GetBySchoolAndIdAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = _users.GetValueOrDefault(userId);
            return Task.FromResult(user?.SchoolId == schoolId ? user : null);
        }

        public Task<SchoolUserPersistenceResult> CreateAsync(
            Guid schoolId, string email, string role,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> SetActiveAsync(
            Guid schoolId, Guid userId, bool isActive,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> SetLockedAsync(
            Guid schoolId, Guid userId, bool isLocked,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> SetRoleAsync(
            Guid schoolId, Guid userId, string role,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> GeneratePasswordSetupAsync(
            Guid schoolId, Guid userId,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> CompletePasswordSetupAsync(
            Guid userId, string token, string newPassword,
            CancellationToken cancellationToken = default) =>
            Failure();

        private static Task<SchoolUserPersistenceResult> Failure() =>
            Task.FromResult(
                SchoolUserPersistenceResult.Failure(
                    SchoolUserPersistenceError.IdentityFailure));
    }
}
