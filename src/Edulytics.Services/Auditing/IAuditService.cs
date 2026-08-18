namespace Edulytics.Services.Auditing;

public interface IAuditService
{
    // Queue the record in the current scoped DbContext.
    // The caller's next domain SaveChanges persists the
    // business mutation and audit together.
    Task QueueAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default);

    // For operations whose persistence boundary has
    // already completed (for example some Identity flows).
    Task RecordAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default);
}
