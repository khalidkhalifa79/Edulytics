namespace Edulytics.Core.Enums;

public enum OutboxMessageStatus
{
    Pending = 1,
    Processing = 2,
    Processed = 3,
    DeadLetter = 4
}
