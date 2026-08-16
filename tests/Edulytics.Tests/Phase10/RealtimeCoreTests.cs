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
    public void OutboxMessage_DefaultsToPendingWithoutLease()
    {
        var message =
            new OutboxMessage();

        Assert.Equal(
            Edulytics.Core.Enums
                .OutboxMessageStatus.Pending,
            message.Status);

        Assert.Null(
            message.LeaseOwner);

        Assert.Null(
            message.LeaseToken);

        Assert.Null(
            message.LeaseUntilUtc);
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
