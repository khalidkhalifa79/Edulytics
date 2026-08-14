namespace Edulytics.Web.Email;

public sealed record UserInvitationDeliveryRequest(
    string RecipientEmail,
    string SchoolName,
    string Culture,
    string SetupUrl);

public enum UserInvitationDeliveryFailure
{
    None,
    Disabled,
    InvalidConfiguration,
    DeliveryFailed
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

public interface IUserInvitationDeliveryService
{
    Task<UserInvitationDeliveryResult> SendAsync(
        UserInvitationDeliveryRequest request,
        CancellationToken cancellationToken = default);
}
