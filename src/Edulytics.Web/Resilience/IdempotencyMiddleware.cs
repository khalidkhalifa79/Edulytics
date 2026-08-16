using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Edulytics.Core.Resilience;

namespace Edulytics.Web.Resilience;

public sealed class IdempotencyMiddleware
{
    private const string HeaderName =
        "Idempotency-Key";

    private const string FormFieldName =
        "_idempotencyKey";

    private const string AntiforgeryFieldName =
        "__RequestVerificationToken";

    private readonly RequestDelegate _next;
    private readonly ILogger<IdempotencyMiddleware> _logger;

    public IdempotencyMiddleware(
        RequestDelegate next,
        ILogger<IdempotencyMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IIdempotencyService idempotency)
    {
        if (!ShouldProtect(context))
        {
            await _next(context);
            return;
        }

        if (!Guid.TryParse(
                context.User.FindFirstValue(
                    ClaimTypes.NameIdentifier),
                out var actorUserId))
        {
            await _next(context);
            return;
        }

        IFormCollection? form = null;

        if (context.Request.HasFormContentType)
        {
            form = await context.Request.ReadFormAsync(
                context.RequestAborted);
        }

        var key =
            GetKey(context, form);

        if (string.IsNullOrWhiteSpace(key))
        {
            // Current Edulytics browser mutations are antiforgery-protected
            // forms. Non-form clients can opt in with Idempotency-Key.
            await _next(context);
            return;
        }

        key = HashKeyIfNeeded(key);

        var operation =
            $"{context.Request.Method}:{context.Request.Path.Value}"
                .ToUpperInvariant();

        if (operation.Length > 160)
        {
            operation =
                Sha256(operation);
        }

        var schoolId =
            TrySchoolId(form);

        var requestHash =
            BuildRequestHash(
                context,
                form);

        var reservation =
            await idempotency.ReserveAsync(
                actorUserId,
                schoolId,
                operation,
                key,
                requestHash,
                DateTime.UtcNow,
                context.RequestAborted);

        if (reservation.Outcome !=
            IdempotencyReservationOutcome.Acquired)
        {
            context.Response.StatusCode =
                StatusCodes.Status409Conflict;

            context.Response.Headers[
                "Idempotency-Conflict"] =
                reservation.Outcome ==
                    IdempotencyReservationOutcome
                        .DuplicateSameRequest
                    ? "duplicate"
                    : "key-reuse";

            context.Response.Headers.RetryAfter = "0";
            return;
        }

        try
        {
            await _next(context);

            if (context.Response.StatusCode < 500)
            {
                await idempotency.CompleteAsync(
                    reservation.RecordId,
                    context.Response.StatusCode,
                    DateTime.UtcNow,
                    CancellationToken.None);
            }
            else
            {
                await idempotency.MarkIndeterminateAsync(
                    reservation.RecordId,
                    DateTime.UtcNow,
                    CancellationToken.None);
            }
        }
        catch (OperationCanceledException)
        {
            await MarkIndeterminateSafely(
                idempotency,
                reservation.RecordId);

            throw;
        }
        catch
        {
            await MarkIndeterminateSafely(
                idempotency,
                reservation.RecordId);

            throw;
        }
    }

    private async Task MarkIndeterminateSafely(
        IIdempotencyService service,
        Guid recordId)
    {
        try
        {
            await service.MarkIndeterminateAsync(
                recordId,
                DateTime.UtcNow,
                CancellationToken.None);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to mark idempotency reservation {RecordId} indeterminate.",
                recordId);
        }
    }

    private static bool ShouldProtect(
        HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return false;

        if (!HttpMethods.IsPost(context.Request.Method) &&
            !HttpMethods.IsPut(context.Request.Method) &&
            !HttpMethods.IsPatch(context.Request.Method) &&
            !HttpMethods.IsDelete(context.Request.Method))
        {
            return false;
        }

        var path = context.Request.Path;

        if (path.StartsWithSegments("/account") ||
            path.StartsWithSegments("/set-culture") ||
            path.StartsWithSegments("/health") ||
            path.StartsWithSegments("/hubs"))
        {
            return false;
        }

        // Upload already has content-hash idempotency and reading a multipart
        // file here would duplicate buffering before MVC model binding.
        if (path.Equals(
                "/school/imports/upload",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string? GetKey(
        HttpContext context,
        IFormCollection? form)
    {
        var header =
            context.Request.Headers[HeaderName]
                .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(header))
            return header.Trim();

        var explicitFormKey =
            form?[FormFieldName]
                .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(
                explicitFormKey))
        {
            return explicitFormKey.Trim();
        }

        var antiforgery =
            form?[AntiforgeryFieldName]
                .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(antiforgery))
        {
            return "af-" +
                   Sha256(antiforgery);
        }

        return null;
    }

    private static string HashKeyIfNeeded(
        string key) =>
        key.Length <= 128
            ? key
            : Sha256(key);

    private static Guid? TrySchoolId(
        IFormCollection? form)
    {
        if (form is null)
            return null;

        foreach (var name in new[]
                 {
                     "schoolId",
                     "SchoolId"
                 })
        {
            if (Guid.TryParse(
                    form[name].FirstOrDefault(),
                    out var id))
            {
                return id;
            }
        }

        return null;
    }

    private static string BuildRequestHash(
        HttpContext context,
        IFormCollection? form)
    {
        var builder =
            new StringBuilder();

        builder.Append(
            context.Request.Method.ToUpperInvariant());
        builder.Append('|');
        builder.Append(
            context.Request.Path.Value
                ?.ToUpperInvariant());
        builder.Append('|');

        foreach (var query in
                 context.Request.Query
                     .OrderBy(
                         x => x.Key,
                         StringComparer.Ordinal))
        {
            builder.Append(query.Key);
            builder.Append('=');
            builder.Append(
                string.Join(",", query.Value.ToArray()));
            builder.Append('&');
        }

        if (form is not null)
        {
            foreach (var field in
                     form.OrderBy(
                         x => x.Key,
                         StringComparer.Ordinal))
            {
                if (string.Equals(
                        field.Key,
                        FormFieldName,
                        StringComparison.Ordinal) ||
                    string.Equals(
                        field.Key,
                        AntiforgeryFieldName,
                        StringComparison.Ordinal))
                {
                    continue;
                }

                builder.Append(field.Key);
                builder.Append('=');
                builder.Append(
                    string.Join(",", field.Value.ToArray()));
                builder.Append('&');
            }
        }

        return Sha256(
            builder.ToString());
    }

    private static string Sha256(
        string value) =>
        Convert.ToHexString(
            SHA256.HashData(
                Encoding.UTF8.GetBytes(value)));
}
