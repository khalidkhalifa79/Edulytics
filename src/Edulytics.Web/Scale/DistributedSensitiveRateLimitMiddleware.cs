using System.Security.Claims;
using Microsoft.AspNetCore.RateLimiting;

namespace Edulytics.Web.Scale;

public sealed class
    DistributedSensitiveRateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public DistributedSensitiveRateLimitMiddleware(
        RequestDelegate next)
    {
        _next =
            next ??
            throw new ArgumentNullException(
                nameof(next));
    }

    public async Task InvokeAsync(
        HttpContext context,
        MultiInstanceScaleOptions scaleOptions,
        IDistributedSensitiveRateLimiter limiter)
    {
        if (!scaleOptions.Enabled ||
            !scaleOptions
                .DistributedSensitiveRateLimitsEnabled)
        {
            await _next(context);
            return;
        }

        var rateLimitMetadata =
            context
                .GetEndpoint()
                ?.Metadata
                .GetMetadata<
                    EnableRateLimitingAttribute>();

        if (!TryGetPolicy(
                rateLimitMetadata?.PolicyName,
                out var policy))
        {
            await _next(context);
            return;
        }

        var partition =
            ResolvePartition(
                context,
                policy.PartitionKind);

        var decision =
            await limiter.TryAcquireAsync(
                policy.Name,
                partition,
                policy.PermitLimit,
                policy.Window,
                context.RequestAborted);

        if (decision.Allowed)
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;

        context.Response.Headers.RetryAfter =
            Math.Max(
                    1,
                    (int)Math.Ceiling(
                        decision
                            .RetryAfter
                            .TotalSeconds))
                .ToString(
                    System.Globalization
                        .CultureInfo.InvariantCulture);
    }

    private static bool TryGetPolicy(
        string? policyName,
        out SensitivePolicy policy)
    {
        policy =
            policyName switch
            {
                "RequestDemo" =>
                    new SensitivePolicy(
                        "RequestDemo",
                        5,
                        TimeSpan.FromHours(1),
                        PartitionKind.Ip),

                "Login" =>
                    new SensitivePolicy(
                        "Login",
                        20,
                        TimeSpan.FromMinutes(5),
                        PartitionKind.Ip),

                "OperationalMutation" =>
                    new SensitivePolicy(
                        "OperationalMutation",
                        30,
                        TimeSpan.FromMinutes(10),
                        PartitionKind.Actor),

                "SchoolUserCreate" =>
                    new SensitivePolicy(
                        "SchoolUserCreate",
                        20,
                        TimeSpan.FromMinutes(10),
                        PartitionKind.Actor),

                "InvitationResend" =>
                    new SensitivePolicy(
                        "InvitationResend",
                        3,
                        TimeSpan.FromMinutes(10),
                        PartitionKind.ActorAndTarget),

                "PasswordSetup" =>
                    new SensitivePolicy(
                        "PasswordSetup",
                        10,
                        TimeSpan.FromMinutes(15),
                        PartitionKind.Ip),

                "ReportExport" =>
                    new SensitivePolicy(
                        "ReportExport",
                        12,
                        TimeSpan.FromMinutes(10),
                        PartitionKind.Actor),

                _ => default
            };

        return !string.IsNullOrWhiteSpace(
            policy.Name);
    }

    private static string ResolvePartition(
        HttpContext context,
        PartitionKind kind)
    {
        var ip =
            context.Connection
                .RemoteIpAddress
                ?.ToString()
            ?? "unknown";

        if (kind == PartitionKind.Ip)
        {
            return ip;
        }

        var actor =
            context.User.FindFirst(
                    ClaimTypes.NameIdentifier)
                ?.Value
            ?? ip;

        if (kind ==
            PartitionKind.ActorAndTarget)
        {
            var target =
                context.Request
                    .RouteValues["id"]
                    ?.ToString()
                ?? "unknown";

            return $"{actor}:{target}";
        }

        return actor;
    }

    private readonly record struct
        SensitivePolicy(
            string Name,
            int PermitLimit,
            TimeSpan Window,
            PartitionKind PartitionKind);

    private enum PartitionKind
    {
        Ip,
        Actor,
        ActorAndTarget
    }
}
