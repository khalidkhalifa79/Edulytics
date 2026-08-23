namespace Edulytics.Core.Enums;

public enum SubscriptionTerm
{
    ThreeMonths = 3,
    SixMonths = 6,
    SchoolYearTenMonths = 10
}

public enum SubscriptionStatus
{
    PendingActivation = 1,
    Active = 2,
    Suspended = 3,
    Ended = 4
}

public enum SubscriptionBillingCadence
{
    MonthlyInstallments = 1,
    FullTermUpfront = 2
}

public enum CommercialCurrency
{
    PLN = 1,
    AED = 2
}

public enum SeatCommitmentChangeType
{
    Initial = 1,
    Increase = 2,
    RenewalAdjustment = 3
}
