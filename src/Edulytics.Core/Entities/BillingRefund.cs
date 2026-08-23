using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class BillingRefund : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid InvoiceId { get; set; }
    public Guid? PaymentId { get; set; }
    public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid RecordedByUserId { get; set; }
    public DateTime RecordedAtUtc { get; set; }
}
