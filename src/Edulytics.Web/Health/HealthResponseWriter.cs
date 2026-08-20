using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Edulytics.Web.Health;

public static class HealthResponseWriter
{
    public static Task WriteAsync(
        HttpContext context,
        HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(
            context);

        ArgumentNullException.ThrowIfNull(
            report);

        context.Response.ContentType =
            "application/json; charset=utf-8";

        context.Response.Headers.CacheControl =
            "no-store, no-cache";

        context.Response.Headers.Pragma =
            "no-cache";

        // Anonymous health endpoints expose only the
        // minimum state required by infrastructure.
        //
        // Component names, descriptions, timings and
        // health-data dictionaries remain server-side.
        var payload =
            new
            {
                status =
                    report.Status.ToString(),

                correlationId =
                    context.TraceIdentifier
            };

        return context.Response.WriteAsync(
            JsonSerializer.Serialize(
                payload));
    }
}
