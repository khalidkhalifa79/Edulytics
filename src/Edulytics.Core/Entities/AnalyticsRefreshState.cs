namespace Edulytics.Core.Entities;

public sealed class AnalyticsRefreshState
{
    public Guid SchoolId { get; set; }
    public long RequestedVersion { get; set; }
    public long CompletedVersion { get; set; }
    public DateTime FirstRequestedAtUtc { get; set; }
    public DateTime LastRequestedAtUtc { get; set; }
    public DateTime CoalesceDeadlineUtc { get; set; }
    public DateTime AvailableAtUtc { get; set; }
    public string? LeaseOwner { get; set; }
    public Guid? LeaseToken { get; set; }
    public DateTime? LeaseUntilUtc { get; set; }
    public int ProcessingAttempts { get; set; }
    public string? LastError { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
