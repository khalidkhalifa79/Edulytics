using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class BankTransferPayment : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid InvoiceId { get; set; }
    public BankTransferVerificationStatus VerificationStatus { get; set; }
    public string PaymentReference { get; set; } = string.Empty;
    public string? EvidenceNote { get; set; }
    public decimal ReceivedAmount { get; set; }
    public string ReceivedCurrencyCode { get; set; } = string.Empty;
    public decimal AppliedAmount { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public Guid? VerifiedByUserId { get; set; }
    public string? RejectionReason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
