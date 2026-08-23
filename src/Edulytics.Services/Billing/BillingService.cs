using Edulytics.Core.Billing;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Subscriptions;
using Edulytics.Core.Users;
using Edulytics.Services.Auditing;
using Edulytics.Services.Subscriptions;

namespace Edulytics.Services.Billing;

public sealed class BillingService : IBillingService
{
    private readonly IBillingRepository _billing;
    private readonly ISchoolSubscriptionRepository _subscriptions;
    private readonly ISchoolRepository _schools;
    private readonly ISchoolUserRepository _users;
    private readonly IAuditService _audit;
    private readonly IApplicationTransactionManager _transactions;

    public BillingService(
        IBillingRepository billing,
        ISchoolSubscriptionRepository subscriptions,
        ISchoolRepository schools,
        ISchoolUserRepository users,
        IAuditService audit,
        IApplicationTransactionManager transactions)
    {
        _billing = billing;
        _subscriptions = subscriptions;
        _schools = schools;
        _users = users;
        _audit = audit;
        _transactions = transactions;
    }

    public async Task<BillingQueryResult<IReadOnlyList<BillingSchoolDetails>>> ListAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(actorUserId, cancellationToken);
        if (actor is null)
            return BillingQueryResult<IReadOnlyList<BillingSchoolDetails>>.Failure(BillingErrorCode.AccessDenied);

        var subscriptions = await _subscriptions.ListAsync(cancellationToken);
        var rows = new List<BillingSchoolDetails>(subscriptions.Count);

        foreach (var subscription in subscriptions)
        {
            var school = await _schools.GetByIdAsync(subscription.SchoolId, cancellationToken);
            if (school is null)
                continue;

            var profile = await _billing.GetProfileAsync(school.Id, cancellationToken);
            var invoices = await _billing.ListInvoicesAsync(school.Id, cancellationToken);
            var mapped = new List<BillingInvoiceDetails>(invoices.Count);
            foreach (var invoice in invoices)
                mapped.Add(await MapInvoiceAsync(invoice, cancellationToken));

            rows.Add(new BillingSchoolDetails(
                school.Id,
                school.Name,
                school.SchoolCode,
                school.CountryCode,
                subscription.Status,
                subscription.BillingCadence,
                subscription.CommercialCurrency,
                subscription.CommittedSeats,
                subscription.PendingRenewalSeats,
                subscription.PricePerStudentPerMonth,
                subscription.CurrentTermStartsAtUtc,
                subscription.CurrentTermEndsAtUtc,
                profile is null ? null : MapProfile(profile),
                mapped,
                subscription.RowVersion.ToArray()));
        }

        return BillingQueryResult<IReadOnlyList<BillingSchoolDetails>>.Success(rows);
    }

    public async Task<BillingCommandResult> UpsertProfileAsync(
        Guid actorUserId,
        UpsertBillingProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(actorUserId, cancellationToken);
        if (actor is null)
            return Fail(BillingErrorCode.AccessDenied);
        if (!ValidProfile(request))
            return Fail(BillingErrorCode.InvalidInput);

        await using var transaction = await _transactions.BeginAsync(cancellationToken);
        var school = await _schools.GetForUpdateAsync(request.SchoolId, cancellationToken);
        if (school is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.SchoolNotFound);
        }
        if (!SubscriptionCommercialPolicy.TryCurrency(school.CountryCode, out _))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.UnsupportedMarket);
        }

        var existing = await _billing.GetProfileForUpdateAsync(request.SchoolId, cancellationToken);
        var now = DateTime.UtcNow;
        SchoolBillingProfile profile;
        byte[]? expected = null;
        IReadOnlyDictionary<string, object?>? oldValues = null;

        if (existing is null)
        {
            profile = new SchoolBillingProfile
            {
                Id = Guid.NewGuid(),
                SchoolId = request.SchoolId,
                CreatedAtUtc = now,
                RowVersion = []
            };
        }
        else
        {
            if (request.ExpectedRowVersion is not { Length: > 0 } ||
                !request.ExpectedRowVersion.SequenceEqual(existing.RowVersion))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Fail(BillingErrorCode.ConcurrencyConflict);
            }

            profile = existing;
            expected = existing.RowVersion.ToArray();
            oldValues = new Dictionary<string, object?>
            {
                ["legalName"] = existing.LegalName,
                ["invoiceEmail"] = existing.InvoiceEmail,
                ["taxIdentifier"] = existing.TaxIdentifier,
                ["settlementCurrency"] = existing.DefaultSettlementCurrencyCode
            };
        }

        profile.LegalName = Clean(request.LegalName);
        profile.BillingAddress = Clean(request.BillingAddress);
        profile.CountryCode = Clean(request.CountryCode).ToUpperInvariant();
        profile.TaxIdentifier = Clean(request.TaxIdentifier);
        profile.InvoiceEmail = Clean(request.InvoiceEmail);
        profile.TaxTreatmentCode = Optional(request.TaxTreatmentCode);
        profile.DefaultSettlementCurrencyCode = Optional(request.DefaultSettlementCurrencyCode)?.ToUpperInvariant();
        profile.PaymentInstructions = Clean(request.PaymentInstructions);
        profile.UpdatedAtUtc = now;

        await QueueAuditAsync(
            actor.Id,
            profile.SchoolId,
            "BillingProfile.Updated",
            "SchoolBillingProfile",
            profile.Id,
            oldValues,
            new Dictionary<string, object?>
            {
                ["legalName"] = profile.LegalName,
                ["invoiceEmail"] = profile.InvoiceEmail,
                ["taxIdentifier"] = profile.TaxIdentifier,
                ["settlementCurrency"] = profile.DefaultSettlementCurrencyCode
            },
            "School billing identity/payment instructions updated.",
            cancellationToken);

        var saved = await _billing.SaveProfileAsync(profile, expected, cancellationToken);
        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);
        return BillingCommandResult.Success(profile.Id);
    }

    public Task<BillingCommandResult> CreateInitialInvoiceAsync(
        Guid actorUserId, Guid schoolId, decimal taxAmount,
        decimal? settlementEquivalentAmount, CancellationToken cancellationToken = default) =>
        CreateStandardInvoiceAsync(actorUserId, schoolId, BillingInvoiceKind.Initial,
            taxAmount, settlementEquivalentAmount, cancellationToken);

    public Task<BillingCommandResult> CreateNextInstallmentAsync(
        Guid actorUserId, Guid schoolId, decimal taxAmount,
        decimal? settlementEquivalentAmount, CancellationToken cancellationToken = default) =>
        CreateStandardInvoiceAsync(actorUserId, schoolId, BillingInvoiceKind.MonthlyInstallment,
            taxAmount, settlementEquivalentAmount, cancellationToken);

    public Task<BillingCommandResult> CreateRenewalInvoiceAsync(
        Guid actorUserId, Guid schoolId, decimal taxAmount,
        decimal? settlementEquivalentAmount, CancellationToken cancellationToken = default) =>
        CreateStandardInvoiceAsync(actorUserId, schoolId, BillingInvoiceKind.Renewal,
            taxAmount, settlementEquivalentAmount, cancellationToken);

    public async Task<BillingCommandResult> CreateSeatProrationInvoiceAsync(
        Guid actorUserId,
        Guid schoolId,
        decimal taxAmount,
        decimal? settlementEquivalentAmount,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(actorUserId, cancellationToken);
        if (actor is null)
            return Fail(BillingErrorCode.AccessDenied);
        if (taxAmount < 0m || (settlementEquivalentAmount.HasValue && settlementEquivalentAmount.Value < 0m))
            return Fail(BillingErrorCode.InvalidInput);

        await using var transaction = await _transactions.BeginAsync(cancellationToken);
        var subscription = await _subscriptions.GetForUpdateBySchoolAsync(schoolId, cancellationToken);
        if (subscription is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.SubscriptionNotFound);
        }
        if (subscription.Status is not (SubscriptionStatus.Active or SubscriptionStatus.Suspended) ||
            !subscription.CurrentTermStartsAtUtc.HasValue ||
            !subscription.CurrentTermEndsAtUtc.HasValue)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.InvalidState);
        }

        var profile = await _billing.GetProfileAsync(schoolId, cancellationToken);
        if (profile is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.ProfileNotFound);
        }

        var allChanges = await _billing.ListUnbilledSeatIncreasesAsync(schoolId, subscription.Id, cancellationToken);
        var changes = allChanges
            .Where(x => x.EffectiveAtUtc >= subscription.CurrentTermStartsAtUtc.Value &&
                        x.EffectiveAtUtc < subscription.CurrentTermEndsAtUtc.Value)
            .ToArray();

        if (changes.Length == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.NoUnbilledSeatIncrease);
        }

        var now = DateTime.UtcNow;
        var lines = new List<BillingInvoiceLine>(changes.Length);
        decimal net = 0m;

        foreach (var change in changes)
        {
            var proration = BillingCommercialPolicy.SeatIncreaseProration(subscription, change);
            net += proration.NetAmount;
            lines.Add(new BillingInvoiceLine
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                Kind = BillingInvoiceLineKind.SeatProration,
                Description = $"Seat increase {change.PreviousSeats}->{change.NewSeats}",
                SeatDelta = proration.SeatDelta,
                UnitMonthlyPrice = subscription.PricePerStudentPerMonth,
                QuantityMonths = proration.FullMonthsAfter > 0 ? proration.FullMonthsAfter : null,
                ServicePeriodStartsAtUtc = proration.PeriodStartsAtUtc,
                ServicePeriodEndsAtUtc = proration.PeriodEndsAtUtc,
                ProrationNumeratorDays = proration.NumeratorDays,
                ProrationDenominatorDays = proration.DenominatorDays,
                NetAmount = proration.NetAmount,
                SubscriptionSeatChangeId = change.Id,
                CreatedAtUtc = now
            });
        }

        net = BillingCommercialPolicy.RoundMoney(net);
        var invoice = NewInvoice(
            subscription,
            profile,
            BillingInvoiceKind.SeatProration,
            installmentNumber: null,
            billingPeriodStart: lines.Min(x => x.ServicePeriodStartsAtUtc),
            billingPeriodEnd: lines.Max(x => x.ServicePeriodEndsAtUtc),
            net,
            taxAmount,
            settlementEquivalentAmount,
            now);

        foreach (var line in lines)
            line.InvoiceId = invoice.Id;

        await QueueInvoiceCreatedAndIssuedAsync(actor.Id, invoice, "Seat-proration invoice", cancellationToken);
        await QueueAuditAsync(
            actor.Id,
            schoolId,
            "Billing.SeatProrationInvoiced",
            "BillingInvoice",
            invoice.Id,
            null,
            new Dictionary<string, object?>
            {
                ["invoiceNumber"] = invoice.InvoiceNumber,
                ["seatChanges"] = changes.Length,
                ["netAmount"] = invoice.NetAmount
            },
            "Previously unbilled current-term immediate seat increases were invoiced exactly once.",
            cancellationToken);

        var saved = await _billing.AddInvoiceAsync(invoice, lines, cancellationToken);
        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);
        return BillingCommandResult.Success(invoice.Id);
    }

    public async Task<BillingCommandResult> RecordBankTransferAsync(
        Guid actorUserId,
        RecordBankTransferRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(actorUserId, cancellationToken);
        if (actor is null)
            return Fail(BillingErrorCode.AccessDenied);

        if (request.ReceivedAmount <= 0m || request.AppliedAmount <= 0m ||
            request.AppliedAmount > request.ReceivedAmount ||
            string.IsNullOrWhiteSpace(request.PaymentReference) ||
            string.IsNullOrWhiteSpace(request.ReceivedCurrencyCode))
        {
            return Fail(BillingErrorCode.InvalidInput);
        }

        await using var transaction = await _transactions.BeginAsync(cancellationToken);
        var invoice = await _billing.GetInvoiceForUpdateAsync(request.SchoolId, request.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.InvoiceNotFound);
        }
        if (invoice.Status is BillingInvoiceStatus.Cancelled or BillingInvoiceStatus.Refunded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.InvalidState);
        }

        var outstanding = BillingCommercialPolicy.Aging(invoice, DateTime.UtcNow).OutstandingAmount;
        if (request.AppliedAmount > outstanding)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.AmountExceedsOutstanding);
        }

        var now = DateTime.UtcNow;
        var payment = new BankTransferPayment
        {
            Id = Guid.NewGuid(),
            SchoolId = request.SchoolId,
            InvoiceId = invoice.Id,
            VerificationStatus = BankTransferVerificationStatus.Pending,
            PaymentReference = Clean(request.PaymentReference),
            EvidenceNote = Optional(request.EvidenceNote),
            ReceivedAmount = BillingCommercialPolicy.RoundMoney(request.ReceivedAmount),
            ReceivedCurrencyCode = Clean(request.ReceivedCurrencyCode).ToUpperInvariant(),
            AppliedAmount = BillingCommercialPolicy.RoundMoney(request.AppliedAmount),
            ReceivedAtUtc = request.ReceivedAtUtc > now.AddMinutes(1) ? now : request.ReceivedAtUtc,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            RowVersion = []
        };

        await QueueAuditAsync(
            actor.Id,
            request.SchoolId,
            "Payment.Recorded",
            "BankTransferPayment",
            payment.Id,
            null,
            new Dictionary<string, object?>
            {
                ["invoiceId"] = invoice.Id,
                ["paymentReference"] = payment.PaymentReference,
                ["receivedAmount"] = payment.ReceivedAmount,
                ["receivedCurrency"] = payment.ReceivedCurrencyCode,
                ["appliedAmount"] = payment.AppliedAmount,
                ["status"] = payment.VerificationStatus.ToString()
            },
            "Bank transfer recorded pending manual verification.",
            cancellationToken);

        var saved = await _billing.AddPaymentAsync(payment, cancellationToken);
        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);
        return BillingCommandResult.Success(payment.Id);
    }

    public async Task<BillingCommandResult> ConfirmBankTransferAsync(
        Guid actorUserId,
        Guid schoolId,
        Guid paymentId,
        byte[] expectedPaymentRowVersion,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(actorUserId, cancellationToken);
        if (actor is null)
            return Fail(BillingErrorCode.AccessDenied);

        await using var transaction = await _transactions.BeginAsync(cancellationToken);
        var payment = await _billing.GetPaymentForUpdateAsync(schoolId, paymentId, cancellationToken);
        if (payment is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.PaymentNotFound);
        }

        if (payment.VerificationStatus == BankTransferVerificationStatus.Confirmed)
        {
            await transaction.CommitAsync(cancellationToken);
            return BillingCommandResult.Success(payment.Id);
        }

        if (payment.VerificationStatus != BankTransferVerificationStatus.Pending)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.InvalidState);
        }

        if (expectedPaymentRowVersion is not { Length: > 0 } ||
            !expectedPaymentRowVersion.SequenceEqual(payment.RowVersion))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.ConcurrencyConflict);
        }

        var invoice = await _billing.GetInvoiceForUpdateAsync(schoolId, payment.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.InvoiceNotFound);
        }

        var outstandingBeforeConfirmation = BillingCommercialPolicy.RoundMoney(
            Math.Max(0m, invoice.TotalAmount - invoice.PaidAmount));
        if (payment.AppliedAmount > outstandingBeforeConfirmation)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.AmountExceedsOutstanding);
        }

        var paymentVersion = payment.RowVersion.ToArray();
        var invoiceVersion = invoice.RowVersion.ToArray();
        var now = DateTime.UtcNow;

        payment.VerificationStatus = BankTransferVerificationStatus.Confirmed;
        payment.VerifiedAtUtc = now;
        payment.VerifiedByUserId = actor.Id;
        payment.UpdatedAtUtc = now;

        invoice.PaidAmount = BillingCommercialPolicy.RoundMoney(invoice.PaidAmount + payment.AppliedAmount);
        invoice.Status = invoice.PaidAmount >= invoice.TotalAmount
            ? BillingInvoiceStatus.Paid
            : BillingInvoiceStatus.PartiallyPaid;
        invoice.UpdatedAtUtc = now;

        await QueueAuditAsync(
            actor.Id,
            schoolId,
            "Payment.Confirmed",
            "BankTransferPayment",
            payment.Id,
            new Dictionary<string, object?> { ["status"] = BankTransferVerificationStatus.Pending.ToString() },
            new Dictionary<string, object?>
            {
                ["status"] = payment.VerificationStatus.ToString(),
                ["invoicePaidAmount"] = invoice.PaidAmount,
                ["invoiceStatus"] = invoice.Status.ToString()
            },
            "Bank transfer manually confirmed and applied to invoice.",
            cancellationToken);

        var saved = await _billing.SavePaymentAndInvoiceAsync(
            payment, paymentVersion, invoice, invoiceVersion, cancellationToken);
        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        if (invoice.Status == BillingInvoiceStatus.Paid && invoice.Kind == BillingInvoiceKind.Initial)
        {
            var activation = await ActivateInsideTransactionAsync(
                actor,
                schoolId,
                agreedActivationAtUtc: null,
                "Billing.InitialActivationCompleted",
                "First required invoice fully paid; subscription and school activated.",
                cancellationToken);

            if (!activation.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return activation;
            }
        }

        if (invoice.Status == BillingInvoiceStatus.Paid && invoice.Kind == BillingInvoiceKind.Renewal)
        {
            var renewal = await ApplyPaidRenewalInsideTransactionAsync(actor, schoolId, null, cancellationToken);
            if (!renewal.Succeeded && renewal.Error != BillingErrorCode.InvalidState)
            {
                await transaction.RollbackAsync(cancellationToken);
                return renewal;
            }
        }

        await transaction.CommitAsync(cancellationToken);
        return BillingCommandResult.Success(payment.Id);
    }

    public async Task<BillingCommandResult> RejectBankTransferAsync(
        Guid actorUserId,
        Guid schoolId,
        Guid paymentId,
        string reason,
        byte[] expectedPaymentRowVersion,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(actorUserId, cancellationToken);
        if (actor is null)
            return Fail(BillingErrorCode.AccessDenied);
        if (string.IsNullOrWhiteSpace(reason))
            return Fail(BillingErrorCode.InvalidInput);

        await using var transaction = await _transactions.BeginAsync(cancellationToken);
        var payment = await _billing.GetPaymentForUpdateAsync(schoolId, paymentId, cancellationToken);
        if (payment is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.PaymentNotFound);
        }
        if (payment.VerificationStatus == BankTransferVerificationStatus.Rejected)
        {
            await transaction.CommitAsync(cancellationToken);
            return BillingCommandResult.Success(payment.Id);
        }
        if (payment.VerificationStatus != BankTransferVerificationStatus.Pending)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.InvalidState);
        }
        if (expectedPaymentRowVersion is not { Length: > 0 } ||
            !expectedPaymentRowVersion.SequenceEqual(payment.RowVersion))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.ConcurrencyConflict);
        }

        var invoice = await _billing.GetInvoiceForUpdateAsync(schoolId, payment.InvoiceId, cancellationToken);
        if (invoice is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.InvoiceNotFound);
        }

        var paymentVersion = payment.RowVersion.ToArray();
        var invoiceVersion = invoice.RowVersion.ToArray();
        var now = DateTime.UtcNow;

        payment.VerificationStatus = BankTransferVerificationStatus.Rejected;
        payment.RejectionReason = Clean(reason);
        payment.VerifiedAtUtc = now;
        payment.VerifiedByUserId = actor.Id;
        payment.UpdatedAtUtc = now;

        await QueueAuditAsync(
            actor.Id,
            schoolId,
            "Payment.Rejected",
            "BankTransferPayment",
            payment.Id,
            new Dictionary<string, object?> { ["status"] = BankTransferVerificationStatus.Pending.ToString() },
            new Dictionary<string, object?>
            {
                ["status"] = payment.VerificationStatus.ToString(),
                ["reason"] = payment.RejectionReason
            },
            "Bank transfer verification rejected.",
            cancellationToken);

        var saved = await _billing.SavePaymentAndInvoiceAsync(
            payment, paymentVersion, invoice, invoiceVersion, cancellationToken);
        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);
        return BillingCommandResult.Success(payment.Id);
    }

    public async Task<BillingCommandResult> RecordRefundAsync(
        Guid actorUserId,
        Guid schoolId,
        Guid invoiceId,
        Guid? paymentId,
        decimal amount,
        string currencyCode,
        string reason,
        byte[] expectedInvoiceRowVersion,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(actorUserId, cancellationToken);
        if (actor is null)
            return Fail(BillingErrorCode.AccessDenied);
        if (amount <= 0m || string.IsNullOrWhiteSpace(currencyCode) || string.IsNullOrWhiteSpace(reason))
            return Fail(BillingErrorCode.InvalidInput);

        await using var transaction = await _transactions.BeginAsync(cancellationToken);
        var invoice = await _billing.GetInvoiceForUpdateAsync(schoolId, invoiceId, cancellationToken);
        if (invoice is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.InvoiceNotFound);
        }

        if (expectedInvoiceRowVersion is not { Length: > 0 } ||
            !expectedInvoiceRowVersion.SequenceEqual(invoice.RowVersion))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.ConcurrencyConflict);
        }
        if (amount > invoice.PaidAmount - invoice.RefundedAmount)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.InvalidInput);
        }

        var expected = invoice.RowVersion.ToArray();
        var now = DateTime.UtcNow;
        var refundAmount = BillingCommercialPolicy.RoundMoney(amount);
        var refund = new BillingRefund
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            InvoiceId = invoice.Id,
            PaymentId = paymentId,
            Amount = refundAmount,
            CurrencyCode = Clean(currencyCode).ToUpperInvariant(),
            Reason = Clean(reason),
            RecordedByUserId = actor.Id,
            RecordedAtUtc = now
        };

        invoice.RefundedAmount = BillingCommercialPolicy.RoundMoney(invoice.RefundedAmount + refundAmount);
        invoice.Status = invoice.RefundedAmount >= invoice.PaidAmount
            ? BillingInvoiceStatus.Refunded
            : BillingInvoiceStatus.PartiallyRefunded;
        invoice.UpdatedAtUtc = now;

        await QueueAuditAsync(
            actor.Id,
            schoolId,
            "Payment.RefundRecorded",
            "BillingRefund",
            refund.Id,
            null,
            new Dictionary<string, object?>
            {
                ["invoiceId"] = invoice.Id,
                ["paymentId"] = paymentId,
                ["amount"] = refund.Amount,
                ["currency"] = refund.CurrencyCode,
                ["invoiceStatus"] = invoice.Status.ToString()
            },
            "External/manual refund or credit recorded; no payout automation executed.",
            cancellationToken);

        var saved = await _billing.SaveInvoiceAndRefundAsync(invoice, expected, refund, cancellationToken);
        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);
        return BillingCommandResult.Success(refund.Id);
    }

    public async Task<BillingCommandResult> ActivateByAgreementAsync(
        Guid actorUserId,
        Guid schoolId,
        DateTime agreedActivationAtUtc,
        string reason,
        byte[] expectedSubscriptionRowVersion,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(actorUserId, cancellationToken);
        if (actor is null)
            return Fail(BillingErrorCode.AccessDenied);
        if (string.IsNullOrWhiteSpace(reason) || agreedActivationAtUtc > DateTime.UtcNow.AddMinutes(1))
            return Fail(BillingErrorCode.InvalidInput);

        await using var transaction = await _transactions.BeginAsync(cancellationToken);
        var result = await ActivateInsideTransactionAsync(
            actor,
            schoolId,
            agreedActivationAtUtc,
            "Billing.AgreedActivationOverride",
            "SuperAdmin explicitly recorded an agreed commercial activation date: " + Clean(reason),
            cancellationToken,
            expectedSubscriptionRowVersion);

        if (!result.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    public async Task<BillingCommandResult> ApplyPaidRenewalAsync(
        Guid actorUserId,
        Guid schoolId,
        byte[] expectedSubscriptionRowVersion,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(actorUserId, cancellationToken);
        if (actor is null)
            return Fail(BillingErrorCode.AccessDenied);

        await using var transaction = await _transactions.BeginAsync(cancellationToken);
        var result = await ApplyPaidRenewalInsideTransactionAsync(
            actor, schoolId, expectedSubscriptionRowVersion, cancellationToken);
        if (!result.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return result;
        }
        await transaction.CommitAsync(cancellationToken);
        return result;
    }

    private async Task<BillingCommandResult> CreateStandardInvoiceAsync(
        Guid actorUserId,
        Guid schoolId,
        BillingInvoiceKind kind,
        decimal taxAmount,
        decimal? settlementEquivalentAmount,
        CancellationToken cancellationToken)
    {
        var actor = await RequirePlatformActorAsync(actorUserId, cancellationToken);
        if (actor is null)
            return Fail(BillingErrorCode.AccessDenied);
        if (taxAmount < 0m || (settlementEquivalentAmount.HasValue && settlementEquivalentAmount.Value < 0m))
            return Fail(BillingErrorCode.InvalidInput);

        await using var transaction = await _transactions.BeginAsync(cancellationToken);
        var subscription = await _subscriptions.GetForUpdateBySchoolAsync(schoolId, cancellationToken);
        if (subscription is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.SubscriptionNotFound);
        }

        var profile = await _billing.GetProfileAsync(schoolId, cancellationToken);
        if (profile is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(BillingErrorCode.ProfileNotFound);
        }

        var existing = await _billing.ListInvoicesAsync(schoolId, cancellationToken);
        var now = DateTime.UtcNow;
        decimal net;
        int? installment;
        int seats;
        int months;
        DateTime? periodStart;
        DateTime? periodEnd;
        BillingInvoiceLineKind lineKind;
        string description;

        switch (kind)
        {
            case BillingInvoiceKind.Initial:
                if (subscription.Status != SubscriptionStatus.PendingActivation ||
                    await _billing.HasInvoiceAsync(subscription.Id, BillingInvoiceKind.Initial, 1, cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Fail(BillingErrorCode.DuplicateInvoice);
                }

                net = BillingCommercialPolicy.InitialNetAmount(subscription);
                installment = 1;
                seats = BillingCommercialPolicy.BillableSeats(subscription.CommittedSeats);
                months = subscription.BillingCadence == SubscriptionBillingCadence.FullTermUpfront
                    ? SubscriptionCommercialPolicy.Months(subscription.Term)
                    : 1;
                periodStart = null;
                periodEnd = null;
                lineKind = BillingInvoiceLineKind.Subscription;
                description = "Initial commercial subscription invoice";
                break;

            case BillingInvoiceKind.MonthlyInstallment:
                if (subscription.Status is not (SubscriptionStatus.Active or SubscriptionStatus.Suspended) ||
                    subscription.BillingCadence != SubscriptionBillingCadence.MonthlyInstallments ||
                    !subscription.CurrentTermStartsAtUtc.HasValue ||
                    !subscription.CurrentTermEndsAtUtc.HasValue)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Fail(BillingErrorCode.InvalidState);
                }

                installment = existing.Count(x => x.Kind == BillingInvoiceKind.MonthlyInstallment) + 2;
                if (installment > SubscriptionCommercialPolicy.Months(subscription.Term) ||
                    await _billing.HasInvoiceAsync(subscription.Id, BillingInvoiceKind.MonthlyInstallment, installment, cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Fail(BillingErrorCode.DuplicateInvoice);
                }

                periodStart = subscription.CurrentTermStartsAtUtc.Value.AddMonths(installment.Value - 1);
                periodEnd = periodStart.Value.AddMonths(1);
                if (periodEnd > subscription.CurrentTermEndsAtUtc)
                    periodEnd = subscription.CurrentTermEndsAtUtc;

                net = BillingCommercialPolicy.RoundMoney(
                    BillingCommercialPolicy.BillableSeats(subscription.CommittedSeats) *
                    subscription.PricePerStudentPerMonth);
                seats = BillingCommercialPolicy.BillableSeats(subscription.CommittedSeats);
                months = 1;
                lineKind = BillingInvoiceLineKind.Subscription;
                description = $"Monthly installment {installment}";
                break;

            case BillingInvoiceKind.Renewal:
                if (subscription.Status is not (SubscriptionStatus.Active or SubscriptionStatus.Suspended) ||
                    !subscription.CurrentTermEndsAtUtc.HasValue)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Fail(BillingErrorCode.InvalidState);
                }

                installment = existing.Count(x => x.Kind == BillingInvoiceKind.Renewal) + 1;
                if (await _billing.HasInvoiceAsync(subscription.Id, BillingInvoiceKind.Renewal, installment, cancellationToken))
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return Fail(BillingErrorCode.DuplicateInvoice);
                }

                seats = BillingCommercialPolicy.BillableSeats(
                    subscription.PendingRenewalSeats ?? subscription.CommittedSeats);
                net = BillingCommercialPolicy.RenewalNetAmount(subscription, seats);
                months = subscription.BillingCadence == SubscriptionBillingCadence.FullTermUpfront
                    ? SubscriptionCommercialPolicy.Months(subscription.Term)
                    : 1;
                periodStart = subscription.CurrentTermEndsAtUtc;
                periodEnd = periodStart.Value.AddMonths(SubscriptionCommercialPolicy.Months(subscription.Term));
                lineKind = BillingInvoiceLineKind.Renewal;
                description = $"Commercial subscription renewal invoice {installment}";
                break;

            default:
                await transaction.RollbackAsync(cancellationToken);
                return Fail(BillingErrorCode.InvalidInput);
        }

        var invoice = NewInvoice(
            subscription, profile, kind, installment, periodStart, periodEnd,
            net, taxAmount, settlementEquivalentAmount, now);

        var line = new BillingInvoiceLine
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            InvoiceId = invoice.Id,
            Kind = lineKind,
            Description = description,
            SeatCount = seats,
            UnitMonthlyPrice = subscription.PricePerStudentPerMonth,
            QuantityMonths = months,
            ServicePeriodStartsAtUtc = periodStart,
            ServicePeriodEndsAtUtc = periodEnd,
            NetAmount = net,
            CreatedAtUtc = now
        };

        await QueueInvoiceCreatedAndIssuedAsync(actor.Id, invoice, $"{kind} invoice", cancellationToken);

        if (kind == BillingInvoiceKind.Renewal)
        {
            await QueueAuditAsync(
                actor.Id,
                schoolId,
                "Billing.RenewalInvoiced",
                "BillingInvoice",
                invoice.Id,
                null,
                new Dictionary<string, object?>
                {
                    ["invoiceNumber"] = invoice.InvoiceNumber,
                    ["renewalSeats"] = seats,
                    ["netAmount"] = net
                },
                "Renewal invoice created without changing current-term commitment.",
                cancellationToken);
        }

        var saved = await _billing.AddInvoiceAsync(invoice, [line], cancellationToken);
        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);
        return BillingCommandResult.Success(invoice.Id);
    }

    private BillingInvoice NewInvoice(
        SchoolSubscription subscription,
        SchoolBillingProfile profile,
        BillingInvoiceKind kind,
        int? installmentNumber,
        DateTime? billingPeriodStart,
        DateTime? billingPeriodEnd,
        decimal netAmount,
        decimal taxAmount,
        decimal? settlementEquivalentAmount,
        DateTime now)
    {
        var id = Guid.NewGuid();
        var normalizedTax = BillingCommercialPolicy.RoundMoney(taxAmount);
        var total = BillingCommercialPolicy.RoundMoney(netAmount + normalizedTax);
        var settlement = Optional(profile.DefaultSettlementCurrencyCode)?.ToUpperInvariant();
        if (subscription.CommercialCurrency == CommercialCurrency.AED && string.IsNullOrWhiteSpace(settlement))
            settlement = "EUR";

        return new BillingInvoice
        {
            Id = id,
            SchoolId = subscription.SchoolId,
            SubscriptionId = subscription.Id,
            InvoiceNumber = $"EDU-{now:yyyyMMdd}-{id.ToString("N")[..8]}".ToUpperInvariant(),
            Kind = kind,
            Status = BillingInvoiceStatus.Due,
            InvoiceCurrency = subscription.CommercialCurrency,
            SettlementCurrencyCode = settlement,
            SettlementEquivalentAmount = settlementEquivalentAmount.HasValue
                ? BillingCommercialPolicy.RoundMoney(settlementEquivalentAmount.Value)
                : null,
            LegalNameSnapshot = profile.LegalName,
            BillingAddressSnapshot = profile.BillingAddress,
            CountryCodeSnapshot = profile.CountryCode,
            TaxIdentifierSnapshot = profile.TaxIdentifier,
            InvoiceEmailSnapshot = profile.InvoiceEmail,
            TaxTreatmentCodeSnapshot = profile.TaxTreatmentCode,
            PaymentInstructionsSnapshot = profile.PaymentInstructions,
            IssueDateUtc = now,
            DueDateUtc = BillingCommercialPolicy.DueDate(now),
            GraceEndsAtUtc = BillingCommercialPolicy.GraceEnds(BillingCommercialPolicy.DueDate(now)),
            BillingPeriodStartsAtUtc = billingPeriodStart,
            BillingPeriodEndsAtUtc = billingPeriodEnd,
            InstallmentNumber = installmentNumber,
            NetAmount = BillingCommercialPolicy.RoundMoney(netAmount),
            TaxAmount = normalizedTax,
            TotalAmount = total,
            PaidAmount = 0m,
            RefundedAmount = 0m,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            RowVersion = []
        };
    }

    private async Task<BillingCommandResult> ActivateInsideTransactionAsync(
        SchoolUserRecord actor,
        Guid schoolId,
        DateTime? agreedActivationAtUtc,
        string auditAction,
        string auditSummary,
        CancellationToken cancellationToken,
        byte[]? expectedSubscriptionRowVersion = null)
    {
        var subscription = await _subscriptions.GetForUpdateBySchoolAsync(schoolId, cancellationToken);
        if (subscription is null)
            return Fail(BillingErrorCode.SubscriptionNotFound);

        if (subscription.Status == SubscriptionStatus.Active && subscription.ActivatedAtUtc.HasValue)
            return BillingCommandResult.Success(subscription.Id);

        if (subscription.Status != SubscriptionStatus.PendingActivation)
            return Fail(BillingErrorCode.InvalidState);
        if (expectedSubscriptionRowVersion is { Length: > 0 } &&
            !expectedSubscriptionRowVersion.SequenceEqual(subscription.RowVersion))
            return Fail(BillingErrorCode.ConcurrencyConflict);

        var school = await _schools.GetForUpdateAsync(schoolId, cancellationToken);
        if (school is null)
            return Fail(BillingErrorCode.SchoolNotFound);
        if (school.Status != SchoolStatus.Suspended)
            return Fail(BillingErrorCode.InvalidState);

        var now = DateTime.UtcNow;
        var activationAt = agreedActivationAtUtc ?? now;
        if (activationAt > now.AddMinutes(1))
            return Fail(BillingErrorCode.InvalidInput);

        var subscriptionVersion = subscription.RowVersion.ToArray();
        var schoolVersion = school.RowVersion.ToArray();

        subscription.Status = SubscriptionStatus.Active;
        subscription.ActivatedAtUtc = activationAt;
        subscription.CurrentTermStartsAtUtc = activationAt;
        subscription.CurrentTermEndsAtUtc = activationAt.AddMonths(SubscriptionCommercialPolicy.Months(subscription.Term));
        subscription.SuspendedAtUtc = null;
        subscription.EndedAtUtc = null;
        subscription.UpdatedAtUtc = now;

        school.Status = SchoolStatus.Active;
        school.UpdatedAtUtc = now;

        await QueueAuditAsync(
            actor.Id,
            schoolId,
            "Subscription.Activated",
            "SchoolSubscription",
            subscription.Id,
            new Dictionary<string, object?> { ["status"] = SubscriptionStatus.PendingActivation.ToString() },
            new Dictionary<string, object?>
            {
                ["status"] = subscription.Status.ToString(),
                ["activatedAtUtc"] = subscription.ActivatedAtUtc,
                ["currentTermEndsAtUtc"] = subscription.CurrentTermEndsAtUtc
            },
            "Subscription activated through Phase25D commercial gate.",
            cancellationToken);

        await QueueAuditAsync(
            actor.Id,
            schoolId,
            auditAction,
            "SchoolSubscription",
            subscription.Id,
            null,
            new Dictionary<string, object?>
            {
                ["activatedAtUtc"] = activationAt,
                ["schoolStatus"] = school.Status.ToString(),
                ["subscriptionStatus"] = subscription.Status.ToString()
            },
            auditSummary,
            cancellationToken);

        var saved = await _subscriptions.SaveWithSchoolAsync(
            subscription,
            subscriptionVersion,
            school,
            schoolVersion,
            cancellationToken: cancellationToken);

        return saved.Succeeded
            ? BillingCommandResult.Success(subscription.Id)
            : Fail(MapSubscriptionPersistence(saved.Error));
    }

    private async Task<BillingCommandResult> ApplyPaidRenewalInsideTransactionAsync(
        SchoolUserRecord actor,
        Guid schoolId,
        byte[]? expectedSubscriptionRowVersion,
        CancellationToken cancellationToken)
    {
        var subscription = await _subscriptions.GetForUpdateBySchoolAsync(schoolId, cancellationToken);
        if (subscription is null)
            return Fail(BillingErrorCode.SubscriptionNotFound);

        if (subscription.Status is not (SubscriptionStatus.Active or SubscriptionStatus.Suspended) ||
            !subscription.CurrentTermEndsAtUtc.HasValue ||
            subscription.CurrentTermEndsAtUtc.Value > DateTime.UtcNow)
        {
            return Fail(BillingErrorCode.InvalidState);
        }

        if (expectedSubscriptionRowVersion is { Length: > 0 } &&
            !expectedSubscriptionRowVersion.SequenceEqual(subscription.RowVersion))
            return Fail(BillingErrorCode.ConcurrencyConflict);

        var invoices = await _billing.ListInvoicesAsync(schoolId, cancellationToken);
        var paidRenewal = invoices
            .Where(x => x.SubscriptionId == subscription.Id &&
                        x.Kind == BillingInvoiceKind.Renewal &&
                        x.Status == BillingInvoiceStatus.Paid &&
                        x.BillingPeriodStartsAtUtc == subscription.CurrentTermEndsAtUtc)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefault();
        if (paidRenewal is null)
            return Fail(BillingErrorCode.InvalidState);

        var activeStudents = await _subscriptions.CountActiveStudentsAsync(schoolId, cancellationToken);
        var targetSeats = BillingCommercialPolicy.BillableSeats(
            subscription.PendingRenewalSeats ?? subscription.CommittedSeats);
        if (targetSeats < activeStudents)
            return Fail(BillingErrorCode.InvalidState);

        var previousSeats = subscription.CommittedSeats;
        var previousEnd = subscription.CurrentTermEndsAtUtc.Value;
        var expected = subscription.RowVersion.ToArray();
        var now = DateTime.UtcNow;

        subscription.CurrentTermStartsAtUtc = previousEnd;
        subscription.CurrentTermEndsAtUtc = previousEnd.AddMonths(SubscriptionCommercialPolicy.Months(subscription.Term));
        subscription.CommittedSeats = targetSeats;
        subscription.PendingRenewalSeats = null;
        subscription.NonRenewalRequestedAtUtc = null;
        subscription.UpdatedAtUtc = now;

        SubscriptionSeatChange? seatChange = null;
        if (targetSeats != previousSeats)
        {
            seatChange = new SubscriptionSeatChange
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                SubscriptionId = subscription.Id,
                ChangeType = SeatCommitmentChangeType.RenewalAdjustment,
                PreviousSeats = previousSeats,
                NewSeats = targetSeats,
                EffectiveAtUtc = previousEnd,
                CreatedAtUtc = now
            };
        }

        await QueueAuditAsync(
            actor.Id,
            schoolId,
            "Subscription.Renewed",
            "SchoolSubscription",
            subscription.Id,
            new Dictionary<string, object?>
            {
                ["termEnd"] = previousEnd,
                ["committedSeats"] = previousSeats
            },
            new Dictionary<string, object?>
            {
                ["termStart"] = subscription.CurrentTermStartsAtUtc,
                ["termEnd"] = subscription.CurrentTermEndsAtUtc,
                ["committedSeats"] = targetSeats,
                ["paidRenewalInvoice"] = paidRenewal.InvoiceNumber
            },
            "Paid renewal applied without bypassing seat-floor/active-student rules.",
            cancellationToken);

        var saved = await _subscriptions.SaveAsync(subscription, expected, seatChange, cancellationToken);
        return saved.Succeeded
            ? BillingCommandResult.Success(subscription.Id)
            : Fail(MapSubscriptionPersistence(saved.Error));
    }

    private async Task<BillingInvoiceDetails> MapInvoiceAsync(
        BillingInvoice invoice,
        CancellationToken cancellationToken)
    {
        var lines = await _billing.ListInvoiceLinesAsync(invoice.SchoolId, invoice.Id, cancellationToken);
        var payments = await _billing.ListPaymentsAsync(invoice.SchoolId, invoice.Id, cancellationToken);
        var aging = BillingCommercialPolicy.Aging(invoice, DateTime.UtcNow);

        return new BillingInvoiceDetails(
            invoice.Id,
            invoice.SchoolId,
            invoice.SubscriptionId,
            invoice.InvoiceNumber,
            invoice.Kind,
            invoice.Status,
            aging.EffectiveStatus,
            invoice.InvoiceCurrency,
            invoice.SettlementCurrencyCode,
            invoice.SettlementEquivalentAmount,
            invoice.IssueDateUtc,
            invoice.DueDateUtc,
            invoice.GraceEndsAtUtc,
            invoice.BillingPeriodStartsAtUtc,
            invoice.BillingPeriodEndsAtUtc,
            invoice.InstallmentNumber,
            invoice.NetAmount,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.PaidAmount,
            invoice.RefundedAmount,
            aging.OutstandingAmount,
            aging.InGracePeriod,
            aging.SuspensionEligible,
            lines.Select(x => new BillingInvoiceLineDetails(
                x.Id, x.Kind, x.Description, x.SeatCount, x.SeatDelta,
                x.UnitMonthlyPrice, x.QuantityMonths, x.ServicePeriodStartsAtUtc,
                x.ServicePeriodEndsAtUtc, x.ProrationNumeratorDays,
                x.ProrationDenominatorDays, x.NetAmount,
                x.SubscriptionSeatChangeId)).ToArray(),
            payments.Select(x => new BankTransferPaymentDetails(
                x.Id, x.VerificationStatus, x.PaymentReference, x.EvidenceNote,
                x.ReceivedAmount, x.ReceivedCurrencyCode, x.AppliedAmount,
                x.ReceivedAtUtc, x.VerifiedAtUtc, x.RejectionReason,
                x.RowVersion.ToArray())).ToArray(),
            invoice.RowVersion.ToArray());
    }

    private static BillingProfileDetails MapProfile(SchoolBillingProfile profile) =>
        new(
            profile.Id,
            profile.SchoolId,
            profile.LegalName,
            profile.BillingAddress,
            profile.CountryCode,
            profile.TaxIdentifier,
            profile.InvoiceEmail,
            profile.TaxTreatmentCode,
            profile.DefaultSettlementCurrencyCode,
            profile.PaymentInstructions,
            profile.RowVersion.ToArray());

    private async Task<SchoolUserRecord?> RequirePlatformActorAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var actor = await _users.GetActorAsync(actorUserId, cancellationToken);
        if (actor is null || !actor.IsActive || actor.IsLocked || actor.SchoolId.HasValue ||
            actor.Roles.Count != 1 || actor.Roles[0] != RoleNames.SuperAdmin)
            return null;

        return actor;
    }

    private async Task QueueInvoiceCreatedAndIssuedAsync(
        Guid actorUserId,
        BillingInvoice invoice,
        string label,
        CancellationToken cancellationToken)
    {
        var values = InvoiceAuditValues(invoice);
        await QueueAuditAsync(
            actorUserId,
            invoice.SchoolId,
            "Invoice.Created",
            "BillingInvoice",
            invoice.Id,
            null,
            values,
            label + " created.",
            cancellationToken);
        await QueueAuditAsync(
            actorUserId,
            invoice.SchoolId,
            "Invoice.Issued",
            "BillingInvoice",
            invoice.Id,
            null,
            values,
            label + " issued.",
            cancellationToken);
    }

    private async Task QueueAuditAsync(
        Guid actorUserId,
        Guid schoolId,
        string action,
        string entityType,
        Guid entityId,
        IReadOnlyDictionary<string, object?>? oldValues,
        IReadOnlyDictionary<string, object?>? newValues,
        string summary,
        CancellationToken cancellationToken)
    {
        await _audit.QueueAsync(
            new AuditEvent(
                SchoolId: schoolId,
                Action: action,
                EntityType: entityType,
                EntityId: entityId.ToString("D"),
                Feature: "Billing",
                OldValues: oldValues,
                NewValues: newValues,
                ResultSummary: summary,
                ActorUserIdOverride: actorUserId,
                ActorRoleOverride: RoleNames.SuperAdmin),
            cancellationToken);
    }

    private static Dictionary<string, object?> InvoiceAuditValues(BillingInvoice invoice) =>
        new()
        {
            ["invoiceNumber"] = invoice.InvoiceNumber,
            ["kind"] = invoice.Kind.ToString(),
            ["invoiceCurrency"] = invoice.InvoiceCurrency.ToString(),
            ["settlementCurrency"] = invoice.SettlementCurrencyCode,
            ["netAmount"] = invoice.NetAmount,
            ["taxAmount"] = invoice.TaxAmount,
            ["totalAmount"] = invoice.TotalAmount,
            ["dueDateUtc"] = invoice.DueDateUtc,
            ["graceEndsAtUtc"] = invoice.GraceEndsAtUtc
        };

    private static bool ValidProfile(UpsertBillingProfileRequest request) =>
        !string.IsNullOrWhiteSpace(request.LegalName) &&
        !string.IsNullOrWhiteSpace(request.BillingAddress) &&
        !string.IsNullOrWhiteSpace(request.CountryCode) &&
        !string.IsNullOrWhiteSpace(request.InvoiceEmail) &&
        request.InvoiceEmail.Contains('@', StringComparison.Ordinal) &&
        !string.IsNullOrWhiteSpace(request.PaymentInstructions);

    private static string Clean(string value) => value.Trim();

    private static string? Optional(string? value)
    {
        value = value?.Trim();
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static BillingCommandResult Fail(BillingErrorCode error) =>
        BillingCommandResult.Failure(error);

    private static BillingErrorCode MapPersistence(BillingPersistenceError error) =>
        error == BillingPersistenceError.Concurrency
            ? BillingErrorCode.ConcurrencyConflict
            : BillingErrorCode.PersistenceError;

    private static BillingErrorCode MapSubscriptionPersistence(SubscriptionPersistenceError error) =>
        error == SubscriptionPersistenceError.Concurrency
            ? BillingErrorCode.ConcurrencyConflict
            : BillingErrorCode.PersistenceError;
}
