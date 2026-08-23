using Edulytics.Core.Enums;

namespace Edulytics.Services.Subscriptions;

public static class SubscriptionCommercialPolicy
{
    public const int MinimumCommittedSeats = 500;
    public const int NonRenewalNoticeDays = 30;

    public static bool IsSupportedTerm(SubscriptionTerm term) =>
        term is
            SubscriptionTerm.ThreeMonths or
            SubscriptionTerm.SixMonths or
            SubscriptionTerm.SchoolYearTenMonths;

    public static bool IsSupportedCadence(
        SubscriptionBillingCadence cadence) =>
        cadence is
            SubscriptionBillingCadence.MonthlyInstallments or
            SubscriptionBillingCadence.FullTermUpfront;

    public static int Months(SubscriptionTerm term) =>
        term switch
        {
            SubscriptionTerm.ThreeMonths => 3,
            SubscriptionTerm.SixMonths => 6,
            SubscriptionTerm.SchoolYearTenMonths => 10,
            _ => throw new ArgumentOutOfRangeException(nameof(term))
        };

    public static decimal MonthlyUnitPrice(SubscriptionTerm term) =>
        term switch
        {
            SubscriptionTerm.ThreeMonths => 20m,
            SubscriptionTerm.SixMonths => 15m,
            SubscriptionTerm.SchoolYearTenMonths => 10m,
            _ => throw new ArgumentOutOfRangeException(nameof(term))
        };

    public static bool TryCurrency(
        string countryCode,
        out CommercialCurrency currency)
    {
        switch ((countryCode ?? string.Empty).Trim().ToUpperInvariant())
        {
            case "PL":
                currency = CommercialCurrency.PLN;
                return true;

            case "AE":
                currency = CommercialCurrency.AED;
                return true;

            default:
                currency = default;
                return false;
        }
    }
}
