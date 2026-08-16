using System.Diagnostics;

namespace Edulytics.Web.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName =
        "X-Correlation-ID";

    public const string ItemKey =
        "Edulytics.CorrelationId";

    private const int MaxLength = 64;

    private readonly RequestDelegate _next;

    private readonly ILogger<
        CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(
        RequestDelegate next,
        ILogger<CorrelationIdMiddleware> logger)
    {
        _next =
            next ??
            throw new ArgumentNullException(
                nameof(next));

        _logger =
            logger ??
            throw new ArgumentNullException(
                nameof(logger));
    }

    public async Task InvokeAsync(
        HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        var correlationId =
            ResolveCorrelationId(
                context);

        context.Items[ItemKey] =
            correlationId;

        context.TraceIdentifier =
            correlationId;

        context.Response.Headers[
            HeaderName] =
            correlationId;

        using var scope =
            _logger.BeginScope(
                new Dictionary<string, object?>
                {
                    ["CorrelationId"] =
                        correlationId
                });

        var stopwatch =
            Stopwatch.StartNew();

        try
        {
            await _next(
                context);
        }
        finally
        {
            stopwatch.Stop();

            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMilliseconds} ms.",
                context.Request.Method,
                context.Request.Path.Value ?? "/",
                context.Response.StatusCode,
                Math.Round(
                    stopwatch.Elapsed
                        .TotalMilliseconds,
                    2));
        }
    }

    public static string GetCorrelationId(
        HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        if (context.Items.TryGetValue(
                ItemKey,
                out var value) &&
            value is string stored &&
            !string.IsNullOrWhiteSpace(
                stored))
        {
            return stored;
        }

        if (!string.IsNullOrWhiteSpace(
                context.TraceIdentifier))
        {
            return context.TraceIdentifier;
        }

        return Guid.NewGuid()
            .ToString("N");
    }

    private static string ResolveCorrelationId(
        HttpContext context)
    {
        var supplied =
            context.Request.Headers[
                HeaderName]
                .FirstOrDefault()
                ?.Trim();

        if (IsSafe(
                supplied))
        {
            return supplied!;
        }

        return Guid.NewGuid()
            .ToString("N");
    }

    private static bool IsSafe(
        string? value)
    {
        if (string.IsNullOrWhiteSpace(
                value) ||
            value.Length > MaxLength)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (char.IsLetterOrDigit(
                    character) ||
                character is '-' or '_' or '.')
            {
                continue;
            }

            return false;
        }

        return true;
    }
}
