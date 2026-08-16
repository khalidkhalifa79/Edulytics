using System.Xml.Linq;
using Edulytics.Web.Middleware;
using Edulytics.Web.Production;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace Edulytics.Tests.Phase12;

public sealed class ProductionHardeningTests
{
    [Fact]
    public void Program_ContainsProductionHardeningPipeline()
    {
        var root =
            FindRoot();

        var text =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Program.cs"));

        Assert.Contains(
            "AddProductionHardeningPhase12",
            text);

        Assert.Contains(
            "AddJsonConsole",
            text);

        Assert.Contains(
            "\"/health/live\"",
            text);

        Assert.Contains(
            "\"/health/ready\"",
            text);

        Assert.Contains(
            "CorrelationIdMiddleware",
            text);

        Assert.Contains(
            "SecurityHeadersMiddleware",
            text);

        Assert.Contains(
            "UseExceptionHandler",
            text);

        Assert.Contains(
            "UseStatusCodePagesWithReExecute",
            text);
    }

    [Fact]
    public void ProductionSettings_ContainNoSecrets()
    {
        var root =
            FindRoot();

        var text =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "appsettings.Production.json"));

        Assert.DoesNotContain(
            "ConnectionStrings",
            text);

        Assert.DoesNotContain(
            "Password",
            text);

        Assert.Contains(
            "WorkerStaleAfterSeconds",
            text);

        Assert.Contains(
            "DatabaseTimeoutSeconds",
            text);
    }

    [Fact]
    public void ErrorLocalization_HasEnglishPolishKeys()
    {
        var root =
            FindRoot();

        var resources =
            Path.Combine(
                root,
                "src",
                "Edulytics.Web",
                "Resources");

        var en =
            Values(
                Path.Combine(
                    resources,
                    "SharedResource.resx"));

        var pl =
            Values(
                Path.Combine(
                    resources,
                    "SharedResource.pl.resx"));

        foreach (var key in new[]
                 {
                     "ErrorTitle",
                     "ErrorHeading",
                     "ErrorMessage",
                     "ErrorCorrelationLabel",
                     "ErrorReturnHome",
                     "NotFoundTitle",
                     "NotFoundHeading",
                     "NotFoundMessage",
                     "ServiceUnavailableTitle",
                     "ServiceUnavailableHeading",
                     "ServiceUnavailableMessage"
                 })
        {
            Assert.True(
                en.ContainsKey(key),
                $"EN missing {key}");

            Assert.True(
                pl.ContainsKey(key),
                $"PL missing {key}");

            Assert.False(
                string.IsNullOrWhiteSpace(
                    en[key]));

            Assert.False(
                string.IsNullOrWhiteSpace(
                    pl[key]));
        }
    }

    [Fact]
    public void ErrorView_ContainsNoDevelopmentLeak()
    {
        var root =
            FindRoot();

        var text =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Views",
                    "Shared",
                    "Error.cshtml"));

        Assert.DoesNotContain(
            "Development Mode",
            text);

        Assert.DoesNotContain(
            "StackTrace",
            text);

        Assert.Contains(
            "ErrorCorrelationLabel",
            text);

        Assert.Contains(
            "production-error-page",
            text);
    }

    [Fact]
    public async Task CorrelationMiddleware_PreservesSafeId()
    {
        var context =
            new DefaultHttpContext();

        context.Request.Headers[
            CorrelationIdMiddleware
                .HeaderName] =
            "phase12-safe-correlation";

        var called = false;

        var middleware =
            new CorrelationIdMiddleware(
                _ =>
                {
                    called = true;

                    return Task.CompletedTask;
                },
                NullLogger<
                    CorrelationIdMiddleware>
                    .Instance);

        await middleware.InvokeAsync(
            context);

        Assert.True(
            called);

        Assert.Equal(
            "phase12-safe-correlation",
            context.TraceIdentifier);

        Assert.Equal(
            "phase12-safe-correlation",
            context.Response.Headers[
                CorrelationIdMiddleware
                    .HeaderName]
                .ToString());
    }

    [Fact]
    public async Task CorrelationMiddleware_ReplacesUnsafeId()
    {
        var context =
            new DefaultHttpContext();

        context.Request.Headers[
            CorrelationIdMiddleware
                .HeaderName] =
            "unsafe value with spaces";

        var middleware =
            new CorrelationIdMiddleware(
                _ =>
                    Task.CompletedTask,
                NullLogger<
                    CorrelationIdMiddleware>
                    .Instance);

        await middleware.InvokeAsync(
            context);

        Assert.NotEqual(
            "unsafe value with spaces",
            context.TraceIdentifier);

        Assert.Equal(
            32,
            context.TraceIdentifier
                .Length);
    }

    [Fact]
    public void OutboxWorkerHealthState_TracksHeartbeat()
    {
        var state =
            new OutboxWorkerHealthState();

        Assert.False(
            state.Snapshot()
                .Started);

        var started =
            DateTime.UtcNow;

        state.MarkStarted(
            started);

        state.RecordHeartbeat(
            started.AddSeconds(1));

        var snapshot =
            state.Snapshot();

        Assert.True(
            snapshot.Started);

        Assert.Equal(
            started,
            snapshot.StartedAtUtc);

        Assert.Equal(
            started.AddSeconds(1),
            snapshot.LastHeartbeatUtc);
    }

    [Fact]
    public void OperationsDocumentation_Exists()
    {
        var root =
            FindRoot();

        foreach (var name in new[]
                 {
                     "PHASE_12_IMPLEMENTATION_PLAN.md",
                     "PRODUCTION_DEPLOYMENT.md",
                     "MONITORING_RUNBOOK.md",
                     "BACKUP_RESTORE_RUNBOOK.md"
                 })
        {
            Assert.True(
                File.Exists(
                    Path.Combine(
                        root,
                        "docs",
                        name)),
                name);
        }
    }

    [Fact]
    public void Program_ProvidesAnonymous404Fallback()
    {
        var root =
            FindRoot();

        var program =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Program.cs"));

        var fallbackIndex =
            program.IndexOf(
                "app.MapFallback(",
                StringComparison.Ordinal);

        var runIndex =
            program.IndexOf(
                "app.Run();",
                StringComparison.Ordinal);

        Assert.True(
            fallbackIndex >= 0);

        Assert.True(
            runIndex > fallbackIndex);

        var fallback =
            program[
                fallbackIndex..
                runIndex];

        Assert.Contains(
            "StatusCodes.Status404NotFound",
            fallback);

        Assert.Contains(
            ".AllowAnonymous();",
            fallback);
    }

    private static Dictionary<
        string,
        string> Values(
            string path) =>
        XDocument.Load(
                path)
            .Root!
            .Elements(
                "data")
            .ToDictionary(
                x =>
                    (string)
                        x.Attribute(
                            "name")!,
                x =>
                    x.Element(
                            "value")
                        ?.Value
                    ?? string.Empty);

    private static string FindRoot()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "Edulytics.sln")))
            {
                return directory.FullName;
            }

            directory =
                directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Edulytics repository root not found.");
    }
}
