using Edulytics.Web.Email;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Edulytics.Tests.Phase05;

public sealed class InvitationEmailDeliveryTests
{
    [Fact]
    public async Task DisabledEmail_ReturnsDisabled()
    {
        var service = CreateService(
            new SmtpEmailOptions
            {
                Enabled = false
            });

        var result =
            await service.SendAsync(
                Request());

        Assert.False(result.Succeeded);

        Assert.Equal(
            UserInvitationDeliveryFailure.Disabled,
            result.Failure);
    }

    [Fact]
    public async Task InvalidConfiguration_DoesNotAttemptDelivery()
    {
        var service = CreateService(
            new SmtpEmailOptions
            {
                Enabled = true,
                Host = string.Empty,
                FromAddress =
                    "noreply@edulytics.local"
            });

        var result =
            await service.SendAsync(
                Request());

        Assert.False(result.Succeeded);

        Assert.Equal(
            UserInvitationDeliveryFailure
                .InvalidConfiguration,
            result.Failure);
    }

    private static
        MailKitUserInvitationDeliveryService
        CreateService(
            SmtpEmailOptions options) =>
        new(
            Options.Create(options),
            new StubRenderer(),
            NullLogger<
                MailKitUserInvitationDeliveryService>
                .Instance);

    private static UserInvitationDeliveryRequest
        Request() =>
        new(
            "teacher@example.com",
            "Test School",
            "en",
            "https://example.com/account/set-password");

    private sealed class StubRenderer
        : IInvitationEmailTemplateRenderer
    {
        public InvitationEmailContent Render(
            string culture,
            string schoolName,
            string setupUrl) =>
            new(
                "Subject",
                "Text",
                "<p>HTML</p>");
    }
}
