using System.Text.Json;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Notifications;
using Edulytics.Core.Users;
using Edulytics.Data.Contexts;
using Edulytics.Services.Auditing;
using Edulytics.Services.Notifications;
using Edulytics.Web.Email;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase21;

public sealed class Phase21NotificationTests
{
    [Fact]
    public async Task InvitationQueue_IsDurableAndContainsNoToken()
    {
        var f = Fixture.Create();

        var result =
            await f.Service
                .QueuePasswordSetupInvitationAsync(
                    f.Admin.Id,
                    f.Teacher.Id,
                    "en",
                    "https://staging.example.com",
                    "initial");

        Assert.True(result.Succeeded);

        Assert.Single(
            f.Notifications.Notifications);

        Assert.Single(
            f.Notifications.Deliveries);

        var outbox =
            Assert.Single(
                f.Notifications.Outbox);

        Assert.Equal(
            NotificationEventTypes
                .DeliveryRequested,
            outbox.EventType);

        Assert.DoesNotContain(
            "token",
            outbox.PayloadJson,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            f.Teacher.Email,
            outbox.PayloadJson,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "set-password",
            outbox.PayloadJson,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            "https://staging.example.com",
            f.Notifications
                .Deliveries[0]
                .BaseUrl);
    }

    [Fact]
    public async Task SameInvitationReason_IsDeduplicated()
    {
        var f = Fixture.Create();

        var first =
            await f.Service
                .QueuePasswordSetupInvitationAsync(
                    f.Admin.Id,
                    f.Teacher.Id,
                    "en",
                    "https://example.com",
                    "initial");

        var second =
            await f.Service
                .QueuePasswordSetupInvitationAsync(
                    f.Admin.Id,
                    f.Teacher.Id,
                    "en",
                    "https://example.com",
                    "initial");

        Assert.True(first.Succeeded);
        Assert.True(second.Succeeded);
        Assert.True(second.Deduplicated);

        Assert.Single(
            f.Notifications.Notifications);

        Assert.Single(
            f.Notifications.Deliveries);

        Assert.Single(
            f.Notifications.Outbox);
    }

    [Fact]
    public async Task Resend_ReusesInboxNotification_ButQueuesNewDelivery()
    {
        var f = Fixture.Create();

        await f.Service
            .QueuePasswordSetupInvitationAsync(
                f.Admin.Id,
                f.Teacher.Id,
                "en",
                "https://example.com",
                "initial");

        await f.Service
            .QueuePasswordSetupInvitationAsync(
                f.Admin.Id,
                f.Teacher.Id,
                "pl",
                "https://example.com",
                "resend:202608201312");

        Assert.Single(
            f.Notifications.Notifications);

        Assert.Equal(
            2,
            f.Notifications.Deliveries.Count);

        Assert.Equal(
            2,
            f.Notifications.Outbox.Count);
    }

    [Fact]
    public async Task CrossSchoolAdmin_CannotQueueInvitation()
    {
        var f = Fixture.Create();

        var result =
            await f.Service
                .QueuePasswordSetupInvitationAsync(
                    f.OtherAdmin.Id,
                    f.Teacher.Id,
                    "en",
                    "https://example.com",
                    "initial");

        Assert.False(result.Succeeded);

        Assert.Equal(
            NotificationErrorCode.AccessDenied,
            result.Error);

        Assert.Empty(
            f.Notifications.Notifications);
    }

    [Fact]
    public async Task Inbox_IsRecipientScoped()
    {
        var f = Fixture.Create();

        await f.Service
            .QueuePasswordSetupInvitationAsync(
                f.Admin.Id,
                f.Teacher.Id,
                "en",
                "https://example.com",
                "initial");

        var teacher =
            await f.Service.ListInboxAsync(
                f.Teacher.Id);

        Assert.Single(
            Assert.IsAssignableFrom<
                IReadOnlyList<
                    NotificationInboxItem>>(
                teacher.Value));

        var other =
            await f.Service.ListInboxAsync(
                f.OtherSchoolUser.Id);

        Assert.Empty(
            Assert.IsAssignableFrom<
                IReadOnlyList<
                    NotificationInboxItem>>(
                other.Value));
    }

    [Fact]
    public async Task Recipient_CanMarkReadAndUnread()
    {
        var f = Fixture.Create();

        var queued =
            await f.Service
                .QueuePasswordSetupInvitationAsync(
                    f.Admin.Id,
                    f.Teacher.Id,
                    "en",
                    "https://example.com",
                    "initial");

        var id =
            Assert.IsType<Guid>(
                queued.NotificationId);

        var read =
            await f.Service
                .SetReadStateAsync(
                    f.Teacher.Id,
                    id,
                    true);

        Assert.NotNull(
            read.Value?.ReadAtUtc);

        var unread =
            await f.Service
                .SetReadStateAsync(
                    f.Teacher.Id,
                    id,
                    false);

        Assert.Null(
            unread.Value?.ReadAtUtc);
    }

    [Fact]
    public async Task OtherUser_CannotChangeReadState()
    {
        var f = Fixture.Create();

        var queued =
            await f.Service
                .QueuePasswordSetupInvitationAsync(
                    f.Admin.Id,
                    f.Teacher.Id,
                    "en",
                    "https://example.com",
                    "initial");

        var id =
            Assert.IsType<Guid>(
                queued.NotificationId);

        var result =
            await f.Service
                .SetReadStateAsync(
                    f.Admin.Id,
                    id,
                    true);

        Assert.Null(result.Value);

        Assert.Equal(
            NotificationErrorCode.NotFound,
            result.Error);
    }

    [Fact]
    public void CircuitBreaker_OpensAndRecovers()
    {
        var circuit =
            new EmailConnectorCircuitBreaker();

        var now =
            DateTime.UtcNow;

        Assert.True(
            circuit.CanExecute(now));

        circuit.RecordFailure(
            now,
            2,
            30);

        Assert.True(
            circuit.CanExecute(
                now.AddSeconds(1)));

        circuit.RecordFailure(
            now.AddSeconds(1),
            2,
            30);

        Assert.False(
            circuit.CanExecute(
                now.AddSeconds(2)));

        Assert.True(
            circuit.CanExecute(
                now.AddSeconds(32)));
    }

    [Fact]
    public void DeliveryPersistence_HasNoTokenOrSetupUrlProperty()
    {
        var names =
            typeof(NotificationDeliveryJob)
                .GetProperties()
                .Select(x => x.Name)
                .ToArray();

        Assert.DoesNotContain(
            names,
            x =>
                x.Contains(
                    "Token",
                    StringComparison.OrdinalIgnoreCase));

        Assert.DoesNotContain(
            names,
            x =>
                x.Contains(
                    "SetupUrl",
                    StringComparison.OrdinalIgnoreCase));

        var payload =
            JsonSerializer.Serialize(
                new NotificationDeliveryRequestedEvent(
                    Guid.NewGuid(),
                    Guid.NewGuid()));

        Assert.DoesNotContain(
            "token",
            payload,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EfModel_HasNotificationDedupAndConcurrency()
    {
        var options =
            new DbContextOptionsBuilder<
                    EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid()
                        .ToString())
                .Options;

        using var db =
            new EdulyticsDbContext(
                options);

        var notification =
            db.Model.FindEntityType(
                typeof(UserNotification));

        var delivery =
            db.Model.FindEntityType(
                typeof(
                    NotificationDeliveryJob));

        Assert.NotNull(notification);
        Assert.NotNull(delivery);

        Assert.Contains(
            notification!.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties
                    .Select(x => x.Name)
                    .SequenceEqual(
                        new[]
                        {
                            "SchoolId",
                            "RecipientUserId",
                            "DeduplicationKey"
                        }));

        Assert.Contains(
            delivery!.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties
                    .Select(x => x.Name)
                    .SequenceEqual(
                        new[]
                        {
                            "SchoolId",
                            "DeduplicationKey"
                        }));

        Assert.True(
            notification
                .FindProperty("RowVersion")!
                .IsConcurrencyToken);

        Assert.True(
            delivery
                .FindProperty("RowVersion")!
                .IsConcurrencyToken);
    }

    [Fact]
    public void WebRegistration_UsesDurableFacadeNotInlineMailKit()
    {
        var root = FindRoot();

        var registration =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Extensions",
                    "InvitationEmailRegistrationExtensions.cs"));

        Assert.Contains(
                "IUserInvitationDeliveryService",
                registration);

            Assert.Contains(
                "DurableUserInvitationDeliveryService",
                registration);

        Assert.Contains(
            "IUserInvitationConnector",
            registration);

        var controller =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Controllers",
                    "SchoolUsersController.cs"));

        Assert.Contains(
            "\"initial\"",
            controller);

        Assert.Contains(
            "\"resend\"",
            controller);
    }

    [Fact]
    public void OutboxAndConnectorContracts_ArePresent()
    {
        var root = FindRoot();

        var outbox =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Background",
                    "OutboxProcessorBackgroundService.cs"));

        Assert.Contains(
            "NotificationEventTypes.DeliveryRequested",
            outbox);

        Assert.Contains(
            "MarkNotificationDeliveryDeadLetteredAsync",
            outbox);

        var smtp =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Email",
                    "MailKitUserInvitationDeliveryService.cs"));

        Assert.Contains(
            "CancelAfter",
            smtp);

        Assert.Contains(
            "CircuitOpen",
            smtp);

        Assert.DoesNotContain(
            "setupUrl}",
            smtp,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NotificationUi_IsLocalizedAndDashboardLinked()
    {
        var root = FindRoot();

        var en =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Resources",
                    "NotificationResource.resx"));

        var pl =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Resources",
                    "NotificationResource.pl.resx"));

        Assert.Contains(
            "NotificationAccountInvitationTitle",
            en);

        Assert.Contains(
            "NotificationAccountInvitationTitle",
            pl);

        var dashboard =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Views",
                    "SchoolHome",
                    "Dashboard.cshtml"));

        Assert.Contains(
            "asp-controller=\"Notifications\"",
            dashboard);
    }

    private static string FindRoot()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (
            directory is not null &&
            !File.Exists(
                Path.Combine(
                    directory.FullName,
                    "Edulytics.sln")))
        {
            directory =
                directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException(
                "Repository root not found.");
    }

    private sealed class Fixture
    {
        public required School School { get; init; }
        public required SchoolUserRecord Admin { get; init; }
        public required SchoolUserRecord Teacher { get; init; }
        public required SchoolUserRecord OtherAdmin { get; init; }
        public required SchoolUserRecord OtherSchoolUser { get; init; }

        public required FakeNotificationRepository
            Notifications { get; init; }

        public required NotificationService
            Service { get; init; }

        public static Fixture Create()
        {
            var now =
                DateTime.UtcNow;

            var school =
                NewSchool(
                    Guid.NewGuid(),
                    "School A");

            var otherSchool =
                NewSchool(
                    Guid.NewGuid(),
                    "School B");

            var admin =
                NewUser(
                    school.Id,
                    RoleNames.SchoolAdmin);

            var teacher =
                NewUser(
                    school.Id,
                    RoleNames.Teacher);

            var otherAdmin =
                NewUser(
                    otherSchool.Id,
                    RoleNames.SchoolAdmin);

            var otherSchoolUser =
                NewUser(
                    otherSchool.Id,
                    RoleNames.Teacher);

            var users =
                new FakeUserRepository();

            foreach (var user in new[]
                     {
                         admin,
                         teacher,
                         otherAdmin,
                         otherSchoolUser
                     })
            {
                users.Seed(user);
            }

            var schools =
                new FakeSchoolRepository();

            schools.Seed(school);
            schools.Seed(otherSchool);

            var notifications =
                new FakeNotificationRepository();

            var service =
                new NotificationService(
                    users,
                    schools,
                    notifications,
                    new FakeAuditService(),
                    new FakeMetadataProvider());

            return new Fixture
            {
                School = school,
                Admin = admin,
                Teacher = teacher,
                OtherAdmin = otherAdmin,
                OtherSchoolUser =
                    otherSchoolUser,
                Notifications =
                    notifications,
                Service = service
            };
        }

        private static School NewSchool(
            Guid id,
            string name) =>
            new()
            {
                Id = id,
                Name = name,
                SchoolCode =
                    id.ToString("N")[..8],
                NormalizedSchoolCode =
                    id.ToString("N")[..8]
                        .ToUpperInvariant(),
                Status =
                    SchoolStatus.Active,
                CountryCode = "PL",
                City = "Warsaw",
                ContactEmail =
                    "school@example.com",
                DefaultCulture = "en",
                TimeZoneId =
                    "Europe/Warsaw",
                CreatedAtUtc =
                    DateTime.UtcNow,
                UpdatedAtUtc =
                    DateTime.UtcNow
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
    }

    private sealed class FakeNotificationRepository
        : INotificationRepository
    {
        public List<UserNotification>
            Notifications { get; } = [];

        public List<NotificationDeliveryJob>
            Deliveries { get; } = [];

        public List<OutboxMessage>
            Outbox { get; } = [];

        public Task<UserNotification?>
            GetByDeduplicationKeyAsync(
                Guid schoolId,
                Guid recipientUserId,
                string deduplicationKey,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Notifications.SingleOrDefault(
                    x =>
                        x.SchoolId == schoolId &&
                        x.RecipientUserId ==
                            recipientUserId &&
                        x.DeduplicationKey ==
                            deduplicationKey));

        public Task<bool> DeliveryExistsAsync(
            Guid schoolId,
            string deduplicationKey,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Deliveries.Any(
                    x =>
                        x.SchoolId == schoolId &&
                        x.DeduplicationKey ==
                            deduplicationKey));

        public Task AddNotificationAsync(
            UserNotification notification,
            CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task AddDeliveryJobAsync(
            NotificationDeliveryJob job,
            CancellationToken cancellationToken = default)
        {
            Deliveries.Add(job);
            return Task.CompletedTask;
        }

        public Task AddOutboxAsync(
            OutboxMessage message,
            CancellationToken cancellationToken = default)
        {
            Outbox.Add(message);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<NotificationInboxRecord>>
            ListInboxAsync(
                Guid schoolId,
                Guid recipientUserId,
                int maxCount,
                CancellationToken cancellationToken = default)
        {
            var rows =
                Notifications
                    .Where(
                        x =>
                            x.SchoolId == schoolId &&
                            x.RecipientUserId ==
                                recipientUserId)
                    .OrderByDescending(
                        x => x.CreatedAtUtc)
                    .Take(maxCount)
                    .Select(
                        x =>
                        {
                            var status =
                                Deliveries
                                    .Where(
                                        j =>
                                            j.NotificationId ==
                                                x.Id)
                                    .OrderByDescending(
                                        j =>
                                            j.CreatedAtUtc)
                                    .Select(
                                        j =>
                                            (NotificationDeliveryStatus?)
                                                j.Status)
                                    .FirstOrDefault();

                            return new NotificationInboxRecord(
                                x.Id,
                                x.Kind,
                                x.TitleKey,
                                x.MessageKey,
                                x.CreatedAtUtc,
                                x.ReadAtUtc,
                                x.RelatedEntityType,
                                x.RelatedEntityId,
                                status);
                        })
                    .ToArray();

            return Task.FromResult<
                IReadOnlyList<
                    NotificationInboxRecord>>(rows);
        }

        public Task<UserNotification?>
            GetNotificationForUpdateAsync(
                Guid schoolId,
                Guid recipientUserId,
                Guid notificationId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Notifications.SingleOrDefault(
                    x =>
                        x.SchoolId == schoolId &&
                        x.RecipientUserId ==
                            recipientUserId &&
                        x.Id ==
                            notificationId));

        public Task<NotificationDeliveryJob?>
            GetDeliveryForUpdateAsync(
                Guid schoolId,
                Guid deliveryJobId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                Deliveries.SingleOrDefault(
                    x =>
                        x.SchoolId == schoolId &&
                        x.Id ==
                            deliveryJobId));

        public Task<bool> SaveAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }

    private sealed class FakeUserRepository
        : ISchoolUserRepository
    {
        private readonly Dictionary<
            Guid,
            SchoolUserRecord> _users =
            [];

        public void Seed(
            SchoolUserRecord user) =>
            _users[user.Id] = user;

        public Task<SchoolUserRecord?>
            GetActorAsync(
                Guid userId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _users.GetValueOrDefault(userId));

        public Task<IReadOnlyList<SchoolUserRecord>>
            ListBySchoolAsync(
                Guid schoolId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult<
                IReadOnlyList<
                    SchoolUserRecord>>(
                _users.Values
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
            var value =
                _users.GetValueOrDefault(
                    userId);

            return Task.FromResult(
                value?.SchoolId ==
                    schoolId
                    ? value
                    : null);
        }

        public Task<SchoolUserPersistenceResult>
            CreateAsync(
                Guid schoolId,
                string email,
                string role,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult>
            SetActiveAsync(
                Guid schoolId,
                Guid userId,
                bool isActive,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult>
            SetLockedAsync(
                Guid schoolId,
                Guid userId,
                bool isLocked,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult>
            SetRoleAsync(
                Guid schoolId,
                Guid userId,
                string role,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        public Task<SchoolUserPersistenceResult>
            GeneratePasswordSetupAsync(
                Guid schoolId,
                Guid userId,
                CancellationToken cancellationToken = default)
        {
            var user =
                _users.GetValueOrDefault(
                    userId);

            return Task.FromResult(
                user?.SchoolId ==
                    schoolId
                    ? SchoolUserPersistenceResult
                        .Success(
                            user,
                            "transient-test-token")
                    : SchoolUserPersistenceResult
                        .Failure(
                            SchoolUserPersistenceError
                                .NotFound));
        }

        public Task<SchoolUserPersistenceResult>
            CompletePasswordSetupAsync(
                Guid userId,
                string token,
                string newPassword,
                CancellationToken cancellationToken = default) =>
            Unsupported();

        private static Task<
            SchoolUserPersistenceResult>
            Unsupported() =>
            Task.FromResult(
                SchoolUserPersistenceResult
                    .Failure(
                        SchoolUserPersistenceError
                            .NotFound));
    }

    private sealed class FakeSchoolRepository
        : ISchoolRepository
    {
        private readonly Dictionary<
            Guid,
            School> _schools =
            [];

        public void Seed(
            School school) =>
            _schools[school.Id] =
                school;

        public Task<IReadOnlyList<School>>
            ListAsync(
                CancellationToken cancellationToken = default) =>
            Task.FromResult<
                IReadOnlyList<School>>(
                _schools.Values.ToArray());

        public Task<School?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _schools.GetValueOrDefault(
                    id));

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
            Task.FromResult(false);

        public Task AddAsync(
            School school,
            CancellationToken cancellationToken = default)
        {
            Seed(school);
            return Task.CompletedTask;
        }

        public Task<SchoolRepositoryWriteResult>
            SaveAsync(
                School school,
                byte[]? expectedRowVersion,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(
                SchoolRepositoryWriteResult
                    .Success);
    }

    private sealed class FakeAuditService
        : IAuditService
    {
        public List<AuditEvent>
            Events { get; } = [];

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

    private sealed class FakeMetadataProvider
        : IAuditRequestMetadataProvider
    {
        public AuditRequestMetadata GetCurrent() =>
            new(
                null,
                RoleNames.SchoolAdmin,
                "phase21-correlation",
                "127.0.0.1",
                "Phase21Tests",
                "Tests");
    }
}
