using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Realtime;
using Edulytics.Core.Reports;
using Edulytics.Core.Notifications;
using Edulytics.Data.Identity;
using System.Text.Json;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Edulytics.Data.Seeding;
using Microsoft.EntityFrameworkCore;

var connection =
    Environment.GetEnvironmentVariable(
        "EDULYTICS_CI_POSTGRES_CONNECTION")
    ?? Environment.GetEnvironmentVariable(
        "ConnectionStrings__DefaultConnection")
    ?? throw new InvalidOperationException(
        "PostgreSQL CI connection is required.");

DbContextOptions<EdulyticsDbContext> Options() =>
    new DbContextOptionsBuilder<
            EdulyticsDbContext>()
        .UseNpgsql(connection)
        .Options;

async Task<EdulyticsDbContext> NewDbAsync()
{
    var db =
        new EdulyticsDbContext(
            Options());

    if (!await db.Database.CanConnectAsync())
    {
        throw new InvalidOperationException(
            "PostgreSQL CI database is unreachable.");
    }

    return db;
}

async Task<IReadOnlyList<OutboxLease>> ClaimAsync(
    string owner,
    DateTime utcNow,
    TimeSpan lease,
    int count)
{
    await using var db =
        await NewDbAsync();

    return await new OutboxRepository(db)
        .ClaimBatchAsync(
            owner,
            utcNow,
            lease,
            count);
}

async Task<bool> CompleteAsync(
    OutboxLease lease,
    DateTime utcNow)
{
    await using var db =
        await NewDbAsync();

    return await new OutboxRepository(db)
        .MarkProcessedAsync(
            lease.Id,
            lease.LeaseOwner,
            lease.LeaseToken,
            utcNow);
}

async Task RequestAnalyticsAsync(
    Guid schoolId,
    DateTime utcNow)
{
    await using var db =
        await NewDbAsync();

    await new AnalyticsRefreshQueueRepository(db)
        .RequestAsync(
            schoolId,
            utcNow,
            TimeSpan.FromMilliseconds(100),
            TimeSpan.FromSeconds(1));
}

async Task<AnalyticsRefreshLease?> ClaimAnalyticsAsync(
    string owner,
    DateTime utcNow)
{
    await using var db =
        await NewDbAsync();

    return await new AnalyticsRefreshQueueRepository(db)
        .ClaimNextAsync(
            owner,
            utcNow,
            TimeSpan.FromSeconds(10));
}

var now =
    new DateTime(
        2026,
        8,
        17,
        0,
        0,
        0,
        DateTimeKind.Utc);

var schoolA = Guid.NewGuid();
var schoolB = Guid.NewGuid();

await using (var db =
             await NewDbAsync())
{
    await db.Database.MigrateAsync();

    db.Schools.AddRange(
        NewSchool(
            schoolA,
            "CI-A",
            now),
        NewSchool(
            schoolB,
            "CI-B",
            now));

    await db.SaveChangesAsync();

    db.OutboxMessages.AddRange(
        NewMessage(
            schoolA,
            "a",
            now),
        NewMessage(
            schoolB,
            "b",
            now.AddMilliseconds(1)));

    await db.SaveChangesAsync();
}

var claims =
    await Task.WhenAll(
        ClaimAsync(
            "ci-worker-a",
            now.AddSeconds(1),
            TimeSpan.FromSeconds(30),
            10),
        ClaimAsync(
            "ci-worker-b",
            now.AddSeconds(1),
            TimeSpan.FromSeconds(30),
            10));

var allClaims =
    claims
        .SelectMany(x => x)
        .ToArray();

if (allClaims.Length != 2 ||
    allClaims.Select(x => x.Id)
        .Distinct()
        .Count() != 2)
{
    throw new Exception(
        "Concurrent PostgreSQL Outbox claim "
        + "duplicated or lost a durable row.");
}

Console.WriteLine(
    "PASS: PostgreSQL two-worker atomic claim");

await using (var db =
             await NewDbAsync())
{
    await db.OutboxMessages
        .ExecuteDeleteAsync();

    db.OutboxMessages.Add(
        NewMessage(
            schoolA,
            "reclaim",
            now));

    await db.SaveChangesAsync();
}

var oldLease =
    (await ClaimAsync(
        "ci-old-owner",
        now.AddSeconds(2),
        TimeSpan.FromSeconds(1),
        1))
    .Single();

var newLease =
    (await ClaimAsync(
        "ci-new-owner",
        now.AddSeconds(4),
        TimeSpan.FromSeconds(30),
        1))
    .Single();

if (oldLease.Id != newLease.Id ||
    oldLease.LeaseToken ==
        newLease.LeaseToken)
{
    throw new Exception(
        "Expired PostgreSQL lease reclaim "
        + "did not produce a new lease token.");
}

if (await CompleteAsync(
        oldLease,
        now.AddSeconds(5)))
{
    throw new Exception(
        "Stale PostgreSQL Outbox owner "
        + "was allowed to complete.");
}

if (!await CompleteAsync(
        newLease,
        now.AddSeconds(5)))
{
    throw new Exception(
        "Current PostgreSQL Outbox owner "
        + "could not complete.");
}

Console.WriteLine(
    "PASS: PostgreSQL stale-owner fencing");

await using (var db =
             await NewDbAsync())
{
    await db.AnalyticsRefreshStates
        .ExecuteDeleteAsync();
}

await Task.WhenAll(
    Enumerable.Range(0, 8)
        .Select(
            index =>
                RequestAnalyticsAsync(
                    schoolA,
                    now.AddMilliseconds(index))));

var analyticsClaims =
    await Task.WhenAll(
        ClaimAnalyticsAsync(
            "ci-analytics-a",
            now.AddSeconds(2)),
        ClaimAnalyticsAsync(
            "ci-analytics-b",
            now.AddSeconds(2)));

if (analyticsClaims.Count(
        x => x is not null) != 1)
{
    throw new Exception(
        "PostgreSQL analytics queue violated "
        + "single-flight by SchoolId.");
}

Console.WriteLine(
    "PASS: PostgreSQL analytics single-flight");

Console.WriteLine(
    "PHASE16_POSTGRES_GATE_PASS");

Console.WriteLine(
    "===== PHASE 18 POSTGRES AUDIT GATE =====");

var auditCorrelationPrefix =
    $"phase18-audit-{Guid.NewGuid():N}";

var auditAId = Guid.NewGuid();
var auditBId = Guid.NewGuid();

await using (var db =
             await NewDbAsync())
{
    db.AuditLogs.AddRange(
        new AuditLog
        {
            Id = auditAId,
            SchoolId = schoolA,
            ActorUserId = null,
            ActorRole = "SuperAdmin",
            Action = "Phase18.Postgres.A",
            EntityType = "Phase18Gate",
            EntityId = auditAId.ToString("D"),
            OccurredAtUtc = now.AddMinutes(10),
            CorrelationId =
                auditCorrelationPrefix + "-A",
            IpAddress = "127.0.0.1",
            UserAgent = "Edulytics.PostgresGate",
            OldValuesJson = "{}",
            NewValuesJson = "{}",
            ResultSummary =
                "Phase 18 PostgreSQL audit A.",
            Source = "CI",
            Feature = "Phase18"
        },
        new AuditLog
        {
            Id = auditBId,
            SchoolId = schoolB,
            ActorUserId = null,
            ActorRole = "SuperAdmin",
            Action = "Phase18.Postgres.B",
            EntityType = "Phase18Gate",
            EntityId = auditBId.ToString("D"),
            OccurredAtUtc = now.AddMinutes(11),
            CorrelationId =
                auditCorrelationPrefix + "-B",
            IpAddress = "127.0.0.1",
            UserAgent = "Edulytics.PostgresGate",
            OldValuesJson = "{}",
            NewValuesJson = "{}",
            ResultSummary =
                "Phase 18 PostgreSQL audit B.",
            Source = "CI",
            Feature = "Phase18"
        });

    await db.SaveChangesAsync();
}

await using (var db =
             await NewDbAsync())
{
    var page =
        await new AuditQueryRepository(db)
            .QueryAsync(
                new AuditLogQuerySpec(
                    AllSchools: false,
                    SchoolId: schoolA,
                    Action: null,
                    EntityType: "Phase18Gate",
                    CorrelationId:
                        auditCorrelationPrefix,
                    ActorUserId: null,
                    FromUtc: null,
                    ToUtc: null,
                    Skip: 0,
                    Take: 100));

    if (page.TotalCount < 1 ||
        page.Items.Count < 1 ||
        page.Items.Any(
            x => x.SchoolId != schoolA) ||
        page.Items.Any(
            x => x.Id == auditBId))
    {
        throw new Exception(
            "Phase 18 PostgreSQL audit tenant isolation failed.");
    }
}

Console.WriteLine(
    "PASS: PostgreSQL audit correlation search + tenant isolation");

await using (var db =
             await NewDbAsync())
{
    var audit =
        await db.AuditLogs
            .SingleAsync(
                x => x.Id == auditAId);

    audit.ResultSummary =
        "Attempted mutation";

    var blocked = false;

    try
    {
        await db.SaveChangesAsync();
    }
    catch (InvalidOperationException)
    {
        blocked = true;
    }

    if (!blocked)
    {
        throw new Exception(
            "AuditLog append-only enforcement did not block modification.");
    }
}

Console.WriteLine(
    "PASS: PostgreSQL AuditLog append-only enforcement");

var rollbackAuditId =
    Guid.NewGuid();

await using (var db =
             await NewDbAsync())
{
    await using var transaction =
        await db.Database
            .BeginTransactionAsync();

    db.AuditLogs.Add(
        new AuditLog
        {
            Id = rollbackAuditId,
            SchoolId = schoolA,
            ActorUserId = null,
            ActorRole = "SuperAdmin",
            Action = "Phase18.Rollback",
            EntityType = "Phase18Gate",
            EntityId =
                rollbackAuditId.ToString("D"),
            OccurredAtUtc = now.AddMinutes(12),
            CorrelationId =
                $"phase18-rollback-{rollbackAuditId:N}",
            IpAddress = string.Empty,
            UserAgent = "Edulytics.PostgresGate",
            OldValuesJson = "{}",
            NewValuesJson = "{}",
            ResultSummary =
                "Must be rolled back.",
            Source = "CI",
            Feature = "Phase18"
        });

    await db.SaveChangesAsync();

    await transaction.RollbackAsync();
}

await using (var db =
             await NewDbAsync())
{
    if (await db.AuditLogs
            .AsNoTracking()
            .AnyAsync(
                x => x.Id == rollbackAuditId))
    {
        throw new Exception(
            "Rolled-back PostgreSQL audit row persisted.");
    }
}

Console.WriteLine(
    "PASS: PostgreSQL rolled-back audit does not persist");

var operatorUserId =
    Guid.NewGuid();

var deadLetterId =
    Guid.NewGuid();

await using (var db =
             await NewDbAsync())
{
    var deadLetter =
        NewMessage(
            schoolA,
            "phase18-requeue",
            now.AddMinutes(20));

    deadLetter.Id =
        deadLetterId;

    deadLetter.Status =
        OutboxMessageStatus.DeadLetter;

    deadLetter.ProcessingAttempts = 4;

    deadLetter.DeadLetteredAtUtc =
        now.AddMinutes(21);

    deadLetter.LastError =
        "Phase 18 CI dead letter";

    db.OutboxMessages.Add(
        deadLetter);

    await db.SaveChangesAsync();
}

await using (var db =
             await NewDbAsync())
{
    var requeued =
        await new OutboxRepository(db)
            .RequeueDeadLetterAsync(
                deadLetterId,
                operatorUserId,
                "Phase 18 PostgreSQL gate",
                now.AddMinutes(22));

    if (!requeued)
    {
        throw new Exception(
            "Phase 18 PostgreSQL Outbox requeue failed.");
    }

    var generalAudit =
        await db.AuditLogs
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.EntityId ==
                    deadLetterId.ToString("D") &&
                    x.Action ==
                    "Outbox.DeadLetterRequeued");

    var legacyAudit =
        await db.OutboxRequeueAudits
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.OutboxMessageId ==
                    deadLetterId &&
                    x.ActorUserId ==
                    operatorUserId);

    if (!generalAudit ||
        !legacyAudit)
    {
        throw new Exception(
            "Outbox requeue did not persist both audit contracts.");
    }
}

Console.WriteLine(
    "PASS: PostgreSQL Outbox requeue dual audit is atomic");

var pendingId =
    Guid.NewGuid();

await using (var db =
             await NewDbAsync())
{
    var pending =
        NewMessage(
            schoolA,
            "phase18-no-false-success",
            now.AddMinutes(30));

    pending.Id =
        pendingId;

    db.OutboxMessages.Add(
        pending);

    await db.SaveChangesAsync();
}

await using (var db =
             await NewDbAsync())
{
    var requeued =
        await new OutboxRepository(db)
            .RequeueDeadLetterAsync(
                pendingId,
                operatorUserId,
                "Must not succeed",
                now.AddMinutes(31));

    if (requeued)
    {
        throw new Exception(
            "Pending Outbox row was incorrectly requeued.");
    }

    var falseAudit =
        await db.AuditLogs
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.EntityId ==
                    pendingId.ToString("D") &&
                    x.Action ==
                    "Outbox.DeadLetterRequeued");

    if (falseAudit)
    {
        throw new Exception(
            "Failed Outbox mutation produced a false success audit.");
    }
}

Console.WriteLine(
    "PASS: failed mutation writes no false success audit");

Console.WriteLine(
    "PHASE18_POSTGRES_GATE_PASS");

Console.WriteLine(
    "===== PHASE 20 POSTGRES REPORT GATE =====");

var reportUserId =
    Guid.NewGuid();

var reportJobId =
    Guid.NewGuid();

var reportOutboxId =
    Guid.NewGuid();

await using (var db =
             await NewDbAsync())
{
    db.Users.Add(
        new ApplicationUser
        {
            Id = reportUserId,
            SchoolId = schoolA,
            UserName =
                $"phase20-{reportUserId:N}@example.invalid",
            NormalizedUserName =
                $"PHASE20-{reportUserId:N}@EXAMPLE.INVALID",
            Email =
                $"phase20-{reportUserId:N}@example.invalid",
            NormalizedEmail =
                $"PHASE20-{reportUserId:N}@EXAMPLE.INVALID",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAtUtc =
                now.AddMinutes(40),
            UpdatedAtUtc =
                now.AddMinutes(40),
            SecurityStamp =
                Guid.NewGuid().ToString("N")
        });

    var job =
        new ReportExportJob
        {
            Id = reportJobId,
            SchoolId = schoolA,
            RequestedByUserId =
                reportUserId,
            ReportKind =
                ReportKind.School,
            ExportFormat =
                ReportExportFormat.Csv,
            Culture = "en",
            Status =
                ReportExportJobStatus.Pending,
            CreatedAtUtc =
                now.AddMinutes(40),
            ExpiresAtUtc =
                now.AddHours(24)
        };

    db.ReportExportJobs.Add(job);

    db.OutboxMessages.Add(
        new OutboxMessage
        {
            Id = reportOutboxId,
            SchoolId = schoolA,
            EventType =
                ReportEventTypes.ExportRequested,
            PayloadJson =
                JsonSerializer.Serialize(
                    new ReportExportRequestedEvent(
                        schoolA,
                        reportJobId)),
            OccurredAtUtc =
                now.AddMinutes(40),
            AvailableAtUtc =
                now.AddMinutes(40),
            Status =
                OutboxMessageStatus.Pending,
            CorrelationId =
                $"phase20-report-{reportJobId:N}"
        });

    db.AuditLogs.Add(
        new AuditLog
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolA,
            ActorUserId =
                reportUserId,
            ActorRole =
                "SchoolAdmin",
            Action =
                "Report.ExportRequested",
            EntityType =
                "ReportExportJob",
            EntityId =
                reportJobId.ToString("D"),
            OccurredAtUtc =
                now.AddMinutes(40),
            CorrelationId =
                $"phase20-report-{reportJobId:N}",
            IpAddress =
                "127.0.0.1",
            UserAgent =
                "Edulytics.PostgresGate",
            OldValuesJson =
                "{}",
            NewValuesJson =
                "{\"ReportKind\":\"School\",\"Format\":\"Csv\"}",
            ResultSummary =
                "Phase20 atomic report export gate.",
            Source =
                "CI",
            Feature =
                "Reports"
        });

    await db.SaveChangesAsync();
}

await using (var db =
             await NewDbAsync())
{
    var job =
        await db.ReportExportJobs
            .AsNoTracking()
            .SingleAsync(
                x =>
                    x.Id ==
                    reportJobId);

    if (job.SchoolId != schoolA ||
        job.RequestedByUserId !=
            reportUserId ||
        job.RowVersion.Length == 0)
    {
        throw new Exception(
            "Phase20 PostgreSQL report job "
            + "tenant/concurrency contract failed.");
    }

    var hasOutbox =
        await db.OutboxMessages
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Id ==
                    reportOutboxId &&
                    x.EventType ==
                        ReportEventTypes
                            .ExportRequested);

    var hasAudit =
        await db.AuditLogs
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.EntityId ==
                        reportJobId
                            .ToString("D") &&
                    x.Action ==
                        "Report.ExportRequested");

    if (!hasOutbox || !hasAudit)
    {
        throw new Exception(
            "Phase20 PostgreSQL atomic "
            + "job/outbox/audit contract failed.");
    }
}

Console.WriteLine(
    "PASS: PostgreSQL report job + outbox + audit atomic persistence");

Console.WriteLine(
    "PHASE20_POSTGRES_GATE_PASS");

Console.WriteLine(
    "===== PHASE 21 POSTGRES NOTIFICATION GATE =====");

var notificationId =
    Guid.NewGuid();

var deliveryJobId =
    Guid.NewGuid();

var notificationOutboxId =
    Guid.NewGuid();

await using (var db =
             await NewDbAsync())
{
    db.UserNotifications.Add(
        new UserNotification
        {
            Id = notificationId,
            SchoolId = schoolA,
            RecipientUserId =
                reportUserId,
            Kind =
                NotificationKind.AccountInvitation,
            TitleKey =
                "NotificationAccountInvitationTitle",
            MessageKey =
                "NotificationAccountInvitationMessage",
            DeduplicationKey =
                $"phase21-notification-{notificationId:N}",
            RelatedEntityType =
                "ApplicationUser",
            RelatedEntityId =
                reportUserId,
            CreatedAtUtc =
                now.AddMinutes(50)
        });

    db.NotificationDeliveryJobs.Add(
        new NotificationDeliveryJob
        {
            Id = deliveryJobId,
            SchoolId = schoolA,
            NotificationId =
                notificationId,
            RecipientUserId =
                reportUserId,
            Channel =
                NotificationDeliveryChannel.Email,
            Status =
                NotificationDeliveryStatus.Pending,
            Culture = "en",
            BaseUrl =
                "https://example.invalid",
            DeduplicationKey =
                $"phase21-delivery-{deliveryJobId:N}",
            CreatedAtUtc =
                now.AddMinutes(50)
        });

    db.OutboxMessages.Add(
        new OutboxMessage
        {
            Id = notificationOutboxId,
            SchoolId = schoolA,
            EventType =
                NotificationEventTypes
                    .DeliveryRequested,
            PayloadJson =
                JsonSerializer.Serialize(
                    new NotificationDeliveryRequestedEvent(
                        schoolA,
                        deliveryJobId)),
            OccurredAtUtc =
                now.AddMinutes(50),
            AvailableAtUtc =
                now.AddMinutes(50),
            Status =
                OutboxMessageStatus.Pending,
            CorrelationId =
                $"phase21-{deliveryJobId:N}"
        });

    db.AuditLogs.Add(
        new AuditLog
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolA,
            ActorUserId =
                reportUserId,
            ActorRole =
                "SchoolAdmin",
            Action =
                "Notification.InvitationQueued",
            EntityType =
                "NotificationDeliveryJob",
            EntityId =
                deliveryJobId.ToString("D"),
            OccurredAtUtc =
                now.AddMinutes(50),
            CorrelationId =
                $"phase21-{deliveryJobId:N}",
            IpAddress =
                "127.0.0.1",
            UserAgent =
                "Edulytics.PostgresGate",
            OldValuesJson =
                "{}",
            NewValuesJson =
                "{\"channel\":\"Email\"}",
            ResultSummary =
                "Phase21 durable notification gate.",
            Source = "CI",
            Feature = "Notifications"
        });

    await db.SaveChangesAsync();
}

await using (var db =
             await NewDbAsync())
{
    var notification =
        await db.UserNotifications
            .AsNoTracking()
            .SingleAsync(
                x => x.Id == notificationId);

    var delivery =
        await db.NotificationDeliveryJobs
            .AsNoTracking()
            .SingleAsync(
                x => x.Id == deliveryJobId);

    var outbox =
        await db.OutboxMessages
            .AsNoTracking()
            .SingleAsync(
                x => x.Id == notificationOutboxId);

    if (notification.SchoolId != schoolA ||
        notification.RecipientUserId !=
            reportUserId ||
        notification.RowVersion.Length == 0 ||
        delivery.SchoolId != schoolA ||
        delivery.RecipientUserId !=
            reportUserId ||
        delivery.RowVersion.Length == 0 ||
        outbox.EventType !=
            NotificationEventTypes
                .DeliveryRequested)
    {
        throw new Exception(
            "Phase21 notification tenant/durability contract failed.");
    }

    var payload =
        outbox.PayloadJson;

    if (payload.Contains(
            "token",
            StringComparison.OrdinalIgnoreCase) ||
        payload.Contains(
            "password",
            StringComparison.OrdinalIgnoreCase) ||
        payload.Contains(
            "@",
            StringComparison.Ordinal))
    {
        throw new Exception(
            "Phase21 outbox payload contains sensitive invitation data.");
    }
}

Console.WriteLine(
    "PASS: PostgreSQL notification + delivery + outbox + audit persistence");

Console.WriteLine(
    "PASS: notification outbox payload contains no token/email");

Console.WriteLine(
    "PHASE21_POSTGRES_GATE_PASS");


// ============================================================
// PHASE 22 — OPERATIONAL CONSOLE POSTGRESQL GATE
// ============================================================

var phase22Now =
    DateTime.UtcNow;

var phase22NotificationId =
    Guid.NewGuid();

var phase22DeliveryId =
    Guid.NewGuid();

var phase22NotificationOutboxId =
    Guid.NewGuid();

await using (var db =
             await NewDbAsync())
{
    db.UserNotifications.Add(
        new UserNotification
        {
            Id = phase22NotificationId,
            SchoolId = schoolA,
            RecipientUserId =
                reportUserId,
            Kind =
                NotificationKind
                    .AccountInvitation,
            TitleKey =
                "NotificationAccountInvitationTitle",
            MessageKey =
                "NotificationAccountInvitationMessage",
            DeduplicationKey =
                "phase22-notification-"
                + Guid.NewGuid().ToString("N"),
            RelatedEntityType =
                "ApplicationUser",
            RelatedEntityId =
                reportUserId,
            CreatedAtUtc =
                phase22Now
        });

    db.NotificationDeliveryJobs.Add(
        new NotificationDeliveryJob
        {
            Id = phase22DeliveryId,
            SchoolId = schoolA,
            NotificationId =
                phase22NotificationId,
            RecipientUserId =
                reportUserId,
            Channel =
                NotificationDeliveryChannel
                    .Email,
            Status =
                NotificationDeliveryStatus
                    .Failed,
            Culture = "en",
            BaseUrl =
                "https://staging.edulytiks.com",
            DeduplicationKey =
                "phase22-delivery-"
                + Guid.NewGuid().ToString("N"),
            AttemptCount = 5,
            LastAttemptAtUtc =
                phase22Now,
            LastErrorCode =
                "OutboxDeadLettered",
            CreatedAtUtc =
                phase22Now
        });

    db.OutboxMessages.Add(
        new OutboxMessage
        {
            Id =
                phase22NotificationOutboxId,
            SchoolId = schoolA,
            EventType =
                NotificationEventTypes
                    .DeliveryRequested,
            PayloadJson =
                JsonSerializer.Serialize(
                    new NotificationDeliveryRequestedEvent(
                        schoolA,
                        phase22DeliveryId)),
            OccurredAtUtc =
                phase22Now,
            AvailableAtUtc =
                phase22Now,
            Status =
                OutboxMessageStatus
                    .DeadLetter,
            ProcessingAttempts = 5,
            LastError =
                "phase22-safe-test",
            DeadLetteredAtUtc =
                phase22Now,
            CorrelationId =
                "phase22-notification-"
                + Guid.NewGuid().ToString("N")
        });

    await db.SaveChangesAsync();
}

await using (var db =
             await NewDbAsync())
{
    var requeued =
        await new OutboxRepository(db)
            .RequeueDeadLetterAsync(
                phase22NotificationOutboxId,
                reportUserId,
                "Phase22 safe notification requeue gate.",
                phase22Now.AddSeconds(1));

    if (!requeued)
    {
        throw new Exception(
            "Phase22 notification dead-letter requeue was rejected.");
    }
}

await using (var db =
             await NewDbAsync())
{
    var outbox =
        await db.OutboxMessages
            .AsNoTracking()
            .SingleAsync(
                x =>
                    x.Id ==
                    phase22NotificationOutboxId);

    var job =
        await db.NotificationDeliveryJobs
            .AsNoTracking()
            .SingleAsync(
                x =>
                    x.Id ==
                    phase22DeliveryId);

    var audit =
        await db.AuditLogs
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Action ==
                        "Outbox.DeadLetterRequeued" &&
                    x.EntityId ==
                        phase22NotificationOutboxId
                            .ToString("D"));

    if (outbox.Status !=
            OutboxMessageStatus.Pending ||
        outbox.ProcessingAttempts != 0 ||
        outbox.DeadLetteredAtUtc.HasValue ||
        outbox.LeaseOwner is not null ||
        outbox.LeaseToken.HasValue ||
        job.Status !=
            NotificationDeliveryStatus.Pending ||
        job.LastErrorCode is not null ||
        !audit)
    {
        throw new Exception(
            "Phase22 atomic notification requeue contract failed.");
    }
}

var phase22ReportJobId =
    Guid.NewGuid();

var phase22ReportOutboxId =
    Guid.NewGuid();

await using (var db =
             await NewDbAsync())
{
    db.ReportExportJobs.Add(
        new ReportExportJob
        {
            Id = phase22ReportJobId,
            SchoolId = schoolA,
            RequestedByUserId =
                reportUserId,
            ReportKind =
                (ReportKind)1,
            ExportFormat =
                (ReportExportFormat)1,
            Culture = "en",
            Status =
                ReportExportJobStatus.Failed,
            LastError =
                "BackgroundDeliveryDeadLettered",
            CreatedAtUtc =
                phase22Now,
            ExpiresAtUtc =
                phase22Now.AddHours(24),
            CompletedAtUtc =
                phase22Now
        });

    db.OutboxMessages.Add(
        new OutboxMessage
        {
            Id =
                phase22ReportOutboxId,
            SchoolId = schoolA,
            EventType =
                ReportEventTypes
                    .ExportRequested,
            PayloadJson =
                JsonSerializer.Serialize(
                    new ReportExportRequestedEvent(
                        schoolA,
                        phase22ReportJobId)),
            OccurredAtUtc =
                phase22Now,
            AvailableAtUtc =
                phase22Now,
            Status =
                OutboxMessageStatus
                    .DeadLetter,
            ProcessingAttempts = 5,
            LastError =
                "phase22-safe-report-test",
            DeadLetteredAtUtc =
                phase22Now,
            CorrelationId =
                "phase22-report-"
                + Guid.NewGuid().ToString("N")
        });

    await db.SaveChangesAsync();
}

await using (var db =
             await NewDbAsync())
{
    var requeued =
        await new OutboxRepository(db)
            .RequeueDeadLetterAsync(
                phase22ReportOutboxId,
                reportUserId,
                "Phase22 safe report requeue gate.",
                phase22Now.AddSeconds(2));

    if (!requeued)
    {
        throw new Exception(
            "Phase22 report dead-letter requeue was rejected.");
    }
}

await using (var db =
             await NewDbAsync())
{
    var outbox =
        await db.OutboxMessages
            .AsNoTracking()
            .SingleAsync(
                x =>
                    x.Id ==
                    phase22ReportOutboxId);

    var job =
        await db.ReportExportJobs
            .AsNoTracking()
            .SingleAsync(
                x =>
                    x.Id ==
                    phase22ReportJobId);

    if (outbox.Status !=
            OutboxMessageStatus.Pending ||
        job.Status !=
            ReportExportJobStatus.Pending ||
        job.LastError is not null ||
        job.CompletedAtUtc.HasValue)
    {
        throw new Exception(
            "Phase22 atomic report requeue contract failed.");
    }
}

await using (var db =
             await NewDbAsync())
{
    var operations =
        new OperationsRepository(db);

    var summary =
        await operations
            .GetOutboxSummaryAsync();

    var backlog =
        await operations
            .GetOutboxBacklogAsync(100);

    var analytics =
        await operations
            .GetAnalyticsFreshnessAsync(100);

    var importFailures =
        await operations
            .GetImportFailuresAsync(100);

    var migration =
        await operations
            .GetLatestMigrationAsync();

    if (summary.PendingCount < 2 ||
        !backlog.Any(
            x =>
                x.Id ==
                    phase22NotificationOutboxId) ||
        !backlog.Any(
            x =>
                x.Id ==
                    phase22ReportOutboxId) ||
        string.IsNullOrWhiteSpace(
            migration))
    {
        throw new Exception(
            "Phase22 operational read model contract failed.");
    }

    _ = analytics.Count;
    _ = importFailures.Count;
}

Console.WriteLine(
    "PASS: Phase22 operational read models");

Console.WriteLine(
    "PASS: Phase22 notification dead-letter atomic requeue");

Console.WriteLine(
    "PASS: Phase22 report dead-letter atomic requeue");

Console.WriteLine(
    "PASS: Phase22 operator requeue audit");

Console.WriteLine(
    "PHASE22_POSTGRES_GATE_PASS");


// ============================================================
// PHASE 23 — PRIVACY / RETENTION POSTGRESQL GATE
// ============================================================

var phase23Now =
    DateTime.UtcNow;

var phase23ImportId =
    Guid.NewGuid();

var phase23ReportId =
    Guid.NewGuid();

var phase23NotificationId =
    Guid.NewGuid();

var phase23DeliveryId =
    Guid.NewGuid();

await using (var db =
             await NewDbAsync())
{
    db.ImportBatches.Add(
        new ImportBatch
        {
            Id = phase23ImportId,
            SchoolId = schoolA,
            ImportType =
                ImportType.Students,
            Status =
                ImportBatchStatus.Completed,
            OriginalFileName =
                "sensitive-students.xlsx",
            FileHash =
                Guid.NewGuid().ToString("N")
                + Guid.NewGuid().ToString("N"),
            RowsJson =
                "[{\"student\":\"sensitive-test\"}]",
            RowCount = 1,
            ValidRowCount = 1,
            ErrorCount = 0,
            UploadedByUserId =
                reportUserId,
            CompletedByUserId =
                reportUserId,
            CreatedAtUtc =
                phase23Now.AddDays(-3),
            CompletedAtUtc =
                phase23Now.AddDays(-3)
        });

    db.ReportExportJobs.Add(
        new ReportExportJob
        {
            Id = phase23ReportId,
            SchoolId = schoolA,
            RequestedByUserId =
                reportUserId,
            ReportKind =
                ReportKind.School,
            ExportFormat =
                ReportExportFormat.Csv,
            Culture = "en",
            Status =
                ReportExportJobStatus.Completed,
            RowCount = 1,
            FileName =
                "phase23-sensitive.csv",
            ContentType =
                "text/csv",
            FileContent =
                [1, 2, 3, 4],
            CreatedAtUtc =
                phase23Now.AddDays(-2),
            ExpiresAtUtc =
                phase23Now.AddHours(-1),
            CompletedAtUtc =
                phase23Now.AddDays(-2)
        });

    db.UserNotifications.Add(
        new UserNotification
        {
            Id =
                phase23NotificationId,
            SchoolId =
                schoolA,
            RecipientUserId =
                reportUserId,
            Kind =
                NotificationKind
                    .AccountInvitation,
            TitleKey =
                "NotificationAccountInvitationTitle",
            MessageKey =
                "NotificationAccountInvitationMessage",
            DeduplicationKey =
                "phase23-retention-"
                + Guid.NewGuid().ToString("N"),
            RelatedEntityType =
                "ApplicationUser",
            RelatedEntityId =
                reportUserId,
            CreatedAtUtc =
                phase23Now.AddDays(-210),
            ReadAtUtc =
                phase23Now.AddDays(-200)
        });

    db.NotificationDeliveryJobs.Add(
        new NotificationDeliveryJob
        {
            Id =
                phase23DeliveryId,
            SchoolId =
                schoolA,
            NotificationId =
                phase23NotificationId,
            RecipientUserId =
                reportUserId,
            Channel =
                NotificationDeliveryChannel
                    .Email,
            Status =
                NotificationDeliveryStatus
                    .Sent,
            Culture =
                "en",
            BaseUrl =
                "https://staging.edulytiks.com",
            DeduplicationKey =
                "phase23-delivery-"
                + Guid.NewGuid().ToString("N"),
            AttemptCount =
                1,
            LastAttemptAtUtc =
                phase23Now.AddDays(-210),
            SentAtUtc =
                phase23Now.AddDays(-210),
            CreatedAtUtc =
                phase23Now.AddDays(-210)
        });

    await db.SaveChangesAsync();
}

SensitiveDataRetentionResult
    phase23RetentionResult;

await using (var db =
             await NewDbAsync())
{
    phase23RetentionResult =
        await new SensitiveDataRetentionRepository(
                db)
            .ApplyAsync(
                phase23Now,
                TimeSpan.FromHours(24),
                TimeSpan.FromDays(180));
}

if (phase23RetentionResult
        .ImportPayloadsScrubbed < 1 ||
    phase23RetentionResult
        .ExportArtifactsPurged < 1 ||
    phase23RetentionResult
        .NotificationDeliveriesDeleted < 1 ||
    phase23RetentionResult
        .NotificationsDeleted < 1)
{
    throw new Exception(
        "Phase23 retention counters did not record all expected operations.");
}

await using (var db =
             await NewDbAsync())
{
    var import =
        await db.ImportBatches
            .AsNoTracking()
            .SingleAsync(
                x =>
                    x.Id ==
                    phase23ImportId);

    var report =
        await db.ReportExportJobs
            .AsNoTracking()
            .SingleAsync(
                x =>
                    x.Id ==
                    phase23ReportId);

    var notificationExists =
        await db.UserNotifications
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Id ==
                    phase23NotificationId);

    var deliveryExists =
        await db.NotificationDeliveryJobs
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.Id ==
                    phase23DeliveryId);

    if (import.RowsJson !=
            string.Empty ||
        import.OriginalFileName !=
            string.Empty ||
        import.FileHash.Length != 64)
    {
        throw new Exception(
            "Phase23 import payload retention contract failed.");
    }

    if (report.Status !=
            ReportExportJobStatus.Expired ||
        report.FileContent is not null ||
        report.FileName is not null ||
        report.ContentType is not null)
    {
        throw new Exception(
            "Phase23 export physical-retention contract failed.");
    }

    if (notificationExists ||
        deliveryExists)
    {
        throw new Exception(
            "Phase23 read-notification retention contract failed.");
    }
}

Console.WriteLine(
    "PASS: Phase23 terminal import payload physically scrubbed");

Console.WriteLine(
    "PASS: Phase23 expired report binary physically purged");

Console.WriteLine(
    "PASS: Phase23 old read notification/delivery retention");

Console.WriteLine(
    "PHASE23_POSTGRES_GATE_PASS");


Console.WriteLine(
    "===== PHASE 27.5 VERIFIED CURRICULUM POSTGRES GATE =====");

await using (var db = await NewDbAsync())
{
    var seeder = new MathematicsCurriculumPackSeeder(db);
    await seeder.SeedAsync();
    await seeder.SeedAsync();

    var states = await db.CurriculumPackImportStates
        .AsNoTracking()
        .ToListAsync();

    if (states.Count != 4)
        throw new Exception("Phase27.5 PostgreSQL pack-state count failed.");

    var uae = states.Single(x => x.FrameworkCode == "UAE-MOE-MATH");
    if (uae.VersionCode != "MOE-2026-2027-T1" ||
        uae.OfficialNodeCount != 22 ||
        uae.UnitCount != 6 ||
        uae.LessonCount != 42 ||
        uae.LinkCount != 48)
        throw new Exception("Phase27.5 PostgreSQL UAE verified-count contract failed.");

    var lessons = await db.CurriculumPackContentNodes
        .AsNoTracking()
        .Where(x => x.FrameworkVersionId == uae.FrameworkVersionId && x.NodeKind == "Lesson")
        .Select(x => x.Id)
        .ToListAsync();

    var linked = await db.CurriculumPackNodeLinks
        .AsNoTracking()
        .Where(x => x.FrameworkVersionId == uae.FrameworkVersionId && x.LinkKind == "LessonStandardAlignment")
        .Select(x => x.FromNodeId)
        .Distinct()
        .ToListAsync();

    if (lessons.Count != 42 ||
        !lessons.OrderBy(x => x).SequenceEqual(linked.OrderBy(x => x)))
        throw new Exception("Phase27.5 PostgreSQL lesson-standard linkage failed.");
}

Console.WriteLine(
    "PHASE275_VERIFIED_CURRICULUM_POSTGRES_GATE_PASS");

static School NewSchool(
    Guid id,
    string code,
    DateTime utcNow) =>
    new()
    {
        Id = id,
        Name = $"Phase16 {code}",
        SchoolCode = $"P16-{code}",
        NormalizedSchoolCode = $"P16-{code}",
        Status = SchoolStatus.Active,
        CountryCode = "PL",
        City = "Warsaw",
        ContactEmail =
            $"{code.ToLowerInvariant()}"
            + "@example.invalid",
        DefaultCulture = "en",
        TimeZoneId = "Europe/Warsaw",
        CreatedAtUtc = utcNow,
        UpdatedAtUtc = utcNow
    };

static OutboxMessage NewMessage(
    Guid schoolId,
    string suffix,
    DateTime utcNow) =>
    new()
    {
        Id = Guid.NewGuid(),
        SchoolId = schoolId,
        EventType =
            RealtimeEventTypes
                .AssessmentResultEntered,
        PayloadJson = "{}",
        OccurredAtUtc = utcNow,
        AvailableAtUtc = utcNow,
        Status =
            OutboxMessageStatus.Pending,
        CorrelationId =
            $"phase16-{suffix}-"
            + $"{Guid.NewGuid():N}"
    };
