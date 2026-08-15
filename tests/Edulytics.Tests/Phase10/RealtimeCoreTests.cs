using Edulytics.Core.Entities;
using Edulytics.Core.Realtime;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase10;

public sealed class RealtimeCoreTests
{
    [Fact]
    public void GroupNames_AreStrictlyTenantAndAssignmentScoped()
    {
        var schoolA = Guid.NewGuid();
        var schoolB = Guid.NewGuid();
        var classId = Guid.NewGuid();
        var subjectA = Guid.NewGuid();
        var subjectB = Guid.NewGuid();

        Assert.NotEqual(
            RealtimeGroupNames.SchoolAdmins(schoolA),
            RealtimeGroupNames.SchoolAdmins(schoolB));

        Assert.NotEqual(
            RealtimeGroupNames.Teachers(
                schoolA,
                classId,
                subjectA),
            RealtimeGroupNames.Teachers(
                schoolA,
                classId,
                subjectB));

        var group =
            RealtimeGroupNames.Teachers(
                schoolA,
                classId,
                subjectA);

        Assert.Contains(
            schoolA.ToString("N"),
            group);

        Assert.Contains(
            classId.ToString("N"),
            group);

        Assert.Contains(
            subjectA.ToString("N"),
            group);
    }

    [Fact]
    public async Task RealtimeAccessRepository_IsSchoolAndUserScoped()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    $"p10-access-{Guid.NewGuid():N}")
                .Options;

        await using var db =
            new EdulyticsDbContext(options);

        var schoolA = Guid.NewGuid();
        var schoolB = Guid.NewGuid();
        var teacherA = Guid.NewGuid();
        var teacherB = Guid.NewGuid();

        db.TeacherAssignments.AddRange(
            NewAssignment(schoolA, teacherA),
            NewAssignment(schoolA, teacherB),
            NewAssignment(schoolB, teacherA));

        await db.SaveChangesAsync();

        var repository =
            new RealtimeAccessRepository(db);

        var rows =
            await repository.GetTeacherAssignmentsAsync(
                schoolA,
                teacherA);

        var row = Assert.Single(rows);

        Assert.Equal(
            schoolA,
            row.SchoolId);

        Assert.Equal(
            teacherA,
            row.TeacherUserId);
    }

    [Fact]
    public async Task OutboxRepository_ClaimsAndCompletesMessage()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    $"p10-outbox-{Guid.NewGuid():N}")
                .Options;

        await using var db =
            new EdulyticsDbContext(options);

        var now =
            new DateTime(
                2026,
                8,
                15,
                20,
                0,
                0,
                DateTimeKind.Utc);

        var message =
            new OutboxMessage
            {
                Id = Guid.NewGuid(),
                SchoolId = Guid.NewGuid(),
                EventType =
                    RealtimeEventTypes.AssessmentResultEntered,
                PayloadJson = "{}",
                OccurredAtUtc = now,
                AvailableAtUtc = now,
                CorrelationId =
                    Guid.NewGuid().ToString("N")
            };

        db.OutboxMessages.Add(message);

        await db.SaveChangesAsync();

        var repository =
            new OutboxRepository(db);

        var pending =
            await repository.GetPendingAsync(
                now,
                10);

        var candidate =
            Assert.Single(pending);

        Assert.True(
            await repository.TryClaimAsync(
                candidate.Id,
                candidate.RowVersion,
                now,
                now.AddSeconds(30)));

        Assert.False(
            await repository.TryClaimAsync(
                candidate.Id,
                candidate.RowVersion,
                now,
                now.AddSeconds(30)));

        Assert.True(
            await repository.MarkProcessedAsync(
                candidate.Id,
                now.AddSeconds(1)));

        Assert.Empty(
            await repository.GetPendingAsync(
                now.AddSeconds(2),
                10));
    }

    private static TeacherAssignment NewAssignment(
        Guid schoolId,
        Guid teacherId) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            TeacherUserId = teacherId,
            ClassGroupId = Guid.NewGuid(),
            SubjectId = Guid.NewGuid(),
            AcademicYearId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow
        };
}
