using Edulytics.Core.Academics;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase25C;

public sealed class Phase25CSeatEnforcementTests
{
    [Fact]
    public async Task DirectActiveStudentCreation_RejectsAtCommittedCapacity()
    {
        await using var db = NewDb();
        var school = NewSchool();
        db.Schools.Add(school);
        db.SchoolSubscriptions.Add(
            ActiveSubscription(school.Id, 500));

        for (var i = 0; i < 500; i++)
            db.StudentProfiles.Add(Student(school.Id, i));

        await db.SaveChangesAsync();

        var repository =
            new AcademicStructureRepository(db);

        var result =
            await repository.AddStudentProfileWithSeatGuardAsync(
                Student(school.Id, 9000));

        Assert.False(result.Succeeded);
        Assert.Equal(
            AcademicPersistenceError.SeatLimit,
            result.Error);
    }

    [Fact]
    public async Task ArchivedStudent_ReleasesSeat_ForNextDirectCreate()
    {
        await using var db = NewDb();
        var school = NewSchool();
        db.Schools.Add(school);
        db.SchoolSubscriptions.Add(
            ActiveSubscription(school.Id, 500));

        for (var i = 0; i < 499; i++)
            db.StudentProfiles.Add(Student(school.Id, i));

        db.StudentProfiles.Add(
            Student(
                school.Id,
                8000,
                archived: true));

        await db.SaveChangesAsync();

        var repository =
            new AcademicStructureRepository(db);

        var result =
            await repository.AddStudentProfileWithSeatGuardAsync(
                Student(school.Id, 9001));

        Assert.True(result.Succeeded);

        Assert.Equal(
            501,
            await db.StudentProfiles.CountAsync());
    }

    [Fact]
    public async Task RestoreActiveStudent_RejectsWhenCapacityIsFull()
    {
        await using var db = NewDb();
        var school = NewSchool();
        db.Schools.Add(school);
        db.SchoolSubscriptions.Add(
            ActiveSubscription(school.Id, 500));

        for (var i = 0; i < 500; i++)
            db.StudentProfiles.Add(Student(school.Id, i));

        var archived =
            Student(
                school.Id,
                8001,
                archived: true);

        db.StudentProfiles.Add(archived);
        await db.SaveChangesAsync();

        var expected =
            archived.RowVersion.ToArray();

        archived.IsArchived = false;
        archived.ArchivedAtUtc = null;
        archived.UpdatedAtUtc = DateTime.UtcNow;

        var repository =
            new AcademicStructureRepository(db);

        var result =
            await repository.SaveStudentArchiveStateWithSeatGuardAsync(
                archived,
                expected,
                restoring: true);

        Assert.False(result.Succeeded);
        Assert.Equal(
            AcademicPersistenceError.SeatLimit,
            result.Error);
    }

    [Fact]
    public void StudentProfile_ArchiveAndConcurrency_AreDurableModelState()
    {
        using var db = NewDb();

        var entity =
            db.Model.FindEntityType(
                typeof(StudentProfile));

        Assert.NotNull(entity);
        Assert.NotNull(
            entity!.FindProperty(
                nameof(StudentProfile.IsArchived)));
        Assert.NotNull(
            entity.FindProperty(
                nameof(StudentProfile.ArchivedAtUtc)));
        Assert.True(
            entity.FindProperty(
                nameof(StudentProfile.RowVersion))!
                .IsConcurrencyToken);
    }

    private static EdulyticsDbContext NewDb()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    Guid.NewGuid().ToString("N"))
                .Options;

        return new EdulyticsDbContext(options);
    }

    private static School NewSchool() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Phase25C Seat School",
            SchoolCode = "P25CSEAT",
            NormalizedSchoolCode = "P25CSEAT",
            Status = SchoolStatus.Active,
            CountryCode = "PL",
            City = "Warsaw",
            ContactEmail = "seat@example.com",
            DefaultCulture = "en",
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = []
        };

    private static SchoolSubscription ActiveSubscription(
        Guid schoolId,
        int seats)
    {
        var start = DateTime.UtcNow.AddDays(-1);

        return new SchoolSubscription
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Term = SubscriptionTerm.ThreeMonths,
            BillingCadence =
                SubscriptionBillingCadence.MonthlyInstallments,
            CommercialCurrency = CommercialCurrency.PLN,
            PricePerStudentPerMonth = 20m,
            CommittedSeats = seats,
            AutoRenew = true,
            Status = SubscriptionStatus.Active,
            ActivatedAtUtc = start,
            CurrentTermStartsAtUtc = start,
            CurrentTermEndsAtUtc = start.AddMonths(3),
            CreatedAtUtc = start,
            UpdatedAtUtc = start,
            RowVersion = []
        };
    }

    private static StudentProfile Student(
        Guid schoolId,
        int number,
        bool archived = false) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            StudentNumber = $"S{number:00000}",
            NormalizedStudentNumber = $"S{number:00000}",
            FirstName = "Seat",
            LastName = $"Student {number}",
            DisplayName = $"Seat Student {number}",
            Status = AcademicStructureStatus.Active,
            IsArchived = archived,
            ArchivedAtUtc =
                archived
                    ? DateTime.UtcNow
                    : null,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = []
        };
}
