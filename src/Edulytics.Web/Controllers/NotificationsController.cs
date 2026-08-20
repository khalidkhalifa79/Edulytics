using System.Security.Claims;
using Edulytics.Services.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Web.Controllers;

[Authorize]
[Route("school/notifications")]
public sealed class NotificationsController
    : Controller
{
    private readonly INotificationService
        _notifications;

    public NotificationsController(
        INotificationService notifications)
    {
        _notifications = notifications;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        if (!TryActor(
                out var actorUserId))
        {
            return Forbid();
        }

        var result =
            await _notifications.ListInboxAsync(
                actorUserId,
                cancellationToken);

        if (result.Value is null)
        {
            return Forbid();
        }

        return View(result.Value);
    }

    [HttpPost("{id:guid}/read")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Read(
        Guid id,
        CancellationToken cancellationToken) =>
        SetReadState(
            id,
            true,
            cancellationToken);

    [HttpPost("{id:guid}/unread")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Unread(
        Guid id,
        CancellationToken cancellationToken) =>
        SetReadState(
            id,
            false,
            cancellationToken);

    private async Task<IActionResult>
        SetReadState(
            Guid id,
            bool isRead,
            CancellationToken cancellationToken)
    {
        if (!TryActor(
                out var actorUserId))
        {
            return Forbid();
        }

        var result =
            await _notifications.SetReadStateAsync(
                actorUserId,
                id,
                isRead,
                cancellationToken);

        if (result.Value is null)
        {
            return result.Error ==
                NotificationErrorCode.NotFound
                ? NotFound()
                : Forbid();
        }

        return RedirectToAction(
            nameof(Index));
    }

    private bool TryActor(
        out Guid actorUserId) =>
        Guid.TryParse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier),
            out actorUserId);
}
