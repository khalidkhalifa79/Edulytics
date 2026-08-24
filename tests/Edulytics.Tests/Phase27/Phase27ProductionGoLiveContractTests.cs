namespace Edulytics.Tests.Phase27;

public sealed class Phase27ProductionGoLiveContractTests
{
    private static readonly string Root =
        FindRepositoryRoot();

    [Fact]
    public void Phase27_ProductionArtifactsExist()
    {
        Assert.True(File.Exists(Path.Combine(
            Root,
            "docker",
            "phase27-predeploy.sh")));

        Assert.True(File.Exists(Path.Combine(
            Root,
            "tools",
            "phase27",
            "phase27_preflight.py")));

        Assert.True(File.Exists(Path.Combine(
            Root,
            "tools",
            "phase27",
            "phase27_public_smoke.py")));

        Assert.True(File.Exists(Path.Combine(
            Root,
            "docs",
            "PHASE_27_PRODUCTION_GO_LIVE.md")));
    }

    [Fact]
    public void Phase27_StartupMigrationIsExplicitOptIn()
    {
        var text = File.ReadAllText(Path.Combine(
            Root,
            "docker",
            "render-entrypoint.sh"));

        Assert.Contains(
            "Edulytics__Deployment__RunStartupMigrations:-false",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "Startup migrations disabled; expecting controlled pre-deploy migration.",
            text,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "if [ -n \"${ConnectionStrings__MigrationConnection:-}\" ]",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Phase27_StagingExplicitlyKeepsTemporaryStartupMigrationCompatibility()
    {
        var text = File.ReadAllText(Path.Combine(
            Root,
            "render.yaml"));

        Assert.Contains(
            "Edulytics__Deployment__RunStartupMigrations",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "value: \"true\"",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Phase27_ImageContainsDedicatedPreDeployMigrationScript()
    {
        var dockerfile = File.ReadAllText(Path.Combine(
            Root,
            "Dockerfile"));

        Assert.Contains(
            "COPY docker/phase27-predeploy.sh /app/phase27-predeploy.sh",
            dockerfile,
            StringComparison.Ordinal);

        Assert.Contains(
            "/app/phase27-predeploy.sh",
            dockerfile,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Phase27_PreDeployRejectsPooledMigrationEndpoint()
    {
        var text = File.ReadAllText(Path.Combine(
            Root,
            "docker",
            "phase27-predeploy.sh"));

        Assert.Contains(
            "*-pooler.*",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "rejected a pooled migration endpoint",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Phase27_OperationsDocsDoNotClaimSqlServerIsActive()
    {
        var backup = File.ReadAllText(Path.Combine(
            Root,
            "docs",
            "BACKUP_RESTORE_RUNBOOK.md"));

        var monitoring = File.ReadAllText(Path.Combine(
            Root,
            "docs",
            "MONITORING_RUNBOOK.md"));

        Assert.DoesNotContain(
            "SQL Server availability",
            backup,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "SQL Server availability",
            monitoring,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "PostgreSQL",
            backup,
            StringComparison.Ordinal);

        Assert.Contains(
            "Neon",
            monitoring,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Phase27_ProductionSmokeHardBlocksStaging()
    {
        var text = File.ReadAllText(Path.Combine(
            Root,
            "tools",
            "phase27",
            "phase27_public_smoke.py"));

        Assert.Contains(
            "staging.edulytiks.com",
            text,
            StringComparison.Ordinal);

        Assert.Contains(
            "production smoke is hard-blocked from staging",
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
