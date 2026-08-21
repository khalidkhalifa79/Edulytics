using System.Net;
using Edulytics.Web.Scale;

namespace Edulytics.Tests.Phase25;

public sealed class Phase25ScaleCoreTests
{
    [Fact]
    public void RuntimeRoles_HaveExpectedProcessResponsibilities()
    {
        Assert.True(
            RuntimeRoles.RunsWebTraffic(
                RuntimeRoles.Combined));

        Assert.True(
            RuntimeRoles.RunsBackgroundWorkers(
                RuntimeRoles.Combined));

        Assert.True(
            RuntimeRoles.RunsWebTraffic(
                RuntimeRoles.Web));

        Assert.False(
            RuntimeRoles.RunsBackgroundWorkers(
                RuntimeRoles.Web));

        Assert.False(
            RuntimeRoles.RunsWebTraffic(
                RuntimeRoles.Worker));

        Assert.True(
            RuntimeRoles.RunsBackgroundWorkers(
                RuntimeRoles.Worker));
    }

    [Fact]
    public void ConnectionBudget_CoversEveryExpectedProcess()
    {
        var options =
            new MultiInstanceScaleOptions
            {
                ExpectedWebInstances = 2,
                ExpectedWorkerInstances = 2,
                DatabaseConnectionBudget = 160
            };

        Assert.Equal(
            4,
            options.ExpectedProcessCount);

        Assert.Equal(
            160,
            options
                .RequiredDatabasePoolCapacity(
                    40));
    }

    [Fact]
    public async Task DisabledDistributedLimiter_AllowsRequest()
    {
        var limiter =
            new DisabledDistributedSensitiveRateLimiter();

        var decision =
            await limiter.TryAcquireAsync(
                "Login",
                "127.0.0.1",
                20,
                TimeSpan.FromMinutes(5),
                CancellationToken.None);

        Assert.True(decision.Allowed);
        Assert.Equal(
            TimeSpan.Zero,
            decision.RetryAfter);
    }

    [Fact]
    public void RedisParser_AcceptsStackExchangeEndpointForm()
    {
        var options =
            RedisConnectionConfiguration.Parse(
                "redis:6379");

        var endpoint =
            Assert.IsType<DnsEndPoint>(
                Assert.Single(
                    options.EndPoints));

        Assert.Equal(
            "redis",
            endpoint.Host);

        Assert.Equal(
            6379,
            endpoint.Port);

        Assert.False(
            options.Ssl);
    }

    [Fact]
    public void RedisParser_AcceptsRedisUriForm()
    {
        var options =
            RedisConnectionConfiguration.Parse(
                "redis://redis:6379");

        var endpoint =
            Assert.IsType<DnsEndPoint>(
                Assert.Single(
                    options.EndPoints));

        Assert.Equal(
            "redis",
            endpoint.Host);

        Assert.Equal(
            6379,
            endpoint.Port);

        Assert.False(
            options.Ssl);
    }

    [Fact]
    public void RealtimeRegistration_UsesRedisBackplaneAndRoleGate()
    {
        var source =
            ReadSource(
                "src/Edulytics.Web/Extensions/"
                + "RealtimeRegistrationExtensions.cs");

        Assert.Contains(
            "AddStackExchangeRedis",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "scale.RunsBackgroundWorkers",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RetentionAndReadiness_AreWorkerRoleAware()
    {
        var retention =
            ReadSource(
                "src/Edulytics.Web/Extensions/"
                + "SecurityPrivacyRegistrationExtensions.cs");

        var production =
            ReadSource(
                "src/Edulytics.Web/Extensions/"
                + "ProductionHardeningRegistrationExtensions.cs");

        Assert.Contains(
            "scale.RunsBackgroundWorkers",
            retention,
            StringComparison.Ordinal);

        Assert.Contains(
            "scale.RunsBackgroundWorkers",
            production,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Program_EnforcesDistributedQuotaBeforeLocalLimiter()
    {
        var program =
            ReadSource(
                "src/Edulytics.Web/Program.cs");

        var distributed =
            program.IndexOf(
                "DistributedSensitiveRateLimitMiddleware",
                StringComparison.Ordinal);

        var local =
            program.IndexOf(
                "UseRateLimiter",
                StringComparison.Ordinal);

        Assert.True(distributed >= 0);
        Assert.True(local >= 0);
        Assert.True(distributed < local);
    }

    [Fact]
    public void ScaleConfiguration_IsDisabledByDefault()
    {
        var settings =
            ReadSource(
                "src/Edulytics.Web/appsettings.json");

        Assert.Contains(
            "\"Scale\"",
            settings,
            StringComparison.Ordinal);

        Assert.Contains(
            "\"Enabled\": false",
            settings,
            StringComparison.Ordinal);

        Assert.Contains(
            "\"RuntimeRole\": \"Combined\"",
            settings,
            StringComparison.Ordinal);
    }

    [Fact]
    public void WebProject_UsesMicrosoftRedisScaleOutPackage()
    {
        var project =
            ReadSource(
                "src/Edulytics.Web/"
                + "Edulytics.Web.csproj");

        Assert.Contains(
            "Microsoft.AspNetCore.SignalR.StackExchangeRedis",
            project,
            StringComparison.Ordinal);

        Assert.Contains(
            "10.0.11",
            project,
            StringComparison.Ordinal);
    }

    private static string ReadSource(
        string relativePath) =>
        File.ReadAllText(
            Path.Combine(
                FindRepositoryRoot(),
                relativePath));

    private static string FindRepositoryRoot()
    {
        var current =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        current.FullName,
                        "Edulytics.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root not found.");
    }
}
