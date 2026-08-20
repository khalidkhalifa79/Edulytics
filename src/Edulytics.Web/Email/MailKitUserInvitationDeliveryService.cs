using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Edulytics.Web.Email;

public sealed class MailKitUserInvitationDeliveryService
    : IUserInvitationDeliveryService,
      IUserInvitationConnector
{
    private readonly SmtpEmailOptions _options;

    private readonly
        IInvitationEmailTemplateRenderer
        _templates;

    private readonly ILogger<
        MailKitUserInvitationDeliveryService>
        _logger;

    private readonly
        EmailConnectorCircuitBreaker
        _circuit;

    public MailKitUserInvitationDeliveryService(
        IOptions<SmtpEmailOptions> options,
        IInvitationEmailTemplateRenderer templates,
        ILogger<
            MailKitUserInvitationDeliveryService> logger,
        EmailConnectorCircuitBreaker? circuit = null)
    {
        _options = options.Value;
        _templates = templates;
        _logger = logger;
        _circuit =
            circuit
            ?? new EmailConnectorCircuitBreaker();
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

        if (!_circuit.CanExecute(
                DateTime.UtcNow))
        {
            return UserInvitationDeliveryResult.Failed(
                UserInvitationDeliveryFailure
                    .CircuitOpen);
        }

        using var connectorTimeout =
            CancellationTokenSource
                .CreateLinkedTokenSource(
                    cancellationToken);

        connectorTimeout.CancelAfter(
            TimeSpan.FromSeconds(
                _options.TimeoutSeconds));

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
                new SmtpClient
                {
                    Timeout =
                        checked(
                            _options
                                .TimeoutSeconds *
                            1000)
                };

            await client.ConnectAsync(
                _options.Host,
                _options.Port,
                ResolveSecurity(),
                connectorTimeout.Token);

            if (!string.IsNullOrWhiteSpace(
                    _options.Username))
            {
                await client.AuthenticateAsync(
                    _options.Username,
                    _options.Password,
                    connectorTimeout.Token);
            }

            await client.SendAsync(
                message,
                connectorTimeout.Token);

            await client.DisconnectAsync(
                true,
                connectorTimeout.Token);

            _circuit.RecordSuccess();

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
        catch (OperationCanceledException)
        {
            _circuit.RecordFailure(
                DateTime.UtcNow,
                _options
                    .CircuitFailureThreshold,
                _options
                    .CircuitBreakSeconds);

            _logger.LogWarning(
                "Invitation email connector timed out.");

            return UserInvitationDeliveryResult.Failed(
                UserInvitationDeliveryFailure
                    .TimedOut);
        }
        catch (Exception exception)
        {
            _circuit.RecordFailure(
                DateTime.UtcNow,
                _options
                    .CircuitFailureThreshold,
                _options
                    .CircuitBreakSeconds);

            // Intentionally log only the exception type:
            // setup URLs contain short-lived security tokens.
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
                _options.FromAddress) ||
            _options.TimeoutSeconds <= 0 ||
            _options.CircuitFailureThreshold <= 0 ||
            _options.CircuitBreakSeconds <= 0)
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
