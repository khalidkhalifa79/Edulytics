namespace Edulytics.Tests.Phase25;

public sealed class Phase25ScaleFoundationTests
{
    [Fact]
    public void DataProtection_UsesSharedDatabasePersistence()
    {
        var source =
            ReadSource(
                "src/Edulytics.Web/Extensions/"
                + "ServiceCollectionExtensions.cs");

        Assert.Contains(
            "PersistKeysToDbContext",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "SetApplicationName",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "ProtectKeysWithCertificate",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Phase25Plan_RequiresProcessIndependentBehavior()
    {
        var plan =
            ReadSource(
                "docs/PHASE_25_IMPLEMENTATION_PLAN.md");

        Assert.Contains(
            "2 web instances",
            plan,
            StringComparison.Ordinal);

        Assert.Contains(
            "2 workers",
            plan,
            StringComparison.Ordinal);

        Assert.Contains(
            "distributed sensitive-rate-limit",
            plan,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "No behavior depends incorrectly on process-local memory",
            plan,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Discovery_DoesNotClaimPhase26PerformanceAcceptance()
    {
        var plan =
            ReadSource(
                "docs/PHASE_25_IMPLEMENTATION_PLAN.md");

        Assert.Contains(
            "does not perform Phase 26",
            plan,
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
