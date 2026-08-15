using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase09;

public sealed class AnalyticsModelTests
{
    [Fact]
    public void ProjectionEntities_AreMapped()
    {
        using var db = CreateDb();

        foreach (var type in new[]
                 {
                     typeof(StudentOutcomeMastery),
                     typeof(ClassOutcomeSummary),
                     typeof(ClassTopicSummary),
                     typeof(ClassAssessmentTrend),
                     typeof(SchoolAnalyticsSnapshot)
                 })
        {
            Assert.NotNull(
                db.Model.FindEntityType(type));
        }
    }

    [Fact]
    public void StudentOutcomeMastery_HasTenantUniqueIndex()
    {
        using var db = CreateDb();

        var entity =
            db.Model.FindEntityType(
                typeof(StudentOutcomeMastery));

        Assert.NotNull(entity);

        Assert.Contains(
            entity!.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties
                    .Select(x => x.Name)
                    .SequenceEqual(
                        new[]
                        {
                            nameof(StudentOutcomeMastery.SchoolId),
                            nameof(StudentOutcomeMastery.AcademicYearId),
                            nameof(StudentOutcomeMastery.ClassGroupId),
                            nameof(StudentOutcomeMastery.SubjectId),
                            nameof(StudentOutcomeMastery.StudentProfileId),
                            nameof(StudentOutcomeMastery.LearningOutcomeId)
                        }));
    }

    [Fact]
    public void ProjectionRelationships_IncludeSchoolId()
    {
        using var db = CreateDb();

        var entity =
            db.Model.FindEntityType(
                typeof(StudentOutcomeMastery));

        Assert.NotNull(entity);

        Assert.All(
            entity!.GetForeignKeys()
                .Where(
                    x =>
                        x.PrincipalEntityType.ClrType !=
                        typeof(School)),
            fk =>
                Assert.Contains(
                    fk.Properties,
                    x =>
                        x.Name ==
                        nameof(StudentOutcomeMastery.SchoolId)));
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<
                EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    $"phase09-model-{Guid.NewGuid():N}")
                .Options;

        return new EdulyticsDbContext(options);
    }
}
