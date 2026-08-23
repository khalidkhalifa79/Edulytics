using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Edulytics.Services.Auditing;
using Edulytics.Services.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase25C;

public sealed class Phase25CCoreTests
{
    [Fact]
    public void CommercialPolicy_MatchesAcceptedPhase25A()
    {
        Assert.Equal(
            500,
            SubscriptionCommercialPolicy.MinimumCommittedSeats);

        Assert.Equal(
            20m,
            SubscriptionCommercialPolicy.MonthlyUnitPrice(
                SubscriptionTerm.ThreeMonths));

        Assert.Equal(
            15m,
            SubscriptionCommercialPolicy.MonthlyUnitPrice(
                SubscriptionTerm.SixMonths));

        Assert.Equal(
            10m,
            SubscriptionCommercialPolicy.MonthlyUnitPrice(
                SubscriptionTerm.SchoolYearTenMonths));

        Assert.Equal(
            3,
            SubscriptionCommercialPolicy.Months(
                SubscriptionTerm.ThreeMonths));

        Assert.Equal(
            6,
            SubscriptionCommercialPolicy.Months(
                SubscriptionTerm.SixMonths));

        Assert.Equal(
            10,
            SubscriptionCommercialPolicy.Months(
                SubscriptionTerm.SchoolYearTenMonths));

        Assert.True(
            SubscriptionCommercialPolicy.TryCurrency(
                "PL",
                out var pl));

        Assert.Equal(
            CommercialCurrency.PLN,
            pl);

        Assert.True(
            SubscriptionCommercialPolicy.TryCurrency(
                "AE",
                out var ae));

        Assert.Equal(
            CommercialCurrency.AED,
            ae);

        Assert.False(
            SubscriptionCommercialPolicy.TryCurrency(
                "US",
                out _));
    }

    [Fact]
    public void Model_UsesTenantSafeHistoryAndConcurrencyToken()
    {
        using var db = NewDb();

        var entity =
            db.Model.FindEntityType(
                typeof(SchoolSubscription));

        Assert.NotNull(entity);

        Assert.True(
            entity!
                .FindProperty(
                    nameof(SchoolSubscription.RowVersion))!
                .IsConcurrencyToken);

        Assert.Contains(
            entity.GetIndexes(),
            x =>
                x.IsUnique &&
                x.Properties.Count == 1 &&
                x.Properties[0].Name ==
                    nameof(SchoolSubscription.SchoolId));

        var history =
            db.Model.FindEntityType(
                typeof(SubscriptionSeatChange));

        Assert.NotNull(history);

        Assert.Contains(
            history!.GetForeignKeys(),
            x =>
                x.PrincipalEntityType.ClrType ==
                    typeof(SchoolSubscription) &&
                x.Properties.Select(p => p.Name)
                    .SequenceEqual(
                        [
                            nameof(SubscriptionSeatChange.SchoolId),
                            nameof(SubscriptionSeatChange.SubscriptionId)
                        ]));
    }

    [Fact]
    public async Task Create_RequiresMinimum500Seats()
    {
        await using var fixture =
            await Fixture.CreateAsync(
                SchoolStatus.Suspended);

        var result =
            await fixture.Service.CreateAsync(
                fixture.SuperAdmin.Id,
                new CreateSubscriptionRequest(
                    fixture.School.Id,
                    SubscriptionTerm.ThreeMonths,
                    SubscriptionBillingCadence
                        .MonthlyInstallments,
                    499,
                    AutoRenew: true));

        Assert.False(result.Succeeded);
        Assert.Equal(
            SubscriptionErrorCode.InvalidCommittedSeats,
            result.Error);
    }

    [Fact]
    public async Task Create_PersistsLaunchCommercialTermsAndAudit()
    {
        await using var fixture =
            await Fixture.CreateAsync(
                SchoolStatus.Suspended,
                countryCode: "PL");

        var result =
            await fixture.CreateSubscriptionAsync(
                committedSeats: 500,
                autoRenew: true);

        Assert.True(result.Succeeded);

        var subscription =
            Assert.IsType<SchoolSubscriptionDetails>(
                result.Subscription);

        Assert.Equal(
            SubscriptionStatus.PendingActivation,
            subscription.Status);

        Assert.Equal(
            CommercialCurrency.PLN,
            subscription.CommercialCurrency);

        Assert.Equal(
            20m,
            subscription.PricePerStudentPerMonth);

        Assert.Equal(
            500,
            subscription.CommittedSeats);

        Assert.True(subscription.AutoRenew);

        Assert.Single(
            fixture.Db.SubscriptionSeatChanges);

        Assert.Contains(
            fixture.Audit.Events,
            x =>
                x.Action == "Subscription.Created" &&
                x.SchoolId == fixture.School.Id &&
                x.Feature == "Subscriptions");
    }

    [Fact]
    public async Task Activate_SetsExactTermAndActivatesSchool()
    {
        await using var fixture =
            await Fixture.CreateAsync(
                SchoolStatus.Suspended);

        var created =
            await fixture.CreateSubscriptionAsync();

        var activation =
            DateTime.UtcNow.AddHours(-1);

        var activated =
            await fixture.Service.ActivateAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                activation,
                created.Subscription!.RowVersion);

        Assert.True(activated.Succeeded);

        Assert.Equal(
            SubscriptionStatus.Active,
            activated.Subscription!.Status);

        Assert.Equal(
            activation,
            activated.Subscription.ActivatedAtUtc);

        Assert.Equal(
            activation.AddMonths(3),
            activated.Subscription
                .CurrentTermEndsAtUtc);

        var school =
            await fixture.Db.Schools
                .SingleAsync();

        Assert.Equal(
            SchoolStatus.Active,
            school.Status);
    }

    [Fact]
    public async Task Increase_IsImmediate_ButReductionWaitsForRenewal()
    {
        await using var fixture =
            await Fixture.CreateAsync(
                SchoolStatus.Suspended);

        var created =
            await fixture.CreateSubscriptionAsync();

        var increased =
            await fixture.Service.IncreaseSeatsAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                650,
                created.Subscription!.RowVersion);

        Assert.True(increased.Succeeded);
        Assert.Equal(
            650,
            increased.Subscription!.CommittedSeats);

        var scheduled =
            await fixture.Service
                .ScheduleRenewalSeatReductionAsync(
                    fixture.SuperAdmin.Id,
                    fixture.School.Id,
                    550,
                    increased.Subscription.RowVersion);

        Assert.True(scheduled.Succeeded);

        Assert.Equal(
            650,
            scheduled.Subscription!.CommittedSeats);

        Assert.Equal(
            550,
            scheduled.Subscription.PendingRenewalSeats);

        var changes =
            await fixture.Db.SubscriptionSeatChanges
                .OrderBy(x => x.CreatedAtUtc)
                .ToArrayAsync();

        Assert.Equal(2, changes.Length);

        Assert.Equal(
            SeatCommitmentChangeType.Increase,
            changes[1].ChangeType);

        Assert.Equal(500, changes[1].PreviousSeats);
        Assert.Equal(650, changes[1].NewSeats);
    }

    [Fact]
    public async Task Renewal_AppliesScheduledReductionOnlyAtBoundary()
    {
        await using var fixture =
            await Fixture.CreateAsync(
                SchoolStatus.Suspended);

        var created =
            await fixture.CreateSubscriptionAsync(
                committedSeats: 700);

        var activation =
            DateTime.UtcNow
                .AddMonths(-3)
                .AddMinutes(-5);

        var activated =
            await fixture.Service.ActivateAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                activation,
                created.Subscription!.RowVersion);

        var scheduled =
            await fixture.Service
                .ScheduleRenewalSeatReductionAsync(
                    fixture.SuperAdmin.Id,
                    fixture.School.Id,
                    600,
                    activated.Subscription!.RowVersion);

        var renewed =
            await fixture.Service.RenewAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                scheduled.Subscription!.RowVersion);

        Assert.True(renewed.Succeeded);
        Assert.Equal(
            600,
            renewed.Subscription!.CommittedSeats);
        Assert.Null(
            renewed.Subscription.PendingRenewalSeats);

        Assert.Equal(
            activation.AddMonths(3),
            renewed.Subscription
                .CurrentTermStartsAtUtc);

        Assert.Equal(
            activation.AddMonths(6),
            renewed.Subscription
                .CurrentTermEndsAtUtc);
    }

    [Fact]
    public async Task Renewal_CannotDropBelowActiveStudents()
    {
        await using var fixture =
            await Fixture.CreateAsync(
                SchoolStatus.Suspended);

        var created =
            await fixture.CreateSubscriptionAsync(
                committedSeats: 700);

        var activation =
            DateTime.UtcNow
                .AddMonths(-3)
                .AddMinutes(-5);

        var activated =
            await fixture.Service.ActivateAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                activation,
                created.Subscription!.RowVersion);

        var scheduled =
            await fixture.Service
                .ScheduleRenewalSeatReductionAsync(
                    fixture.SuperAdmin.Id,
                    fixture.School.Id,
                    500,
                    activated.Subscription!.RowVersion);

        for (var i = 0; i < 501; i++)
        {
            fixture.Db.StudentProfiles.Add(
                NewStudent(
                    fixture.School.Id,
                    i,
                    AcademicStructureStatus.Active));
        }

        await fixture.Db.SaveChangesAsync();

        var renewed =
            await fixture.Service.RenewAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                scheduled.Subscription!.RowVersion);

        Assert.False(renewed.Succeeded);

        Assert.Equal(
            SubscriptionErrorCode
                .RenewalBelowActiveStudents,
            renewed.Error);
    }

    [Fact]
    public async Task AutoRenew_Requires30DayNonRenewalNotice()
    {
        await using var fixture =
            await Fixture.CreateAsync(
                SchoolStatus.Suspended);

        var created =
            await fixture.CreateSubscriptionAsync(
                autoRenew: true);

        var activation =
            DateTime.UtcNow
                .AddMonths(-3)
                .AddDays(20);

        var activated =
            await fixture.Service.ActivateAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                activation,
                created.Subscription!.RowVersion);

        var result =
            await fixture.Service.SetAutoRenewAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                false,
                activated.Subscription!.RowVersion);

        Assert.False(result.Succeeded);

        Assert.Equal(
            SubscriptionErrorCode
                .AutoRenewNoticeTooLate,
            result.Error);
    }

    [Fact]
    public async Task SuspendAndReactivate_SynchronizeSchoolStatus()
    {
        await using var fixture =
            await Fixture.CreateAsync(
                SchoolStatus.Suspended);

        var created =
            await fixture.CreateSubscriptionAsync();

        var activated =
            await fixture.Service.ActivateAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                DateTime.UtcNow,
                created.Subscription!.RowVersion);

        var suspended =
            await fixture.Service.SuspendAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                activated.Subscription!.RowVersion);

        Assert.True(suspended.Succeeded);
        Assert.Equal(
            SubscriptionStatus.Suspended,
            suspended.Subscription!.Status);

        Assert.Equal(
            SchoolStatus.Suspended,
            (await fixture.Db.Schools.SingleAsync()).Status);

        var reactivated =
            await fixture.Service.ReactivateAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                suspended.Subscription.RowVersion);

        Assert.True(reactivated.Succeeded);
        Assert.Equal(
            SubscriptionStatus.Active,
            reactivated.Subscription!.Status);

        Assert.Equal(
            SchoolStatus.Active,
            (await fixture.Db.Schools.SingleAsync()).Status);
    }

    [Fact]
    public async Task Entitlement_CountsOnlyCurrentActiveStudents()
    {
        await using var fixture =
            await Fixture.CreateAsync(
                SchoolStatus.Suspended);

        var created =
            await fixture.CreateSubscriptionAsync();

        var activated =
            await fixture.Service.ActivateAsync(
                fixture.SuperAdmin.Id,
                fixture.School.Id,
                DateTime.UtcNow.AddMinutes(-1),
                created.Subscription!.RowVersion);

        Assert.True(activated.Succeeded);

        for (var i = 0; i < 499; i++)
        {
            fixture.Db.StudentProfiles.Add(
                NewStudent(
                    fixture.School.Id,
                    i,
                    AcademicStructureStatus.Active));
        }

        fixture.Db.StudentProfiles.Add(
            NewStudent(
                fixture.School.Id,
                9000,
                AcademicStructureStatus.Inactive));

        await fixture.Db.SaveChangesAsync();

        var oneMore =
            await fixture.Service
                .EvaluateEntitlementsAsync(
                    fixture.School.Id,
                    additionalActiveStudents: 1);

        Assert.True(oneMore.IsCommerciallyManaged);
        Assert.True(oneMore.OperationalAccessAllowed);
        Assert.True(oneMore.SeatCapacityAvailable);
        Assert.Equal(499, oneMore.ActiveStudents);
        Assert.Equal(1, oneMore.AvailableSeats);

        var twoMore =
            await fixture.Service
                .EvaluateEntitlementsAsync(
                    fixture.School.Id,
                    additionalActiveStudents: 2);

        Assert.False(twoMore.SeatCapacityAvailable);
    }

    private static StudentProfile NewStudent(
        Guid schoolId,
        int n,
        AcademicStructureStatus status) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            StudentNumber = $"S{n:00000}",
            NormalizedStudentNumber =
                $"S{n:00000}",
            FirstName = "Test",
            LastName = $"Student {n}",
            DisplayName = $"Test Student {n}",
            Status = status,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    private static EdulyticsDbContext NewDb()
    {
        var options =
            new DbContextOptionsBuilder<
                EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString("N"))
                .Options;

        return new EdulyticsDbContext(options);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private Fixture(
            EdulyticsDbContext db,
            School school,
            SchoolUserRecord superAdmin,
            RecordingAudit audit,
            SchoolSubscriptionService service)
        {
            Db = db;
            School = school;
            SuperAdmin = superAdmin;
            Audit = audit;
            Service = service;
        }

        public EdulyticsDbContext Db { get; }
        public School School { get; }
        public SchoolUserRecord SuperAdmin { get; }
        public RecordingAudit Audit { get; }
        public SchoolSubscriptionService Service { get; }

        public static async Task<Fixture> CreateAsync(
            SchoolStatus status,
            string countryCode = "PL")
        {
            var db = NewDb();

            var school =
                new School
                {
                    Id = Guid.NewGuid(),
                    Name = "Phase25C School",
                    SchoolCode = "P25C",
                    NormalizedSchoolCode = "P25C",
                    Status = status,
                    CountryCode = countryCode,
                    City = "Warsaw",
                    ContactEmail =
                        "school@example.com",
                    DefaultCulture = "en",
                    TimeZoneId = "Europe/Warsaw",
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow,
                    RowVersion = []
                };

            db.Schools.Add(school);
            await db.SaveChangesAsync();

            var superAdmin =
                new SchoolUserRecord(
                    Guid.NewGuid(),
                    null,
                    "admin@example.com",
                    true,
                    false,
                    DateTime.UtcNow,
                    DateTime.UtcNow,
                    [RoleNames.SuperAdmin]);

            var users =
                new FakeUsers(superAdmin);

            var audit =
                new RecordingAudit();

            var service =
                new SchoolSubscriptionService(
                    new SchoolSubscriptionRepository(db),
                    new SchoolRepository(db),
                    users,
                    audit,
                    new NoOpTransactionManager());

            return new Fixture(
                db,
                school,
                superAdmin,
                audit,
                service);
        }

        public Task<SubscriptionCommandResult>
            CreateSubscriptionAsync(
                int committedSeats = 500,
                bool autoRenew = true) =>
            Service.CreateAsync(
                SuperAdmin.Id,
                new CreateSubscriptionRequest(
                    School.Id,
                    SubscriptionTerm.ThreeMonths,
                    SubscriptionBillingCadence
                        .MonthlyInstallments,
                    committedSeats,
                    autoRenew));

        public ValueTask DisposeAsync() =>
            Db.DisposeAsync();
    }

    private sealed class FakeUsers : ISchoolUserRepository
    {
        private readonly SchoolUserRecord _actor;

        public FakeUsers(SchoolUserRecord actor)
        {
            _actor = actor;
        }

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

        public Task<SchoolUserRecord?>
            GetBySchoolAndIdAsync(
                Guid schoolId,
                Guid userId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<SchoolUserRecord?>(null);

        public Task<SchoolUserPersistenceResult> CreateAsync(
            Guid schoolId,
            string email,
            string role,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                SchoolUserPersistenceResult.Failure(
                    SchoolUserPersistenceError.IdentityFailure));

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

    public sealed class RecordingAudit : IAuditService
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

    private sealed class NoOpTransactionManager
        : IApplicationTransactionManager
    {
        public Task<IApplicationTransaction> BeginAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IApplicationTransaction>(
                new NoOpTransaction());

        private sealed class NoOpTransaction
            : IApplicationTransaction
        {
            public Task CommitAsync(
                CancellationToken cancellationToken = default) =>
                Task.CompletedTask;

            public Task RollbackAsync(
                CancellationToken cancellationToken = default) =>
                Task.CompletedTask;

            public ValueTask DisposeAsync() =>
                ValueTask.CompletedTask;
        }
    }
}
