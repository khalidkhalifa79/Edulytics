using Edulytics.Core.Entities;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class ReportExportRepository
    : IReportExportRepository
{
    private readonly EdulyticsDbContext _db;

    public ReportExportRepository(
        EdulyticsDbContext db)
    {
        _db = db;
    }

    public Task AddAsync(
        ReportExportJob job,
        CancellationToken cancellationToken = default) =>
        _db.ReportExportJobs
            .AddAsync(
                job,
                cancellationToken)
            .AsTask();

    public Task AddOutboxAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default) =>
        _db.OutboxMessages
            .AddAsync(
                message,
                cancellationToken)
            .AsTask();

    public Task<ReportExportJob?> GetAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        _db.ReportExportJobs
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.Id == id,
                cancellationToken);

    public Task<ReportExportJob?> GetForUpdateAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        _db.ReportExportJobs
            .SingleOrDefaultAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.Id == id,
                cancellationToken);

    public async Task<IReadOnlyList<ReportExportJob>>
        ListRecentAsync(
            Guid schoolId,
            Guid requestedByUserId,
            int maxCount,
            CancellationToken cancellationToken = default) =>
        await _db.ReportExportJobs
            .AsNoTracking()
            .Where(
                x =>
                    x.SchoolId == schoolId &&
                    x.RequestedByUserId ==
                        requestedByUserId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(maxCount)
            .ToListAsync(cancellationToken);

    public async Task<bool> SaveAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(
                cancellationToken);

            return true;
        }
        catch (
            DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();
            return false;
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();
            return false;
        }
    }
}
