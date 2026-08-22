namespace Edulytics.Tests.Phase25B;

public sealed class Phase25BPublicLanguageSwitchTests
{
    [Fact]
    public void PublicLayout_ExposesReusableLanguageSwitcher()
    {
        var root = FindRoot();

        var layout = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Shared/"
                + "_PublicLayout.cshtml"));

        var partial = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Shared/"
                + "_PublicLanguageSwitcher.cshtml"));

        Assert.Contains(
            "<partial name=\"_PublicLanguageSwitcher\" />",
            layout,
            StringComparison.Ordinal);

        Assert.Contains(
            "name=\"culture\"",
            partial,
            StringComparison.Ordinal);

        Assert.Contains(
            "value=\"en\"",
            partial,
            StringComparison.Ordinal);

        Assert.Contains(
            "value=\"pl\"",
            partial,
            StringComparison.Ordinal);

        Assert.Contains(
            "name=\"returnUrl\"",
            partial,
            StringComparison.Ordinal);

        Assert.Contains(
            "asp-action=\"SetCulture\"",
            partial,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SetCulture_ReturnUrl_IsRestrictedToLocalUrls()
    {
        var root = FindRoot();

        var controller = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Controllers/"
                + "HomeController.cs"));

        Assert.Contains(
            "string? returnUrl",
            controller,
            StringComparison.Ordinal);

        Assert.Contains(
            "Url.IsLocalUrl(returnUrl)",
            controller,
            StringComparison.Ordinal);

        Assert.Contains(
            "LocalRedirect(returnUrl)",
            controller,
            StringComparison.Ordinal);

        Assert.Contains(
            "[ValidateAntiForgeryToken]",
            controller,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PublicLanguageSwitcher_IsResponsiveAndAccessible()
    {
        var root = FindRoot();

        var partial = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Shared/"
                + "_PublicLanguageSwitcher.cshtml"));

        var css = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/wwwroot/css/site.css"));

        Assert.Contains(
            "aria-label=\"Language\"",
            partial,
            StringComparison.Ordinal);

        Assert.Contains(
            "aria-pressed=",
            partial,
            StringComparison.Ordinal);

        Assert.Contains(
            ".public-language-switcher",
            css,
            StringComparison.Ordinal);

        Assert.Contains(
            ".public-language-option",
            css,
            StringComparison.Ordinal);

        Assert.Contains(
            "@media (max-width: 420px)",
            css,
            StringComparison.Ordinal);
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

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Edulytics solution root not found.");
    }
}
