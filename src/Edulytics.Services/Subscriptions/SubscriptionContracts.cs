using Edulytics.Core.Enums;

namespace Edulytics.Services.Subscriptions;

public enum SubscriptionErrorCode
{
    None = 0,
    AccessDenied = 1,
    SchoolNotFound = 2,
    SchoolMustBeSuspended = 3,
    UnsupportedMarket = 4,
    InvalidTerm = 5,
    InvalidBillingCadence = 6,
    InvalidCommittedSeats = 7,
    SubscriptionAlreadyExists = 8,
    SubscriptionNotFound = 9,
    InvalidState = 10,
    InvalidActivationDate = 11,
    SeatIncreaseMustExceedCurrent = 12,
    SeatReductionMustBeLower = 13,
    RenewalSeatFloor = 14,
    RenewalBelowActiveStudents = 15,
    AutoRenewNoticeTooLate = 16,
    SubscriptionExpired = 17,
    ConcurrencyConflict = 18,
    PersistenceError = 19
}

public sealed record CreateSubscriptionRequest(
    Guid SchoolId,
    SubscriptionTerm Term,
    SubscriptionBillingCadence BillingCadence,
    int CommittedSeats,
    bool AutoRenew);

public sealed record SchoolSubscriptionDetails(
    Guid Id,
    Guid SchoolId,
    SubscriptionTerm Term,
    SubscriptionBillingCadence BillingCadence,
    CommercialCurrency CommercialCurrency,
    decimal PricePerStudentPerMonth,
    int CommittedSeats,
    int? PendingRenewalSeats,
    int ActiveStudents,
    int AvailableSeats,
    bool AutoRenew,
    DateTime? NonRenewalRequestedAtUtc,
    SubscriptionStatus Status,
    DateTime? ActivatedAtUtc,
    DateTime? CurrentTermStartsAtUtc,
    DateTime? CurrentTermEndsAtUtc,
    DateTime? SuspendedAtUtc,
    DateTime? EndedAtUtc,
    byte[] RowVersion);

public sealed record SubscriptionCommandResult(
    bool Succeeded,
    SubscriptionErrorCode Error,
    SchoolSubscriptionDetails? Subscription = null)
{
    public static SubscriptionCommandResult Success(
        SchoolSubscriptionDetails subscription) =>
        new(true, SubscriptionErrorCode.None, subscription);

    public static SubscriptionCommandResult Failure(
        SubscriptionErrorCode error) =>
        new(false, error);
}

public sealed record SubscriptionQueryResult<T>(
    T? Value,
    SubscriptionErrorCode Error)
    where T : class
{
    public static SubscriptionQueryResult<T> Success(T value) =>
        new(value, SubscriptionErrorCode.None);

    public static SubscriptionQueryResult<T> Failure(
        SubscriptionErrorCode error) =>
        new(null, error);
}

public sealed record SubscriptionEntitlementSnapshot(
    bool IsCommerciallyManaged,
    bool OperationalAccessAllowed,
    bool SeatCapacityAvailable,
    int ActiveStudents,
    int? CommittedSeats,
    int? AvailableSeats,
    SubscriptionStatus? Status,
    DateTime? CurrentTermEndsAtUtc);
