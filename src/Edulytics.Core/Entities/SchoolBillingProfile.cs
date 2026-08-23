using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class SchoolBillingProfile : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public string LegalName { get; set; } = string.Empty;
    public string BillingAddress { get; set; } = string.Empty;
    public string CountryCode { get; set; } = string.Empty;
    public string TaxIdentifier { get; set; } = string.Empty;
    public string InvoiceEmail { get; set; } = string.Empty;
    public string? TaxTreatmentCode { get; set; }
    public string? DefaultSettlementCurrencyCode { get; set; }
    public string PaymentInstructions { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
