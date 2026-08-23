using System.Security.Claims;
using Edulytics.Core.Enums;
using Edulytics.Services.Schools;
using Edulytics.Services.Subscriptions;
using Edulytics.Web.ViewModels.Subscriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "PlatformAdministration")]
[Route("Platform/Subscriptions")]
public sealed class SubscriptionsController : Controller
{
    private readonly ISchoolSubscriptionService _subscriptions;
    private readonly ISchoolManagementService _schools;
    private readonly IStringLocalizer<SubscriptionResource> _text;

    public SubscriptionsController(
        ISchoolSubscriptionService subscriptions,
        ISchoolManagementService schools,
        IStringLocalizer<SubscriptionResource> text)
    {
        _subscriptions = subscriptions;
        _schools = schools;
        _text = text;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var subscriptions =
            await _subscriptions.ListAsync(
                actorId,
                cancellationToken);

        if (subscriptions.Value is null)
            return Forbid();

        var schools =
            await _schools.ListAsync(
                cancellationToken);

        var byId = schools.ToDictionary(x => x.Id);
        var subscribed =
            subscriptions.Value
                .Select(x => x.SchoolId)
                .ToHashSet();

        var model = new SubscriptionIndexViewModel
        {
            Subscriptions =
                subscriptions.Value
                    .Select(x =>
                    {
                        byId.TryGetValue(x.SchoolId, out var school);

                        return new SubscriptionRowViewModel(
                            x,
                            school?.Name ?? x.SchoolId.ToString("D"),
                            school?.SchoolCode ?? string.Empty,
                            school?.CountryCode ?? string.Empty);
                    })
                    .ToArray(),

            EligibleSchools =
                schools
                    .Where(x =>
                        x.Status == SchoolStatus.Suspended &&
                        !subscribed.Contains(x.Id) &&
                        (x.CountryCode == "PL" ||
                         x.CountryCode == "AE"))
                    .OrderBy(x => x.Name)
                    .Select(x =>
                        new SubscriptionSchoolOptionViewModel(
                            x.Id,
                            x.Name,
                            x.SchoolCode,
                            x.CountryCode))
                    .ToArray()
        };

        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        Guid schoolId,
        SubscriptionTerm term,
        SubscriptionBillingCadence billingCadence,
        int committedSeats,
        bool autoRenew,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result =
            await _subscriptions.CreateAsync(
                actorId,
                new CreateSubscriptionRequest(
                    schoolId,
                    term,
                    billingCadence,
                    committedSeats,
                    autoRenew),
                cancellationToken);

        return RedirectResult(result, "SuccessCreated");
    }

    [HttpPost("{schoolId:guid}/Activate")]
    [ValidateAntiForgeryToken]
    public IActionResult Activate(
        Guid schoolId,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        TempData["SubscriptionError"] = _text["Phase25DBillingRequired"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{schoolId:guid}/IncreaseSeats")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> IncreaseSeats(
        Guid schoolId,
        int committedSeats,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            rowVersion,
            (actor, version) =>
                _subscriptions.IncreaseSeatsAsync(
                    actor,
                    schoolId,
                    committedSeats,
                    version,
                    cancellationToken),
            "SuccessSeatsIncreased");

    [HttpPost("{schoolId:guid}/ScheduleReduction")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ScheduleReduction(
        Guid schoolId,
        int renewalSeats,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            rowVersion,
            (actor, version) =>
                _subscriptions
                    .ScheduleRenewalSeatReductionAsync(
                        actor,
                        schoolId,
                        renewalSeats,
                        version,
                        cancellationToken),
            "SuccessReductionScheduled");

    [HttpPost("{schoolId:guid}/AutoRenew")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AutoRenew(
        Guid schoolId,
        bool autoRenew,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            rowVersion,
            (actor, version) =>
                _subscriptions.SetAutoRenewAsync(
                    actor,
                    schoolId,
                    autoRenew,
                    version,
                    cancellationToken),
            "SuccessAutoRenewChanged");

    [HttpPost("{schoolId:guid}/Renew")]
    [ValidateAntiForgeryToken]
    public IActionResult Renew(
        Guid schoolId,
        string rowVersion,
        CancellationToken cancellationToken)
    {
        TempData["SubscriptionError"] = _text["Phase25DBillingRequired"].Value;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{schoolId:guid}/Suspend")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Suspend(
        Guid schoolId,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            rowVersion,
            (actor, version) =>
                _subscriptions.SuspendAsync(
                    actor,
                    schoolId,
                    version,
                    cancellationToken),
            "SuccessSuspended");

    [HttpPost("{schoolId:guid}/Reactivate")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Reactivate(
        Guid schoolId,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            rowVersion,
            (actor, version) =>
                _subscriptions.ReactivateAsync(
                    actor,
                    schoolId,
                    version,
                    cancellationToken),
            "SuccessReactivated");

    [HttpPost("{schoolId:guid}/EndExpired")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> EndExpired(
        Guid schoolId,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ExecuteAsync(
            rowVersion,
            (actor, version) =>
                _subscriptions.EndExpiredAsync(
                    actor,
                    schoolId,
                    version,
                    cancellationToken),
            "SuccessEnded");

    private async Task<IActionResult> ExecuteAsync(
        string rowVersion,
        Func<Guid, byte[], Task<SubscriptionCommandResult>> command,
        string successKey)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        if (!TryRowVersion(rowVersion, out var version))
        {
            TempData["SubscriptionError"] =
                _text["ConcurrencyConflict"].Value;

            return RedirectToAction(nameof(Index));
        }

        var result = await command(actorId, version);
        return RedirectResult(result, successKey);
    }

    private IActionResult RedirectResult(
        SubscriptionCommandResult result,
        string successKey)
    {
        if (result.Succeeded)
        {
            TempData["SubscriptionSuccess"] =
                _text[successKey].Value;
        }
        else
        {
            TempData["SubscriptionError"] =
                _text[result.Error.ToString()].Value;
        }

        return RedirectToAction(nameof(Index));
    }

    private bool TryActor(out Guid actorId) =>
        Guid.TryParse(
            User.FindFirstValue(ClaimTypes.NameIdentifier),
            out actorId);

    private static bool TryRowVersion(
        string? value,
        out byte[] version)
    {
        version = [];

        if (string.IsNullOrWhiteSpace(value))
            return false;

        try
        {
            version = Convert.FromBase64String(value);
            return version.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
