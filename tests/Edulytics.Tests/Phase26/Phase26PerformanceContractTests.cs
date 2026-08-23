using System.Text.Json;

namespace Edulytics.Tests.Phase26;

public sealed class Phase26PerformanceContractTests
{
    private static readonly string Root =
        FindRepositoryRoot();

    [Fact]
    public void Phase26_PerformanceAssetsExist()
    {
        Assert.True(File.Exists(Path.Combine(
            Root,
            "tools",
            "phase26",
            "phase26_load.py")));

        Assert.True(File.Exists(Path.Combine(
            Root,
            "tools",
            "phase26",
            "phase26_local_qualification.py")));

        Assert.True(File.Exists(Path.Combine(
            Root,
            "tools",
            "phase26",
            "phase26_report.py")));

        Assert.True(File.Exists(Path.Combine(
            Root,
            "tools",
            "phase26",
            "slo.json")));
    }

    [Fact]
    public void Phase26_SoakContractRequiresAtLeastSixHours()
    {
        var json = JsonDocument.Parse(
            File.ReadAllText(Path.Combine(
                Root,
                "tools",
                "phase26",
                "slo.json")));

        Assert.True(
            json.RootElement
                .GetProperty("soak")
                .GetProperty("minimum_minutes")
                .GetInt32() >= 360);
    }

    [Fact]
    public void Phase26_LiveLoadIsLockedToStagingHost()
    {
        var text = File.ReadAllText(Path.Combine(
            Root,
            "tools",
            "phase26",
            "phase26_load.py"));

        Assert.Contains(
            "LOCKED_HOST = \"staging.edulytiks.com\"",
            text,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "production.edulytiks.com",
            text,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Phase26_UsesSeparateSchoolScopedSignalRActor()
    {
        var text = File.ReadAllText(Path.Combine(
            Root,
            "tools",
            "phase26",
            "phase26_load.py"));

        Assert.Contains(
            "--signalr-email",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "SignalR SchoolAdmin",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "signalr_load(",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "signalr_browser",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Phase26_LiveEvidenceCanResumeWithoutRepeatingPreSoakWork()
    {
        var text = File.ReadAllText(Path.Combine(
            Root,
            "tools",
            "phase26",
            "phase26_load.py"));

        Assert.Contains(
            "pre_soak_pass",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "phase26_live_status",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "progress_interval=300",
            text,
            StringComparison.Ordinal);
    }


    [Fact]
    public void Phase26_SchoolAdminAuthenticationUsesRealSchoolDashboard()
    {
        var text = File.ReadAllText(Path.Combine(
            Root,
            "tools",
            "phase26",
            "phase26_load.py"));

        Assert.Contains(
            "\"/school/dashboard\",",
            text,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "\"/Analytics\",",
            text,
            StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(Path.Combine(
                current.FullName,
                "Edulytics.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Edulytics repository root was not found.");
    }
}
