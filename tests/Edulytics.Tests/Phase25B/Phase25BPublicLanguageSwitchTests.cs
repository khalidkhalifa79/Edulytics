namespace Edulytics.Tests.Phase25B;

public sealed class Phase25BPublicLanguageSwitchTests
{
    [Fact]
    public void PublicLayout_DoesNotRenderFloatingLanguageSwitcher()
    {
        var root = FindRoot();

        var layout = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Shared/"
                + "_PublicLayout.cshtml"));

        Assert.DoesNotContain(
            "<partial name=\"_PublicLanguageSwitcher\" />",
            layout,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RequestDemo_RendersBrandAndLanguageControlInsideCard()
    {
        var root = FindRoot();

        var view = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Onboarding/"
                + "Index.cshtml"));

        Assert.Contains(
            "class=\"onboarding-public-header\"",
            view,
            StringComparison.Ordinal);

        Assert.Contains(
            "<partial name=\"_LocalizedBrand\" />",
            view,
            StringComparison.Ordinal);

        Assert.Contains(
            "<partial name=\"_PublicLanguageSwitcher\" />",
            view,
            StringComparison.Ordinal);

        var headerIndex = view.IndexOf(
            "onboarding-public-header",
            StringComparison.Ordinal);

        var languageIndex = view.IndexOf(
            "_PublicLanguageSwitcher",
            StringComparison.Ordinal);

        var formIndex = view.IndexOf(
            "onboarding-request-form",
            StringComparison.Ordinal);

        Assert.True(headerIndex >= 0);
        Assert.True(languageIndex > headerIndex);
        Assert.True(formIndex > languageIndex);
    }

    [Fact]
    public void ThankYouPage_UsesSamePublicHeader()
    {
        var root = FindRoot();

        var view = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Onboarding/"
                + "Thanks.cshtml"));

        Assert.Contains(
            "onboarding-public-header",
            view,
            StringComparison.Ordinal);

        Assert.Contains(
            "_LocalizedBrand",
            view,
            StringComparison.Ordinal);

        Assert.Contains(
            "_PublicLanguageSwitcher",
            view,
            StringComparison.Ordinal);
    }

    [Fact]
    public void LanguageSwitcher_PreservesSafeSamePageReturn()
    {
        var root = FindRoot();

        var partial = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Shared/"
                + "_PublicLanguageSwitcher.cshtml"));

        var controller = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Controllers/"
                + "HomeController.cs"));

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
    public void RequestDemo_HasPolishedResponsiveVisualContract()
    {
        var root = FindRoot();

        var css = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/wwwroot/css/site.css"));

        foreach (var required in new[]
        {
            ".onboarding-public-header",
            ".onboarding-brand-wrap",
            ".public-language-switcher",
            ".public-language-option.is-active",
            ".onboarding-public-copy",
            ".onboarding-seat-note",
            ".onboarding-request-form",
            ".onboarding-field",
            ".onboarding-consent",
            ".onboarding-submit",
            "@media (max-width: 767px)",
            "@media (max-width: 420px)"
        })
        {
            Assert.Contains(
                required,
                css,
                StringComparison.Ordinal);
        }
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
