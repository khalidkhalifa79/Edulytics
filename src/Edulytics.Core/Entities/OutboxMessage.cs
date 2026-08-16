using Edulytics.Core.Enums;

namespace Edulytics.Core.Entities;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }
    public Guid? SchoolId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; }
    public DateTime AvailableAtUtc { get; set; }
    public DateTime? ProcessedAtUtc { get; set; }
    public DateTime? DeadLetteredAtUtc { get; set; }
    public OutboxMessageStatus Status { get; set; } =
        OutboxMessageStatus.Pending;
    public int ProcessingAttempts { get; set; }
    public string? LastError { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public string? LeaseOwner { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTime? LeaseUntilUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
