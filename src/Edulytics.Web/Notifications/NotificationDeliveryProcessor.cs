using Edulytics.Core.Interfaces;
using Edulytics.Core.Notifications;
using Edulytics.Services.Auditing;
using Edulytics.Web.Email;

namespace Edulytics.Web.Notifications;

public interface INotificationDeliveryProcessor
{
    Task ProcessAsync(
        Guid schoolId,
        Guid deliveryJobId,
        CancellationToken cancellationToken = default);

    Task MarkDeadLetteredAsync(
        Guid schoolId,
        Guid deliveryJobId,
        CancellationToken cancellationToken = default);
}

public sealed class NotificationDeliveryProcessor
    : INotificationDeliveryProcessor
{
    private readonly INotificationRepository
        _notifications;

    private readonly ISchoolUserRepository
        _users;

    private readonly ISchoolRepository
        _schools;

    private readonly IUserInvitationConnector
        _connector;

    private readonly IAuditService _audit;

    public NotificationDeliveryProcessor(
        INotificationRepository notifications,
        ISchoolUserRepository users,
        ISchoolRepository schools,
        IUserInvitationConnector connector,
        IAuditService audit)
    {
        _notifications = notifications;
        _users = users;
        _schools = schools;
        _connector = connector;
        _audit = audit;
    }

    public async Task ProcessAsync(
        Guid schoolId,
        Guid deliveryJobId,
        CancellationToken cancellationToken = default)
    {
        var job =
            await _notifications
                .GetDeliveryForUpdateAsync(
                    schoolId,
                    deliveryJobId,
                    cancellationToken);

        if (job is null)
        {
            throw new InvalidOperationException(
                "Notification delivery job does not exist.");
        }

        if (job.Status is
            NotificationDeliveryStatus.Sent or
            NotificationDeliveryStatus.Failed)
        {
            return;
        }

        var user =
            await _users.GetBySchoolAndIdAsync(
                schoolId,
                job.RecipientUserId,
                cancellationToken);

        var school =
            await _schools.GetByIdAsync(
                schoolId,
                cancellationToken);

        if (user is null ||
            school is null)
        {
            throw new InvalidOperationException(
                "Notification delivery target is unavailable.");
        }

        job.AttemptCount++;
        job.LastAttemptAtUtc =
            DateTime.UtcNow;
        job.LastErrorCode = null;

        if (!await _notifications
            .SaveAsync(
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Notification delivery attempt could not be persisted.");
        }

        // Generate a fresh token only at delivery time.
        // The token is never written to NotificationDeliveryJobs,
        // UserNotifications, AuditLogs or Outbox payloads.
        var setup =
            await _users.GeneratePasswordSetupAsync(
                schoolId,
                user.Id,
                cancellationToken);

        if (!setup.Succeeded ||
            string.IsNullOrWhiteSpace(
                setup.PasswordSetupToken))
        {
            await RecordAttemptFailureAsync(
                job,
                "TokenGenerationFailed",
                cancellationToken);

            throw new InvalidOperationException(
                "Invitation delivery token generation failed.");
        }

        var setupUrl =
            BuildSetupUrl(
                job.BaseUrl,
                user.Id,
                setup.PasswordSetupToken,
                job.Culture);

        var delivery =
            await _connector.SendAsync(
                new UserInvitationDeliveryRequest(
                    user.Email,
                    school.Name,
                    job.Culture,
                    setupUrl),
                cancellationToken);

        if (!delivery.Succeeded)
        {
            await RecordAttemptFailureAsync(
                job,
                delivery.Failure.ToString(),
                cancellationToken);

            // Safe error only. Never include setupUrl/token.
            throw new InvalidOperationException(
                $"Invitation connector failed: {delivery.Failure}.");
        }

        job.Status =
            NotificationDeliveryStatus.Sent;

        job.SentAtUtc =
            DateTime.UtcNow;

        job.LastErrorCode = null;

        await _audit.QueueAsync(
            new AuditEvent(
                SchoolId: job.SchoolId,
                Action:
                    "Notification.EmailSent",
                EntityType:
                    "NotificationDeliveryJob",
                EntityId:
                    job.Id.ToString("D"),
                Feature:
                    "Notifications",
                NewValues:
                    new Dictionary<
                        string,
                        object?>
                    {
                        ["notificationId"] =
                            job.NotificationId,
                        ["recipientUserId"] =
                            job.RecipientUserId,
                        ["attemptCount"] =
                            job.AttemptCount,
                        ["channel"] =
                            "Email"
                    },
                ResultSummary:
                    "Notification email delivered."),
            cancellationToken);

        if (!await _notifications
            .SaveAsync(
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Notification delivery completion could not be persisted.");
        }
    }

    public async Task MarkDeadLetteredAsync(
        Guid schoolId,
        Guid deliveryJobId,
        CancellationToken cancellationToken = default)
    {
        var job =
            await _notifications
                .GetDeliveryForUpdateAsync(
                    schoolId,
                    deliveryJobId,
                    cancellationToken);

        if (job is null ||
            job.Status !=
                NotificationDeliveryStatus.Pending)
        {
            return;
        }

        job.Status =
            NotificationDeliveryStatus.Failed;

        job.LastErrorCode =
            "OutboxDeadLettered";

        await _audit.QueueAsync(
            new AuditEvent(
                SchoolId: job.SchoolId,
                Action:
                    "Notification.EmailFailed",
                EntityType:
                    "NotificationDeliveryJob",
                EntityId:
                    job.Id.ToString("D"),
                Feature:
                    "Notifications",
                NewValues:
                    new Dictionary<
                        string,
                        object?>
                    {
                        ["notificationId"] =
                            job.NotificationId,
                        ["recipientUserId"] =
                            job.RecipientUserId,
                        ["attemptCount"] =
                            job.AttemptCount,
                        ["failureCode"] =
                            job.LastErrorCode
                    },
                ResultSummary:
                    "Notification email delivery dead-lettered."),
            cancellationToken);

        if (!await _notifications
            .SaveAsync(
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Notification dead-letter status could not be persisted.");
        }
    }

    private async Task
        RecordAttemptFailureAsync(
            Edulytics.Core.Entities.NotificationDeliveryJob job,
            string code,
            CancellationToken cancellationToken)
    {
        job.LastErrorCode =
            code.Length <= 120
                ? code
                : code[..120];

        if (!await _notifications
            .SaveAsync(
                cancellationToken))
        {
            throw new InvalidOperationException(
                "Notification delivery failure state could not be persisted.");
        }
    }

    private static string BuildSetupUrl(
        string baseUrl,
        Guid userId,
        string token,
        string culture)
    {
        culture =
            string.Equals(
                culture,
                "pl",
                StringComparison.OrdinalIgnoreCase)
                ? "pl"
                : "en";

        var relative =
            "/account/set-password"
            + $"?userId={Uri.EscapeDataString(userId.ToString("D"))}"
            + $"&token={Uri.EscapeDataString(token)}"
            + $"&culture={Uri.EscapeDataString(culture)}";

        return new Uri(
            new Uri(
                baseUrl.TrimEnd('/') + "/"),
            relative.TrimStart('/'))
            .ToString();
    }
}
