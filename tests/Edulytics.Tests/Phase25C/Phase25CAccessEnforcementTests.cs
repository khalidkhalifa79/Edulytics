using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Subscriptions;
using Edulytics.Core.Users;
using Edulytics.Services.Users;

namespace Edulytics.Tests.Phase25C;

public sealed class Phase25CAccessEnforcementTests
{
    [Fact]
    public async Task SuperAdmin_RemainsPlatformAllowed()
    {
        var actor = User(
            schoolId: null,
            RoleNames.SuperAdmin);

        var service = Service(
            actor,
            school: null,
            subscription: null);

        var decision =
            await service.EvaluateSignInAsync(actor.Id);

        Assert.True(decision.Allowed);
        Assert.True(decision.IsPlatformAdministrator);
    }

    [Fact]
    public async Task LegacyActiveSchool_WithoutSubscription_RemainsAllowed()
    {
        var school = ActiveSchool();

        var actor = User(
            school.Id,
            RoleNames.SchoolAdmin);

        var service = Service(
            actor,
            school,
            subscription: null);

        var decision =
            await service.EvaluateSignInAsync(actor.Id);

        Assert.True(decision.Allowed);
        Assert.False(decision.IsPlatformAdministrator);
        Assert.Equal(school.Id, decision.SchoolId);
    }

    [Fact]
    public async Task ActiveCurrentSubscription_AllowsSchoolAccess()
    {
        var school = ActiveSchool();

        var actor = User(
            school.Id,
            RoleNames.Teacher);

        var service = Service(
            actor,
            school,
            Subscription(
                school.Id,
                SubscriptionStatus.Active,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(20)));

        var decision =
            await service.EvaluateSignInAsync(actor.Id);

        Assert.True(decision.Allowed);
    }

    [Theory]
    [InlineData(SubscriptionStatus.PendingActivation)]
    [InlineData(SubscriptionStatus.Suspended)]
    [InlineData(SubscriptionStatus.Ended)]
    public async Task NonOperationalSubscription_DeniesSchoolAccess(
        SubscriptionStatus status)
    {
        var school = ActiveSchool();

        var actor = User(
            school.Id,
            RoleNames.SchoolAdmin);

        var service = Service(
            actor,
            school,
            Subscription(
                school.Id,
                status,
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(20)));

        var decision =
            await service.EvaluateSignInAsync(actor.Id);

        Assert.False(decision.Allowed);

        Assert.False(
            await service.CanManageUsersAsync(actor.Id));
    }

    [Fact]
    public async Task ExpiredActiveSubscription_DeniesSchoolAccess()
    {
        var school = ActiveSchool();

        var actor = User(
            school.Id,
            RoleNames.Student);

        var service = Service(
            actor,
            school,
            Subscription(
                school.Id,
                SubscriptionStatus.Active,
                DateTime.UtcNow.AddDays(-20),
                DateTime.UtcNow.AddMinutes(-1)));

        var decision =
            await service.EvaluateSignInAsync(actor.Id);

        Assert.False(decision.Allowed);
    }

    [Fact]
    public async Task FutureActiveSubscription_DeniesSchoolAccess()
    {
        var school = ActiveSchool();

        var actor = User(
            school.Id,
            RoleNames.SubjectSupervisor);

        var service = Service(
            actor,
            school,
            Subscription(
                school.Id,
                SubscriptionStatus.Active,
                DateTime.UtcNow.AddHours(1),
                DateTime.UtcNow.AddDays(20)));

        var decision =
            await service.EvaluateSignInAsync(actor.Id);

        Assert.False(decision.Allowed);
    }

    private static SchoolUserManagementService Service(
        SchoolUserRecord actor,
        School? school,
        SchoolSubscription? subscription) =>
        new(
            new FakeUsers(actor),
            new FakeSchools(school),
            audit: null,
            transactions: null,
            onboarding: null,
            subscriptions:
                new FakeSubscriptions(subscription));

    private static School ActiveSchool() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Phase25C Access School",
            SchoolCode = "P25ACCESS",
            NormalizedSchoolCode = "P25ACCESS",
            Status = SchoolStatus.Active,
            CountryCode = "PL",
            City = "Warsaw",
            ContactEmail = "school@example.com",
            DefaultCulture = "en",
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = [1]
        };

    private static SchoolUserRecord User(
        Guid? schoolId,
        string role) =>
        new(
            Guid.NewGuid(),
            schoolId,
            "actor@example.com",
            true,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            [role]);

    private static SchoolSubscription Subscription(
        Guid schoolId,
        SubscriptionStatus status,
        DateTime startsAt,
        DateTime endsAt) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Term = SubscriptionTerm.ThreeMonths,
            BillingCadence =
                SubscriptionBillingCadence.MonthlyInstallments,
            CommercialCurrency = CommercialCurrency.PLN,
            PricePerStudentPerMonth = 20m,
            CommittedSeats = 500,
            AutoRenew = true,
            Status = status,
            ActivatedAtUtc = startsAt,
            CurrentTermStartsAtUtc = startsAt,
            CurrentTermEndsAtUtc = endsAt,
            CreatedAtUtc = startsAt,
            UpdatedAtUtc = startsAt,
            RowVersion = [1]
        };

    private sealed class FakeUsers : ISchoolUserRepository
    {
        private readonly SchoolUserRecord _actor;

        public FakeUsers(SchoolUserRecord actor) =>
            _actor = actor;

        public Task<SchoolUserRecord?> GetActorAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SchoolUserRecord?>(
                userId == _actor.Id
                    ? _actor
                    : null);

        public Task<IReadOnlyList<SchoolUserRecord>>
            ListBySchoolAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<
                IReadOnlyList<SchoolUserRecord>>([]);

        public Task<SchoolUserRecord?> GetBySchoolAndIdAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<SchoolUserRecord?>(null);

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
                    SchoolUserPersistenceError.IdentityFailure));
    }

    private sealed class FakeSchools : ISchoolRepository
    {
        private readonly School? _school;

        public FakeSchools(School? school) =>
            _school = school;

        public Task<IReadOnlyList<School>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<School>>(
                _school is null
                    ? []
                    : [_school]);

        public Task<School?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _school?.Id == id
                    ? _school
                    : null);

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
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<SchoolRepositoryWriteResult> SaveAsync(
            School school,
            byte[]? expectedRowVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                SchoolRepositoryWriteResult.Success);
    }

    private sealed class FakeSubscriptions
        : ISchoolSubscriptionRepository
    {
        private readonly SchoolSubscription? _subscription;

        public FakeSubscriptions(
            SchoolSubscription? subscription) =>
            _subscription = subscription;

        public Task<IReadOnlyList<SchoolSubscription>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<
                IReadOnlyList<SchoolSubscription>>(
                    _subscription is null
                        ? []
                        : [_subscription]);

        public Task<SchoolSubscription?> GetBySchoolAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _subscription?.SchoolId == schoolId
                    ? _subscription
                    : null);

        public Task<SchoolSubscription?>
            GetForUpdateBySchoolAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            GetBySchoolAsync(
                schoolId,
                cancellationToken);

        public Task<int> CountActiveStudentsAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);

        public Task<bool> HasActiveStudentProfileForUserAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<SubscriptionPersistenceResult> AddAsync(
            SchoolSubscription subscription,
            SubscriptionSeatChange initialSeatChange,
            CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SubscriptionPersistenceResult> SaveAsync(
            SchoolSubscription subscription,
            byte[] expectedRowVersion,
            SubscriptionSeatChange? seatChange = null,
            CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SubscriptionPersistenceResult>
            SaveWithSchoolAsync(
                SchoolSubscription subscription,
                byte[] expectedSubscriptionRowVersion,
                School school,
                byte[] expectedSchoolRowVersion,
                SubscriptionSeatChange? seatChange = null,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        private static Task<SubscriptionPersistenceResult>
            Unsupported() =>
            Task.FromResult(
                SubscriptionPersistenceResult.Failure(
                    SubscriptionPersistenceError.Unknown));
    }
}
