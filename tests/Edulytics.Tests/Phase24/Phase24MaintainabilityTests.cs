namespace Edulytics.Tests.Phase24;

public sealed class Phase24MaintainabilityTests
{
    [Fact]
    public void TemplateFiles_AreRemoved()
    {
        var root = FindRepositoryRoot();

        string[] files =
        [
            "src/Edulytics.Core/Class1.cs",
            "src/Edulytics.Data/Class1.cs",
            "src/Edulytics.Services/Class1.cs",
            "tests/Edulytics.Tests/UnitTest1.cs"
        ];

        foreach (var file in files)
        {
            Assert.False(
                File.Exists(Path.Combine(root, file)),
                $"Template file still exists: {file}");
        }
    }

    [Fact]
    public void Readme_DescribesCurrentArchitecture()
    {
        var readme = ReadSource("README.md");

        Assert.Contains(
            "PostgreSQL",
            readme,
            StringComparison.Ordinal);

        Assert.Contains(
            "Npgsql",
            readme,
            StringComparison.Ordinal);

        Assert.Contains(
            "Render",
            readme,
            StringComparison.Ordinal);

        Assert.Contains(
            "Phase 24",
            readme,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "built with ASP.NET Core MVC, EF Core, SQL Server",
            readme,
            StringComparison.Ordinal);
    }

    [Fact]
    public void BuildPolicy_UsesStableLanguageAndCiWarningGate()
    {
        var props = ReadSource("Directory.Build.props");

        Assert.DoesNotContain(
            "<LangVersion>preview</LangVersion>",
            props,
            StringComparison.Ordinal);

        Assert.Contains(
            "TreatWarningsAsErrors",
            props,
            StringComparison.Ordinal);

        Assert.Contains(
            "$(CI)",
            props,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ScreenshotArtifacts_AreIgnored()
    {
        var ignore = ReadSource(".gitignore");

        Assert.Contains(
            "phase*-screenshots*.zip",
            ignore,
            StringComparison.Ordinal);

        Assert.Contains(
            "*browser-artifacts*.zip",
            ignore,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendVendorVersions_AreDocumented()
    {
        var doc = ReadSource("docs/FRONTEND_VENDORING.md");

        string[] expected =
        [
            "Bootstrap",
            "5.3.3",
            "jQuery",
            "3.7.1",
            "1.21.0",
            "4.0.0",
            "SignalR",
            "10.0.11"
        ];

        foreach (var value in expected)
        {
            Assert.Contains(
                value,
                doc,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void DomainTestTaxonomy_IsRepositoryTracked()
    {
        var taxonomy =
            ReadSource("tests/DOMAIN_TEST_CATEGORIES.md");

        var runner =
            ReadSource("scripts/test-domain.sh");

        string[] domains =
        [
            "architecture",
            "authorization",
            "tenancy",
            "schools",
            "users",
            "academics",
            "curriculum",
            "assessments",
            "analytics",
            "realtime",
            "imports",
            "audit",
            "supervisors",
            "reports",
            "notifications",
            "operations",
            "security",
            "production"
        ];

        foreach (var domain in domains)
        {
            Assert.Contains(
                domain,
                taxonomy,
                StringComparison.Ordinal);

            Assert.Contains(
                domain,
                runner,
                StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CssRefactorPolicy_PreservesCascadeSafety()
    {
        var doc = ReadSource("docs/CSS_ORGANIZATION.md");

        Assert.Contains(
            "cascade",
            doc,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "320px",
            doc,
            StringComparison.Ordinal);

        Assert.Contains(
            "375px",
            doc,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SchoolsIndex_HasPlatformDashboardBackNavigation()
    {
        var view =
            ReadSource(
                "src/Edulytics.Web/Views/Schools/Index.cshtml");

        Assert.Contains(
            "school-back-link",
            view,
            StringComparison.Ordinal);

        Assert.Contains(
            "asp-controller=\"Platform\"",
            view,
            StringComparison.Ordinal);

        Assert.Contains(
            "asp-action=\"Dashboard\"",
            view,
            StringComparison.Ordinal);

        Assert.Contains(
            "@T[\"BackToDashboard\"]",
            view,
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
            new DirectoryInfo(AppContext.BaseDirectory);

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
