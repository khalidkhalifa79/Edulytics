namespace Edulytics.Core.Entities;

public sealed class OutboxRequeueAudit
{
    public Guid Id { get; set; }
    public Guid OutboxMessageId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int PreviousAttempts { get; set; }
    public DateTime RequeuedAtUtc { get; set; }
}
