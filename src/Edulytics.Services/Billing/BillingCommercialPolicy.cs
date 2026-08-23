using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Services.Subscriptions;

namespace Edulytics.Services.Billing;

public sealed record BillingProration(
    DateTime PeriodStartsAtUtc,
    DateTime PeriodEndsAtUtc,
    int NumeratorDays,
    int DenominatorDays,
    int FullMonthsAfter,
    int SeatDelta,
    decimal NetAmount);

public sealed record BillingAgingState(
    BillingInvoiceStatus EffectiveStatus,
    bool InGracePeriod,
    bool SuspensionEligible,
    decimal OutstandingAmount);

public static class BillingCommercialPolicy
{
    public const int InvoiceTermDays = 14;
    public const int GraceDays = 7;

    public static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    public static int BillableSeats(int committedSeats) =>
        Math.Max(SubscriptionCommercialPolicy.MinimumCommittedSeats, committedSeats);

    public static decimal InitialNetAmount(SchoolSubscription subscription)
    {
        var months = subscription.BillingCadence == SubscriptionBillingCadence.FullTermUpfront
            ? SubscriptionCommercialPolicy.Months(subscription.Term)
            : 1;

        return RoundMoney(
            BillableSeats(subscription.CommittedSeats) *
            subscription.PricePerStudentPerMonth *
            months);
    }

    public static decimal RenewalNetAmount(
        SchoolSubscription subscription,
        int renewalSeats)
    {
        var months = subscription.BillingCadence == SubscriptionBillingCadence.FullTermUpfront
            ? SubscriptionCommercialPolicy.Months(subscription.Term)
            : 1;

        return RoundMoney(
            BillableSeats(renewalSeats) *
            subscription.PricePerStudentPerMonth *
            months);
    }

    public static DateTime DueDate(DateTime issueDateUtc) =>
        issueDateUtc.AddDays(InvoiceTermDays);

    public static DateTime GraceEnds(DateTime dueDateUtc) =>
        dueDateUtc.AddDays(GraceDays);

    public static BillingProration SeatIncreaseProration(
        SchoolSubscription subscription,
        SubscriptionSeatChange change)
    {
        if (change.ChangeType != SeatCommitmentChangeType.Increase)
            throw new ArgumentOutOfRangeException(nameof(change));

        if (!subscription.CurrentTermStartsAtUtc.HasValue ||
            !subscription.CurrentTermEndsAtUtc.HasValue)
        {
            throw new InvalidOperationException("Subscription term dates are required for proration.");
        }

        var termStart = subscription.CurrentTermStartsAtUtc.Value;
        var termEnd = subscription.CurrentTermEndsAtUtc.Value;

        if (change.EffectiveAtUtc < termStart || change.EffectiveAtUtc >= termEnd)
            throw new InvalidOperationException("Seat increase is outside the current term.");

        var periodStart = termStart;
        var periodEnd = termStart.AddMonths(1);

        while (change.EffectiveAtUtc >= periodEnd && periodEnd < termEnd)
        {
            periodStart = periodEnd;
            periodEnd = periodStart.AddMonths(1);
        }

        if (periodEnd > termEnd)
            periodEnd = termEnd;

        var denominator = Math.Max(1, (periodEnd.Date - periodStart.Date).Days);
        var numerator = Math.Max(1, (periodEnd.Date - change.EffectiveAtUtc.Date).Days);
        numerator = Math.Min(numerator, denominator);

        var delta = change.NewSeats - change.PreviousSeats;
        if (delta <= 0)
            throw new InvalidOperationException("Seat delta must be positive.");

        var fullMonthsAfter = 0;
        if (subscription.BillingCadence == SubscriptionBillingCadence.FullTermUpfront)
        {
            var cursor = periodEnd;
            while (cursor < termEnd)
            {
                var next = cursor.AddMonths(1);
                if (next > termEnd)
                    next = termEnd;
                if (next > cursor)
                    fullMonthsAfter++;
                cursor = next;
            }
        }

        var amount = RoundMoney(
            delta *
            subscription.PricePerStudentPerMonth *
            ((decimal)numerator / denominator + fullMonthsAfter));

        return new BillingProration(
            periodStart,
            periodEnd,
            numerator,
            denominator,
            fullMonthsAfter,
            delta,
            amount);
    }

    public static BillingAgingState Aging(
        BillingInvoice invoice,
        DateTime utcNow)
    {
        var outstanding = RoundMoney(Math.Max(0m, invoice.TotalAmount - invoice.PaidAmount));

        if (invoice.Status is BillingInvoiceStatus.Cancelled or
            BillingInvoiceStatus.Refunded or
            BillingInvoiceStatus.PartiallyRefunded)
        {
            return new BillingAgingState(invoice.Status, false, false, outstanding);
        }

        if (outstanding <= 0m)
            return new BillingAgingState(BillingInvoiceStatus.Paid, false, false, 0m);

        if (utcNow > invoice.GraceEndsAtUtc)
            return new BillingAgingState(BillingInvoiceStatus.Overdue, false, true, outstanding);

        if (utcNow > invoice.DueDateUtc)
            return new BillingAgingState(BillingInvoiceStatus.Overdue, true, false, outstanding);

        if (invoice.PaidAmount > 0m)
            return new BillingAgingState(BillingInvoiceStatus.PartiallyPaid, false, false, outstanding);

        return new BillingAgingState(BillingInvoiceStatus.Due, false, false, outstanding);
    }
}
