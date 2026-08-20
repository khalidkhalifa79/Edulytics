using System.Security.Claims;
using Edulytics.Core.Interfaces;
using Edulytics.Web.Operations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "PlatformAdministration")]
public sealed class OperationsController
    : Controller
{
    private readonly OperationalConsoleService
        _operations;

    private readonly IOutboxRepository
        _outbox;

    private readonly
        IStringLocalizer<OperationsResource>
        _text;

    public OperationsController(
        OperationalConsoleService operations,
        IOutboxRepository outbox,
        IStringLocalizer<OperationsResource> text)
    {
        _operations = operations;
        _outbox = outbox;
        _text = text;
    }

    [HttpGet("/platform/operations")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var model =
            await _operations.GetAsync(
                cancellationToken);

        return View(model);
    }

    [HttpPost(
        "/platform/operations/outbox/{id:guid}/requeue")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("OperationalMutation")]
    public async Task<IActionResult> Requeue(
        Guid id,
        string reason,
        CancellationToken cancellationToken)
    {
        var actorValue =
            User.FindFirstValue(
                ClaimTypes.NameIdentifier);

        if (!Guid.TryParse(
                actorValue,
                out var actorUserId))
        {
            return Forbid();
        }

        reason =
            reason?.Trim()
            ?? string.Empty;

        if (reason.Length == 0 ||
            reason.Length > 500)
        {
            TempData["OperationsMessage"] =
                _text[
                    "RequeueReasonValidation"
                ].Value;

            return RedirectToAction(
                nameof(Index));
        }

        var requeued =
            await _outbox
                .RequeueDeadLetterAsync(
                    id,
                    actorUserId,
                    reason,
                    DateTime.UtcNow,
                    cancellationToken);

        TempData["OperationsMessage"] =
            _text[
                requeued
                    ? "RequeueSucceeded"
                    : "RequeueRejected"
            ].Value;

        return RedirectToAction(
            nameof(Index));
    }
}
