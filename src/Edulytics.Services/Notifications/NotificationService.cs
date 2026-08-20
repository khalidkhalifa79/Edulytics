using System.Text.Json;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Notifications;
using Edulytics.Services.Auditing;

namespace Edulytics.Services.Notifications;

public sealed class NotificationService
    : INotificationService
{
    private const int InboxLimit = 100;

    private readonly ISchoolUserRepository _users;
    private readonly ISchoolRepository _schools;
    private readonly INotificationRepository _notifications;
    private readonly IAuditService? _audit;

    private readonly
        IAuditRequestMetadataProvider?
        _metadata;

    public NotificationService(
        ISchoolUserRepository users,
        ISchoolRepository schools,
        INotificationRepository notifications,
        IAuditService? audit = null,
        IAuditRequestMetadataProvider? metadata = null)
    {
        _users = users;
        _schools = schools;
        _notifications = notifications;
        _audit = audit;
        _metadata = metadata;
    }

    public async Task<NotificationQueueResult>
        QueuePasswordSetupInvitationAsync(
            Guid actorUserId,
            Guid recipientUserId,
            string culture,
            string baseUrl,
            string deliveryReason,
            CancellationToken cancellationToken = default)
    {
        if (actorUserId == Guid.Empty ||
            recipientUserId == Guid.Empty)
        {
            return NotificationQueueResult.Failure(
                NotificationErrorCode.InvalidRequest);
        }

        var actor =
            await _users.GetActorAsync(
                actorUserId,
                cancellationToken);

        var recipient =
            await _users.GetActorAsync(
                recipientUserId,
                cancellationToken);

        if (actor is null ||
            recipient is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            !recipient.SchoolId.HasValue)
        {
            return NotificationQueueResult.Failure(
                NotificationErrorCode.AccessDenied);
        }

        var actorRole =
            SingleRole(actor.Roles);

        var canManage =
            actor.SchoolId is null
                ? actorRole ==
                    RoleNames.SuperAdmin
                : actor.SchoolId ==
                    recipient.SchoolId &&
                  actorRole ==
                    RoleNames.SchoolAdmin;

        if (!canManage)
        {
            return NotificationQueueResult.Failure(
                NotificationErrorCode.AccessDenied);
        }

        var school =
            await _schools.GetByIdAsync(
                recipient.SchoolId.Value,
                cancellationToken);

        if (school is null ||
            school.Status != SchoolStatus.Active)
        {
            return NotificationQueueResult.Failure(
                NotificationErrorCode.SchoolNotActive);
        }

        culture =
            string.Equals(
                culture,
                "pl",
                StringComparison.OrdinalIgnoreCase)
                ? "pl"
                : "en";

        if (!TryNormalizeBaseUrl(
                baseUrl,
                out var normalizedBaseUrl))
        {
            return NotificationQueueResult.Failure(
                NotificationErrorCode.InvalidRequest);
        }

        deliveryReason =
            deliveryReason?.Trim()
            ?? string.Empty;

        if (!IsValidDeliveryReason(
                deliveryReason))
        {
            return NotificationQueueResult.Failure(
                NotificationErrorCode.InvalidRequest);
        }

        var now =
            DateTime.UtcNow;

        var inboxDedup =
            $"account-invitation:{recipientUserId:N}";

        var notification =
            await _notifications
                .GetByDeduplicationKeyAsync(
                    school.Id,
                    recipientUserId,
                    inboxDedup,
                    cancellationToken);

        var notificationIsNew =
            notification is null;

        notification ??=
            new UserNotification
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                RecipientUserId =
                    recipientUserId,
                Kind =
                    NotificationKind
                        .AccountInvitation,
                TitleKey =
                    "NotificationAccountInvitationTitle",
                MessageKey =
                    "NotificationAccountInvitationMessage",
                DeduplicationKey =
                    inboxDedup,
                RelatedEntityType =
                    "ApplicationUser",
                RelatedEntityId =
                    recipientUserId,
                CreatedAtUtc =
                    now
            };

        var deliveryDedup =
            $"password-setup:{recipientUserId:N}:{deliveryReason}";

        if (await _notifications
            .DeliveryExistsAsync(
                school.Id,
                deliveryDedup,
                cancellationToken))
        {
            return NotificationQueueResult.Success(
                notification.Id,
                Guid.Empty,
                deduplicated: true);
        }

        if (notificationIsNew)
        {
            await _notifications
                .AddNotificationAsync(
                    notification,
                    cancellationToken);
        }

        var job =
            new NotificationDeliveryJob
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                NotificationId =
                    notification.Id,
                RecipientUserId =
                    recipientUserId,
                Channel =
                    NotificationDeliveryChannel.Email,
                Status =
                    NotificationDeliveryStatus.Pending,
                Culture = culture,
                BaseUrl =
                    normalizedBaseUrl,
                DeduplicationKey =
                    deliveryDedup,
                CreatedAtUtc =
                    now
            };

        var correlation =
            _metadata
                ?.GetCurrent()
                .CorrelationId;

        if (string.IsNullOrWhiteSpace(
                correlation))
        {
            correlation =
                Guid.NewGuid()
                    .ToString("N");
        }

        var outbox =
            new OutboxMessage
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                EventType =
                    NotificationEventTypes
                        .DeliveryRequested,
                PayloadJson =
                    JsonSerializer.Serialize(
                        new NotificationDeliveryRequestedEvent(
                            school.Id,
                            job.Id)),
                OccurredAtUtc = now,
                AvailableAtUtc = now,
                Status =
                    OutboxMessageStatus.Pending,
                CorrelationId =
                    correlation
            };

        await _notifications
            .AddDeliveryJobAsync(
                job,
                cancellationToken);

        await _notifications
            .AddOutboxAsync(
                outbox,
                cancellationToken);

        if (_audit is not null)
        {
            await _audit.QueueAsync(
                new AuditEvent(
                    SchoolId: school.Id,
                    Action:
                        "Notification.InvitationQueued",
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
                            ["recipientUserId"] =
                                recipientUserId,
                            ["notificationId"] =
                                notification.Id,
                            ["deliveryReason"] =
                                deliveryReason,
                            ["channel"] =
                                "Email"
                        },
                    ResultSummary:
                        "Password setup invitation queued for durable delivery.",
                    ActorUserIdOverride:
                        actorUserId,
                    ActorRoleOverride:
                        actorRole
                        ?? string.Empty),
                cancellationToken);
        }

        if (!await _notifications
            .SaveAsync(
                cancellationToken))
        {
            return NotificationQueueResult.Failure(
                NotificationErrorCode.PersistenceError);
        }

        return NotificationQueueResult.Success(
            notification.Id,
            job.Id,
            deduplicated: false);
    }

    public async Task<
        NotificationQueryResult<
            IReadOnlyList<
                NotificationInboxItem>>>
        ListInboxAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default)
    {
        var actor =
            await ResolveInboxActorAsync(
                actorUserId,
                cancellationToken);

        if (actor is null)
        {
            return NotificationQueryResult<
                IReadOnlyList<
                    NotificationInboxItem>>
                .Failure(
                    NotificationErrorCode
                        .AccessDenied);
        }

        var rows =
            await _notifications.ListInboxAsync(
                actor.Value.SchoolId,
                actorUserId,
                InboxLimit,
                cancellationToken);

        return NotificationQueryResult<
            IReadOnlyList<
                NotificationInboxItem>>
            .Success(
                rows
                    .Select(
                        x =>
                            new NotificationInboxItem(
                                x.Id,
                                x.Kind,
                                x.TitleKey,
                                x.MessageKey,
                                x.CreatedAtUtc,
                                x.ReadAtUtc,
                                x.EmailDeliveryStatus))
                    .ToArray());
    }

    public async Task<
        NotificationQueryResult<
            NotificationInboxItem>>
        SetReadStateAsync(
            Guid actorUserId,
            Guid notificationId,
            bool isRead,
            CancellationToken cancellationToken = default)
    {
        var actor =
            await ResolveInboxActorAsync(
                actorUserId,
                cancellationToken);

        if (actor is null)
        {
            return NotificationQueryResult<
                NotificationInboxItem>
                .Failure(
                    NotificationErrorCode
                        .AccessDenied);
        }

        var notification =
            await _notifications
                .GetNotificationForUpdateAsync(
                    actor.Value.SchoolId,
                    actorUserId,
                    notificationId,
                    cancellationToken);

        if (notification is null)
        {
            return NotificationQueryResult<
                NotificationInboxItem>
                .Failure(
                    NotificationErrorCode.NotFound);
        }

        notification.ReadAtUtc =
            isRead
                ? DateTime.UtcNow
                : null;

        if (_audit is not null)
        {
            await _audit.QueueAsync(
                new AuditEvent(
                    SchoolId:
                        actor.Value.SchoolId,
                    Action:
                        isRead
                            ? "Notification.Read"
                            : "Notification.Unread",
                    EntityType:
                        "UserNotification",
                    EntityId:
                        notification.Id
                            .ToString("D"),
                    Feature:
                        "Notifications",
                    NewValues:
                        new Dictionary<
                            string,
                            object?>
                        {
                            ["isRead"] = isRead
                        },
                    ResultSummary:
                        isRead
                            ? "Notification marked read."
                            : "Notification marked unread.",
                    ActorUserIdOverride:
                        actorUserId,
                    ActorRoleOverride:
                        actor.Value.Role),
                cancellationToken);
        }

        if (!await _notifications
            .SaveAsync(
                cancellationToken))
        {
            return NotificationQueryResult<
                NotificationInboxItem>
                .Failure(
                    NotificationErrorCode
                        .PersistenceError);
        }

        var rows =
            await _notifications.ListInboxAsync(
                actor.Value.SchoolId,
                actorUserId,
                InboxLimit,
                cancellationToken);

        var row =
            rows.FirstOrDefault(
                x =>
                    x.Id ==
                    notification.Id);

        if (row is null)
        {
            return NotificationQueryResult<
                NotificationInboxItem>
                .Failure(
                    NotificationErrorCode.NotFound);
        }

        return NotificationQueryResult<
            NotificationInboxItem>
            .Success(
                new NotificationInboxItem(
                    row.Id,
                    row.Kind,
                    row.TitleKey,
                    row.MessageKey,
                    row.CreatedAtUtc,
                    row.ReadAtUtc,
                    row.EmailDeliveryStatus));
    }

    private async Task<
        (Guid SchoolId, string Role)?>
        ResolveInboxActorAsync(
            Guid actorUserId,
            CancellationToken cancellationToken)
    {
        var actor =
            await _users.GetActorAsync(
                actorUserId,
                cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            !actor.SchoolId.HasValue)
        {
            return null;
        }

        var role =
            SingleRole(actor.Roles);

        if (role is null)
        {
            return null;
        }

        var school =
            await _schools.GetByIdAsync(
                actor.SchoolId.Value,
                cancellationToken);

        if (school is null ||
            school.Status != SchoolStatus.Active)
        {
            return null;
        }

        return (
            school.Id,
            role
        );
    }

    private static string?
        SingleRole(
            IReadOnlyList<string> roles) =>
        roles.Count == 1
            ? roles[0]
            : null;

    private static bool TryNormalizeBaseUrl(
        string baseUrl,
        out string normalized)
    {
        normalized = string.Empty;

        if (!Uri.TryCreate(
                baseUrl,
                UriKind.Absolute,
                out var uri))
        {
            return false;
        }

        if (uri.Scheme !=
                Uri.UriSchemeHttp &&
            uri.Scheme !=
                Uri.UriSchemeHttps)
        {
            return false;
        }

        normalized =
            uri.GetLeftPart(
                UriPartial.Authority);

        return normalized.Length <= 500;
    }

    private static bool IsValidDeliveryReason(
        string value) =>
        value == "initial" ||
        (
            value.StartsWith(
                "resend:",
                StringComparison.Ordinal) &&
            value.Length <= 40
        );
}
