namespace Edulytics.Services.Subscriptions;

public interface ISchoolSubscriptionService
{
    Task<SubscriptionQueryResult<IReadOnlyList<SchoolSubscriptionDetails>>>
        ListAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default);

    Task<SubscriptionQueryResult<SchoolSubscriptionDetails>>
        GetAsync(
            Guid actorUserId,
            Guid schoolId,
            CancellationToken cancellationToken = default);

    Task<SubscriptionCommandResult> CreateAsync(
        Guid actorUserId,
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    Task<SubscriptionCommandResult> ActivateAsync(
        Guid actorUserId,
        Guid schoolId,
        DateTime? agreedActivationAtUtc,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<SubscriptionCommandResult> IncreaseSeatsAsync(
        Guid actorUserId,
        Guid schoolId,
        int newCommittedSeats,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<SubscriptionCommandResult> ScheduleRenewalSeatReductionAsync(
        Guid actorUserId,
        Guid schoolId,
        int renewalSeats,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<SubscriptionCommandResult> SetAutoRenewAsync(
        Guid actorUserId,
        Guid schoolId,
        bool autoRenew,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<SubscriptionCommandResult> RenewAsync(
        Guid actorUserId,
        Guid schoolId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<SubscriptionCommandResult> SuspendAsync(
        Guid actorUserId,
        Guid schoolId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<SubscriptionCommandResult> ReactivateAsync(
        Guid actorUserId,
        Guid schoolId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<SubscriptionCommandResult> EndExpiredAsync(
        Guid actorUserId,
        Guid schoolId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    Task<SubscriptionEntitlementSnapshot> EvaluateEntitlementsAsync(
        Guid schoolId,
        int additionalActiveStudents = 0,
        DateTime? utcNow = null,
        CancellationToken cancellationToken = default);
}
