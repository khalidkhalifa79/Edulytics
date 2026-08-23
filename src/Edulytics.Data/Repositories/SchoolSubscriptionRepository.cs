using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Subscriptions;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class SchoolSubscriptionRepository
    : ISchoolSubscriptionRepository
{
    private readonly EdulyticsDbContext _db;

    public SchoolSubscriptionRepository(
        EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SchoolSubscription>>
        ListAsync(
            CancellationToken cancellationToken = default) =>
        await _db.SchoolSubscriptions
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

    public Task<SchoolSubscription?> GetBySchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        _db.SchoolSubscriptions
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.SchoolId == schoolId,
                cancellationToken);

    public Task<SchoolSubscription?> GetForUpdateBySchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        if (!_db.Database.IsRelational())
        {
            return _db.SchoolSubscriptions
                .SingleOrDefaultAsync(
                    x => x.SchoolId == schoolId,
                    cancellationToken);
        }

        return _db.SchoolSubscriptions
            .FromSqlInterpolated(
                $@"SELECT *
FROM ""SchoolSubscriptions""
WHERE ""SchoolId"" = {schoolId}
FOR UPDATE")
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    public Task<int> CountActiveStudentsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        _db.StudentProfiles
            .AsNoTracking()
            .CountAsync(
                x =>
                    x.SchoolId == schoolId &&
                    !x.IsArchived &&
                    x.Status ==
                        AcademicStructureStatus.Active,
                cancellationToken);

    public Task<bool> HasActiveStudentProfileForUserAsync(
        Guid schoolId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        _db.StudentProfiles
            .AsNoTracking()
            .AnyAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.UserId == userId &&
                    !x.IsArchived &&
                    x.Status ==
                        AcademicStructureStatus.Active,
                cancellationToken);

    public async Task<SubscriptionPersistenceResult> AddAsync(
        SchoolSubscription subscription,
        SubscriptionSeatChange initialSeatChange,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SchoolSubscriptions.AddAsync(
                subscription,
                cancellationToken);

            await _db.SubscriptionSeatChanges.AddAsync(
                initialSeatChange,
                cancellationToken);

            await _db.SaveChangesAsync(
                cancellationToken);

            return SubscriptionPersistenceResult.Success();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();

            return SubscriptionPersistenceResult.Failure(
                SubscriptionPersistenceError.Constraint);
        }
    }

    public Task<SubscriptionPersistenceResult> SaveAsync(
        SchoolSubscription subscription,
        byte[] expectedRowVersion,
        SubscriptionSeatChange? seatChange = null,
        CancellationToken cancellationToken = default) =>
        SaveInternalAsync(
            subscription,
            expectedRowVersion,
            school: null,
            expectedSchoolRowVersion: null,
            seatChange,
            cancellationToken);

    public Task<SubscriptionPersistenceResult> SaveWithSchoolAsync(
        SchoolSubscription subscription,
        byte[] expectedSubscriptionRowVersion,
        School school,
        byte[] expectedSchoolRowVersion,
        SubscriptionSeatChange? seatChange = null,
        CancellationToken cancellationToken = default) =>
        SaveInternalAsync(
            subscription,
            expectedSubscriptionRowVersion,
            school,
            expectedSchoolRowVersion,
            seatChange,
            cancellationToken);

    private async Task<SubscriptionPersistenceResult>
        SaveInternalAsync(
            SchoolSubscription subscription,
            byte[] expectedSubscriptionRowVersion,
            School? school,
            byte[]? expectedSchoolRowVersion,
            SubscriptionSeatChange? seatChange,
            CancellationToken cancellationToken)
    {
        try
        {
            _db.Entry(subscription)
                .Property(x => x.RowVersion)
                .OriginalValue =
                    expectedSubscriptionRowVersion;

            if (school is not null)
            {
                if (expectedSchoolRowVersion is not
                    { Length: > 0 })
                {
                    return SubscriptionPersistenceResult
                        .Failure(
                            SubscriptionPersistenceError
                                .Concurrency);
                }

                _db.Entry(school)
                    .Property(x => x.RowVersion)
                    .OriginalValue =
                        expectedSchoolRowVersion;
            }

            if (seatChange is not null)
            {
                await _db.SubscriptionSeatChanges
                    .AddAsync(
                        seatChange,
                        cancellationToken);
            }

            // One SaveChanges boundary persists subscription,
            // optional School status, seat history and queued
            // AuditLog together.
            await _db.SaveChangesAsync(
                cancellationToken);

            return SubscriptionPersistenceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();

            return SubscriptionPersistenceResult.Failure(
                SubscriptionPersistenceError.Concurrency);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();

            return SubscriptionPersistenceResult.Failure(
                SubscriptionPersistenceError.Constraint);
        }
        catch
        {
            _db.ChangeTracker.Clear();

            return SubscriptionPersistenceResult.Failure(
                SubscriptionPersistenceError.Unknown);
        }
    }
}
