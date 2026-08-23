namespace Edulytics.Core.Subscriptions;

public enum SubscriptionPersistenceError
{
    None = 0,
    NotFound = 1,
    Concurrency = 2,
    Constraint = 3,
    Unknown = 4
}

public sealed record SubscriptionPersistenceResult(
    bool Succeeded,
    SubscriptionPersistenceError Error)
{
    public static SubscriptionPersistenceResult Success() =>
        new(true, SubscriptionPersistenceError.None);

    public static SubscriptionPersistenceResult Failure(
        SubscriptionPersistenceError error) =>
        new(false, error);
}
