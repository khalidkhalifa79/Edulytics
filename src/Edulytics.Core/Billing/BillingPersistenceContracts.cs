namespace Edulytics.Core.Billing;

public enum BillingPersistenceError
{
    None = 0,
    Concurrency = 1,
    Constraint = 2,
    Unknown = 3
}

public sealed record BillingPersistenceResult(
    bool Succeeded,
    BillingPersistenceError Error)
{
    public static BillingPersistenceResult Success() =>
        new(true, BillingPersistenceError.None);

    public static BillingPersistenceResult Failure(
        BillingPersistenceError error) =>
        new(false, error);
}
