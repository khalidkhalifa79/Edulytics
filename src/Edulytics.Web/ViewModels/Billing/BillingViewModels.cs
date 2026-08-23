using Edulytics.Services.Billing;

namespace Edulytics.Web.ViewModels.Billing;

public sealed class BillingIndexViewModel
{
    public IReadOnlyList<BillingSchoolDetails> Schools { get; init; } = [];
}
