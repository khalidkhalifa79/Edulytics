using Edulytics.Core.Enums;
using Edulytics.Services.Onboarding;
using Edulytics.Web.Email;
using Edulytics.Web.Resilience;
using Edulytics.Web.ViewModels.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "PlatformAdministration")]
[Route("Platform/Onboarding")]
public sealed class CustomerOnboardingController : Controller
{
    private readonly ICustomerOnboardingService _onboarding;
    private readonly IUserInvitationDeliveryService _invitations;
    private readonly IStringLocalizer<OnboardingResource> _text;

    public CustomerOnboardingController(
        ICustomerOnboardingService onboarding,
        IUserInvitationDeliveryService invitations,
        IStringLocalizer<OnboardingResource> text)
    {
        _onboarding = onboarding;
        _invitations = invitations;
        _text = text;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken) =>
        View(new DemoLeadListViewModel(
            await _onboarding.ListAsync(cancellationToken)));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var lead = await _onboarding.GetAsync(id, cancellationToken);
        return lead is null
            ? NotFound()
            : View(new DemoLeadDetailsViewModel { Lead = lead });
    }

    [HttpPost("{id:guid}/status")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("OperationalMutation")]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        DemoRequestStatus targetStatus,
        DateTime? demoScheduledAtUtc,
        string? internalNote,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(rowVersion, out var expected))
        {
            SetError(OnboardingErrorCode.ConcurrencyConflict);
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _onboarding.UpdateLeadAsync(
            id,
            targetStatus,
            demoScheduledAtUtc,
            internalNote,
            expected,
            cancellationToken);

        SetResult(result);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/grant-demo")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("OperationalMutation")]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    public async Task<IActionResult> GrantDemo(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(rowVersion, out var expected))
        {
            SetError(OnboardingErrorCode.ConcurrencyConflict);
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _onboarding.GrantDemoAsync(id, expected, cancellationToken);
        await SetResultAndDeliverInvitationAsync(result, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/extend-demo")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("OperationalMutation")]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    public async Task<IActionResult> ExtendDemo(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        var result = TryRowVersion(rowVersion, out var expected)
            ? await _onboarding.ExtendDemoAsync(id, expected, cancellationToken)
            : OnboardingCommandResult.Failure(
                string.Empty,
                OnboardingErrorCode.ConcurrencyConflict);
        SetResult(result);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/expire-demo")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("OperationalMutation")]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    public async Task<IActionResult> ExpireDemo(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        var result = TryRowVersion(rowVersion, out var expected)
            ? await _onboarding.ExpireDemoAsync(id, expected, cancellationToken)
            : OnboardingCommandResult.Failure(
                string.Empty,
                OnboardingErrorCode.ConcurrencyConflict);
        SetResult(result);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/revoke-demo")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("OperationalMutation")]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    public async Task<IActionResult> RevokeDemo(
        Guid id,
        string reason,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        var result = TryRowVersion(rowVersion, out var expected)
            ? await _onboarding.RevokeDemoAsync(id, reason, expected, cancellationToken)
            : OnboardingCommandResult.Failure(
                string.Empty,
                OnboardingErrorCode.ConcurrencyConflict);
        SetResult(result);
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost("{id:guid}/provision")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("OperationalMutation")]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    public async Task<IActionResult> Provision(
        Guid id,
        string schoolCode,
        string defaultCulture,
        string timeZoneId,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryRowVersion(rowVersion, out var expected))
        {
            SetError(OnboardingErrorCode.ConcurrencyConflict);
            return RedirectToAction(nameof(Details), new { id });
        }

        var result = await _onboarding.ProvisionCustomerAsync(
            id,
            schoolCode,
            defaultCulture,
            timeZoneId,
            expected,
            cancellationToken);

        // A suspended production school may not yet pass the Phase21
        // notification eligibility gate. That is intentional until Phase25D
        // activates the customer after first-payment confirmation.
        await SetResultAndDeliverInvitationAsync(result, cancellationToken);
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task SetResultAndDeliverInvitationAsync(
        OnboardingCommandResult result,
        CancellationToken cancellationToken)
    {
        SetResult(result);
        if (!result.Succeeded || result.Invitation is null)
            return;

        var link = Url.Action(
            "SetPassword",
            "Account",
            new
            {
                userId = result.Invitation.UserId,
                token = result.Invitation.Token,
                culture = result.Invitation.Culture
            },
            Request.Scheme);

        if (string.IsNullOrWhiteSpace(link))
        {
            TempData["OnboardingWarning"] = _text["InvitationFailed"].Value;
            return;
        }

        var delivery = await _invitations.SendAsync(
            new UserInvitationDeliveryRequest(
                result.Invitation.RecipientEmail,
                result.Invitation.SchoolName,
                result.Invitation.Culture,
                link,
                "initial"),
            cancellationToken);

        TempData["OnboardingWarning"] = delivery.Succeeded
            ? _text["InvitationSent"].Value
            : _text["InvitationFailed"].Value;
    }

    private void SetResult(OnboardingCommandResult result)
    {
        if (result.Succeeded)
        {
            TempData["OnboardingSuccess"] = _text["ActionSucceeded"].Value;
            return;
        }
        SetError(result.Errors.First().Code);
    }

    private void SetError(OnboardingErrorCode code) =>
        TempData["OnboardingError"] = _text[$"Error_{code}"].Value;

    private static bool TryRowVersion(string? value, out byte[] bytes)
    {
        bytes = [];
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
}
