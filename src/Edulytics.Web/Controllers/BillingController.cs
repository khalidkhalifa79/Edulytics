using System.Security.Claims;
using Edulytics.Services.Billing;
using Edulytics.Web.ViewModels.Billing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "PlatformAdministration")]
[Route("Platform/Billing")]
public sealed class BillingController : Controller
{
    private readonly IBillingService _billing;
    private readonly IStringLocalizer<BillingResource> _text;

    public BillingController(
        IBillingService billing,
        IStringLocalizer<BillingResource> text)
    {
        _billing = billing;
        _text = text;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await _billing.ListAsync(actorId, cancellationToken);
        if (result.Value is null)
            return Forbid();

        return View(new BillingIndexViewModel { Schools = result.Value });
    }

    [HttpPost("{schoolId:guid}/Profile")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(
        Guid schoolId,
        string legalName,
        string billingAddress,
        string countryCode,
        string taxIdentifier,
        string invoiceEmail,
        string? taxTreatmentCode,
        string? defaultSettlementCurrencyCode,
        string paymentInstructions,
        string? rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        byte[]? version = null;
        if (!string.IsNullOrWhiteSpace(rowVersion) && !TryDecode(rowVersion, out version))
            return RedirectError(BillingErrorCode.ConcurrencyConflict);

        var result = await _billing.UpsertProfileAsync(
            actorId,
            new UpsertBillingProfileRequest(
                schoolId,
                legalName,
                billingAddress,
                countryCode,
                taxIdentifier,
                invoiceEmail,
                taxTreatmentCode,
                defaultSettlementCurrencyCode,
                paymentInstructions,
                version),
            cancellationToken);

        return RedirectResult(result, "SuccessProfile");
    }

    [HttpPost("{schoolId:guid}/Invoices/Initial")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> InitialInvoice(
        Guid schoolId,
        decimal taxAmount,
        decimal? settlementEquivalentAmount,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            actor => _billing.CreateInitialInvoiceAsync(
                actor, schoolId, taxAmount, settlementEquivalentAmount, cancellationToken),
            "SuccessInitialInvoice");

    [HttpPost("{schoolId:guid}/Invoices/Installment")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> InstallmentInvoice(
        Guid schoolId,
        decimal taxAmount,
        decimal? settlementEquivalentAmount,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            actor => _billing.CreateNextInstallmentAsync(
                actor, schoolId, taxAmount, settlementEquivalentAmount, cancellationToken),
            "SuccessInstallmentInvoice");

    [HttpPost("{schoolId:guid}/Invoices/Proration")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ProrationInvoice(
        Guid schoolId,
        decimal taxAmount,
        decimal? settlementEquivalentAmount,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            actor => _billing.CreateSeatProrationInvoiceAsync(
                actor, schoolId, taxAmount, settlementEquivalentAmount, cancellationToken),
            "SuccessProrationInvoice");

    [HttpPost("{schoolId:guid}/Invoices/Renewal")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RenewalInvoice(
        Guid schoolId,
        decimal taxAmount,
        decimal? settlementEquivalentAmount,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            actor => _billing.CreateRenewalInvoiceAsync(
                actor, schoolId, taxAmount, settlementEquivalentAmount, cancellationToken),
            "SuccessRenewalInvoice");

    [HttpPost("{schoolId:guid}/Invoices/{invoiceId:guid}/Payments")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RecordPayment(
        Guid schoolId,
        Guid invoiceId,
        string paymentReference,
        string? evidenceNote,
        decimal receivedAmount,
        string receivedCurrencyCode,
        decimal appliedAmount,
        DateTime receivedAtUtc,
        CancellationToken cancellationToken)
    {
        receivedAtUtc = NormalizeUtc(receivedAtUtc);
        return ExecuteAsync(
            actor => _billing.RecordBankTransferAsync(
                actor,
                new RecordBankTransferRequest(
                    schoolId,
                    invoiceId,
                    paymentReference,
                    evidenceNote,
                    receivedAmount,
                    receivedCurrencyCode,
                    appliedAmount,
                    receivedAtUtc),
                cancellationToken),
            "SuccessPaymentRecorded");
    }

    [HttpPost("{schoolId:guid}/Payments/{paymentId:guid}/Confirm")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ConfirmPayment(
        Guid schoolId,
        Guid paymentId,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ExecuteVersionedAsync(
            rowVersion,
            (actor, version) => _billing.ConfirmBankTransferAsync(
                actor, schoolId, paymentId, version, cancellationToken),
            "SuccessPaymentConfirmed");

    [HttpPost("{schoolId:guid}/Payments/{paymentId:guid}/Reject")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> RejectPayment(
        Guid schoolId,
        Guid paymentId,
        string reason,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ExecuteVersionedAsync(
            rowVersion,
            (actor, version) => _billing.RejectBankTransferAsync(
                actor, schoolId, paymentId, reason, version, cancellationToken),
            "SuccessPaymentRejected");

    [HttpPost("{schoolId:guid}/Invoices/{invoiceId:guid}/Refund")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Refund(
        Guid schoolId,
        Guid invoiceId,
        Guid? paymentId,
        decimal amount,
        string currencyCode,
        string reason,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ExecuteVersionedAsync(
            rowVersion,
            (actor, version) => _billing.RecordRefundAsync(
                actor,
                schoolId,
                invoiceId,
                paymentId,
                amount,
                currencyCode,
                reason,
                version,
                cancellationToken),
            "SuccessRefund");

    [HttpPost("{schoolId:guid}/ActivateByAgreement")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ActivateByAgreement(
        Guid schoolId,
        DateTime agreedActivationAtUtc,
        string reason,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ExecuteVersionedAsync(
            rowVersion,
            (actor, version) => _billing.ActivateByAgreementAsync(
                actor,
                schoolId,
                NormalizeUtc(agreedActivationAtUtc),
                reason,
                version,
                cancellationToken),
            "SuccessAgreementActivation");

    [HttpPost("{schoolId:guid}/ApplyPaidRenewal")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ApplyPaidRenewal(
        Guid schoolId,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ExecuteVersionedAsync(
            rowVersion,
            (actor, version) => _billing.ApplyPaidRenewalAsync(
                actor, schoolId, version, cancellationToken),
            "SuccessRenewalApplied");

    private async Task<IActionResult> ExecuteAsync(
        Func<Guid, Task<BillingCommandResult>> command,
        string successKey)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result = await command(actorId);
        return RedirectResult(result, successKey);
    }

    private Task<IActionResult> ExecuteVersionedAsync(
        string rowVersion,
        Func<Guid, byte[], Task<BillingCommandResult>> command,
        string successKey)
    {
        if (!TryDecode(rowVersion, out var version) || version is null)
            return Task.FromResult(RedirectError(BillingErrorCode.ConcurrencyConflict));

        return ExecuteAsync(actor => command(actor, version), successKey);
    }

    private IActionResult RedirectResult(BillingCommandResult result, string successKey)
    {
        if (result.Succeeded)
            TempData["BillingSuccess"] = _text[successKey].Value;
        else
            TempData["BillingError"] = _text[result.Error.ToString()].Value;

        return RedirectToAction(nameof(Index));
    }

    private IActionResult RedirectError(BillingErrorCode error)
    {
        TempData["BillingError"] = _text[error.ToString()].Value;
        return RedirectToAction(nameof(Index));
    }

    private bool TryActor(out Guid actorId)
    {
        actorId = default;
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out actorId);
    }

    private static bool TryDecode(string? value, out byte[]? bytes)
    {
        bytes = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            bytes = Convert.FromBase64String(value);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
