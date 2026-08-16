using System.Security.Claims;
using System.Threading.RateLimiting;
using Edulytics.Core.Enums;
using Edulytics.Core.Resilience;
using Edulytics.Data.Contexts;
using Edulytics.Web.Resilience;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Edulytics.Tests.Phase14;

public sealed class BackendResilienceTests
{
    private static readonly Guid ActorId =
        Guid.Parse("14141414-1414-1414-1414-141414141414");

    [Fact]
    public void Idempotency_model_has_required_unique_constraint()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString())
                .Options;

        using var db =
            new EdulyticsDbContext(options);

        var entity =
            db.Model.FindEntityType(
                "Edulytics.Core.Entities.IdempotencyRecord");

        Assert.NotNull(entity);

        var unique =
            entity!.GetIndexes()
                .Single(x =>
                    x.GetDatabaseName() ==
                    "UX_IdempotencyRecords_Actor_Operation_Key");

        Assert.True(unique.IsUnique);
        Assert.Equal(
            new[]
            {
                "ActorUserId",
                "Operation",
                "IdempotencyKey"
            },
            unique.Properties.Select(x => x.Name));

        var rowVersion =
            entity.FindProperty("RowVersion");

        Assert.NotNull(rowVersion);
        Assert.True(rowVersion!.IsConcurrencyToken);
    }

    [Fact]
    public async Task Duplicate_same_key_executes_business_delegate_once()
    {
        var service =
            new FakeIdempotencyService();

        var executions = 0;

        var middleware =
            new IdempotencyMiddleware(
                _ =>
                {
                    executions++;
                    return Task.CompletedTask;
                },
                NullLogger<IdempotencyMiddleware>.Instance);

        var first =
            CreateContext("same-key");

        await middleware.InvokeAsync(
            first,
            service);

        var second =
            CreateContext("same-key");

        await middleware.InvokeAsync(
            second,
            service);

        Assert.Equal(1, executions);
        Assert.Equal(
            StatusCodes.Status409Conflict,
            second.Response.StatusCode);
        Assert.Equal(
            "duplicate",
            second.Response.Headers[
                "Idempotency-Conflict"].ToString());
    }

    [Fact]
    public async Task Different_keys_are_independent_operations()
    {
        var service =
            new FakeIdempotencyService();

        var executions = 0;

        var middleware =
            new IdempotencyMiddleware(
                _ =>
                {
                    executions++;
                    return Task.CompletedTask;
                },
                NullLogger<IdempotencyMiddleware>.Instance);

        await middleware.InvokeAsync(
            CreateContext("key-a"),
            service);

        await middleware.InvokeAsync(
            CreateContext("key-b"),
            service);

        Assert.Equal(2, executions);
    }

    [Fact]
    public async Task Reusing_key_for_different_request_is_conflict()
    {
        var service =
            new FakeIdempotencyService();

        var executions = 0;

        var middleware =
            new IdempotencyMiddleware(
                _ =>
                {
                    executions++;
                    return Task.CompletedTask;
                },
                NullLogger<IdempotencyMiddleware>.Instance);

        var first =
            CreateContext(
                "same-key",
                "/school/one");

        await middleware.InvokeAsync(
            first,
            service);

        var second =
            CreateContext(
                "same-key",
                "/school/one?different=1");

        second.Request.QueryString =
            new QueryString("?different=1");

        await middleware.InvokeAsync(
            second,
            service);

        Assert.Equal(1, executions);
        Assert.Equal(
            StatusCodes.Status409Conflict,
            second.Response.StatusCode);
        Assert.Equal(
            "key-reuse",
            second.Response.Headers[
                "Idempotency-Conflict"].ToString());
    }

    [Fact]
    public async Task Cancelled_or_lost_response_is_marked_indeterminate()
    {
        var service =
            new FakeIdempotencyService();

        var middleware =
            new IdempotencyMiddleware(
                _ => throw new OperationCanceledException(),
                NullLogger<IdempotencyMiddleware>.Instance);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => middleware.InvokeAsync(
                CreateContext("cancel-key"),
                service));

        Assert.True(service.IndeterminateMarked);
    }

    [Fact]
    public async Task Concurrency_limiter_has_bounded_queue_and_rejects_overflow()
    {
        using var limiter =
            new ConcurrencyLimiter(
                new ConcurrencyLimiterOptions
                {
                    PermitLimit = 1,
                    QueueLimit = 1,
                    QueueProcessingOrder =
                        QueueProcessingOrder.OldestFirst
                });

        using var first =
            await limiter.AcquireAsync(1);

        Assert.True(first.IsAcquired);

        var queuedTask =
            limiter.AcquireAsync(1).AsTask();

        await Task.Delay(25);

        using var overflow =
            await limiter.AcquireAsync(1);

        Assert.False(overflow.IsAcquired);

        first.Dispose();

        using var queued =
            await queuedTask;

        Assert.True(queued.IsAcquired);
    }

    [Fact]
    public void Phase14_policy_names_are_stable()
    {
        Assert.Equal(
            "ImportConcurrency",
            BackendResiliencePolicyNames.ImportConcurrency);

        Assert.Equal(
            "AnalyticsConcurrency",
            BackendResiliencePolicyNames.AnalyticsConcurrency);

        Assert.Equal(
            "InteractiveWrite",
            BackendResiliencePolicyNames.InteractiveWrite);
    }

    private static DefaultHttpContext CreateContext(
        string key,
        string path = "/school/test")
    {
        var context =
            new DefaultHttpContext();

        context.User =
            new ClaimsPrincipal(
                new ClaimsIdentity(
                    new[]
                    {
                        new Claim(
                            ClaimTypes.NameIdentifier,
                            ActorId.ToString())
                    },
                    "test"));

        context.Request.Method = "POST";
        context.Request.Path = path.Split('?')[0];
        context.Request.Headers[
            "Idempotency-Key"] = key;

        return context;
    }

    private sealed class FakeIdempotencyService
        : IIdempotencyService
    {
        private readonly Dictionary<
            (Guid Actor, string Operation, string Key),
            (Guid Id, string Hash, IdempotencyStatus Status)>
            _records = [];

        public bool IndeterminateMarked { get; private set; }

        public Task<IdempotencyReservation> ReserveAsync(
            Guid actorUserId,
            Guid? schoolId,
            string operation,
            string key,
            string requestHash,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            var compound =
                (actorUserId, operation, key);

            if (_records.TryGetValue(
                    compound,
                    out var existing))
            {
                return Task.FromResult(
                    new IdempotencyReservation(
                        existing.Hash == requestHash
                            ? IdempotencyReservationOutcome
                                .DuplicateSameRequest
                            : IdempotencyReservationOutcome
                                .KeyReusedForDifferentRequest,
                        existing.Id,
                        existing.Status,
                        null));
            }

            var id = Guid.NewGuid();

            _records[compound] =
                (id, requestHash, IdempotencyStatus.Processing);

            return Task.FromResult(
                new IdempotencyReservation(
                    IdempotencyReservationOutcome.Acquired,
                    id,
                    IdempotencyStatus.Processing,
                    null));
        }

        public Task CompleteAsync(
            Guid recordId,
            int statusCode,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        public Task MarkIndeterminateAsync(
            Guid recordId,
            DateTime nowUtc,
            CancellationToken cancellationToken)
        {
            IndeterminateMarked = true;
            return Task.CompletedTask;
        }
    }
}
