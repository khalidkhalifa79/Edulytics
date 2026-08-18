using Edulytics.Core.Entities;

namespace Edulytics.Core.Interfaces;

public interface IAuditRepository
{
    void Add(AuditLog auditLog);

    Task SaveChangesAsync(
        CancellationToken cancellationToken = default);
}
