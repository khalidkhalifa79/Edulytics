namespace Edulytics.Web.Email;

public sealed record UserInvitationDeliveryRequest(
    string RecipientEmail,
    string SchoolName,
    string Culture,
    string SetupUrl,
    string DeliveryReason = "initial");

public enum UserInvitationDeliveryFailure
{
    None,
    Disabled,
    InvalidConfiguration,
    DeliveryFailed,
    TimedOut,
    CircuitOpen,
    QueueFailed,
    InvalidRequest
}

public sealed record UserInvitationDeliveryResult(
    bool Succeeded,
    UserInvitationDeliveryFailure Failure)
{
    public static UserInvitationDeliveryResult Success() =>
        new(
            true,
            UserInvitationDeliveryFailure.None);

    public static UserInvitationDeliveryResult Failed(
        UserInvitationDeliveryFailure failure) =>
        new(
            false,
            failure);
}

// Durable request-facing service.
public interface IUserInvitationDeliveryService
{
    Task<UserInvitationDeliveryResult> SendAsync(
        UserInvitationDeliveryRequest request,
        CancellationToken cancellationToken = default);
}

// External connector. Only background delivery should use it.
public interface IUserInvitationConnector
{
    Task<UserInvitationDeliveryResult> SendAsync(
        UserInvitationDeliveryRequest request,
        CancellationToken cancellationToken = default);
}
