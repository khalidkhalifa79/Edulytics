using Edulytics.Core.Enums;

namespace Edulytics.Core.Entities;

public sealed class IdempotencyRecord
{
    public Guid Id { get; set; }
    public Guid? SchoolId { get; set; }
    public Guid ActorUserId { get; set; }
    public string Operation { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public IdempotencyStatus Status { get; set; }
    public int? ResultStatusCode { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime ExpiresAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
