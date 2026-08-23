using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class BillingInvoiceLine : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid InvoiceId { get; set; }
    public BillingInvoiceLineKind Kind { get; set; }
    public string Description { get; set; } = string.Empty;
    public int? SeatCount { get; set; }
    public int? SeatDelta { get; set; }
    public decimal? UnitMonthlyPrice { get; set; }
    public int? QuantityMonths { get; set; }
    public DateTime? ServicePeriodStartsAtUtc { get; set; }
    public DateTime? ServicePeriodEndsAtUtc { get; set; }
    public int? ProrationNumeratorDays { get; set; }
    public int? ProrationDenominatorDays { get; set; }
    public decimal NetAmount { get; set; }
    public Guid? SubscriptionSeatChangeId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
