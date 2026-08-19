using Edulytics.Core.Entities;

namespace Edulytics.Core.Interfaces;

public interface IReportExportRepository
{
    Task AddAsync(
        ReportExportJob job,
        CancellationToken cancellationToken = default);

    Task AddOutboxAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default);

    Task<ReportExportJob?> GetAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<ReportExportJob?> GetForUpdateAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReportExportJob>>
        ListRecentAsync(
            Guid schoolId,
            Guid requestedByUserId,
            int maxCount,
            CancellationToken cancellationToken = default);

    Task<bool> SaveAsync(
        CancellationToken cancellationToken = default);
}
