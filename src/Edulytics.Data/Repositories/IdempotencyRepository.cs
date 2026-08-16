using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Resilience;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class IdempotencyRepository
    : IIdempotencyRepository
{
    private static readonly TimeSpan Lifetime =
        TimeSpan.FromHours(24);

    private readonly EdulyticsDbContext _db;

    public IdempotencyRepository(
        EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<IdempotencyReservation> ReserveAsync(
        Guid actorUserId,
        Guid? schoolId,
        string operation,
        string key,
        string requestHash,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var record = new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            ActorUserId = actorUserId,
            Operation = operation,
            IdempotencyKey = key,
            RequestHash = requestHash,
            Status = IdempotencyStatus.Processing,
            CreatedAtUtc = nowUtc,
            ExpiresAtUtc = nowUtc.Add(Lifetime)
        };

        _db.IdempotencyRecords.Add(record);

        try
        {
            await _db.SaveChangesAsync(
                cancellationToken);

            return new IdempotencyReservation(
                IdempotencyReservationOutcome.Acquired,
                record.Id,
                record.Status,
                null);
        }
        catch (DbUpdateException)
        {
            _db.Entry(record).State =
                EntityState.Detached;

            var existing =
                await _db.IdempotencyRecords
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x =>
                            x.ActorUserId == actorUserId &&
                            x.Operation == operation &&
                            x.IdempotencyKey == key,
                        cancellationToken);

            if (existing is null)
                throw;

            var outcome =
                string.Equals(
                    existing.RequestHash,
                    requestHash,
                    StringComparison.Ordinal)
                    ? IdempotencyReservationOutcome
                        .DuplicateSameRequest
                    : IdempotencyReservationOutcome
                        .KeyReusedForDifferentRequest;

            return new IdempotencyReservation(
                outcome,
                existing.Id,
                existing.Status,
                existing.ResultStatusCode);
        }
    }

    public async Task MarkCompletedAsync(
        Guid recordId,
        int statusCode,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var record =
            await _db.IdempotencyRecords
                .SingleOrDefaultAsync(
                    x => x.Id == recordId,
                    cancellationToken);

        if (record is null)
            return;

        record.Status =
            IdempotencyStatus.Completed;

        record.ResultStatusCode =
            statusCode;

        record.CompletedAtUtc =
            nowUtc;

        await _db.SaveChangesAsync(
            cancellationToken);
    }

    public async Task MarkIndeterminateAsync(
        Guid recordId,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        var record =
            await _db.IdempotencyRecords
                .SingleOrDefaultAsync(
                    x => x.Id == recordId,
                    cancellationToken);

        if (record is null)
            return;

        record.Status =
            IdempotencyStatus.Indeterminate;

        record.CompletedAtUtc =
            nowUtc;

        await _db.SaveChangesAsync(
            cancellationToken);
    }
}
