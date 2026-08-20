using System.Security.Cryptography;

namespace Edulytics.Web.Middleware;

public sealed class SecurityHeadersMiddleware
{
    public const string CspNonceItemKey =
        "__EdulyticsCspNonce";

    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(
        RequestDelegate next)
    {
        _next =
            next ??
            throw new ArgumentNullException(
                nameof(next));
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        var nonce =
            Convert.ToBase64String(
                RandomNumberGenerator
                    .GetBytes(18));

        context.Items[
            CspNonceItemKey] = nonce;

        var headers =
            context.Response.Headers;

        headers[
            "X-Content-Type-Options"] =
            "nosniff";

        headers[
            "X-Frame-Options"] =
            "DENY";

        headers[
            "Referrer-Policy"] =
            "strict-origin-when-cross-origin";

        headers[
            "Permissions-Policy"] =
            "camera=(), microphone=(), "
            + "geolocation=(), "
            + "payment=(), usb=()";

        headers[
            "Cross-Origin-Opener-Policy"] =
            "same-origin";

        headers[
            "Cross-Origin-Resource-Policy"] =
            "same-origin";

        headers[
            "X-Permitted-Cross-Domain-Policies"] =
            "none";

        var websocketSchemes =
            context.Request.IsHttps
                ? "wss:"
                : "ws: wss:";

        headers[
            "Content-Security-Policy"] =
            string.Join(
                ' ',
                "default-src 'self';",
                "base-uri 'self';",
                "object-src 'none';",
                "frame-ancestors 'none';",
                "form-action 'self';",
                $"script-src 'self' 'nonce-{nonce}';",
                "script-src-attr 'none';",
                "style-src 'self' 'unsafe-inline';",
                "img-src 'self' data:;",
                "font-src 'self' data:;",
                $"connect-src 'self' {websocketSchemes};",
                "worker-src 'self';",
                "manifest-src 'self';");

        await _next(
            context);
    }
}
