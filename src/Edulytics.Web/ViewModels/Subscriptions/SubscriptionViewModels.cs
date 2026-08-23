using Edulytics.Services.Subscriptions;

namespace Edulytics.Web.ViewModels.Subscriptions;

public sealed record SubscriptionSchoolOptionViewModel(
    Guid Id,
    string Name,
    string SchoolCode,
    string CountryCode);

public sealed record SubscriptionRowViewModel(
    SchoolSubscriptionDetails Subscription,
    string SchoolName,
    string SchoolCode,
    string CountryCode)
{
    public string RowVersionBase64 =>
        Convert.ToBase64String(
            Subscription.RowVersion);
}

public sealed class SubscriptionIndexViewModel
{
    public IReadOnlyList<SubscriptionRowViewModel>
        Subscriptions { get; init; } = [];

    public IReadOnlyList<SubscriptionSchoolOptionViewModel>
        EligibleSchools { get; init; } = [];
}
