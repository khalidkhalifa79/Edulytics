using Edulytics.Core.Resilience;

namespace Edulytics.Services.Resilience;

public sealed class IdempotencyService
    : IIdempotencyService
{
    private readonly IIdempotencyRepository _repository;

    public IdempotencyService(
        IIdempotencyRepository repository)
    {
        _repository = repository;
    }

    public Task<IdempotencyReservation> ReserveAsync(
        Guid actorUserId,
        Guid? schoolId,
        string operation,
        string key,
        string requestHash,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            operation);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            key);

        ArgumentException.ThrowIfNullOrWhiteSpace(
            requestHash);

        if (operation.Length > 160)
            throw new ArgumentOutOfRangeException(nameof(operation));

        if (key.Length > 128)
            throw new ArgumentOutOfRangeException(nameof(key));

        if (requestHash.Length != 64)
            throw new ArgumentOutOfRangeException(nameof(requestHash));

        return _repository.ReserveAsync(
            actorUserId,
            schoolId,
            operation,
            key,
            requestHash,
            nowUtc,
            cancellationToken);
    }

    public Task CompleteAsync(
        Guid recordId,
        int statusCode,
        DateTime nowUtc,
        CancellationToken cancellationToken) =>
        _repository.MarkCompletedAsync(
            recordId,
            statusCode,
            nowUtc,
            cancellationToken);

    public Task MarkIndeterminateAsync(
        Guid recordId,
        DateTime nowUtc,
        CancellationToken cancellationToken) =>
        _repository.MarkIndeterminateAsync(
            recordId,
            nowUtc,
            cancellationToken);
}
