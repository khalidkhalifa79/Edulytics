using Edulytics.Core.Entities;

namespace Edulytics.Core.Interfaces;

public interface IOutboxRepository
{
    Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        DateTime utcNow,
        int maxCount,
        CancellationToken cancellationToken = default);

    Task<bool> TryClaimAsync(
        Guid id,
        byte[] expectedRowVersion,
        DateTime utcNow,
        DateTime leaseUntilUtc,
        CancellationToken cancellationToken = default);

    Task<bool> MarkProcessedAsync(
        Guid id,
        DateTime processedAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> MarkFailedAsync(
        Guid id,
        string error,
        DateTime availableAtUtc,
        CancellationToken cancellationToken = default);
}
