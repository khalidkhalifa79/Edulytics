using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;
using Edulytics.Services.Users;

namespace Edulytics.Tests.Phase05;

public sealed class SchoolUserManagementServiceTests
{
    [Fact]
    public async Task SuperAdmin_CanCreateSchoolAdmin()
    {
        var school = NewSchool(
            SchoolStatus.Active);

        var users = new FakeUserRepository();

        var superAdmin =
            NewUser(
                null,
                RoleNames.SuperAdmin);

        users.Seed(superAdmin);

        var schools =
            new FakeSchoolRepository();

        schools.Seed(school);

        var service =
            new SchoolUserManagementService(
                users,
                schools);

        var result = await service.CreateAsync(
            superAdmin.Id,
            school.Id,
            new CreateSchoolUserRequest(
                "admin@example.com",
                RoleNames.SchoolAdmin));

        Assert.True(result.Succeeded);
        Assert.NotNull(result.UserId);
        Assert.False(
            string.IsNullOrWhiteSpace(
                result.PasswordSetupToken));

        var created =
            Assert.Single(
                users.Users.Values,
                x => x.Id == result.UserId);

        Assert.Equal(
            school.Id,
            created.SchoolId);

        Assert.Equal(
            RoleNames.SchoolAdmin,
            Assert.Single(created.Roles));
    }

    [Fact]
    public async Task Create_RejectsSuperAdminRole()
    {
        var school = NewSchool(
            SchoolStatus.Active);

        var users = new FakeUserRepository();

        var superAdmin =
            NewUser(
                null,
                RoleNames.SuperAdmin);

        users.Seed(superAdmin);

        var schools =
            new FakeSchoolRepository();

        schools.Seed(school);

        var service =
            new SchoolUserManagementService(
                users,
                schools);

        var result = await service.CreateAsync(
            superAdmin.Id,
            school.Id,
            new CreateSchoolUserRequest(
                "user@example.com",
                RoleNames.SuperAdmin));

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            x =>
                x.Code ==
                SchoolUserErrorCode.UserInvalidRole);
    }

    [Fact]
    public async Task SchoolAdmin_CannotManageAnotherSchool()
    {
        var schoolA = NewSchool(
            SchoolStatus.Active);

        var schoolB = NewSchool(
            SchoolStatus.Active);

        var users = new FakeUserRepository();

        var schoolAdmin =
            NewUser(
                schoolA.Id,
                RoleNames.SchoolAdmin);

        users.Seed(schoolAdmin);

        var schools =
            new FakeSchoolRepository();

        schools.Seed(schoolA);
        schools.Seed(schoolB);

        var service =
            new SchoolUserManagementService(
                users,
                schools);

        var result = await service.ListAsync(
            schoolAdmin.Id,
            schoolB.Id);

        Assert.Null(result.Value);

        Assert.Equal(
            SchoolUserErrorCode.UserAccessDenied,
            result.Error);
    }

    [Fact]
    public async Task SchoolAdmin_CannotDeactivateSelf()
    {
        var school = NewSchool(
            SchoolStatus.Active);

        var users = new FakeUserRepository();

        var schoolAdmin =
            NewUser(
                school.Id,
                RoleNames.SchoolAdmin);

        users.Seed(schoolAdmin);

        var schools =
            new FakeSchoolRepository();

        schools.Seed(school);

        var service =
            new SchoolUserManagementService(
                users,
                schools);

        var result =
            await service.SetActiveAsync(
                schoolAdmin.Id,
                school.Id,
                schoolAdmin.Id,
                false);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            x =>
                x.Code ==
                SchoolUserErrorCode.UserCannotManageSelf);
    }

    [Fact]
    public async Task ArchivedSchool_CannotCreateUser()
    {
        var school = NewSchool(
            SchoolStatus.Archived);

        var users = new FakeUserRepository();

        var superAdmin =
            NewUser(
                null,
                RoleNames.SuperAdmin);

        users.Seed(superAdmin);

        var schools =
            new FakeSchoolRepository();

        schools.Seed(school);

        var service =
            new SchoolUserManagementService(
                users,
                schools);

        var result =
            await service.CreateAsync(
                superAdmin.Id,
                school.Id,
                new CreateSchoolUserRequest(
                    "teacher@example.com",
                    RoleNames.Teacher));

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            x =>
                x.Code ==
                SchoolUserErrorCode.UserSchoolArchived);
    }

    [Fact]
    public async Task ActiveTeacher_CanSignIn()
    {
        var school = NewSchool(
            SchoolStatus.Active);

        var users = new FakeUserRepository();

        var teacher =
            NewUser(
                school.Id,
                RoleNames.Teacher);

        users.Seed(teacher);

        var schools =
            new FakeSchoolRepository();

        schools.Seed(school);

        var service =
            new SchoolUserManagementService(
                users,
                schools);

        var result =
            await service.EvaluateSignInAsync(
                teacher.Id);

        Assert.True(result.Allowed);
        Assert.False(
            result.IsPlatformAdministrator);

        Assert.Equal(
            school.Id,
            result.SchoolId);
    }

    [Fact]
    public async Task SuspendedSchoolUser_CannotSignIn()
    {
        var school = NewSchool(
            SchoolStatus.Suspended);

        var users = new FakeUserRepository();

        var teacher =
            NewUser(
                school.Id,
                RoleNames.Teacher);

        users.Seed(teacher);

        var schools =
            new FakeSchoolRepository();

        schools.Seed(school);

        var service =
            new SchoolUserManagementService(
                users,
                schools);

        var result =
            await service.EvaluateSignInAsync(
                teacher.Id);

        Assert.False(result.Allowed);
    }

    private static School NewSchool(
        SchoolStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Test School",
            SchoolCode = "TEST",
            NormalizedSchoolCode = "TEST",
            Status = status,
            CountryCode = "PL",
            City = "Warsaw",
            ContactEmail =
                "school@example.com",
            DefaultCulture = "en",
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion =
                BitConverter.GetBytes(1L)
        };

    private static SchoolUserRecord NewUser(
        Guid? schoolId,
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

    private sealed class FakeUserRepository
        : ISchoolUserRepository
    {
        public Dictionary<Guid, SchoolUserRecord>
            Users { get; } = [];

        public void Seed(
            SchoolUserRecord user) =>
            Users[user.Id] = user;

        public Task<SchoolUserRecord?> GetActorAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Users.GetValueOrDefault(userId));

        public Task<IReadOnlyList<SchoolUserRecord>>
            ListBySchoolAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchoolUserRecord>>(
                Users.Values
                    .Where(x => x.SchoolId == schoolId)
                    .ToArray());

        public Task<SchoolUserRecord?>
            GetBySchoolAndIdAsync(
                Guid schoolId,
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var user =
                Users.GetValueOrDefault(userId);

            return Task.FromResult(
                user?.SchoolId == schoolId
                    ? user
                    : null);
        }

        public Task<SchoolUserPersistenceResult>
            CreateAsync(
                Guid schoolId,
                string email,
                string role,
                CancellationToken cancellationToken = default)
        {
            if (Users.Values.Any(
                    x => x.Email.Equals(
                        email,
                        StringComparison.OrdinalIgnoreCase)))
            {
                return Task.FromResult(
                    SchoolUserPersistenceResult.Failure(
                        SchoolUserPersistenceError
                            .DuplicateEmail));
            }

            var user = new SchoolUserRecord(
                Guid.NewGuid(),
                schoolId,
                email,
                true,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                [role]);

            Users[user.Id] = user;

            return Task.FromResult(
                SchoolUserPersistenceResult.Success(
                    user,
                    "test-token"));
        }

        public Task<SchoolUserPersistenceResult>
            SetActiveAsync(
                Guid schoolId,
                Guid userId,
                bool isActive,
                CancellationToken cancellationToken = default) =>
            Update(
                schoolId,
                userId,
                x => x with
                {
                    IsActive = isActive,
                    UpdatedAtUtc = DateTime.UtcNow
                });

        public Task<SchoolUserPersistenceResult>
            SetLockedAsync(
                Guid schoolId,
                Guid userId,
                bool isLocked,
                CancellationToken cancellationToken = default) =>
            Update(
                schoolId,
                userId,
                x => x with
                {
                    IsLocked = isLocked,
                    UpdatedAtUtc = DateTime.UtcNow
                });

        public Task<SchoolUserPersistenceResult>
            SetRoleAsync(
                Guid schoolId,
                Guid userId,
                string role,
                CancellationToken cancellationToken = default) =>
            Update(
                schoolId,
                userId,
                x => x with
                {
                    Roles = [role],
                    UpdatedAtUtc = DateTime.UtcNow
                });

        public async Task<SchoolUserPersistenceResult>
            GeneratePasswordSetupAsync(
                Guid schoolId,
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var user =
                await GetBySchoolAndIdAsync(
                    schoolId,
                    userId,
                    cancellationToken);

            return user is null
                ? SchoolUserPersistenceResult.Failure(
                    SchoolUserPersistenceError.NotFound)
                : SchoolUserPersistenceResult.Success(
                    user,
                    "test-token");
        }

        public Task<SchoolUserPersistenceResult>
            CompletePasswordSetupAsync(
                Guid userId,
                string token,
                string newPassword,
                CancellationToken cancellationToken = default)
        {
            var user =
                Users.GetValueOrDefault(userId);

            return Task.FromResult(
                user is null
                    ? SchoolUserPersistenceResult.Failure(
                        SchoolUserPersistenceError.InvalidToken)
                    : SchoolUserPersistenceResult.Success(user));
        }

        private Task<SchoolUserPersistenceResult> Update(
            Guid schoolId,
            Guid userId,
            Func<SchoolUserRecord, SchoolUserRecord> update)
        {
            var user =
                Users.GetValueOrDefault(userId);

            if (user is null ||
                user.SchoolId != schoolId)
            {
                return Task.FromResult(
                    SchoolUserPersistenceResult.Failure(
                        SchoolUserPersistenceError.NotFound));
            }

            var changed = update(user);

            Users[userId] = changed;

            return Task.FromResult(
                SchoolUserPersistenceResult.Success(
                    changed));
        }
    }

    private sealed class FakeSchoolRepository
        : ISchoolRepository
    {
        private readonly List<School> _schools = [];

        public void Seed(
            School school) =>
            _schools.Add(school);

        public Task<IReadOnlyList<School>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<School>>(
                _schools.ToArray());

        public Task<School?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _schools.SingleOrDefault(
                    x => x.Id == id));

        public Task<School?> GetForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            GetByIdAsync(
                id,
                cancellationToken);

        public Task<bool> ExistsByNormalizedCodeAsync(
            string normalizedSchoolCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _schools.Any(
                    x =>
                        x.NormalizedSchoolCode ==
                        normalizedSchoolCode));

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
            Task.FromResult(
                SchoolRepositoryWriteResult.Success);
    }
}
