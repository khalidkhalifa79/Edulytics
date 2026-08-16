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

        var payload =
            new
            {
                status =
                    report.Status
                        .ToString(),

                correlationId =
                    context.TraceIdentifier,

                totalDurationMs =
                    Math.Round(
                        report.TotalDuration
                            .TotalMilliseconds,
                        2),

                checks =
                    report.Entries
                        .OrderBy(
                            x => x.Key)
                        .Select(
                            x =>
                                new
                                {
                                    name =
                                        x.Key,

                                    status =
                                        x.Value.Status
                                            .ToString(),

                                    description =
                                        x.Value.Description,

                                    durationMs =
                                        Math.Round(
                                            x.Value.Duration
                                                .TotalMilliseconds,
                                            2),

                                    data =
                                        x.Value.Data
                                })
            };

        var json =
            JsonSerializer.Serialize(
                payload);

        return context.Response
            .WriteAsync(
                json);
    }
}
