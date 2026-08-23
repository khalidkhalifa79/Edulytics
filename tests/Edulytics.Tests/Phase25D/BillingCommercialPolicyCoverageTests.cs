using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Services.Billing;

namespace Edulytics.Tests.Phase25D;

public sealed class BillingCommercialPolicyCoverageTests
{
    [Fact]
    public void RenewalMonthly_UsesFloorAndSingleMonth()
    {
        var subscription = Subscription(
            SubscriptionBillingCadence.MonthlyInstallments,
            SubscriptionTerm.ThreeMonths,
            price: 20m,
            committedSeats: 200);

        Assert.Equal(
            10000m,
            BillingCommercialPolicy.RenewalNetAmount(subscription, 200));
    }

    [Fact]
    public void RenewalUpfront_UsesRequestedSeatsAndWholeTerm()
    {
        var subscription = Subscription(
            SubscriptionBillingCadence.FullTermUpfront,
            SubscriptionTerm.SixMonths,
            price: 15m,
            committedSeats: 700);

        Assert.Equal(
            63000m,
            BillingCommercialPolicy.RenewalNetAmount(subscription, 700));
    }

    [Fact]
    public void Proration_RejectsNonIncreaseChange()
    {
        var subscription = Active(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));

        var change = Change(
            subscription,
            SeatCommitmentChangeType.RenewalAdjustment,
            new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            500,
            550);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => BillingCommercialPolicy.SeatIncreaseProration(subscription, change));
    }

    [Fact]
    public void Proration_RejectsMissingTermStart()
    {
        var subscription = Active(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        subscription.CurrentTermStartsAtUtc = null;

        var change = Change(
            subscription,
            SeatCommitmentChangeType.Increase,
            new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            500,
            550);

        Assert.Throws<InvalidOperationException>(
            () => BillingCommercialPolicy.SeatIncreaseProration(subscription, change));
    }

    [Fact]
    public void Proration_RejectsMissingTermEnd()
    {
        var subscription = Active(
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        subscription.CurrentTermEndsAtUtc = null;

        var change = Change(
            subscription,
            SeatCommitmentChangeType.Increase,
            new DateTime(2026, 1, 10, 0, 0, 0, DateTimeKind.Utc),
            500,
            550);

        Assert.Throws<InvalidOperationException>(
            () => BillingCommercialPolicy.SeatIncreaseProration(subscription, change));
    }

    [Fact]
    public void Proration_RejectsChangeBeforeTerm()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = Active(start, start.AddMonths(3));

        var change = Change(
            subscription,
            SeatCommitmentChangeType.Increase,
            start.AddDays(-1),
            500,
            550);

        Assert.Throws<InvalidOperationException>(
            () => BillingCommercialPolicy.SeatIncreaseProration(subscription, change));
    }

    [Fact]
    public void Proration_RejectsChangeAtTermEnd()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(3);
        var subscription = Active(start, end);

        var change = Change(
            subscription,
            SeatCommitmentChangeType.Increase,
            end,
            500,
            550);

        Assert.Throws<InvalidOperationException>(
            () => BillingCommercialPolicy.SeatIncreaseProration(subscription, change));
    }

    [Fact]
    public void Proration_RejectsNonPositiveSeatDelta()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = Active(start, start.AddMonths(3));

        var change = Change(
            subscription,
            SeatCommitmentChangeType.Increase,
            start.AddDays(5),
            550,
            550);

        Assert.Throws<InvalidOperationException>(
            () => BillingCommercialPolicy.SeatIncreaseProration(subscription, change));
    }

    [Fact]
    public void MonthlyProration_WalksAcrossAnchoredMonths()
    {
        var start = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var subscription = Active(start, start.AddMonths(6));

        var change = Change(
            subscription,
            SeatCommitmentChangeType.Increase,
            new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc),
            500,
            525);

        var result =
            BillingCommercialPolicy.SeatIncreaseProration(subscription, change);

        Assert.Equal(
            new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
            result.PeriodStartsAtUtc);
        Assert.Equal(
            new DateTime(2026, 5, 15, 0, 0, 0, DateTimeKind.Utc),
            result.PeriodEndsAtUtc);
        Assert.Equal(25, result.NumeratorDays);
        Assert.Equal(30, result.DenominatorDays);
        Assert.Equal(0, result.FullMonthsAfter);
    }

    [Fact]
    public void MonthlyProration_TruncatesFinalPartialPeriodAtTermEnd()
    {
        var start = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = Active(start, end);

        var change = Change(
            subscription,
            SeatCommitmentChangeType.Increase,
            new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc),
            500,
            525);

        var result =
            BillingCommercialPolicy.SeatIncreaseProration(subscription, change);

        Assert.Equal(end, result.PeriodEndsAtUtc);
        Assert.Equal(12, result.NumeratorDays);
        Assert.Equal(17, result.DenominatorDays);
    }

    [Fact]
    public void UpfrontProration_HandlesFinalPartialContractPeriod()
    {
        var start = new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = Active(
            start,
            end,
            SubscriptionBillingCadence.FullTermUpfront);

        var change = Change(
            subscription,
            SeatCommitmentChangeType.Increase,
            new DateTime(2026, 2, 20, 0, 0, 0, DateTimeKind.Utc),
            500,
            525);

        var result =
            BillingCommercialPolicy.SeatIncreaseProration(subscription, change);

        Assert.Equal(1, result.FullMonthsAfter);
        Assert.True(result.NetAmount > 0m);
    }

    [Fact]
    public void Aging_Cancelled_RemainsCancelled()
    {
        var now = UtcNow();
        var invoice = Invoice(BillingInvoiceStatus.Cancelled, 100m, 0m, now);

        var result = BillingCommercialPolicy.Aging(invoice, now.AddDays(30));

        Assert.Equal(BillingInvoiceStatus.Cancelled, result.EffectiveStatus);
        Assert.False(result.InGracePeriod);
        Assert.False(result.SuspensionEligible);
    }

    [Fact]
    public void Aging_Refunded_RemainsRefunded()
    {
        var now = UtcNow();
        var invoice = Invoice(BillingInvoiceStatus.Refunded, 100m, 100m, now);
        invoice.RefundedAmount = 100m;

        var result = BillingCommercialPolicy.Aging(invoice, now.AddDays(30));

        Assert.Equal(BillingInvoiceStatus.Refunded, result.EffectiveStatus);
        Assert.False(result.SuspensionEligible);
    }

    [Fact]
    public void Aging_PartiallyRefunded_RemainsPartiallyRefunded()
    {
        var now = UtcNow();
        var invoice = Invoice(
            BillingInvoiceStatus.PartiallyRefunded,
            100m,
            100m,
            now);
        invoice.RefundedAmount = 25m;

        var result = BillingCommercialPolicy.Aging(invoice, now.AddDays(30));

        Assert.Equal(
            BillingInvoiceStatus.PartiallyRefunded,
            result.EffectiveStatus);
        Assert.False(result.SuspensionEligible);
    }

    [Fact]
    public void Aging_FullyPaidDueInvoice_BecomesPaid()
    {
        var now = UtcNow();
        var invoice = Invoice(BillingInvoiceStatus.Due, 100m, 100m, now);

        var result = BillingCommercialPolicy.Aging(invoice, now);

        Assert.Equal(BillingInvoiceStatus.Paid, result.EffectiveStatus);
        Assert.Equal(0m, result.OutstandingAmount);
    }

    [Fact]
    public void Aging_OverdueInsideGrace_IsNotSuspensionEligible()
    {
        var now = UtcNow();
        var invoice = Invoice(BillingInvoiceStatus.Due, 100m, 0m, now);

        var result =
            BillingCommercialPolicy.Aging(invoice, now.AddDays(16));

        Assert.Equal(BillingInvoiceStatus.Overdue, result.EffectiveStatus);
        Assert.True(result.InGracePeriod);
        Assert.False(result.SuspensionEligible);
    }

    [Fact]
    public void Aging_OverdueAfterGrace_IsSuspensionEligible()
    {
        var now = UtcNow();
        var invoice = Invoice(BillingInvoiceStatus.Due, 100m, 0m, now);

        var result =
            BillingCommercialPolicy.Aging(invoice, now.AddDays(22));

        Assert.Equal(BillingInvoiceStatus.Overdue, result.EffectiveStatus);
        Assert.False(result.InGracePeriod);
        Assert.True(result.SuspensionEligible);
    }

    [Fact]
    public void Aging_PartiallyPaidBeforeDue_RemainsPartiallyPaid()
    {
        var now = UtcNow();
        var invoice = Invoice(
            BillingInvoiceStatus.PartiallyPaid,
            100m,
            25m,
            now);

        var result = BillingCommercialPolicy.Aging(invoice, now);

        Assert.Equal(
            BillingInvoiceStatus.PartiallyPaid,
            result.EffectiveStatus);
        Assert.Equal(75m, result.OutstandingAmount);
    }

    [Fact]
    public void Aging_UnpaidBeforeDue_IsDue()
    {
        var now = UtcNow();
        var invoice = Invoice(BillingInvoiceStatus.Pending, 100m, 0m, now);

        var result = BillingCommercialPolicy.Aging(invoice, now);

        Assert.Equal(BillingInvoiceStatus.Due, result.EffectiveStatus);
        Assert.False(result.InGracePeriod);
        Assert.False(result.SuspensionEligible);
    }

    private static SchoolSubscription Subscription(
        SubscriptionBillingCadence cadence,
        SubscriptionTerm term,
        decimal price,
        int committedSeats) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = Guid.NewGuid(),
            Status = SubscriptionStatus.Active,
            BillingCadence = cadence,
            Term = term,
            CommercialCurrency = CommercialCurrency.PLN,
            PricePerStudentPerMonth = price,
            CommittedSeats = committedSeats
        };

    private static SchoolSubscription Active(
        DateTime start,
        DateTime end,
        SubscriptionBillingCadence cadence =
            SubscriptionBillingCadence.MonthlyInstallments)
    {
        var subscription = Subscription(
            cadence,
            SubscriptionTerm.ThreeMonths,
            20m,
            500);
        subscription.CurrentTermStartsAtUtc = start;
        subscription.CurrentTermEndsAtUtc = end;
        return subscription;
    }

    private static SubscriptionSeatChange Change(
        SchoolSubscription subscription,
        SeatCommitmentChangeType type,
        DateTime effective,
        int previous,
        int next) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = subscription.SchoolId,
            SubscriptionId = subscription.Id,
            ChangeType = type,
            PreviousSeats = previous,
            NewSeats = next,
            EffectiveAtUtc = effective,
            CreatedAtUtc = effective
        };

    private static DateTime UtcNow() =>
        new(2026, 8, 23, 7, 0, 0, DateTimeKind.Utc);

    private static BillingInvoice Invoice(
        BillingInvoiceStatus status,
        decimal total,
        decimal paid,
        DateTime issue) =>
        new()
        {
            Status = status,
            TotalAmount = total,
            PaidAmount = paid,
            DueDateUtc = issue.AddDays(14),
            GraceEndsAtUtc = issue.AddDays(21)
        };
}
