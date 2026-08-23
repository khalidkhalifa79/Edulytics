namespace Edulytics.Core.Enums;

public enum BillingInvoiceKind
{
    Initial = 1,
    MonthlyInstallment = 2,
    SeatProration = 3,
    Renewal = 4,
    ManualAdjustment = 5
}

public enum BillingInvoiceStatus
{
    Draft = 1,
    Pending = 2,
    Due = 3,
    PartiallyPaid = 4,
    Paid = 5,
    Overdue = 6,
    PartiallyRefunded = 7,
    Refunded = 8,
    Cancelled = 9
}

public enum BillingInvoiceLineKind
{
    Subscription = 1,
    SeatProration = 2,
    Renewal = 3,
    ManualAdjustment = 4
}

public enum BankTransferVerificationStatus
{
    Pending = 1,
    Confirmed = 2,
    Rejected = 3
}
