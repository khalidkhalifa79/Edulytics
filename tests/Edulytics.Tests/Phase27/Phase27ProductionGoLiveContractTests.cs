namespace Edulytics.Tests.Phase27;

public sealed class Phase27ProductionGoLiveContractTests
{
    private static readonly string Root =
        FindRepositoryRoot();

    [Fact]
    public void Phase27_ProductionReadinessAssetsExist()
    {
        Assert.True(File.Exists(Path.Combine(
            Root,
            "docs",
            "PHASE_27_PRODUCTION_GO_LIVE.md")));

        Assert.True(File.Exists(Path.Combine(
            Root,
            "docs",
            "ORACLE_PRODUCTION_HANDOFF.md")));

        Assert.True(File.Exists(Path.Combine(
            Root,
            "tools",
            "phase27",
            "phase27_free_readiness.py")));

        Assert.True(File.Exists(Path.Combine(
            Root,
            "docker",
            "phase27-predeploy.sh")));
    }

    [Fact]
    public void Phase27_CurrentPlanDoesNotRequirePaidRender()
    {
        var phase27 = File.ReadAllText(Path.Combine(
            Root,
            "docs",
            "PHASE_27_PRODUCTION_GO_LIVE.md"));

        Assert.Contains(
            "Render Free",
            phase27,
            StringComparison.Ordinal);

        Assert.Contains(
            "do not create a paid Render production service",
            phase27,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "one paid always-on Render web service",
            phase27,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Phase27_FinalDomainIsRootEdulytiksDomain()
    {
        var phase27 = File.ReadAllText(Path.Combine(
            Root,
            "docs",
            "PHASE_27_PRODUCTION_GO_LIVE.md"));

        Assert.Contains(
            "https://edulytiks.com",
            phase27,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "app.edulytiks.com` is the approved",
            phase27,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Phase27_OracleGoLiveIsExplicitlyDeferred()
    {
        var phase27 = File.ReadAllText(Path.Combine(
            Root,
            "docs",
            "PHASE_27_PRODUCTION_GO_LIVE.md"));

        var oracle = File.ReadAllText(Path.Combine(
            Root,
            "docs",
            "ORACLE_PRODUCTION_HANDOFF.md"));

        Assert.Contains(
            "Oracle",
            phase27,
            StringComparison.Ordinal);

        Assert.Contains(
            "Do not begin Oracle production provisioning until",
            oracle,
            StringComparison.Ordinal);

        Assert.Contains(
            "all required regression/security/performance/acceptance tests are green",
            oracle,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Phase27_FreeReadinessIsLockedToStaging()
    {
        var tool = File.ReadAllText(Path.Combine(
            Root,
            "tools",
            "phase27",
            "phase27_free_readiness.py"));

        Assert.Contains(
            "LOCKED_HOST = \"staging.edulytiks.com\"",
            tool,
            StringComparison.Ordinal);

        Assert.Contains(
            "PHASE27_FREE_ENVIRONMENT_READINESS_PASS",
            tool,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Phase27_StartupMigrationRemainsExplicitOptIn()
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
    public void Phase27_StagingKeepsTemporaryMigrationCompatibility()
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
    public void Phase27_ProductionSmokeStillRejectsStaging()
    {
        var text = File.ReadAllText(Path.Combine(
            Root,
            "tools",
            "phase27",
            "phase27_public_smoke.py"));

        Assert.Contains(
            "production smoke is hard-blocked from staging",
            text,
            StringComparison.Ordinal);
    }

    [Fact]
    public void Phase27_OperationsDocsRemainPostgreSqlNeonAware()
    {
        var backup = File.ReadAllText(Path.Combine(
            Root,
            "docs",
            "BACKUP_RESTORE_RUNBOOK.md"));

        var monitoring = File.ReadAllText(Path.Combine(
            Root,
            "docs",
            "MONITORING_RUNBOOK.md"));

        Assert.Contains(
            "PostgreSQL",
            backup,
            StringComparison.Ordinal);

        Assert.Contains(
            "Neon",
            monitoring,
            StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(
            AppContext.BaseDirectory);

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
