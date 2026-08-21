namespace Edulytics.Tests.Phase25;

public sealed class Phase25BootstrapConcurrencyTests
{
    [Fact]
    public void PostgreSqlBootstrap_IsSerializedAcrossProcesses()
    {
        var source =
            ReadSource(
                "src/Edulytics.Web/Bootstrap/"
                + "EdulyticsDatabaseBootstrapper.cs");

        Assert.Contains(
            "Database.IsNpgsql()",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "pg_advisory_lock",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "pg_advisory_unlock",
            source,
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
