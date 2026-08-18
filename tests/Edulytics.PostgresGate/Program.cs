using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Realtime;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
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
            OldValuesJson = null,
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
            OldValuesJson = null,
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
            IpAddress = null,
            UserAgent = "Edulytics.PostgresGate",
            OldValuesJson = null,
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
