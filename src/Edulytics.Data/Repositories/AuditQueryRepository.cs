using Edulytics.Core.Entities;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class AuditQueryRepository
    : IAuditQueryRepository
{
    private readonly EdulyticsDbContext _db;

    public AuditQueryRepository(
        EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<AuditLogQueryPageData> QueryAsync(
        AuditLogQuerySpec spec,
        CancellationToken cancellationToken = default)
    {
        IQueryable<AuditLog> query =
            _db.AuditLogs.AsNoTracking();

        if (!spec.AllSchools)
        {
            query = query.Where(
                x => x.SchoolId == spec.SchoolId);
        }

        if (!string.IsNullOrWhiteSpace(
                spec.Action))
        {
            var action =
                spec.Action.Trim();

            query = query.Where(
                x => x.Action == action);
        }

        if (!string.IsNullOrWhiteSpace(
                spec.EntityType))
        {
            var entityType =
                spec.EntityType.Trim();

            query = query.Where(
                x => x.EntityType == entityType);
        }

        if (!string.IsNullOrWhiteSpace(
                spec.CorrelationId))
        {
            var correlation =
                spec.CorrelationId.Trim();

            query = query.Where(
                x =>
                    x.CorrelationId != null &&
                    x.CorrelationId.Contains(
                        correlation));
        }

        if (spec.ActorUserId.HasValue)
        {
            query = query.Where(
                x =>
                    x.ActorUserId ==
                    spec.ActorUserId.Value);
        }

        if (spec.FromUtc.HasValue)
        {
            query = query.Where(
                x =>
                    x.OccurredAtUtc >=
                    spec.FromUtc.Value);
        }

        if (spec.ToUtc.HasValue)
        {
            query = query.Where(
                x =>
                    x.OccurredAtUtc <=
                    spec.ToUtc.Value);
        }

        var total =
            await query.CountAsync(
                cancellationToken);

        var skip =
            Math.Max(0, spec.Skip);

        var take =
            Math.Clamp(
                spec.Take,
                1,
                100);

        var items =
            await query
                .OrderByDescending(
                    x => x.OccurredAtUtc)
                .ThenByDescending(
                    x => x.Id)
                .Skip(skip)
                .Take(take)
                .ToArrayAsync(
                    cancellationToken);

        return new AuditLogQueryPageData(
            total,
            items);
    }
}
