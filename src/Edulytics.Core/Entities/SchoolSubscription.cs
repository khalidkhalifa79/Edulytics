using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class SchoolSubscription : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }

    public SubscriptionTerm Term { get; set; }
    public SubscriptionBillingCadence BillingCadence { get; set; }
    public CommercialCurrency CommercialCurrency { get; set; }

    public decimal PricePerStudentPerMonth { get; set; }

    public int CommittedSeats { get; set; }
    public int? PendingRenewalSeats { get; set; }

    public bool AutoRenew { get; set; }
    public DateTime? NonRenewalRequestedAtUtc { get; set; }

    public SubscriptionStatus Status { get; set; }

    public DateTime? ActivatedAtUtc { get; set; }
    public DateTime? CurrentTermStartsAtUtc { get; set; }
    public DateTime? CurrentTermEndsAtUtc { get; set; }
    public DateTime? SuspendedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
