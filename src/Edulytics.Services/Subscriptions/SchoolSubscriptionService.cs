using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Subscriptions;
using Edulytics.Core.Users;
using Edulytics.Services.Auditing;

namespace Edulytics.Services.Subscriptions;

public sealed class SchoolSubscriptionService
    : ISchoolSubscriptionService
{
    private readonly ISchoolSubscriptionRepository _subscriptions;
    private readonly ISchoolRepository _schools;
    private readonly ISchoolUserRepository _users;
    private readonly IAuditService _audit;
    private readonly IApplicationTransactionManager _transactions;

    public SchoolSubscriptionService(
        ISchoolSubscriptionRepository subscriptions,
        ISchoolRepository schools,
        ISchoolUserRepository users,
        IAuditService audit,
        IApplicationTransactionManager transactions)
    {
        _subscriptions = subscriptions;
        _schools = schools;
        _users = users;
        _audit = audit;
        _transactions = transactions;
    }

    public async Task<
        SubscriptionQueryResult<
            IReadOnlyList<SchoolSubscriptionDetails>>>
        ListAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null)
        {
            return SubscriptionQueryResult<
                IReadOnlyList<SchoolSubscriptionDetails>>
                .Failure(SubscriptionErrorCode.AccessDenied);
        }

        var rows = await _subscriptions.ListAsync(
            cancellationToken);

        var result = new List<SchoolSubscriptionDetails>(
            rows.Count);

        foreach (var row in rows)
        {
            result.Add(
                await MapAsync(
                    row,
                    cancellationToken));
        }

        return SubscriptionQueryResult<
            IReadOnlyList<SchoolSubscriptionDetails>>
            .Success(result);
    }

    public async Task<
        SubscriptionQueryResult<SchoolSubscriptionDetails>>
        GetAsync(
            Guid actorUserId,
            Guid schoolId,
            CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null)
        {
            return SubscriptionQueryResult<
                SchoolSubscriptionDetails>
                .Failure(SubscriptionErrorCode.AccessDenied);
        }

        var subscription =
            await _subscriptions.GetBySchoolAsync(
                schoolId,
                cancellationToken);

        if (subscription is null)
        {
            return SubscriptionQueryResult<
                SchoolSubscriptionDetails>
                .Failure(
                    SubscriptionErrorCode.SubscriptionNotFound);
        }

        return SubscriptionQueryResult<
            SchoolSubscriptionDetails>
            .Success(
                await MapAsync(
                    subscription,
                    cancellationToken));
    }

    public async Task<SubscriptionCommandResult> CreateAsync(
        Guid actorUserId,
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null)
            return Fail(SubscriptionErrorCode.AccessDenied);

        if (!SubscriptionCommercialPolicy
                .IsSupportedTerm(request.Term))
        {
            return Fail(SubscriptionErrorCode.InvalidTerm);
        }

        if (!SubscriptionCommercialPolicy
                .IsSupportedCadence(request.BillingCadence))
        {
            return Fail(
                SubscriptionErrorCode.InvalidBillingCadence);
        }

        if (request.CommittedSeats <
            SubscriptionCommercialPolicy.MinimumCommittedSeats)
        {
            return Fail(
                SubscriptionErrorCode.InvalidCommittedSeats);
        }

        await using var transaction =
            await _transactions.BeginAsync(
                cancellationToken);

        var school = await _schools.GetForUpdateAsync(
            request.SchoolId,
            cancellationToken);

        if (school is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.SchoolNotFound);
        }

        if (school.Status != SchoolStatus.Suspended)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.SchoolMustBeSuspended);
        }

        if (!SubscriptionCommercialPolicy.TryCurrency(
                school.CountryCode,
                out var currency))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.UnsupportedMarket);
        }

        if (await _subscriptions.GetBySchoolAsync(
                school.Id,
                cancellationToken) is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.SubscriptionAlreadyExists);
        }

        var activeStudents =
            await _subscriptions.CountActiveStudentsAsync(
                school.Id,
                cancellationToken);

        if (activeStudents > request.CommittedSeats)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.InvalidCommittedSeats);
        }

        var now = DateTime.UtcNow;

        var subscription =
            new SchoolSubscription
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                Term = request.Term,
                BillingCadence = request.BillingCadence,
                CommercialCurrency = currency,
                PricePerStudentPerMonth =
                    SubscriptionCommercialPolicy
                        .MonthlyUnitPrice(request.Term),
                CommittedSeats = request.CommittedSeats,
                PendingRenewalSeats = null,
                AutoRenew = request.AutoRenew,
                Status =
                    SubscriptionStatus.PendingActivation,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
                RowVersion = []
            };

        var initialSeatChange =
            SeatChange(
                subscription,
                SeatCommitmentChangeType.Initial,
                previousSeats: 0,
                newSeats: subscription.CommittedSeats,
                effectiveAtUtc: now);

        await QueueAuditAsync(
            actor.Id,
            subscription,
            "Subscription.Created",
            oldValues: null,
            new Dictionary<string, object?>
            {
                ["term"] = subscription.Term.ToString(),
                ["billingCadence"] =
                    subscription.BillingCadence.ToString(),
                ["commercialCurrency"] =
                    subscription.CommercialCurrency.ToString(),
                ["pricePerStudentPerMonth"] =
                    subscription.PricePerStudentPerMonth,
                ["committedSeats"] =
                    subscription.CommittedSeats,
                ["autoRenew"] = subscription.AutoRenew,
                ["status"] = subscription.Status.ToString()
            },
            "Commercial subscription created pending activation.",
            cancellationToken);

        var persisted =
            await _subscriptions.AddAsync(
                subscription,
                initialSeatChange,
                cancellationToken);

        if (!persisted.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(persisted.Error));
        }

        await transaction.CommitAsync(cancellationToken);

        return SubscriptionCommandResult.Success(
            await MapAsync(
                subscription,
                cancellationToken));
    }

    public async Task<SubscriptionCommandResult> ActivateAsync(
        Guid actorUserId,
        Guid schoolId,
        DateTime? agreedActivationAtUtc,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null)
            return Fail(SubscriptionErrorCode.AccessDenied);

        await using var transaction =
            await _transactions.BeginAsync(
                cancellationToken);

        var subscription =
            await _subscriptions.GetForUpdateBySchoolAsync(
                schoolId,
                cancellationToken);

        if (subscription is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.SubscriptionNotFound);
        }

        if (subscription.Status !=
            SubscriptionStatus.PendingActivation)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.InvalidState);
        }

        var school =
            await _schools.GetForUpdateAsync(
                schoolId,
                cancellationToken);

        if (school is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.SchoolNotFound);
        }

        if (school.Status != SchoolStatus.Suspended)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.SchoolMustBeSuspended);
        }

        var now = DateTime.UtcNow;
        var activationAt =
            agreedActivationAtUtc ?? now;

        if (activationAt > now.AddMinutes(1))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.InvalidActivationDate);
        }

        var schoolRowVersion =
            school.RowVersion.ToArray();

        subscription.Status = SubscriptionStatus.Active;
        subscription.ActivatedAtUtc = activationAt;
        subscription.CurrentTermStartsAtUtc = activationAt;
        subscription.CurrentTermEndsAtUtc =
            activationAt.AddMonths(
                SubscriptionCommercialPolicy
                    .Months(subscription.Term));
        subscription.SuspendedAtUtc = null;
        subscription.EndedAtUtc = null;
        subscription.UpdatedAtUtc = now;

        school.Status = SchoolStatus.Active;
        school.UpdatedAtUtc = now;

        await QueueAuditAsync(
            actor.Id,
            subscription,
            "Subscription.Activated",
            new Dictionary<string, object?>
            {
                ["status"] =
                    SubscriptionStatus.PendingActivation
                        .ToString()
            },
            new Dictionary<string, object?>
            {
                ["status"] =
                    subscription.Status.ToString(),
                ["activatedAtUtc"] =
                    subscription.ActivatedAtUtc,
                ["currentTermStartsAtUtc"] =
                    subscription.CurrentTermStartsAtUtc,
                ["currentTermEndsAtUtc"] =
                    subscription.CurrentTermEndsAtUtc
            },
            "Subscription activated and school operational access enabled.",
            cancellationToken);

        var saved =
            await _subscriptions.SaveWithSchoolAsync(
                subscription,
                expectedRowVersion,
                school,
                schoolRowVersion,
                cancellationToken: cancellationToken);

        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);

        return SubscriptionCommandResult.Success(
            await MapAsync(
                subscription,
                cancellationToken));
    }

    public async Task<SubscriptionCommandResult> IncreaseSeatsAsync(
        Guid actorUserId,
        Guid schoolId,
        int newCommittedSeats,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null)
            return Fail(SubscriptionErrorCode.AccessDenied);

        await using var transaction =
            await _transactions.BeginAsync(
                cancellationToken);

        var subscription =
            await _subscriptions.GetForUpdateBySchoolAsync(
                schoolId,
                cancellationToken);

        if (subscription is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.SubscriptionNotFound);
        }

        if (subscription.Status ==
            SubscriptionStatus.Ended)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.InvalidState);
        }

        if (newCommittedSeats <=
            subscription.CommittedSeats)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode
                    .SeatIncreaseMustExceedCurrent);
        }

        var now = DateTime.UtcNow;
        var previous =
            subscription.CommittedSeats;

        subscription.CommittedSeats =
            newCommittedSeats;
        subscription.UpdatedAtUtc = now;

        var change =
            SeatChange(
                subscription,
                SeatCommitmentChangeType.Increase,
                previous,
                newCommittedSeats,
                now);

        await QueueAuditAsync(
            actor.Id,
            subscription,
            "Subscription.SeatsIncreased",
            new Dictionary<string, object?>
            {
                ["committedSeats"] = previous
            },
            new Dictionary<string, object?>
            {
                ["committedSeats"] =
                    newCommittedSeats,
                ["effectiveAtUtc"] = now
            },
            "Committed seats increased immediately.",
            cancellationToken);

        var saved =
            await _subscriptions.SaveAsync(
                subscription,
                expectedRowVersion,
                change,
                cancellationToken);

        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);

        return SubscriptionCommandResult.Success(
            await MapAsync(
                subscription,
                cancellationToken));
    }

    public async Task<SubscriptionCommandResult>
        ScheduleRenewalSeatReductionAsync(
            Guid actorUserId,
            Guid schoolId,
            int renewalSeats,
            byte[] expectedRowVersion,
            CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null)
            return Fail(SubscriptionErrorCode.AccessDenied);

        if (renewalSeats <
            SubscriptionCommercialPolicy.MinimumCommittedSeats)
        {
            return Fail(
                SubscriptionErrorCode.RenewalSeatFloor);
        }

        await using var transaction =
            await _transactions.BeginAsync(
                cancellationToken);

        var subscription =
            await _subscriptions.GetForUpdateBySchoolAsync(
                schoolId,
                cancellationToken);

        if (subscription is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.SubscriptionNotFound);
        }

        if (subscription.Status ==
            SubscriptionStatus.Ended)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.InvalidState);
        }

        if (renewalSeats >=
            subscription.CommittedSeats)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode
                    .SeatReductionMustBeLower);
        }

        var previousPending =
            subscription.PendingRenewalSeats;

        subscription.PendingRenewalSeats =
            renewalSeats;
        subscription.UpdatedAtUtc =
            DateTime.UtcNow;

        await QueueAuditAsync(
            actor.Id,
            subscription,
            "Subscription.SeatReductionScheduled",
            new Dictionary<string, object?>
            {
                ["pendingRenewalSeats"] =
                    previousPending
            },
            new Dictionary<string, object?>
            {
                ["pendingRenewalSeats"] =
                    renewalSeats,
                ["currentCommittedSeats"] =
                    subscription.CommittedSeats
            },
            "Seat reduction scheduled for renewal; current commitment unchanged.",
            cancellationToken);

        var saved =
            await _subscriptions.SaveAsync(
                subscription,
                expectedRowVersion,
                cancellationToken: cancellationToken);

        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);

        return SubscriptionCommandResult.Success(
            await MapAsync(
                subscription,
                cancellationToken));
    }

    public async Task<SubscriptionCommandResult> SetAutoRenewAsync(
        Guid actorUserId,
        Guid schoolId,
        bool autoRenew,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null)
            return Fail(SubscriptionErrorCode.AccessDenied);

        await using var transaction =
            await _transactions.BeginAsync(
                cancellationToken);

        var subscription =
            await _subscriptions.GetForUpdateBySchoolAsync(
                schoolId,
                cancellationToken);

        if (subscription is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.SubscriptionNotFound);
        }

        if (subscription.Status ==
            SubscriptionStatus.Ended)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.InvalidState);
        }

        var now = DateTime.UtcNow;

        if (!autoRenew &&
            subscription.AutoRenew &&
            subscription.CurrentTermEndsAtUtc.HasValue &&
            now >
                subscription.CurrentTermEndsAtUtc.Value
                    .AddDays(
                        -SubscriptionCommercialPolicy
                            .NonRenewalNoticeDays))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode
                    .AutoRenewNoticeTooLate);
        }

        var previous =
            subscription.AutoRenew;

        subscription.AutoRenew = autoRenew;
        subscription.NonRenewalRequestedAtUtc =
            autoRenew
                ? null
                : now;
        subscription.UpdatedAtUtc = now;

        await QueueAuditAsync(
            actor.Id,
            subscription,
            "Subscription.AutoRenewChanged",
            new Dictionary<string, object?>
            {
                ["autoRenew"] = previous
            },
            new Dictionary<string, object?>
            {
                ["autoRenew"] =
                    subscription.AutoRenew,
                ["nonRenewalRequestedAtUtc"] =
                    subscription.NonRenewalRequestedAtUtc
            },
            "Subscription auto-renew preference changed.",
            cancellationToken);

        var saved =
            await _subscriptions.SaveAsync(
                subscription,
                expectedRowVersion,
                cancellationToken: cancellationToken);

        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);

        return SubscriptionCommandResult.Success(
            await MapAsync(
                subscription,
                cancellationToken));
    }

    public async Task<SubscriptionCommandResult> RenewAsync(
        Guid actorUserId,
        Guid schoolId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null)
            return Fail(SubscriptionErrorCode.AccessDenied);

        await using var transaction =
            await _transactions.BeginAsync(
                cancellationToken);

        var subscription =
            await _subscriptions.GetForUpdateBySchoolAsync(
                schoolId,
                cancellationToken);

        if (subscription is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.SubscriptionNotFound);
        }

        if (subscription.Status is
                SubscriptionStatus.PendingActivation or
                SubscriptionStatus.Ended ||
            !subscription.CurrentTermEndsAtUtc.HasValue)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.InvalidState);
        }

        var now = DateTime.UtcNow;
        var oldEnd =
            subscription.CurrentTermEndsAtUtc.Value;

        if (now < oldEnd)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.InvalidState);
        }

        var nextSeats =
            subscription.PendingRenewalSeats ??
            subscription.CommittedSeats;

        if (nextSeats <
            SubscriptionCommercialPolicy.MinimumCommittedSeats)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.RenewalSeatFloor);
        }

        var activeStudents =
            await _subscriptions.CountActiveStudentsAsync(
                schoolId,
                cancellationToken);

        if (nextSeats < activeStudents)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode
                    .RenewalBelowActiveStudents);
        }

        var previousSeats =
            subscription.CommittedSeats;

        subscription.CurrentTermStartsAtUtc =
            oldEnd;
        subscription.CurrentTermEndsAtUtc =
            oldEnd.AddMonths(
                SubscriptionCommercialPolicy
                    .Months(subscription.Term));
        subscription.CommittedSeats =
            nextSeats;
        subscription.PendingRenewalSeats =
            null;
        subscription.NonRenewalRequestedAtUtc =
            null;
        subscription.UpdatedAtUtc =
            now;

        SubscriptionSeatChange? seatChange = null;

        if (previousSeats != nextSeats)
        {
            seatChange =
                SeatChange(
                    subscription,
                    SeatCommitmentChangeType
                        .RenewalAdjustment,
                    previousSeats,
                    nextSeats,
                    oldEnd);
        }

        await QueueAuditAsync(
            actor.Id,
            subscription,
            "Subscription.Renewed",
            new Dictionary<string, object?>
            {
                ["currentTermEndsAtUtc"] =
                    oldEnd,
                ["committedSeats"] =
                    previousSeats
            },
            new Dictionary<string, object?>
            {
                ["currentTermStartsAtUtc"] =
                    subscription.CurrentTermStartsAtUtc,
                ["currentTermEndsAtUtc"] =
                    subscription.CurrentTermEndsAtUtc,
                ["committedSeats"] =
                    subscription.CommittedSeats
            },
            "Subscription term renewed; scheduled seat target applied.",
            cancellationToken);

        var saved =
            await _subscriptions.SaveAsync(
                subscription,
                expectedRowVersion,
                seatChange,
                cancellationToken);

        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);

        return SubscriptionCommandResult.Success(
            await MapAsync(
                subscription,
                cancellationToken));
    }

    public Task<SubscriptionCommandResult> SuspendAsync(
        Guid actorUserId,
        Guid schoolId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default) =>
        ChangeOperationalStateAsync(
            actorUserId,
            schoolId,
            expectedRowVersion,
            SubscriptionStatus.Active,
            SubscriptionStatus.Suspended,
            SchoolStatus.Suspended,
            "Subscription.Suspended",
            cancellationToken);

    public async Task<SubscriptionCommandResult> ReactivateAsync(
        Guid actorUserId,
        Guid schoolId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        var entitlement =
            await EvaluateEntitlementsAsync(
                schoolId,
                utcNow: DateTime.UtcNow,
                cancellationToken: cancellationToken);

        if (entitlement.IsCommerciallyManaged &&
            entitlement.CurrentTermEndsAtUtc.HasValue &&
            entitlement.CurrentTermEndsAtUtc.Value <=
                DateTime.UtcNow)
        {
            return Fail(
                SubscriptionErrorCode.SubscriptionExpired);
        }

        return await ChangeOperationalStateAsync(
            actorUserId,
            schoolId,
            expectedRowVersion,
            SubscriptionStatus.Suspended,
            SubscriptionStatus.Active,
            SchoolStatus.Active,
            "Subscription.Reactivated",
            cancellationToken);
    }

    public async Task<SubscriptionCommandResult> EndExpiredAsync(
        Guid actorUserId,
        Guid schoolId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        var actor = await RequirePlatformActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null)
            return Fail(SubscriptionErrorCode.AccessDenied);

        await using var transaction =
            await _transactions.BeginAsync(
                cancellationToken);

        var subscription =
            await _subscriptions.GetForUpdateBySchoolAsync(
                schoolId,
                cancellationToken);

        if (subscription is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.SubscriptionNotFound);
        }

        var now = DateTime.UtcNow;

        if (!subscription.CurrentTermEndsAtUtc.HasValue ||
            subscription.CurrentTermEndsAtUtc.Value > now ||
            subscription.Status is
                SubscriptionStatus.PendingActivation or
                SubscriptionStatus.Ended)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.InvalidState);
        }

        var school =
            await _schools.GetForUpdateAsync(
                schoolId,
                cancellationToken);

        if (school is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.SchoolNotFound);
        }

        var previousStatus =
            subscription.Status;
        var schoolRowVersion =
            school.RowVersion.ToArray();

        subscription.Status =
            SubscriptionStatus.Ended;
        subscription.EndedAtUtc = now;
        subscription.UpdatedAtUtc = now;

        var shouldSuspendSchool =
            school.Status != SchoolStatus.Archived;

        if (shouldSuspendSchool)
        {
            school.Status = SchoolStatus.Suspended;
            school.UpdatedAtUtc = now;
        }

        await QueueAuditAsync(
            actor.Id,
            subscription,
            "Subscription.Ended",
            new Dictionary<string, object?>
            {
                ["status"] =
                    previousStatus.ToString()
            },
            new Dictionary<string, object?>
            {
                ["status"] =
                    subscription.Status.ToString(),
                ["endedAtUtc"] =
                    subscription.EndedAtUtc
            },
            "Expired subscription ended; school operational access disabled.",
            cancellationToken);

        SubscriptionPersistenceResult saved;

        if (shouldSuspendSchool)
        {
            saved =
                await _subscriptions.SaveWithSchoolAsync(
                    subscription,
                    expectedRowVersion,
                    school,
                    schoolRowVersion,
                    cancellationToken: cancellationToken);
        }
        else
        {
            saved =
                await _subscriptions.SaveAsync(
                    subscription,
                    expectedRowVersion,
                    cancellationToken: cancellationToken);
        }

        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);

        return SubscriptionCommandResult.Success(
            await MapAsync(
                subscription,
                cancellationToken));
    }

    public async Task<SubscriptionEntitlementSnapshot>
        EvaluateEntitlementsAsync(
            Guid schoolId,
            int additionalActiveStudents = 0,
            DateTime? utcNow = null,
            CancellationToken cancellationToken = default)
    {
        if (additionalActiveStudents < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(additionalActiveStudents));
        }

        var subscription =
            await _subscriptions.GetBySchoolAsync(
                schoolId,
                cancellationToken);

        if (subscription is null)
        {
            return new SubscriptionEntitlementSnapshot(
                IsCommerciallyManaged: false,
                OperationalAccessAllowed: true,
                SeatCapacityAvailable: true,
                ActiveStudents: 0,
                CommittedSeats: null,
                AvailableSeats: null,
                Status: null,
                CurrentTermEndsAtUtc: null);
        }

        var now =
            utcNow ?? DateTime.UtcNow;

        var activeStudents =
            await _subscriptions.CountActiveStudentsAsync(
                schoolId,
                cancellationToken);

        var available =
            Math.Max(
                0,
                subscription.CommittedSeats -
                activeStudents);

        var termActive =
            subscription.CurrentTermStartsAtUtc.HasValue &&
            subscription.CurrentTermEndsAtUtc.HasValue &&
            subscription.CurrentTermStartsAtUtc.Value <= now &&
            now < subscription.CurrentTermEndsAtUtc.Value;

        var operational =
            subscription.Status ==
                SubscriptionStatus.Active &&
            termActive;

        return new SubscriptionEntitlementSnapshot(
            IsCommerciallyManaged: true,
            OperationalAccessAllowed: operational,
            SeatCapacityAvailable:
                operational &&
                activeStudents +
                    additionalActiveStudents <=
                subscription.CommittedSeats,
            ActiveStudents: activeStudents,
            CommittedSeats:
                subscription.CommittedSeats,
            AvailableSeats:
                available,
            Status:
                subscription.Status,
            CurrentTermEndsAtUtc:
                subscription.CurrentTermEndsAtUtc);
    }

    private async Task<SubscriptionCommandResult>
        ChangeOperationalStateAsync(
            Guid actorUserId,
            Guid schoolId,
            byte[] expectedRowVersion,
            SubscriptionStatus requiredCurrent,
            SubscriptionStatus target,
            SchoolStatus targetSchoolStatus,
            string action,
            CancellationToken cancellationToken)
    {
        var actor = await RequirePlatformActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null)
            return Fail(SubscriptionErrorCode.AccessDenied);

        await using var transaction =
            await _transactions.BeginAsync(
                cancellationToken);

        var subscription =
            await _subscriptions.GetForUpdateBySchoolAsync(
                schoolId,
                cancellationToken);

        if (subscription is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(
                SubscriptionErrorCode.SubscriptionNotFound);
        }

        if (subscription.Status != requiredCurrent)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.InvalidState);
        }

        var school =
            await _schools.GetForUpdateAsync(
                schoolId,
                cancellationToken);

        if (school is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.SchoolNotFound);
        }

        if (school.Status == SchoolStatus.Archived)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(SubscriptionErrorCode.InvalidState);
        }

        var now = DateTime.UtcNow;
        var previous =
            subscription.Status;
        var schoolRowVersion =
            school.RowVersion.ToArray();

        subscription.Status = target;
        subscription.UpdatedAtUtc = now;
        subscription.SuspendedAtUtc =
            target == SubscriptionStatus.Suspended
                ? now
                : null;

        school.Status = targetSchoolStatus;
        school.UpdatedAtUtc = now;

        await QueueAuditAsync(
            actor.Id,
            subscription,
            action,
            new Dictionary<string, object?>
            {
                ["status"] = previous.ToString()
            },
            new Dictionary<string, object?>
            {
                ["status"] =
                    subscription.Status.ToString(),
                ["schoolStatus"] =
                    school.Status.ToString()
            },
            target == SubscriptionStatus.Suspended
                ? "Subscription suspended and school operational access disabled."
                : "Subscription reactivated and school operational access restored.",
            cancellationToken);

        var saved =
            await _subscriptions.SaveWithSchoolAsync(
                subscription,
                expectedRowVersion,
                school,
                schoolRowVersion,
                cancellationToken: cancellationToken);

        if (!saved.Succeeded)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Fail(MapPersistence(saved.Error));
        }

        await transaction.CommitAsync(cancellationToken);

        return SubscriptionCommandResult.Success(
            await MapAsync(
                subscription,
                cancellationToken));
    }

    private async Task<SchoolUserRecord?> RequirePlatformActorAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var actor =
            await _users.GetActorAsync(
                actorUserId,
                cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            actor.SchoolId.HasValue ||
            actor.Roles.Count != 1 ||
            actor.Roles[0] != RoleNames.SuperAdmin)
        {
            return null;
        }

        return actor;
    }

    private async Task<SchoolSubscriptionDetails> MapAsync(
        SchoolSubscription subscription,
        CancellationToken cancellationToken)
    {
        var activeStudents =
            await _subscriptions.CountActiveStudentsAsync(
                subscription.SchoolId,
                cancellationToken);

        return new SchoolSubscriptionDetails(
            subscription.Id,
            subscription.SchoolId,
            subscription.Term,
            subscription.BillingCadence,
            subscription.CommercialCurrency,
            subscription.PricePerStudentPerMonth,
            subscription.CommittedSeats,
            subscription.PendingRenewalSeats,
            activeStudents,
            Math.Max(
                0,
                subscription.CommittedSeats -
                activeStudents),
            subscription.AutoRenew,
            subscription.NonRenewalRequestedAtUtc,
            subscription.Status,
            subscription.ActivatedAtUtc,
            subscription.CurrentTermStartsAtUtc,
            subscription.CurrentTermEndsAtUtc,
            subscription.SuspendedAtUtc,
            subscription.EndedAtUtc,
            subscription.RowVersion.ToArray());
    }

    private async Task QueueAuditAsync(
        Guid actorUserId,
        SchoolSubscription subscription,
        string action,
        IReadOnlyDictionary<string, object?>? oldValues,
        IReadOnlyDictionary<string, object?>? newValues,
        string summary,
        CancellationToken cancellationToken)
    {
        await _audit.QueueAsync(
            new AuditEvent(
                SchoolId: subscription.SchoolId,
                Action: action,
                EntityType: "SchoolSubscription",
                EntityId: subscription.Id.ToString("D"),
                Feature: "Subscriptions",
                OldValues: oldValues,
                NewValues: newValues,
                ResultSummary: summary,
                ActorUserIdOverride: actorUserId,
                ActorRoleOverride: RoleNames.SuperAdmin),
            cancellationToken);
    }

    private static SubscriptionSeatChange SeatChange(
        SchoolSubscription subscription,
        SeatCommitmentChangeType changeType,
        int previousSeats,
        int newSeats,
        DateTime effectiveAtUtc) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = subscription.SchoolId,
            SubscriptionId = subscription.Id,
            ChangeType = changeType,
            PreviousSeats = previousSeats,
            NewSeats = newSeats,
            EffectiveAtUtc = effectiveAtUtc,
            CreatedAtUtc = DateTime.UtcNow
        };

    private static SubscriptionCommandResult Fail(
        SubscriptionErrorCode error) =>
        SubscriptionCommandResult.Failure(error);

    private static SubscriptionErrorCode MapPersistence(
        SubscriptionPersistenceError error) =>
        error == SubscriptionPersistenceError.Concurrency
            ? SubscriptionErrorCode.ConcurrencyConflict
            : SubscriptionErrorCode.PersistenceError;
}
