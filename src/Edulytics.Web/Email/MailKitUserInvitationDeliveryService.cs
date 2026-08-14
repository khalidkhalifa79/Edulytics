using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Edulytics.Web.Email;

public sealed class MailKitUserInvitationDeliveryService
    : IUserInvitationDeliveryService
{
    private readonly SmtpEmailOptions _options;
    private readonly IInvitationEmailTemplateRenderer
        _templates;
    private readonly ILogger<
        MailKitUserInvitationDeliveryService> _logger;

    public MailKitUserInvitationDeliveryService(
        IOptions<SmtpEmailOptions> options,
        IInvitationEmailTemplateRenderer templates,
        ILogger<
            MailKitUserInvitationDeliveryService> logger)
    {
        _options = options.Value;
        _templates = templates;
        _logger = logger;
    }

    public async Task<UserInvitationDeliveryResult>
        SendAsync(
            UserInvitationDeliveryRequest request,
            CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            return UserInvitationDeliveryResult.Failed(
                UserInvitationDeliveryFailure.Disabled);
        }

        if (!IsConfigurationValid())
        {
            return UserInvitationDeliveryResult.Failed(
                UserInvitationDeliveryFailure
                    .InvalidConfiguration);
        }

        try
        {
            var content =
                _templates.Render(
                    request.Culture,
                    request.SchoolName,
                    request.SetupUrl);

            var message =
                new MimeMessage();

            message.From.Add(
                new MailboxAddress(
                    _options.FromName,
                    _options.FromAddress));

            message.To.Add(
                MailboxAddress.Parse(
                    request.RecipientEmail));

            message.Subject =
                content.Subject;

            message.Body =
                new BodyBuilder
                {
                    TextBody =
                        content.TextBody,

                    HtmlBody =
                        content.HtmlBody
                }
                .ToMessageBody();

            using var client =
                new SmtpClient();

            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                ResolveSecurity(),
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(
                    _options.Username))
            {
                await client.AuthenticateAsync(
                    _options.Username,
                    _options.Password,
                    cancellationToken);
            }

            await client.SendAsync(
                message,
                cancellationToken);

            await client.DisconnectAsync(
                true,
                cancellationToken);

            return
                UserInvitationDeliveryResult
                    .Success();
        }
        catch (OperationCanceledException)
            when (cancellationToken
                .IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                "Invitation email delivery failed. Error type: {ErrorType}",
                exception.GetType().Name);

            return UserInvitationDeliveryResult.Failed(
                UserInvitationDeliveryFailure
                    .DeliveryFailed);
        }
    }

    private bool IsConfigurationValid()
    {
        if (string.IsNullOrWhiteSpace(
                _options.Host) ||
            _options.Port <= 0 ||
            string.IsNullOrWhiteSpace(
                _options.FromAddress))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(
                _options.Username) &&
            string.IsNullOrWhiteSpace(
                _options.Password))
        {
            return false;
        }

        return true;
    }

    private SecureSocketOptions ResolveSecurity() =>
        _options.Security
            .Trim()
            .ToLowerInvariant()
        switch
        {
            "none" =>
                SecureSocketOptions.None,

            "sslonconnect" =>
                SecureSocketOptions.SslOnConnect,

            "starttlswhenavailable" =>
                SecureSocketOptions
                    .StartTlsWhenAvailable,

            "auto" =>
                SecureSocketOptions.Auto,

            _ =>
                SecureSocketOptions.StartTls
        };
}
