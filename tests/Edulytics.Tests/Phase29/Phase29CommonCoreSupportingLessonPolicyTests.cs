using Edulytics.Core.Curriculum;
using Edulytics.Data.Contexts;
using Edulytics.Data.Seeding;
using Edulytics.Services.LessonContent;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase29;

public sealed class
    Phase29CommonCoreSupportingLessonPolicyTests
{
    [Fact]
    public void
        AcceptedSourceBlueprintsLockExactly94SupportingLessons()
    {
        var documents =
            PedagogicalLessonBlueprintRegistry
                .LoadEmbeddedDocuments()
                .Where(
                    x =>
                        x.PackCode ==
                        MathematicsCurriculumPackRegistry
                            .CommonCoreCode)
                .ToArray();

        Assert.Equal(
            17,
            documents.Length);

        Assert.Equal(
            8,
            documents.Count(
                x => x.SchemaVersion == 1));

        Assert.Equal(
            9,
            documents.Count(
                x => x.SchemaVersion == 2));

        Assert.Equal(
            1560,
            documents.Sum(
                x => x.Lessons.Count));

        var supporting =
            documents
                .SelectMany(
                    document =>
                        document.Lessons.Select(
                            lesson =>
                                new
                                {
                                    Document = document,
                                    Lesson = lesson
                                }))
                .Where(
                    x =>
                        x.Document.SchemaVersion == 2
                            ? x.Lesson.FormalTargets.Count == 0
                            : x.Lesson.OutcomeCodes.Count == 0)
                .ToArray();

        Assert.Equal(
            94,
            supporting.Length);

        var distribution =
            supporting
                .GroupBy(
                    x =>
                        x.Document.SchemaVersion == 2
                            ? x.Document.CourseCode
                            : x.Document.NativeLevel)
                .ToDictionary(
                    x => x.Key,
                    x => x.Count(),
                    StringComparer.Ordinal);

        var expected =
            new Dictionary<string, int>(
                StringComparer.Ordinal)
            {
                ["Grade 1"] = 2,
                ["Grade 2"] = 3,
                ["Grade 3"] = 8,
                ["Grade 4"] = 7,
                ["Grade 5"] = 5,
                ["Grade 6"] = 17,
                ["Grade 7"] = 7,
                ["Grade 8"] = 22,
                ["ALG1"] = 6,
                ["GEO"] = 5,
                ["ALG2"] = 12
            };

        Assert.Equal(
            expected.Count,
            distribution.Count);

        foreach (var pair in expected)
        {
            Assert.True(
                distribution.TryGetValue(
                    pair.Key,
                    out var actual));

            Assert.Equal(
                pair.Value,
                actual);
        }
    }

    [Fact]
    public async Task
        SeededRuntimeGraphExcludesKindergartenAndLocks1466StandaloneAnd94Supporting()
    {
        await using var db =
            CreateDb();

        await new MathematicsCurriculumPackSeeder(
                db)
            .SeedAsync();

        await new MathematicsPedagogicalLessonSeeder(
                db)
            .SeedAsync();

        var versionId =
            await db
                .CurriculumPackImportStates
                .Where(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry
                            .CommonCoreCode &&
                        x.VersionCode ==
                            "CCSSM-2010" &&
                        x.IsComplete)
                .Select(
                    x =>
                        x.FrameworkVersionId)
                .SingleAsync();

        var lessonIds =
            await db
                .CurriculumPedagogicalLessons
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        versionId)
                .Select(
                    x => x.Id)
                .ToArrayAsync();

        var kindergartenLessonCount =
            await db
                .CurriculumPedagogicalLessons
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                            versionId &&
                        x.NativeLevel ==
                            "Kindergarten");

        var officialKindergartenCount =
            await db
                .CurriculumPackContentNodes
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                            versionId &&
                        x.FrameworkCode ==
                            MathematicsCurriculumPackRegistry
                                .CommonCoreCode &&
                        x.IsOfficial &&
                        x.IsActive &&
                        (
                            x.NodeKind == "Standard" ||
                            x.NodeKind == "Outcome"
                        ) &&
                        x.LogicalLevelFrom <= 1 &&
                        x.LogicalLevelTo >= 1);

        var mappedLessonIds =
            await db
                .CurriculumPedagogicalLessonOutcomes
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        versionId &&
                        lessonIds.Contains(
                            x.PedagogicalLessonId))
                .Select(
                    x =>
                        x.PedagogicalLessonId)
                .Distinct()
                .ToArrayAsync();

        Assert.Equal(
            1560,
            lessonIds.Length);

        Assert.Equal(
            1466,
            mappedLessonIds.Length);

        Assert.Equal(
            94,
            lessonIds.Length -
            mappedLessonIds.Length);

        Assert.Equal(
            0,
            kindergartenLessonCount);

        Assert.True(
            officialKindergartenCount > 0,
            "Official Common Core Kindergarten Standards must remain preserved.");

        Assert.True(
            LessonContentPolicy
                .IsStandaloneCanonicalTarget(
                    1));

        Assert.False(
            LessonContentPolicy
                .IsStandaloneCanonicalTarget(
                    0));
    }

    [Fact]
    public void
        ProductionReadyIncludesPublishedSupportingContent()
    {
        Assert.True(
            LessonContentPolicy
                .IsProductionReady(
                    Core.Enums
                        .CanonicalLessonContentStatus
                        .Published,
                    true));

        Assert.True(
            LessonContentPolicy
                .IsProductionReady(
                    Core.Enums
                        .CanonicalLessonContentStatus
                        .Published,
                    false));

        Assert.False(
            LessonContentPolicy
                .IsProductionReady(
                    Core.Enums
                        .CanonicalLessonContentStatus
                        .Verified,
                    true));

        Assert.False(
            LessonContentPolicy
                .IsProductionReady(
                    null,
                    true));
    }

    private static EdulyticsDbContext
        CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<
                    EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    "phase29-supporting-policy-" +
                    Guid.NewGuid())
                .Options;

        return new EdulyticsDbContext(
            options);
    }
}
