using Edulytics.Core.Analytics;
using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase09;

public sealed class AnalyticsRepositoryTests
{
    [Fact]
    public async Task ReplaceProjections_IsIdempotentForSchool()
    {
        var options =
            new DbContextOptionsBuilder<
                EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    $"p9-repo-{Guid.NewGuid():N}")
                .Options;

        await using var db =
            new EdulyticsDbContext(options);

        var schoolId = Guid.NewGuid();
        var yearId = Guid.NewGuid();

        var repository =
            new AnalyticsRepository(db);

        AnalyticsProjectionSet Set(decimal mastery) =>
            new(
                [],
                [],
                [],
                [],
                [
                    new SchoolAnalyticsSnapshot
                    {
                        Id = Guid.NewGuid(),
                        SchoolId = schoolId,
                        AcademicYearId = yearId,
                        OverallMasteryPercentage =
                            mastery,
                        CalculatedAtUtc =
                            DateTime.UtcNow
                    }
                ]);

        Assert.True(
            (await repository
                .ReplaceProjectionsAsync(
                    schoolId,
                    Set(50m)))
            .Succeeded);

        Assert.True(
            (await repository
                .ReplaceProjectionsAsync(
                    schoolId,
                    Set(75m)))
            .Succeeded);

        var rows =
            await db.SchoolAnalyticsSnapshots
                .Where(
                    x =>
                        x.SchoolId ==
                        schoolId)
                .ToListAsync();

        var row = Assert.Single(rows);

        Assert.Equal(
            75m,
            row.OverallMasteryPercentage);
    }
}
