using System.Security.Cryptography;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class AnalyticsRefreshQueueRepository
    : IAnalyticsRefreshQueueRepository
{
    private readonly EdulyticsDbContext _db;

    public AnalyticsRefreshQueueRepository(
        EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task RequestAsync(
        Guid schoolId,
        DateTime utcNow,
        TimeSpan debounce,
        TimeSpan maxCoalesceWindow,
        CancellationToken cancellationToken = default)
    {
        if (schoolId == Guid.Empty)
        {
            throw new ArgumentException(
                "School is required.",
                nameof(schoolId));
        }

        ValidateWindows(
            debounce,
            maxCoalesceWindow);

        var debouncedAt =
            utcNow.Add(debounce);

        var maxDeadline =
            utcNow.Add(
                maxCoalesceWindow);

        var rowVersion =
            RandomNumberGenerator
                .GetBytes(16);

        await _db.Database
            .ExecuteSqlInterpolatedAsync(
                $@"INSERT INTO ""AnalyticsRefreshStates"" AS current
(
    ""SchoolId"",
    ""RequestedVersion"",
    ""CompletedVersion"",
    ""FirstRequestedAtUtc"",
    ""LastRequestedAtUtc"",
    ""CoalesceDeadlineUtc"",
    ""AvailableAtUtc"",
    ""LeaseOwner"",
    ""LeaseToken"",
    ""LeaseUntilUtc"",
    ""ProcessingAttempts"",
    ""LastError"",
    ""RowVersion""
)
VALUES
(
    {schoolId},
    {1L},
    {0L},
    {utcNow},
    {utcNow},
    {maxDeadline},
    {debouncedAt},
    NULL,
    NULL,
    NULL,
    {0},
    NULL,
    {rowVersion}
)
ON CONFLICT (""SchoolId"")
DO UPDATE SET
    ""RequestedVersion"" =
        current.""RequestedVersion"" + 1,
    ""FirstRequestedAtUtc"" =
        CASE
            WHEN current.""CompletedVersion"" >=
                 current.""RequestedVersion""
                THEN {utcNow}
            ELSE current.""FirstRequestedAtUtc""
        END,
    ""LastRequestedAtUtc"" = {utcNow},
    ""CoalesceDeadlineUtc"" =
        CASE
            WHEN current.""CompletedVersion"" >=
                 current.""RequestedVersion""
                THEN {maxDeadline}
            ELSE current.""CoalesceDeadlineUtc""
        END,
    ""AvailableAtUtc"" =
        CASE
            WHEN current.""CompletedVersion"" >=
                 current.""RequestedVersion""
                THEN {debouncedAt}
            ELSE LEAST(
                {debouncedAt},
                current.""CoalesceDeadlineUtc"")
        END,
    ""RowVersion"" = {rowVersion}",
                cancellationToken);
    }

    public async Task<AnalyticsRefreshLease?>
        ClaimNextAsync(
            string leaseOwner,
            DateTime utcNow,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            leaseOwner);

        if (leaseOwner.Length > 200)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseOwner));
        }

        if (leaseDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(leaseDuration));
        }

        await using var transaction =
            await _db.Database
                .BeginTransactionAsync(
                    cancellationToken);

        var entity =
            await _db.AnalyticsRefreshStates
                .FromSqlInterpolated(
                    $@"SELECT *
FROM ""AnalyticsRefreshStates""
WHERE
    ""RequestedVersion"" > ""CompletedVersion""
    AND ""AvailableAtUtc"" <= {utcNow}
    AND (
        ""LeaseUntilUtc"" IS NULL
        OR ""LeaseUntilUtc"" <= {utcNow}
    )
ORDER BY
    ""AvailableAtUtc"",
    ""LastRequestedAtUtc"",
    ""SchoolId""
LIMIT 1
FOR UPDATE SKIP LOCKED")
                .SingleOrDefaultAsync(
                    cancellationToken);

        if (entity is null)
        {
            await transaction.CommitAsync(
                cancellationToken);

            return null;
        }

        var token =
            Guid.NewGuid();

        var until =
            utcNow.Add(
                leaseDuration);

        entity.LeaseOwner =
            leaseOwner;

        entity.LeaseToken =
            token;

        entity.LeaseUntilUtc =
            until;

        entity.ProcessingAttempts++;

        await _db.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        return new AnalyticsRefreshLease(
            entity.SchoolId,
            entity.RequestedVersion,
            entity.ProcessingAttempts,
            leaseOwner,
            token,
            until);
    }

    public async Task<bool> CompleteAsync(
        AnalyticsRefreshLease lease,
        DateTime utcNow,
        TimeSpan debounce,
        TimeSpan maxCoalesceWindow,
        CancellationToken cancellationToken = default)
    {
        ValidateWindows(
            debounce,
            maxCoalesceWindow);

        var debouncedAt =
            utcNow.Add(debounce);

        var maxDeadline =
            utcNow.Add(
                maxCoalesceWindow);

        var rowVersion =
            RandomNumberGenerator
                .GetBytes(16);

        var affected =
            await _db.Database
                .ExecuteSqlInterpolatedAsync(
                    $@"UPDATE ""AnalyticsRefreshStates""
SET
    ""CompletedVersion"" =
        GREATEST(
            ""CompletedVersion"",
            {lease.RequestedVersion}),
    ""FirstRequestedAtUtc"" =
        CASE
            WHEN ""RequestedVersion"" >
                 {lease.RequestedVersion}
                THEN {utcNow}
            ELSE ""FirstRequestedAtUtc""
        END,
    ""CoalesceDeadlineUtc"" =
        CASE
            WHEN ""RequestedVersion"" >
                 {lease.RequestedVersion}
                THEN {maxDeadline}
            ELSE ""CoalesceDeadlineUtc""
        END,
    ""AvailableAtUtc"" =
        CASE
            WHEN ""RequestedVersion"" >
                 {lease.RequestedVersion}
                THEN {debouncedAt}
            ELSE {utcNow}
        END,
    ""LeaseOwner"" = NULL,
    ""LeaseToken"" = NULL,
    ""LeaseUntilUtc"" = NULL,
    ""ProcessingAttempts"" = 0,
    ""LastError"" = NULL,
    ""RowVersion"" = {rowVersion}
WHERE
    ""SchoolId"" = {lease.SchoolId}
    AND ""LeaseOwner"" = {lease.LeaseOwner}
    AND ""LeaseToken"" = {lease.LeaseToken}",
                    cancellationToken);

        return affected == 1;
    }

    public async Task<bool> MarkFailedAsync(
        AnalyticsRefreshLease lease,
        string error,
        DateTime nextAvailableAtUtc,
        CancellationToken cancellationToken = default)
    {
        var value =
            string.IsNullOrWhiteSpace(error)
                ? "Unknown analytics refresh failure."
                : error.Trim();

        if (value.Length > 2000)
            value = value[..2000];

        var rowVersion =
            RandomNumberGenerator
                .GetBytes(16);

        var affected =
            await _db.Database
                .ExecuteSqlInterpolatedAsync(
                    $@"UPDATE ""AnalyticsRefreshStates""
SET
    ""AvailableAtUtc"" = {nextAvailableAtUtc},
    ""LeaseOwner"" = NULL,
    ""LeaseToken"" = NULL,
    ""LeaseUntilUtc"" = NULL,
    ""LastError"" = {value},
    ""RowVersion"" = {rowVersion}
WHERE
    ""SchoolId"" = {lease.SchoolId}
    AND ""LeaseOwner"" = {lease.LeaseOwner}
    AND ""LeaseToken"" = {lease.LeaseToken}",
                    cancellationToken);

        return affected == 1;
    }

    private static void ValidateWindows(
        TimeSpan debounce,
        TimeSpan maxCoalesceWindow)
    {
        if (debounce <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(debounce));
        }

        if (maxCoalesceWindow < debounce)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxCoalesceWindow));
        }
    }
}
