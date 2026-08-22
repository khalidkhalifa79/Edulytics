using Edulytics.Services.Onboarding;

namespace Edulytics.Tests.Phase25B;

public sealed class Phase25BSupportedCountryTests
{
    [Theory]
    [InlineData("PL")]
    [InlineData("pl")]
    [InlineData("AE")]
    [InlineData("ae")]
    public void SupportedCountries_AreAccepted(
        string code) =>
        Assert.True(
            SupportedCustomerCountries.IsSupported(code));

    [Theory]
    [InlineData("06")]
    [InlineData("EG")]
    [InlineData("US")]
    [InlineData("")]
    [InlineData(null)]
    public void UnsupportedCountries_AreRejected(
        string? code) =>
        Assert.False(
            SupportedCustomerCountries.IsSupported(code));

    [Fact]
    public void Service_EnforcesSameCountryAllowList()
    {
        var root = FindRoot();

        var source = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Services/Onboarding/"
                + "CustomerOnboardingService.cs"));

        Assert.Contains(
            "SupportedCustomerCountries.IsSupported(request.CountryCode)",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "OnboardingErrorCode.UnsupportedCountry",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void RequestDemo_UsesOnlySupportedCountryOptions()
    {
        var root = FindRoot();

        var view = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Onboarding/"
                + "Index.cshtml"));

        Assert.Contains(
            "<select asp-for=\"CountryCode\"",
            view,
            StringComparison.Ordinal);

        Assert.Contains(
            "<option value=\"PL\">",
            view,
            StringComparison.Ordinal);

        Assert.Contains(
            "<option value=\"AE\">",
            view,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "maxlength=\"10\"",
            view,
            StringComparison.Ordinal);
    }

    private static string FindRoot()
    {
        var d =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (d is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        d.FullName,
                        "Edulytics.sln")))
            {
                return d.FullName;
            }

            d = d.Parent;
        }

        throw new DirectoryNotFoundException();
    }
}
