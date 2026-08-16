using Edulytics.Core.Realtime;

namespace Edulytics.Tests.Phase15;

public sealed class RealtimeConsistencyContractTests
{
    [Fact]
    public void SchoolAnalyticsGroup_IsTenantScoped()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();

        var first =
            RealtimeGroupNames
                .SchoolAnalytics(a);

        var second =
            RealtimeGroupNames
                .SchoolAnalytics(b);

        Assert.NotEqual(
            first,
            second);

        Assert.Contains(
            a.ToString("N"),
            first);
    }

    [Fact]
    public void Browser_DebouncesAndRefreshesOnReconnect()
    {
        var root = FindRoot();

        var js =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "wwwroot",
                    "js",
                    "analytics-live.js"));

        Assert.Contains(
            "withAutomaticReconnect",
            js);

        Assert.Contains(
            "window.clearTimeout",
            js);

        Assert.Contains(
            "refreshDebounceMs",
            js);

        Assert.Contains(
            "onreconnected",
            js);

        Assert.Contains(
            "scheduleAuthoritativeRefresh",
            js);

        Assert.Contains(
            "window.location.reload",
            js);

        Assert.DoesNotContain(
            ".invoke(",
            js,
            StringComparison
                .OrdinalIgnoreCase);
    }

    [Fact]
    public void Worker_UsesAtomicClaimsAndNoInlineAnalyticsRefresh()
    {
        var root = FindRoot();

        var worker =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Background",
                    "OutboxProcessorBackgroundService.cs"));

        var repository =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Data",
                    "Repositories",
                    "OutboxRepository.cs"));

        Assert.Contains(
            "ClaimBatchAsync",
            worker);

        Assert.Contains(
            "SKIP LOCKED",
            repository);

        Assert.Contains(
            "LeaseToken",
            repository);

        Assert.DoesNotContain(
            "RefreshSchoolAsync",
            worker);

        Assert.Contains(
            "IAnalyticsRefreshQueueRepository",
            worker);
    }

    [Fact]
    public void AnalyticsWorker_IsBoundedInsideLease()
    {
        var root = FindRoot();

        var registration =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Extensions",
                    "RealtimeRegistrationExtensions.cs"));

        Assert.Contains(
            "AnalyticsRefreshTimeoutSeconds <",
            registration);

        Assert.Contains(
            "options.AnalyticsLeaseSeconds",
            registration);

        Assert.Contains(
            "MessageProcessingTimeoutSeconds <",
            registration);

        Assert.Contains(
            "options.LeaseSeconds",
            registration);
    }

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
