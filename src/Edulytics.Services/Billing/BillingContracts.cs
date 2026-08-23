using Edulytics.Core.Enums;

namespace Edulytics.Services.Billing;

public enum BillingErrorCode
{
    None = 0,
    AccessDenied = 1,
    SchoolNotFound = 2,
    SubscriptionNotFound = 3,
    ProfileNotFound = 4,
    InvoiceNotFound = 5,
    PaymentNotFound = 6,
    InvalidInput = 7,
    InvalidState = 8,
    DuplicateInvoice = 9,
    NoUnbilledSeatIncrease = 10,
    AmountExceedsOutstanding = 11,
    ConcurrencyConflict = 12,
    PersistenceError = 13,
    UnsupportedMarket = 14
}

public sealed record UpsertBillingProfileRequest(
    Guid SchoolId,
    string LegalName,
    string BillingAddress,
    string CountryCode,
    string TaxIdentifier,
    string InvoiceEmail,
    string? TaxTreatmentCode,
    string? DefaultSettlementCurrencyCode,
    string PaymentInstructions,
    byte[]? ExpectedRowVersion);

public sealed record RecordBankTransferRequest(
    Guid SchoolId,
    Guid InvoiceId,
    string PaymentReference,
    string? EvidenceNote,
    decimal ReceivedAmount,
    string ReceivedCurrencyCode,
    decimal AppliedAmount,
    DateTime ReceivedAtUtc);

public sealed record BillingProfileDetails(
    Guid Id,
    Guid SchoolId,
    string LegalName,
    string BillingAddress,
    string CountryCode,
    string TaxIdentifier,
    string InvoiceEmail,
    string? TaxTreatmentCode,
    string? DefaultSettlementCurrencyCode,
    string PaymentInstructions,
    byte[] RowVersion);

public sealed record BillingInvoiceLineDetails(
    Guid Id,
    BillingInvoiceLineKind Kind,
    string Description,
    int? SeatCount,
    int? SeatDelta,
    decimal? UnitMonthlyPrice,
    int? QuantityMonths,
    DateTime? ServicePeriodStartsAtUtc,
    DateTime? ServicePeriodEndsAtUtc,
    int? ProrationNumeratorDays,
    int? ProrationDenominatorDays,
    decimal NetAmount,
    Guid? SubscriptionSeatChangeId);

public sealed record BankTransferPaymentDetails(
    Guid Id,
    BankTransferVerificationStatus VerificationStatus,
    string PaymentReference,
    string? EvidenceNote,
    decimal ReceivedAmount,
    string ReceivedCurrencyCode,
    decimal AppliedAmount,
    DateTime ReceivedAtUtc,
    DateTime? VerifiedAtUtc,
    string? RejectionReason,
    byte[] RowVersion);

public sealed record BillingInvoiceDetails(
    Guid Id,
    Guid SchoolId,
    Guid SubscriptionId,
    string InvoiceNumber,
    BillingInvoiceKind Kind,
    BillingInvoiceStatus StoredStatus,
    BillingInvoiceStatus EffectiveStatus,
    CommercialCurrency InvoiceCurrency,
    string? SettlementCurrencyCode,
    decimal? SettlementEquivalentAmount,
    DateTime IssueDateUtc,
    DateTime DueDateUtc,
    DateTime GraceEndsAtUtc,
    DateTime? BillingPeriodStartsAtUtc,
    DateTime? BillingPeriodEndsAtUtc,
    int? InstallmentNumber,
    decimal NetAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal PaidAmount,
    decimal RefundedAmount,
    decimal OutstandingAmount,
    bool InGracePeriod,
    bool SuspensionEligible,
    IReadOnlyList<BillingInvoiceLineDetails> Lines,
    IReadOnlyList<BankTransferPaymentDetails> Payments,
    byte[] RowVersion);

public sealed record BillingSchoolDetails(
    Guid SchoolId,
    string SchoolName,
    string SchoolCode,
    string CountryCode,
    SubscriptionStatus SubscriptionStatus,
    SubscriptionBillingCadence BillingCadence,
    CommercialCurrency CommercialCurrency,
    int CommittedSeats,
    int? PendingRenewalSeats,
    decimal PricePerStudentPerMonth,
    DateTime? CurrentTermStartsAtUtc,
    DateTime? CurrentTermEndsAtUtc,
    BillingProfileDetails? Profile,
    IReadOnlyList<BillingInvoiceDetails> Invoices,
    byte[] SubscriptionRowVersion);

public sealed record BillingCommandResult(
    bool Succeeded,
    BillingErrorCode Error,
    Guid? EntityId = null)
{
    public static BillingCommandResult Success(Guid? entityId = null) =>
        new(true, BillingErrorCode.None, entityId);

    public static BillingCommandResult Failure(BillingErrorCode error) =>
        new(false, error);
}

public sealed record BillingQueryResult<T>(T? Value, BillingErrorCode Error)
    where T : class
{
    public static BillingQueryResult<T> Success(T value) =>
        new(value, BillingErrorCode.None);

    public static BillingQueryResult<T> Failure(BillingErrorCode error) =>
        new(null, error);
}
