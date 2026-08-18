using Edulytics.Core.Entities;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;

namespace Edulytics.Data.Repositories;

public sealed class AuditRepository
    : IAuditRepository
{
    private readonly EdulyticsDbContext _db;

    public AuditRepository(
        EdulyticsDbContext db)
    {
        _db = db;
    }

    public void Add(
        AuditLog auditLog)
    {
        ArgumentNullException.ThrowIfNull(
            auditLog);

        _db.AuditLogs.Add(
            auditLog);
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(
            cancellationToken);
}
