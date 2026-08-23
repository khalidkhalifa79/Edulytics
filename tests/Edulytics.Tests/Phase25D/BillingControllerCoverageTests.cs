using System.Security.Claims;
using Edulytics.Services.Billing;
using Edulytics.Web;
using Edulytics.Web.Controllers;
using Edulytics.Web.ViewModels.Billing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Localization;

namespace Edulytics.Tests.Phase25D;

public sealed class BillingControllerCoverageTests
{
    [Fact]
    public async Task Index_WithoutActor_ReturnsForbid()
    {
        var (controller, _) = Controller(withActor: false);

        var result = await controller.Index(CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Index_WhenServiceRejectsActor_ReturnsForbid()
    {
        var (controller, service) = Controller();
        service.ListResult =
            BillingQueryResult<IReadOnlyList<BillingSchoolDetails>>
                .Failure(BillingErrorCode.AccessDenied);

        var result = await controller.Index(CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Index_WhenAuthorized_ReturnsView()
    {
        var (controller, service) = Controller();
        service.ListResult =
            BillingQueryResult<IReadOnlyList<BillingSchoolDetails>>
                .Success(Array.Empty<BillingSchoolDetails>());

        var result = await controller.Index(CancellationToken.None);

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<BillingIndexViewModel>(view.Model);
        Assert.Empty(model.Schools);
    }

    [Fact]
    public async Task Profile_WithoutActor_ReturnsForbid()
    {
        var (controller, _) = Controller(withActor: false);

        var result = await controller.Profile(
            Guid.NewGuid(),
            "Legal",
            "Address",
            "PL",
            "NIP",
            "invoice@example.com",
            null,
            "PLN",
            "Bank instructions",
            null,
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Profile_InvalidRowVersion_RedirectsWithConcurrencyError()
    {
        var (controller, _) = Controller();

        var result = await controller.Profile(
            Guid.NewGuid(),
            "Legal",
            "Address",
            "PL",
            "NIP",
            "invoice@example.com",
            null,
            "PLN",
            "Bank instructions",
            "not-base64",
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Contains(
            nameof(BillingErrorCode.ConcurrencyConflict),
            controller.TempData["BillingError"]?.ToString());
    }

    [Fact]
    public async Task Profile_EmptyRowVersion_CallsService()
    {
        var (controller, service) = Controller();
        service.CommandResult = BillingCommandResult.Success(Guid.NewGuid());

        var result = await controller.Profile(
            Guid.NewGuid(),
            "Legal",
            "Address",
            "PL",
            "NIP",
            "invoice@example.com",
            null,
            "PLN",
            "Bank instructions",
            string.Empty,
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(1, service.UpsertCalls);
    }

    [Fact]
    public async Task InitialInvoice_WithoutActor_ReturnsForbid()
    {
        var (controller, _) = Controller(withActor: false);

        var result = await controller.InitialInvoice(
            Guid.NewGuid(),
            0m,
            null,
            CancellationToken.None);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task InitialInvoice_ServiceFailure_UsesErrorTempData()
    {
        var (controller, service) = Controller();
        service.CommandResult =
            BillingCommandResult.Failure(BillingErrorCode.ProfileNotFound);

        var result = await controller.InitialInvoice(
            Guid.NewGuid(),
            0m,
            null,
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Contains(
            nameof(BillingErrorCode.ProfileNotFound),
            controller.TempData["BillingError"]?.ToString());
    }

    [Fact]
    public async Task InitialInvoice_ServiceSuccess_UsesSuccessTempData()
    {
        var (controller, service) = Controller();
        service.CommandResult = BillingCommandResult.Success(Guid.NewGuid());

        var result = await controller.InitialInvoice(
            Guid.NewGuid(),
            0m,
            null,
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Contains(
            "SuccessInitialInvoice",
            controller.TempData["BillingSuccess"]?.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("%%%")]
    public async Task ConfirmPayment_InvalidRowVersion_DoesNotCallService(
        string rowVersion)
    {
        var (controller, service) = Controller();

        var result = await controller.ConfirmPayment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            rowVersion,
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(0, service.ConfirmCalls);
    }

    [Fact]
    public async Task ConfirmPayment_ValidRowVersion_CallsService()
    {
        var (controller, service) = Controller();
        service.CommandResult = BillingCommandResult.Success(Guid.NewGuid());

        var result = await controller.ConfirmPayment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Convert.ToBase64String(new byte[] { 1, 2, 3 }),
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(1, service.ConfirmCalls);
    }

    [Fact]
    public async Task RecordPayment_UnspecifiedTimestamp_BecomesUtc()
    {
        var (controller, service) = Controller();
        service.CommandResult = BillingCommandResult.Success(Guid.NewGuid());

        var unspecified = new DateTime(
            2026, 8, 23, 7, 0, 0, DateTimeKind.Unspecified);

        await controller.RecordPayment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "REF-1",
            null,
            100m,
            "PLN",
            100m,
            unspecified,
            CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, service.LastReceivedAtUtc.Kind);
        Assert.Equal(unspecified.Ticks, service.LastReceivedAtUtc.Ticks);
    }

    [Fact]
    public async Task RecordPayment_UtcTimestamp_StaysUtc()
    {
        var (controller, service) = Controller();
        service.CommandResult = BillingCommandResult.Success(Guid.NewGuid());

        var utc = new DateTime(
            2026, 8, 23, 7, 0, 0, DateTimeKind.Utc);

        await controller.RecordPayment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "REF-2",
            null,
            100m,
            "PLN",
            100m,
            utc,
            CancellationToken.None);

        Assert.Equal(utc, service.LastReceivedAtUtc);
        Assert.Equal(DateTimeKind.Utc, service.LastReceivedAtUtc.Kind);
    }

    [Fact]
    public async Task RecordPayment_LocalTimestamp_IsConvertedToUtc()
    {
        var (controller, service) = Controller();
        service.CommandResult = BillingCommandResult.Success(Guid.NewGuid());

        var local = new DateTime(
            2026, 8, 23, 7, 0, 0, DateTimeKind.Local);

        await controller.RecordPayment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "REF-3",
            null,
            100m,
            "PLN",
            100m,
            local,
            CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, service.LastReceivedAtUtc.Kind);
        Assert.Equal(local.ToUniversalTime(), service.LastReceivedAtUtc);
    }

    [Fact]
    public async Task ActivateByAgreement_UnspecifiedTimestamp_BecomesUtc()
    {
        var (controller, service) = Controller();
        service.CommandResult = BillingCommandResult.Success(Guid.NewGuid());

        var unspecified = new DateTime(
            2026, 8, 23, 7, 0, 0, DateTimeKind.Unspecified);

        await controller.ActivateByAgreement(
            Guid.NewGuid(),
            unspecified,
            "Commercial agreement",
            Convert.ToBase64String(new byte[] { 8, 9 }),
            CancellationToken.None);

        Assert.Equal(DateTimeKind.Utc, service.LastAgreementAtUtc.Kind);
        Assert.Equal(unspecified.Ticks, service.LastAgreementAtUtc.Ticks);
    }

    [Fact]
    public async Task ActivateByAgreement_LocalTimestamp_IsConvertedToUtc()
    {
        var (controller, service) = Controller();
        service.CommandResult = BillingCommandResult.Success(Guid.NewGuid());

        var local = new DateTime(
            2026, 8, 23, 7, 0, 0, DateTimeKind.Local);

        await controller.ActivateByAgreement(
            Guid.NewGuid(),
            local,
            "Commercial agreement",
            Convert.ToBase64String(new byte[] { 8, 9 }),
            CancellationToken.None);

        Assert.Equal(local.ToUniversalTime(), service.LastAgreementAtUtc);
    }

    [Fact]
    public async Task RejectPayment_ValidVersion_CallsService()
    {
        var (controller, service) = Controller();
        service.CommandResult = BillingCommandResult.Success(Guid.NewGuid());

        var result = await controller.RejectPayment(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Mismatch",
            Convert.ToBase64String(new byte[] { 4, 5, 6 }),
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(1, service.RejectCalls);
    }

    [Fact]
    public async Task Refund_ValidVersion_CallsService()
    {
        var (controller, service) = Controller();
        service.CommandResult = BillingCommandResult.Success(Guid.NewGuid());

        var result = await controller.Refund(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            10m,
            "PLN",
            "Credit",
            Convert.ToBase64String(new byte[] { 4, 5, 6 }),
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(1, service.RefundCalls);
    }

    [Fact]
    public async Task ApplyPaidRenewal_ValidVersion_CallsService()
    {
        var (controller, service) = Controller();
        service.CommandResult = BillingCommandResult.Success(Guid.NewGuid());

        var result = await controller.ApplyPaidRenewal(
            Guid.NewGuid(),
            Convert.ToBase64String(new byte[] { 4, 5, 6 }),
            CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(1, service.RenewalCalls);
    }

    private static (BillingController Controller, FakeBillingService Service)
        Controller(bool withActor = true)
    {
        var service = new FakeBillingService();
        var controller = new BillingController(
            service,
            new EchoLocalizer());

        var http = new DefaultHttpContext();

        if (withActor)
        {
            var identity = new ClaimsIdentity(
                new[]
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        Guid.NewGuid().ToString("D"))
                },
                "test");

            http.User = new ClaimsPrincipal(identity);
        }

        controller.ControllerContext =
            new ControllerContext { HttpContext = http };

        controller.TempData = new TempDataDictionary(
            http,
            new MemoryTempDataProvider());

        return (controller, service);
    }

    private sealed class MemoryTempDataProvider : ITempDataProvider
    {
        private IDictionary<string, object> _values =
            new Dictionary<string, object>();

        public IDictionary<string, object> LoadTempData(
            HttpContext context) =>
            new Dictionary<string, object>(_values);

        public void SaveTempData(
            HttpContext context,
            IDictionary<string, object> values)
        {
            _values = new Dictionary<string, object>(values);
        }
    }

    private sealed class EchoLocalizer : IStringLocalizer<BillingResource>
    {
        public LocalizedString this[string name] =>
            new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments] =>
            new(
                name,
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    name,
                    arguments),
                resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(
            bool includeParentCultures) =>
            Array.Empty<LocalizedString>();
    }

    private sealed class FakeBillingService : IBillingService
    {
        public BillingCommandResult CommandResult { get; set; } =
            BillingCommandResult.Success();

        public BillingQueryResult<IReadOnlyList<BillingSchoolDetails>>
            ListResult { get; set; } =
                BillingQueryResult<IReadOnlyList<BillingSchoolDetails>>
                    .Success(Array.Empty<BillingSchoolDetails>());

        public int UpsertCalls { get; private set; }
        public int ConfirmCalls { get; private set; }
        public int RejectCalls { get; private set; }
        public int RefundCalls { get; private set; }
        public int RenewalCalls { get; private set; }
        public DateTime LastReceivedAtUtc { get; private set; }
        public DateTime LastAgreementAtUtc { get; private set; }

        public Task<BillingQueryResult<IReadOnlyList<BillingSchoolDetails>>>
            ListAsync(
                Guid actorUserId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(ListResult);

        public Task<BillingCommandResult> UpsertProfileAsync(
            Guid actorUserId,
            UpsertBillingProfileRequest request,
            CancellationToken cancellationToken = default)
        {
            UpsertCalls++;
            return Task.FromResult(CommandResult);
        }

        public Task<BillingCommandResult> CreateInitialInvoiceAsync(
            Guid actorUserId,
            Guid schoolId,
            decimal taxAmount,
            decimal? settlementEquivalentAmount,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<BillingCommandResult> CreateNextInstallmentAsync(
            Guid actorUserId,
            Guid schoolId,
            decimal taxAmount,
            decimal? settlementEquivalentAmount,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<BillingCommandResult> CreateSeatProrationInvoiceAsync(
            Guid actorUserId,
            Guid schoolId,
            decimal taxAmount,
            decimal? settlementEquivalentAmount,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<BillingCommandResult> CreateRenewalInvoiceAsync(
            Guid actorUserId,
            Guid schoolId,
            decimal taxAmount,
            decimal? settlementEquivalentAmount,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(CommandResult);

        public Task<BillingCommandResult> RecordBankTransferAsync(
            Guid actorUserId,
            RecordBankTransferRequest request,
            CancellationToken cancellationToken = default)
        {
            LastReceivedAtUtc = request.ReceivedAtUtc;
            return Task.FromResult(CommandResult);
        }

        public Task<BillingCommandResult> ConfirmBankTransferAsync(
            Guid actorUserId,
            Guid schoolId,
            Guid paymentId,
            byte[] expectedPaymentRowVersion,
            CancellationToken cancellationToken = default)
        {
            ConfirmCalls++;
            return Task.FromResult(CommandResult);
        }

        public Task<BillingCommandResult> RejectBankTransferAsync(
            Guid actorUserId,
            Guid schoolId,
            Guid paymentId,
            string reason,
            byte[] expectedPaymentRowVersion,
            CancellationToken cancellationToken = default)
        {
            RejectCalls++;
            return Task.FromResult(CommandResult);
        }

        public Task<BillingCommandResult> RecordRefundAsync(
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
            RefundCalls++;
            return Task.FromResult(CommandResult);
        }

        public Task<BillingCommandResult> ActivateByAgreementAsync(
            Guid actorUserId,
            Guid schoolId,
            DateTime agreedActivationAtUtc,
            string reason,
            byte[] expectedSubscriptionRowVersion,
            CancellationToken cancellationToken = default)
        {
            LastAgreementAtUtc = agreedActivationAtUtc;
            return Task.FromResult(CommandResult);
        }

        public Task<BillingCommandResult> ApplyPaidRenewalAsync(
            Guid actorUserId,
            Guid schoolId,
            byte[] expectedSubscriptionRowVersion,
            CancellationToken cancellationToken = default)
        {
            RenewalCalls++;
            return Task.FromResult(CommandResult);
        }
    }
}
