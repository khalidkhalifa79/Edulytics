using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class BillingInvoice : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid SubscriptionId { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public BillingInvoiceKind Kind { get; set; }
    public BillingInvoiceStatus Status { get; set; }
    public CommercialCurrency InvoiceCurrency { get; set; }
    public string? SettlementCurrencyCode { get; set; }
    public decimal? SettlementEquivalentAmount { get; set; }

    public string LegalNameSnapshot { get; set; } = string.Empty;
    public string BillingAddressSnapshot { get; set; } = string.Empty;
    public string CountryCodeSnapshot { get; set; } = string.Empty;
    public string TaxIdentifierSnapshot { get; set; } = string.Empty;
    public string InvoiceEmailSnapshot { get; set; } = string.Empty;
    public string? TaxTreatmentCodeSnapshot { get; set; }
    public string PaymentInstructionsSnapshot { get; set; } = string.Empty;

    public DateTime IssueDateUtc { get; set; }
    public DateTime DueDateUtc { get; set; }
    public DateTime GraceEndsAtUtc { get; set; }
    public DateTime? BillingPeriodStartsAtUtc { get; set; }
    public DateTime? BillingPeriodEndsAtUtc { get; set; }
    public int? InstallmentNumber { get; set; }

    public decimal NetAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RefundedAmount { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
