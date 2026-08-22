namespace Edulytics.Services.Onboarding;

public static class SupportedCustomerCountries
{
    public const string Poland = "PL";
    public const string UnitedArabEmirates = "AE";

    private static readonly HashSet<string> Codes =
        new(StringComparer.OrdinalIgnoreCase)
        {
            Poland,
            UnitedArabEmirates
        };

    public static bool IsSupported(string? countryCode) =>
        !string.IsNullOrWhiteSpace(countryCode) &&
        Codes.Contains(countryCode.Trim());
}
