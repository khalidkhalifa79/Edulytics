using Edulytics.Core.Entities;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly EdulyticsDbContext _db;

    public OutboxRepository(EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetPendingAsync(
        DateTime utcNow,
        int maxCount,
        CancellationToken cancellationToken = default)
    {
        if (maxCount <= 0)
            return [];

        return await _db.OutboxMessages
            .AsNoTracking()
            .Where(x =>
                x.ProcessedAtUtc == null &&
                x.AvailableAtUtc <= utcNow)
            .OrderBy(x => x.OccurredAtUtc)
            .ThenBy(x => x.Id)
            .Take(maxCount)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryClaimAsync(
        Guid id,
        byte[] expectedRowVersion,
        DateTime utcNow,
        DateTime leaseUntilUtc,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.OutboxMessages
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (entity is null ||
            entity.ProcessedAtUtc.HasValue ||
            entity.AvailableAtUtc > utcNow)
        {
            return false;
        }

        _db.Entry(entity)
            .Property(x => x.RowVersion)
            .OriginalValue = expectedRowVersion;

        entity.ProcessingAttempts++;
        entity.AvailableAtUtc = leaseUntilUtc;

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task<bool> MarkProcessedAsync(
        Guid id,
        DateTime processedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.OutboxMessages
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (entity is null)
            return false;

        entity.ProcessedAtUtc = processedAtUtc;
        entity.AvailableAtUtc = processedAtUtc;
        entity.LastError = null;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> MarkFailedAsync(
        Guid id,
        string error,
        DateTime availableAtUtc,
        CancellationToken cancellationToken = default)
    {
        var entity = await _db.OutboxMessages
            .FirstOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        if (entity is null)
            return false;

        entity.LastError =
            error.Length <= 2000
                ? error
                : error[..2000];

        entity.AvailableAtUtc = availableAtUtc;

        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
