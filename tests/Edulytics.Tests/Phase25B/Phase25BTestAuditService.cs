using Edulytics.Services.Auditing;

namespace Edulytics.Tests.Phase25B;

public sealed class Phase25BTestAuditService : IAuditService
{
    public List<AuditEvent> Recorded { get; } = [];

    public Task QueueAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        Recorded.Add(auditEvent);
        return Task.CompletedTask;
    }

    public Task RecordAsync(
        AuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        Recorded.Add(auditEvent);
        return Task.CompletedTask;
    }
}
