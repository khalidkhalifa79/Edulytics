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
