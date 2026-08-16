using Edulytics.Core.Enums;

namespace Edulytics.Core.Resilience;

public enum IdempotencyReservationOutcome
{
    Acquired = 1,
    DuplicateSameRequest = 2,
    KeyReusedForDifferentRequest = 3
}

public sealed record IdempotencyReservation(
    IdempotencyReservationOutcome Outcome,
    Guid RecordId,
    IdempotencyStatus Status,
    int? ResultStatusCode);

public interface IIdempotencyRepository
{
    Task<IdempotencyReservation> ReserveAsync(
        Guid actorUserId,
        Guid? schoolId,
        string operation,
        string key,
        string requestHash,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task MarkCompletedAsync(
        Guid recordId,
        int statusCode,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task MarkIndeterminateAsync(
        Guid recordId,
        DateTime nowUtc,
        CancellationToken cancellationToken);
}

public interface IIdempotencyService
{
    Task<IdempotencyReservation> ReserveAsync(
        Guid actorUserId,
        Guid? schoolId,
        string operation,
        string key,
        string requestHash,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task CompleteAsync(
        Guid recordId,
        int statusCode,
        DateTime nowUtc,
        CancellationToken cancellationToken);

    Task MarkIndeterminateAsync(
        Guid recordId,
        DateTime nowUtc,
        CancellationToken cancellationToken);
}
