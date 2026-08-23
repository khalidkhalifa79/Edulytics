using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Services.Billing;

namespace Edulytics.Tests.Phase25D;

public sealed class BillingCommercialPolicyTests
{
    [Fact]
    public void BillableSeats_Enforces500Floor()
    {
        Assert.Equal(500, BillingCommercialPolicy.BillableSeats(0));
        Assert.Equal(500, BillingCommercialPolicy.BillableSeats(499));
        Assert.Equal(500, BillingCommercialPolicy.BillableSeats(500));
        Assert.Equal(725, BillingCommercialPolicy.BillableSeats(725));
    }

    [Theory]
    [InlineData(SubscriptionTerm.ThreeMonths, 20, 10000)]
    [InlineData(SubscriptionTerm.SixMonths, 15, 7500)]
    [InlineData(SubscriptionTerm.SchoolYearTenMonths, 10, 5000)]
    public void InitialMonthlyInvoice_UsesPlanSnapshot(
        SubscriptionTerm term,
        decimal price,
        decimal expected)
    {
        var subscription = Subscription(term, SubscriptionBillingCadence.MonthlyInstallments, price, 500);
        Assert.Equal(expected, BillingCommercialPolicy.InitialNetAmount(subscription));
    }

    [Theory]
    [InlineData(SubscriptionTerm.ThreeMonths, 20, 30000)]
    [InlineData(SubscriptionTerm.SixMonths, 15, 45000)]
    [InlineData(SubscriptionTerm.SchoolYearTenMonths, 10, 50000)]
    public void InitialUpfrontInvoice_CoversFullTerm(
        SubscriptionTerm term,
        decimal price,
        decimal expected)
    {
        var subscription = Subscription(term, SubscriptionBillingCadence.FullTermUpfront, price, 500);
        Assert.Equal(expected, BillingCommercialPolicy.InitialNetAmount(subscription));
    }

    [Fact]
    public void DueAndGrace_Are14Plus7CalendarDays()
    {
        var issue = new DateTime(2026, 8, 23, 11, 0, 0, DateTimeKind.Utc);
        var due = BillingCommercialPolicy.DueDate(issue);
        Assert.Equal(issue.AddDays(14), due);
        Assert.Equal(issue.AddDays(21), BillingCommercialPolicy.GraceEnds(due));
    }

    [Fact]
    public void MonthlyProration_Handles31DayMonthExactly()
    {
        var start = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = ActiveSubscription(start, SubscriptionBillingCadence.MonthlyInstallments, 20m);
        var p = BillingCommercialPolicy.SeatIncreaseProration(
            subscription,
            Increase(subscription, start.AddDays(15), 500, 550));

        Assert.Equal(16, p.NumeratorDays);
        Assert.Equal(31, p.DenominatorDays);
        Assert.Equal(0, p.FullMonthsAfter);
        Assert.Equal(BillingCommercialPolicy.RoundMoney(50m * 20m * 16m / 31m), p.NetAmount);
    }

    [Fact]
    public void MonthlyProration_HandlesLeapFebruaryExactly()
    {
        var start = new DateTime(2028, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = ActiveSubscription(start, SubscriptionBillingCadence.MonthlyInstallments, 20m);
        var p = BillingCommercialPolicy.SeatIncreaseProration(
            subscription,
            Increase(subscription, new DateTime(2028, 2, 15, 0, 0, 0, DateTimeKind.Utc), 500, 550));

        Assert.Equal(15, p.NumeratorDays);
        Assert.Equal(29, p.DenominatorDays);
    }

    [Fact]
    public void MonthlyProration_FirstDayChargesFullMonth()
    {
        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = ActiveSubscription(start, SubscriptionBillingCadence.MonthlyInstallments, 20m);
        var p = BillingCommercialPolicy.SeatIncreaseProration(subscription, Increase(subscription, start, 500, 550));

        Assert.Equal(p.DenominatorDays, p.NumeratorDays);
        Assert.Equal(1000m, p.NetAmount);
    }

    [Fact]
    public void MonthlyProration_LastBillableDayChargesOneDay()
    {
        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = ActiveSubscription(start, SubscriptionBillingCadence.MonthlyInstallments, 20m);
        var p = BillingCommercialPolicy.SeatIncreaseProration(
            subscription,
            Increase(subscription, new DateTime(2026, 4, 30, 0, 0, 0, DateTimeKind.Utc), 500, 550));

        Assert.Equal(1, p.NumeratorDays);
        Assert.Equal(30, p.DenominatorDays);
        Assert.Equal(BillingCommercialPolicy.RoundMoney(1000m / 30m), p.NetAmount);
    }

    [Fact]
    public void FullTermUpfrontProration_ChargesCurrentFractionAndRemainingMonths()
    {
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var subscription = ActiveSubscription(
            start,
            SubscriptionBillingCadence.FullTermUpfront,
            20m,
            SubscriptionTerm.ThreeMonths);
        var p = BillingCommercialPolicy.SeatIncreaseProration(
            subscription,
            Increase(subscription, new DateTime(2026, 1, 16, 0, 0, 0, DateTimeKind.Utc), 500, 550));

        Assert.Equal(2, p.FullMonthsAfter);
        Assert.Equal(
            BillingCommercialPolicy.RoundMoney(50m * 20m * (16m / 31m + 2m)),
            p.NetAmount);
    }

    [Fact]
    public void Aging_AfterGrace_IsSuspensionEligibleButNotSuspendedState()
    {
        var issue = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var invoice = new BillingInvoice
        {
            Status = BillingInvoiceStatus.Due,
            TotalAmount = 100m,
            PaidAmount = 0m,
            DueDateUtc = issue.AddDays(14),
            GraceEndsAtUtc = issue.AddDays(21)
        };

        var aging = BillingCommercialPolicy.Aging(invoice, issue.AddDays(22));
        Assert.True(aging.SuspensionEligible);
        Assert.Equal(BillingInvoiceStatus.Overdue, aging.EffectiveStatus);
    }

    [Fact]
    public void Aging_SupportsPartialPayment()
    {
        var now = DateTime.UtcNow;
        var invoice = new BillingInvoice
        {
            Status = BillingInvoiceStatus.PartiallyPaid,
            TotalAmount = 100m,
            PaidAmount = 40m,
            DueDateUtc = now.AddDays(14),
            GraceEndsAtUtc = now.AddDays(21)
        };

        var aging = BillingCommercialPolicy.Aging(invoice, now);
        Assert.Equal(60m, aging.OutstandingAmount);
        Assert.Equal(BillingInvoiceStatus.PartiallyPaid, aging.EffectiveStatus);
    }

    private static SchoolSubscription Subscription(
        SubscriptionTerm term,
        SubscriptionBillingCadence cadence,
        decimal price,
        int seats) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = Guid.NewGuid(),
            Term = term,
            BillingCadence = cadence,
            CommercialCurrency = CommercialCurrency.PLN,
            PricePerStudentPerMonth = price,
            CommittedSeats = seats,
            Status = SubscriptionStatus.PendingActivation
        };

    private static SchoolSubscription ActiveSubscription(
        DateTime start,
        SubscriptionBillingCadence cadence,
        decimal price,
        SubscriptionTerm term = SubscriptionTerm.ThreeMonths)
    {
        var row = Subscription(term, cadence, price, 500);
        row.Status = SubscriptionStatus.Active;
        row.CurrentTermStartsAtUtc = start;
        row.CurrentTermEndsAtUtc = start.AddMonths((int)term);
        return row;
    }

    private static SubscriptionSeatChange Increase(
        SchoolSubscription subscription,
        DateTime effective,
        int previous,
        int next) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = subscription.SchoolId,
            SubscriptionId = subscription.Id,
            ChangeType = SeatCommitmentChangeType.Increase,
            PreviousSeats = previous,
            NewSeats = next,
            EffectiveAtUtc = effective,
            CreatedAtUtc = effective
        };
}
