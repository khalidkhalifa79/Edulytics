using Edulytics.Core.Billing;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;

namespace Edulytics.Core.Interfaces;

public interface IBillingRepository
{
    Task<SchoolBillingProfile?> GetProfileAsync(Guid schoolId, CancellationToken cancellationToken = default);
    Task<SchoolBillingProfile?> GetProfileForUpdateAsync(Guid schoolId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BillingInvoice>> ListInvoicesAsync(Guid schoolId, CancellationToken cancellationToken = default);
    Task<BillingInvoice?> GetInvoiceForUpdateAsync(Guid schoolId, Guid invoiceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BillingInvoiceLine>> ListInvoiceLinesAsync(Guid schoolId, Guid invoiceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BankTransferPayment>> ListPaymentsAsync(Guid schoolId, Guid invoiceId, CancellationToken cancellationToken = default);
    Task<BankTransferPayment?> GetPaymentForUpdateAsync(Guid schoolId, Guid paymentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SubscriptionSeatChange>> ListUnbilledSeatIncreasesAsync(Guid schoolId, Guid subscriptionId, CancellationToken cancellationToken = default);
    Task<bool> HasInvoiceAsync(Guid subscriptionId, BillingInvoiceKind kind, int? installmentNumber, CancellationToken cancellationToken = default);
    Task<BillingPersistenceResult> SaveProfileAsync(SchoolBillingProfile profile, byte[]? expectedRowVersion, CancellationToken cancellationToken = default);
    Task<BillingPersistenceResult> AddInvoiceAsync(BillingInvoice invoice, IReadOnlyList<BillingInvoiceLine> lines, CancellationToken cancellationToken = default);
    Task<BillingPersistenceResult> AddPaymentAsync(BankTransferPayment payment, CancellationToken cancellationToken = default);
    Task<BillingPersistenceResult> SavePaymentAndInvoiceAsync(BankTransferPayment payment, byte[] expectedPaymentRowVersion, BillingInvoice invoice, byte[] expectedInvoiceRowVersion, CancellationToken cancellationToken = default);
    Task<BillingPersistenceResult> SaveInvoiceAndRefundAsync(BillingInvoice invoice, byte[] expectedInvoiceRowVersion, BillingRefund refund, CancellationToken cancellationToken = default);
}
