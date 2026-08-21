namespace Edulytics.Tests.Phase25;

public sealed class Phase25PipelineOrderingTests
{
    [Fact]
    public void Routing_PrecedesDistributedQuota_AndLocalLimiter()
    {
        var program =
            ReadSource(
                "src/Edulytics.Web/Program.cs");

        var routing =
            program.IndexOf(
                "app.UseRouting();",
                StringComparison.Ordinal);

        var distributed =
            program.IndexOf(
                "DistributedSensitiveRateLimitMiddleware",
                StringComparison.Ordinal);

        var local =
            program.IndexOf(
                "app.UseRateLimiter();",
                StringComparison.Ordinal);

        Assert.True(routing >= 0);
        Assert.True(distributed >= 0);
        Assert.True(local >= 0);
        Assert.True(routing < distributed);
        Assert.True(distributed < local);
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
