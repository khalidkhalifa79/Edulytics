using System.Security.Claims;
using Edulytics.Services.Notifications;
using Microsoft.AspNetCore.WebUtilities;

namespace Edulytics.Web.Email;

public sealed class DurableUserInvitationDeliveryService
    : IUserInvitationDeliveryService
{
    private readonly IHttpContextAccessor _http;
    private readonly INotificationService _notifications;

    private readonly ILogger<
        DurableUserInvitationDeliveryService>
        _logger;

    public DurableUserInvitationDeliveryService(
        IHttpContextAccessor http,
        INotificationService notifications,
        ILogger<
            DurableUserInvitationDeliveryService> logger)
    {
        _http = http;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<UserInvitationDeliveryResult>
        SendAsync(
            UserInvitationDeliveryRequest request,
            CancellationToken cancellationToken = default)
    {
        var context =
            _http.HttpContext;

        if (context is null ||
            !Guid.TryParse(
                context.User.FindFirstValue(
                    ClaimTypes.NameIdentifier),
                out var actorUserId))
        {
            return UserInvitationDeliveryResult.Failed(
                UserInvitationDeliveryFailure
                    .InvalidRequest);
        }

        if (!Uri.TryCreate(
                request.SetupUrl,
                UriKind.Absolute,
                out var setupUri))
        {
            return UserInvitationDeliveryResult.Failed(
                UserInvitationDeliveryFailure
                    .InvalidRequest);
        }

        var query =
            QueryHelpers.ParseQuery(
                setupUri.Query);

        if (!query.TryGetValue(
                "userId",
                out var rawUserId) ||
            !Guid.TryParse(
                rawUserId.FirstOrDefault(),
                out var recipientUserId))
        {
            return UserInvitationDeliveryResult.Failed(
                UserInvitationDeliveryFailure
                    .InvalidRequest);
        }

        var baseUrl =
            setupUri.GetLeftPart(
                UriPartial.Authority);

        var reason =
            request.DeliveryReason == "initial"
                ? "initial"
                : $"resend:{DateTime.UtcNow:yyyyMMddHHmm}";

        var result =
            await _notifications
                .QueuePasswordSetupInvitationAsync(
                    actorUserId,
                    recipientUserId,
                    request.Culture,
                    baseUrl,
                    reason,
                    cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning(
                "Invitation durable queue request failed. Error: {Error}",
                result.Error);

            return UserInvitationDeliveryResult.Failed(
                UserInvitationDeliveryFailure
                    .QueueFailed);
        }

        return UserInvitationDeliveryResult.Success();
    }
}
