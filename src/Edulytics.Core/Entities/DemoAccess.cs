namespace Edulytics.Core.Entities;

public sealed class DemoAccess
{
    public Guid Id { get; set; }
    public Guid DemoRequestId { get; set; }
    public Guid SchoolId { get; set; }
    public Guid SchoolAdminUserId { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime? RevokedAtUtc { get; set; }
    public string? RevokedReason { get; set; }
    public DateTime? ConvertedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
