using Edulytics.Core.Entities;
using Edulytics.Core.Enums;

namespace Edulytics.Core.Interfaces;

public sealed record OutboxLease(
    Guid Id,
    Guid? SchoolId,
    string EventType,
    string PayloadJson,
    DateTime OccurredAtUtc,
    int ProcessingAttempts,
    string CorrelationId,
    string LeaseOwner,
    Guid LeaseToken,
    DateTime LeaseUntilUtc);

public enum OutboxFailureDisposition
{
    StaleLease = 1,
    RetryScheduled = 2,
    DeadLettered = 3
}

public sealed record OutboxDeadLetter(
    Guid Id,
    Guid? SchoolId,
    string EventType,
    int ProcessingAttempts,
    string? LastError,
    DateTime OccurredAtUtc,
    DateTime? DeadLetteredAtUtc);

public interface IOutboxRepository
{
    Task<IReadOnlyList<OutboxLease>> ClaimBatchAsync(
        string leaseOwner,
        DateTime utcNow,
        TimeSpan leaseDuration,
        int maxCount,
        CancellationToken cancellationToken = default);

    Task<bool> MarkProcessedAsync(
        Guid id,
        string leaseOwner,
        Guid leaseToken,
        DateTime processedAtUtc,
        CancellationToken cancellationToken = default);

    Task<OutboxFailureDisposition> MarkFailedAsync(
        Guid id,
        string leaseOwner,
        Guid leaseToken,
        string error,
        DateTime utcNow,
        DateTime nextAvailableAtUtc,
        int maxAttempts,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OutboxDeadLetter>> GetDeadLettersAsync(
        int maxCount,
        CancellationToken cancellationToken = default);

    Task<bool> RequeueDeadLetterAsync(
        Guid id,
        Guid actorUserId,
        string reason,
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
