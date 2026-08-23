namespace Edulytics.Services.Billing;

public interface IBillingService
{
    Task<BillingQueryResult<IReadOnlyList<BillingSchoolDetails>>> ListAsync(Guid actorUserId, CancellationToken cancellationToken = default);
    Task<BillingCommandResult> UpsertProfileAsync(Guid actorUserId, UpsertBillingProfileRequest request, CancellationToken cancellationToken = default);
    Task<BillingCommandResult> CreateInitialInvoiceAsync(Guid actorUserId, Guid schoolId, decimal taxAmount, decimal? settlementEquivalentAmount, CancellationToken cancellationToken = default);
    Task<BillingCommandResult> CreateNextInstallmentAsync(Guid actorUserId, Guid schoolId, decimal taxAmount, decimal? settlementEquivalentAmount, CancellationToken cancellationToken = default);
    Task<BillingCommandResult> CreateSeatProrationInvoiceAsync(Guid actorUserId, Guid schoolId, decimal taxAmount, decimal? settlementEquivalentAmount, CancellationToken cancellationToken = default);
    Task<BillingCommandResult> CreateRenewalInvoiceAsync(Guid actorUserId, Guid schoolId, decimal taxAmount, decimal? settlementEquivalentAmount, CancellationToken cancellationToken = default);
    Task<BillingCommandResult> RecordBankTransferAsync(Guid actorUserId, RecordBankTransferRequest request, CancellationToken cancellationToken = default);
    Task<BillingCommandResult> ConfirmBankTransferAsync(Guid actorUserId, Guid schoolId, Guid paymentId, byte[] expectedPaymentRowVersion, CancellationToken cancellationToken = default);
    Task<BillingCommandResult> RejectBankTransferAsync(Guid actorUserId, Guid schoolId, Guid paymentId, string reason, byte[] expectedPaymentRowVersion, CancellationToken cancellationToken = default);
    Task<BillingCommandResult> RecordRefundAsync(Guid actorUserId, Guid schoolId, Guid invoiceId, Guid? paymentId, decimal amount, string currencyCode, string reason, byte[] expectedInvoiceRowVersion, CancellationToken cancellationToken = default);
    Task<BillingCommandResult> ActivateByAgreementAsync(Guid actorUserId, Guid schoolId, DateTime agreedActivationAtUtc, string reason, byte[] expectedSubscriptionRowVersion, CancellationToken cancellationToken = default);
    Task<BillingCommandResult> ApplyPaidRenewalAsync(Guid actorUserId, Guid schoolId, byte[] expectedSubscriptionRowVersion, CancellationToken cancellationToken = default);
}
