using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;
using Edulytics.Services.Users;

namespace Edulytics.Tests.Phase05;

public sealed class Phase05AcceptanceCoverageTests
{
    [Fact]
    public async Task SchoolAdmin_CanManageOnlyOwnActiveSchool()
    {
        var school = NewSchool(SchoolStatus.Active);
        var users = new FakeUserRepository();

        var admin = NewUser(
            school.Id,
            RoleNames.SchoolAdmin);

        users.Seed(admin);

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        var service = new SchoolUserManagementService(
            users,
            schools);

        Assert.True(
            await service.CanManageUsersAsync(admin.Id));

        var result = await service.ListAsync(
            admin.Id,
            requestedSchoolId: null);

        Assert.NotNull(result.Value);
        Assert.Equal(
            school.Id,
            result.Value!.Context.SchoolId);
        Assert.False(
            result.Value.Context.IsPlatformActor);
    }

    [Theory]
    [InlineData(RoleNames.Teacher)]
    [InlineData(RoleNames.Student)]
    public async Task NonAdminSchoolRoles_CannotManageUsers(
        string role)
    {
        var school = NewSchool(SchoolStatus.Active);
        var users = new FakeUserRepository();

        var actor = NewUser(
            school.Id,
            role);

        users.Seed(actor);

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        var service = new SchoolUserManagementService(
            users,
            schools);

        Assert.False(
            await service.CanManageUsersAsync(actor.Id));

        var result = await service.ListAsync(
            actor.Id,
            school.Id);

        Assert.Null(result.Value);
        Assert.Equal(
            SchoolUserErrorCode.UserAccessDenied,
            result.Error);
    }

    [Fact]
    public async Task DuplicateEmail_IsRejected()
    {
        var school = NewSchool(SchoolStatus.Active);
        var users = new FakeUserRepository();

        var superAdmin = NewUser(
            null,
            RoleNames.SuperAdmin);

        var existing = NewUser(
            school.Id,
            RoleNames.Teacher,
            "duplicate@example.com");

        users.Seed(superAdmin);
        users.Seed(existing);

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        var service = new SchoolUserManagementService(
            users,
            schools);

        var result = await service.CreateAsync(
            superAdmin.Id,
            school.Id,
            new CreateSchoolUserRequest(
                "DUPLICATE@example.com",
                RoleNames.Student));

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            x =>
                x.Code ==
                SchoolUserErrorCode.UserDuplicateEmail);
    }

    [Fact]
    public async Task ChangeRole_ReplacesExistingTenantRole()
    {
        var school = NewSchool(SchoolStatus.Active);
        var users = new FakeUserRepository();

        var superAdmin = NewUser(
            null,
            RoleNames.SuperAdmin);

        var target = NewUser(
            school.Id,
            RoleNames.Teacher);

        users.Seed(superAdmin);
        users.Seed(target);

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        var service = new SchoolUserManagementService(
            users,
            schools);

        var result = await service.ChangeRoleAsync(
            superAdmin.Id,
            school.Id,
            target.Id,
            RoleNames.SubjectSupervisor);

        Assert.True(result.Succeeded);

        var changed = users.Users[target.Id];

        Assert.Equal(
            RoleNames.SubjectSupervisor,
            Assert.Single(changed.Roles));
    }

    [Fact]
    public async Task ActivateDeactivate_LifecycleWorks()
    {
        var fixture = CreateFixture();

        var deactivate =
            await fixture.Service.SetActiveAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                fixture.Target.Id,
                false);

        Assert.True(deactivate.Succeeded);
        Assert.False(
            fixture.Users.Users[
                fixture.Target.Id
            ].IsActive);

        var activate =
            await fixture.Service.SetActiveAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                fixture.Target.Id,
                true);

        Assert.True(activate.Succeeded);
        Assert.True(
            fixture.Users.Users[
                fixture.Target.Id
            ].IsActive);
    }

    [Fact]
    public async Task LockUnlock_LifecycleWorks()
    {
        var fixture = CreateFixture();

        var lockResult =
            await fixture.Service.SetLockedAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                fixture.Target.Id,
                true);

        Assert.True(lockResult.Succeeded);
        Assert.True(
            fixture.Users.Users[
                fixture.Target.Id
            ].IsLocked);

        var unlockResult =
            await fixture.Service.SetLockedAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                fixture.Target.Id,
                false);

        Assert.True(unlockResult.Succeeded);
        Assert.False(
            fixture.Users.Users[
                fixture.Target.Id
            ].IsLocked);
    }

    [Theory]
    [InlineData(RoleNames.SchoolAdmin)]
    [InlineData(RoleNames.SubjectSupervisor)]
    [InlineData(RoleNames.Teacher)]
    [InlineData(RoleNames.Student)]
    public async Task EveryValidTenantRole_CanSignInToActiveSchool(
        string role)
    {
        var school = NewSchool(SchoolStatus.Active);
        var users = new FakeUserRepository();

        var user = NewUser(
            school.Id,
            role);

        users.Seed(user);

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        var service = new SchoolUserManagementService(
            users,
            schools);

        var decision =
            await service.EvaluateSignInAsync(
                user.Id);

        Assert.True(decision.Allowed);
        Assert.False(
            decision.IsPlatformAdministrator);
        Assert.Equal(
            school.Id,
            decision.SchoolId);
        Assert.Equal(
            role,
            decision.Role);
    }

    [Theory]
    [InlineData(SchoolStatus.Suspended)]
    [InlineData(SchoolStatus.Archived)]
    public async Task NonActiveSchool_UserCannotSignIn(
        SchoolStatus status)
    {
        var school = NewSchool(status);
        var users = new FakeUserRepository();

        var user = NewUser(
            school.Id,
            RoleNames.Teacher);

        users.Seed(user);

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        var service = new SchoolUserManagementService(
            users,
            schools);

        var decision =
            await service.EvaluateSignInAsync(
                user.Id);

        Assert.False(decision.Allowed);
    }

    [Fact]
    public async Task ArchivedSchool_UserManagementIsReadOnly()
    {
        var school = NewSchool(
            SchoolStatus.Archived);

        var users = new FakeUserRepository();

        var superAdmin = NewUser(
            null,
            RoleNames.SuperAdmin);

        users.Seed(superAdmin);

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        var service = new SchoolUserManagementService(
            users,
            schools);

        var context =
            await service.GetManagementContextAsync(
                superAdmin.Id,
                school.Id);

        Assert.NotNull(context.Value);
        Assert.False(
            context.Value!.CanMutate);

        var create =
            await service.CreateAsync(
                superAdmin.Id,
                school.Id,
                new CreateSchoolUserRequest(
                    "teacher@example.com",
                    RoleNames.Teacher));

        Assert.False(create.Succeeded);

        Assert.Contains(
            create.Errors,
            x =>
                x.Code ==
                SchoolUserErrorCode.UserSchoolArchived);
    }

    [Fact]
    public async Task SchoolAdmin_CannotLockSelf()
    {
        var fixture =
            CreateSchoolAdminSelfFixture();

        var result =
            await fixture.Service.SetLockedAsync(
                fixture.Admin.Id,
                fixture.School.Id,
                fixture.Admin.Id,
                true);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            x =>
                x.Code ==
                SchoolUserErrorCode.UserAccessDenied);
    }

    [Fact]
    public async Task SchoolAdmin_CannotChangeOwnRole()
    {
        var fixture =
            CreateSchoolAdminSelfFixture();

        var result =
            await fixture.Service.ChangeRoleAsync(
                fixture.Admin.Id,
                fixture.School.Id,
                fixture.Admin.Id,
                RoleNames.Teacher);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            x =>
                x.Code ==
                SchoolUserErrorCode.UserAccessDenied);
    }

    [Fact]
    public async Task SchoolAdmin_CannotGenerateOwnPasswordLink()
    {
        var fixture =
            CreateSchoolAdminSelfFixture();

        var result =
            await fixture.Service
                .GeneratePasswordSetupAsync(
                    fixture.Admin.Id,
                    fixture.School.Id,
                    fixture.Admin.Id);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            x =>
                x.Code ==
                SchoolUserErrorCode.UserAccessDenied);
    }

    [Fact]
    public async Task SchoolAdmin_CannotCrossTenantBoundary()
    {
        var schoolA = NewSchool(
            SchoolStatus.Active);

        var schoolB = NewSchool(
            SchoolStatus.Active);

        var users = new FakeUserRepository();

        var admin = NewUser(
            schoolA.Id,
            RoleNames.SchoolAdmin);

        var targetB = NewUser(
            schoolB.Id,
            RoleNames.Teacher);

        users.Seed(admin);
        users.Seed(targetB);

        var schools = new FakeSchoolRepository();
        schools.Seed(schoolA);
        schools.Seed(schoolB);

        var service = new SchoolUserManagementService(
            users,
            schools);

        var details = await service.GetAsync(
            admin.Id,
            schoolB.Id,
            targetB.Id);

        Assert.Null(details.Value);
        Assert.Equal(
            SchoolUserErrorCode.UserAccessDenied,
            details.Error);
    }

    [Fact]
    public async Task SuperAdminRole_CannotBeAssignedToSchoolUser()
    {
        var fixture = CreateFixture();

        var result =
            await fixture.Service.ChangeRoleAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                fixture.Target.Id,
                RoleNames.SuperAdmin);

        Assert.False(result.Succeeded);

        Assert.Contains(
            result.Errors,
            x =>
                x.Code ==
                SchoolUserErrorCode.UserInvalidRole);
    }


    [Fact]
    public async Task SchoolAdmin_CanViewUsers_ButCannotMutateThem()
    {
        var school = NewSchool(SchoolStatus.Active);
        var users = new FakeUserRepository();
        var admin = NewUser(school.Id, RoleNames.SchoolAdmin);
        var teacher = NewUser(school.Id, RoleNames.Teacher);
        users.Seed(admin);
        users.Seed(teacher);
        var schools = new FakeSchoolRepository();
        schools.Seed(school);
        var service = new SchoolUserManagementService(users, schools);

        var list = await service.ListAsync(admin.Id, school.Id);
        Assert.NotNull(list.Value);
        Assert.False(list.Value!.Context.CanMutate);

        var mutation = await service.SetLockedAsync(
            admin.Id,
            school.Id,
            teacher.Id,
            true);

        Assert.False(mutation.Succeeded);
        Assert.Contains(
            mutation.Errors,
            x => x.Code == SchoolUserErrorCode.UserAccessDenied);
    }

    [Fact]
    public async Task SubjectSupervisor_ManagesOnlyTeacherAndStudentUsers()
    {
        var school = NewSchool(SchoolStatus.Active);
        var users = new FakeUserRepository();
        var supervisor = NewUser(
            school.Id,
            RoleNames.SubjectSupervisor);
        var admin = NewUser(school.Id, RoleNames.SchoolAdmin);
        users.Seed(supervisor);
        users.Seed(admin);
        var schools = new FakeSchoolRepository();
        schools.Seed(school);
        var service = new SchoolUserManagementService(users, schools);

        Assert.True((await service.CreateAsync(
            supervisor.Id,
            school.Id,
            new CreateSchoolUserRequest(
                "new-teacher@example.com",
                RoleNames.Teacher))).Succeeded);

        Assert.True((await service.CreateAsync(
            supervisor.Id,
            school.Id,
            new CreateSchoolUserRequest(
                "new-student@example.com",
                RoleNames.Student))).Succeeded);

        foreach (var privilegedRole in new[]
                 {
                     RoleNames.SchoolAdmin,
                     RoleNames.SubjectSupervisor
                 })
        {
            var denied = await service.CreateAsync(
                supervisor.Id,
                school.Id,
                new CreateSchoolUserRequest(
                    $"blocked-{Guid.NewGuid():N}@example.com",
                    privilegedRole));

            Assert.False(denied.Succeeded);
            Assert.Contains(
                denied.Errors,
                x => x.Code == SchoolUserErrorCode.UserAccessDenied);
        }

        var privilegedTarget = await service.SetLockedAsync(
            supervisor.Id,
            school.Id,
            admin.Id,
            true);

        Assert.False(privilegedTarget.Succeeded);
        Assert.Contains(
            privilegedTarget.Errors,
            x => x.Code == SchoolUserErrorCode.UserAccessDenied);

        var teacher = users.Users.Values.Single(
            x => x.Email == "new-teacher@example.com");

        var elevate = await service.ChangeRoleAsync(
            supervisor.Id,
            school.Id,
            teacher.Id,
            RoleNames.SubjectSupervisor);

        Assert.False(elevate.Succeeded);
        Assert.Contains(
            elevate.Errors,
            x => x.Code == SchoolUserErrorCode.UserAccessDenied);
    }

    private static Fixture CreateFixture()
    {
        var school = NewSchool(
            SchoolStatus.Active);

        var users = new FakeUserRepository();

        var superAdmin = NewUser(
            null,
            RoleNames.SuperAdmin);

        var target = NewUser(
            school.Id,
            RoleNames.Teacher);

        users.Seed(superAdmin);
        users.Seed(target);

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        return new Fixture(
            school,
            superAdmin,
            target,
            users,
            new SchoolUserManagementService(
                users,
                schools));
    }

    private static SchoolAdminFixture
        CreateSchoolAdminSelfFixture()
    {
        var school = NewSchool(
            SchoolStatus.Active);

        var users = new FakeUserRepository();

        var admin = NewUser(
            school.Id,
            RoleNames.SchoolAdmin);

        users.Seed(admin);

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        return new SchoolAdminFixture(
            school,
            admin,
            new SchoolUserManagementService(
                users,
                schools));
    }

    private static School NewSchool(
        SchoolStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Acceptance School",
            SchoolCode = "ACC-001",
            NormalizedSchoolCode = "ACC-001",
            Status = status,
            CountryCode = "PL",
            City = "Warsaw",
            ContactEmail =
                "school@example.com",
            DefaultCulture = "en",
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            ArchivedAtUtc =
                status == SchoolStatus.Archived
                    ? DateTime.UtcNow
                    : null,
            RowVersion =
                BitConverter.GetBytes(1L)
        };

    private static SchoolUserRecord NewUser(
        Guid? schoolId,
        string role,
        string? email = null) =>
        new(
            Guid.NewGuid(),
            schoolId,
            email ??
            $"{Guid.NewGuid():N}@example.com",
            true,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            [role]);

    private sealed record Fixture(
        School School,
        SchoolUserRecord SuperAdmin,
        SchoolUserRecord Target,
        FakeUserRepository Users,
        SchoolUserManagementService Service);

    private sealed record SchoolAdminFixture(
        School School,
        SchoolUserRecord Admin,
        SchoolUserManagementService Service);

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
                    .Where(
                        x => x.SchoolId == schoolId)
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

            var user =
                new SchoolUserRecord(
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
                    "acceptance-token"));
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
                    "acceptance-token");
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
                    : SchoolUserPersistenceResult.Success(
                        user));
        }

        private Task<SchoolUserPersistenceResult> Update(
            Guid schoolId,
            Guid userId,
            Func<
                SchoolUserRecord,
                SchoolUserRecord> mutation)
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

            var updated = mutation(user);
            Users[userId] = updated;

            return Task.FromResult(
                SchoolUserPersistenceResult.Success(
                    updated));
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

        public Task<bool>
            ExistsByNormalizedCodeAsync(
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

        public Task<SchoolRepositoryWriteResult>
            SaveAsync(
                School school,
                byte[]? expectedRowVersion,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                SchoolRepositoryWriteResult.Success);
    }
}
