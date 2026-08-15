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
    public int ProcessingAttempts { get; set; }
    public string? LastError { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
    public byte[] RowVersion { get; set; } = [];
}
