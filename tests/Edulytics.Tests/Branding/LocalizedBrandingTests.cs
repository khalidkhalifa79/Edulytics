using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Edulytics.Tests.Branding;

public sealed class LocalizedBrandingTests
{
    [Fact]
    public void SharedResources_ContainExactLocalizedBrandContract()
    {
        var root = FindRoot();

        var en = Values(Path.Combine(
            root,
            "src/Edulytics.Web/Resources/SharedResource.resx"));

        var pl = Values(Path.Combine(
            root,
            "src/Edulytics.Web/Resources/SharedResource.pl.resx"));

        Assert.Equal("Edulytics", en["ProductName"]);
        Assert.Equal("Edulityks", pl["ProductName"]);

        Assert.Equal(
            "/images/brand/edulytics-en.png",
            en["BrandLogoPath"]);

        Assert.Equal(
            "/images/brand/edulityks-pl.png",
            pl["BrandLogoPath"]);
    }

    [Fact]
    public void ApprovedLogoAssets_ArePngAndLargeEnough()
    {
        var root = FindRoot();

        foreach (var relative in new[]
        {
            "src/Edulytics.Web/wwwroot/images/brand/edulytics-en.png",
            "src/Edulytics.Web/wwwroot/images/brand/edulityks-pl.png"
        })
        {
            var path = Path.Combine(root, relative);
            Assert.True(File.Exists(path), relative);

            var bytes = File.ReadAllBytes(path);

            Assert.True(
                bytes.Length > 10_000,
                relative);

            Assert.Equal(
                new byte[]
                {
                    137, 80, 78, 71,
                    13, 10, 26, 10
                },
                bytes[..8]);
        }
    }

    [Fact]
    public void SharedBrandPartial_UsesLocalizedPathAndAccessibleName()
    {
        var root = FindRoot();

        var partial = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Shared/_LocalizedBrand.cshtml"));

        Assert.Contains(
            "BrandLogoPath",
            partial);

        Assert.Contains(
            "ProductName",
            partial);

        Assert.Contains(
            "localized-brand-logo",
            partial);

        Assert.Contains(
            "asp-append-version",
            partial);
    }

    [Fact]
    public void Login_UsesLocalizedBrand()
    {
        var root = FindRoot();

        var login = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Account/Login.cshtml"));

        Assert.Contains(
            "_LocalizedBrand",
            login);

        Assert.Contains(
            "@localizedProductName",
            login);

        Assert.DoesNotContain(
            "auth-brand-mark",
            login);

        Assert.DoesNotContain(
            ">Edulytics</",
            login);

        Assert.DoesNotContain(
            "aria-label=\"Edulytics\"",
            login);
    }

    [Fact]
    public void EverySharedLayoutBrowserTitle_IsCultureAware()
    {
        var root = FindRoot();

        foreach (var name in new[]
        {
            "_AppLayout.cshtml",
            "_AuthLayout.cshtml",
            "_PublicLayout.cshtml"
        })
        {
            var text = File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/Views/Shared",
                    name));

            Assert.Contains(
                "localizedProductName",
                text);

            var titles = Regex.Matches(
                text,
                "<title\\b[^>]*>(.*?)</title>",
                RegexOptions.Singleline |
                RegexOptions.IgnoreCase);

            Assert.NotEmpty(
                titles.Cast<Match>());

            foreach (Match title in titles)
            {
                Assert.Contains(
                    "@localizedProductName",
                    title.Groups[1].Value);

                Assert.DoesNotContain(
                    "Edulytics",
                    title.Groups[1].Value);
            }
        }
    }

    [Fact]
    public void PolishResourceValues_DoNotLeakEnglishProductSpelling()
    {
        var root = FindRoot();

        foreach (var path in Directory.GetFiles(
                     Path.Combine(
                         root,
                         "src/Edulytics.Web/Resources"),
                     "*.pl.resx"))
        {
            var doc = XDocument.Load(path);

            var values = doc.Root!
                .Elements("data")
                .Select(
                    x =>
                        x.Element("value")?.Value
                        ?? string.Empty);

            Assert.DoesNotContain(
                values,
                x => x.Contains(
                    "Edulytics",
                    StringComparison.Ordinal));
        }
    }

    [Fact]
    public void ResponsiveBrandCss_IsLargeAndResponsive()
    {
        var root = FindRoot();

        var css = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/wwwroot/css/site.css"));

        Assert.Contains(
            ".localized-brand-logo",
            css);

        Assert.Contains(
            "width: min(94vw, 34rem)",
            css);

        Assert.Contains(
            "height: auto",
            css);

        Assert.Contains(
            "@media (max-width: 768px)",
            css);

        Assert.Contains(
            "@media (max-width: 420px)",
            css);
    }

    private static Dictionary<string, string> Values(
        string path) =>
        XDocument.Load(path)
            .Root!
            .Elements("data")
            .ToDictionary(
                x => (string)x.Attribute("name")!,
                x => x.Element("value")?.Value
                     ?? string.Empty);

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(
            AppContext.BaseDirectory);

        while (dir is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        dir.FullName,
                        "Edulytics.sln")))
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException(
            "Edulytics solution root not found.");
    }
}
