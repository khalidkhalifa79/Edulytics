using Edulytics.Core.Entities;
using Edulytics.Core.Subscriptions;

namespace Edulytics.Core.Interfaces;

public interface ISchoolSubscriptionRepository
{
    Task<IReadOnlyList<SchoolSubscription>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<SchoolSubscription?> GetBySchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<SchoolSubscription?> GetForUpdateBySchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<int> CountActiveStudentsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<bool> HasActiveStudentProfileForUserAsync(
        Guid schoolId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SubscriptionPersistenceResult> AddAsync(
        SchoolSubscription subscription,
        SubscriptionSeatChange initialSeatChange,
        CancellationToken cancellationToken = default);

    Task<SubscriptionPersistenceResult> SaveAsync(
        SchoolSubscription subscription,
        byte[] expectedRowVersion,
        SubscriptionSeatChange? seatChange = null,
        CancellationToken cancellationToken = default);

    Task<SubscriptionPersistenceResult> SaveWithSchoolAsync(
        SchoolSubscription subscription,
        byte[] expectedSubscriptionRowVersion,
        School school,
        byte[] expectedSchoolRowVersion,
        SubscriptionSeatChange? seatChange = null,
        CancellationToken cancellationToken = default);
}
