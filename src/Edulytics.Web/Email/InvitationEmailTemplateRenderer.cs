using System.Globalization;
using System.Text.Encodings.Web;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Email;

public sealed record InvitationEmailContent(
    string Subject,
    string TextBody,
    string HtmlBody);

public interface IInvitationEmailTemplateRenderer
{
    InvitationEmailContent Render(
        string culture,
        string schoolName,
        string setupUrl);
}

public sealed class InvitationEmailTemplateRenderer
    : IInvitationEmailTemplateRenderer
{
    private readonly IStringLocalizer<PlatformResource> _text;

    public InvitationEmailTemplateRenderer(
        IStringLocalizer<PlatformResource> text)
    {
        _text = text;
    }

    public InvitationEmailContent Render(
        string culture,
        string schoolName,
        string setupUrl)
    {
        var normalizedCulture =
            string.Equals(
                culture,
                "pl",
                StringComparison.OrdinalIgnoreCase)
                ? "pl"
                : "en";

        var originalCulture =
            CultureInfo.CurrentCulture;

        var originalUiCulture =
            CultureInfo.CurrentUICulture;

        try
        {
            var targetCulture =
                CultureInfo.GetCultureInfo(
                    normalizedCulture);

            CultureInfo.CurrentCulture =
                targetCulture;

            CultureInfo.CurrentUICulture =
                targetCulture;

            var subject =
                _text[
                    "InvitationEmailSubject"
                ].Value;

            var intro =
                _text[
                    "InvitationEmailIntro",
                    schoolName
                ].Value;

            var instruction =
                _text[
                    "InvitationEmailInstruction"
                ].Value;

            var action =
                _text[
                    "InvitationEmailAction"
                ].Value;

            var fallback =
                _text[
                    "InvitationEmailFallback"
                ].Value;

            var security =
                _text[
                    "InvitationEmailSecurity"
                ].Value;

            var encoder =
                HtmlEncoder.Default;

            var html =
                $"""
                <!doctype html>
                <html lang="{normalizedCulture}">
                <body>
                    <p>{encoder.Encode(intro)}</p>

                    <p>{encoder.Encode(instruction)}</p>

                    <p>
                        <a href="{encoder.Encode(setupUrl)}">
                            {encoder.Encode(action)}
                        </a>
                    </p>

                    <p>{encoder.Encode(fallback)}</p>

                    <p>
                        <a href="{encoder.Encode(setupUrl)}">
                            {encoder.Encode(setupUrl)}
                        </a>
                    </p>

                    <p>{encoder.Encode(security)}</p>
                </body>
                </html>
                """;

            var textBody =
                $"""
                {intro}

                {instruction}

                {action}:
                {setupUrl}

                {security}
                """;

            return new InvitationEmailContent(
                subject,
                textBody,
                html);
        }
        finally
        {
            CultureInfo.CurrentCulture =
                originalCulture;

            CultureInfo.CurrentUICulture =
                originalUiCulture;
        }
    }
}
