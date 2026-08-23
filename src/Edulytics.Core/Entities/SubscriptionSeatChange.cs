using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;

namespace Edulytics.Core.Entities;

public sealed class SubscriptionSeatChange : ISchoolScoped
{
    public Guid Id { get; set; }
    public Guid SchoolId { get; set; }
    public Guid SubscriptionId { get; set; }

    public SeatCommitmentChangeType ChangeType { get; set; }

    public int PreviousSeats { get; set; }
    public int NewSeats { get; set; }

    public DateTime EffectiveAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
