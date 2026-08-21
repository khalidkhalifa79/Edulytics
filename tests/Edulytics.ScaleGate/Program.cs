using System.Net;
using System.Text.Json;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Realtime;
using Edulytics.Data.Contexts;
using Edulytics.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

const string SchoolCode =
    "PHASE25";
const string SchoolAdminEmail =
    "phase25-schooladmin@example.test";
const string SchoolAdminPassword =
    "Phase25!SchoolAdmin#2026";

var schoolId =
    Guid.Parse(
        "25000000-0000-0000-0000-000000000001");

var userId =
    Guid.Parse(
        "25000000-0000-0000-0000-000000000002");

if (args.Length == 0)
{
    throw new InvalidOperationException(
        "Command is required.");
}

switch (args[0])
{
    case "seed":
        await SeedAsync(
            RequireArgument(args, 1));
        break;

    case "enqueue":
        await EnqueueAsync(
            RequireArgument(args, 1));
        break;

    case "wait-outbox":
        await WaitOutboxAsync(
            RequireArgument(args, 1),
            Guid.Parse(
                RequireArgument(args, 2)));
        break;

    case "listen":
        await ListenAsync(
            RequireArgument(args, 1),
            RequireArgument(args, 2));
        break;

    default:
        throw new InvalidOperationException(
            $"Unknown command: {args[0]}");
}

return;

async Task SeedAsync(
    string connectionString)
{
    await using var db =
        CreateDb(connectionString);

    var school =
        await db.Schools
            .SingleOrDefaultAsync(
                x => x.Id == schoolId);

    if (school is null)
    {
        school =
            new School
            {
                Id = schoolId,
                Name = "Phase 25 Scale School",
                SchoolCode = SchoolCode,
                NormalizedSchoolCode =
                    SchoolCode.ToUpperInvariant(),
                Status = SchoolStatus.Active,
                CountryCode = "PL",
                City = "Warsaw",
                ContactEmail =
                    "phase25-school@example.test",
                DefaultCulture = "en",
                TimeZoneId = "Europe/Warsaw",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

        db.Schools.Add(school);
        await db.SaveChangesAsync();
    }

    var role =
        await db.Roles
            .SingleAsync(
                x =>
                    x.Name ==
                    RoleNames.SchoolAdmin);

    var user =
        await db.Users
            .SingleOrDefaultAsync(
                x => x.Id == userId);

    if (user is null)
    {
        user =
            new ApplicationUser
            {
                Id = userId,
                UserName = SchoolAdminEmail,
                NormalizedUserName =
                    SchoolAdminEmail.ToUpperInvariant(),
                Email = SchoolAdminEmail,
                NormalizedEmail =
                    SchoolAdminEmail.ToUpperInvariant(),
                EmailConfirmed = true,
                SchoolId = schoolId,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                SecurityStamp =
                    Guid.NewGuid().ToString("N"),
                ConcurrencyStamp =
                    Guid.NewGuid().ToString("N"),
                LockoutEnabled = true
            };

        var hasher =
            new PasswordHasher<ApplicationUser>();

        user.PasswordHash =
            hasher.HashPassword(
                user,
                SchoolAdminPassword);

        db.Users.Add(user);

        db.UserRoles.Add(
            new IdentityUserRole<Guid>
            {
                UserId = user.Id,
                RoleId = role.Id
            });

        await db.SaveChangesAsync();
    }

    Console.WriteLine(
        $"SCHOOL_ID={schoolId:D}");
}

async Task EnqueueAsync(
    string connectionString)
{
    await using var db =
        CreateDb(connectionString);

    var outboxId =
        Guid.NewGuid();

    var now =
        DateTime.UtcNow;

    var change =
        new AssessmentResultChangedEvent(
            Guid.NewGuid(),
            schoolId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            now);

    db.OutboxMessages.Add(
        new OutboxMessage
        {
            Id = outboxId,
            SchoolId = schoolId,
            EventType =
                RealtimeEventTypes
                    .AssessmentResultEntered,
            PayloadJson =
                JsonSerializer.Serialize(
                    change),
            OccurredAtUtc = now,
            AvailableAtUtc = now,
            Status =
                OutboxMessageStatus.Pending,
            CorrelationId =
                $"phase25-{outboxId:N}"
        });

    await db.SaveChangesAsync();

    Console.WriteLine(
        $"OUTBOX_ID={outboxId:D}");
}

async Task WaitOutboxAsync(
    string connectionString,
    Guid outboxId)
{
    var deadline =
        DateTime.UtcNow.AddSeconds(40);

    while (DateTime.UtcNow < deadline)
    {
        await using var db =
            CreateDb(connectionString);

        var row =
            await db.OutboxMessages
                .AsNoTracking()
                .SingleAsync(
                    x => x.Id == outboxId);

        if (row.Status ==
            OutboxMessageStatus.Processed)
        {
            if (row.ProcessingAttempts != 1)
            {
                throw new InvalidOperationException(
                    "Outbox processing attempts="
                    + row.ProcessingAttempts);
            }

            Console.WriteLine(
                $"OUTBOX_PROCESSED_ONCE={outboxId:D}");
            return;
        }

        if (row.Status ==
            OutboxMessageStatus.DeadLetter)
        {
            throw new InvalidOperationException(
                "Outbox dead-lettered: "
                + row.LastError);
        }

        await Task.Delay(250);
    }

    throw new TimeoutException(
        "Outbox did not complete.");
}

async Task ListenAsync(
    string webBaseUrl,
    string identityCookie)
{
    var cookies =
        new CookieContainer();

    cookies.Add(
        new Uri(webBaseUrl),
        new Cookie(
            ".AspNetCore.Identity.Application",
            identityCookie,
            "/"));

    var connection =
        new HubConnectionBuilder()
            .WithUrl(
                webBaseUrl.TrimEnd('/')
                + "/hubs/analytics",
                options =>
                {
                    options.Cookies =
                        cookies;
                })
            .Build();

    var received =
        new TaskCompletionSource<
            AnalyticsInvalidationMessage>(
                TaskCreationOptions
                    .RunContinuationsAsynchronously);

    connection.On<
        AnalyticsInvalidationMessage>(
        "AnalyticsUpdated",
        message =>
        {
            if (message.SchoolId == schoolId)
            {
                received.TrySetResult(
                    message);
            }
        });

    await connection.StartAsync();

    Console.WriteLine(
        "SIGNALR_CONNECTED");
    Console.Out.Flush();

    using var timeout =
        new CancellationTokenSource(
            TimeSpan.FromSeconds(40));

    var message =
        await received.Task
            .WaitAsync(
                timeout.Token);

    Console.WriteLine(
        $"SIGNALR_RECEIVED={message.RefreshId:D}");

    await connection.StopAsync();
    await connection.DisposeAsync();
}

EdulyticsDbContext CreateDb(
    string connectionString)
{
    var options =
        new DbContextOptionsBuilder<
            EdulyticsDbContext>()
            .UseNpgsql(
                connectionString)
            .Options;

    return new EdulyticsDbContext(
        options);
}

static string RequireArgument(
    string[] values,
    int index)
{
    if (values.Length <= index ||
        string.IsNullOrWhiteSpace(
            values[index]))
    {
        throw new InvalidOperationException(
            $"Argument {index} is required.");
    }

    return values[index];
}
