using System.Text.Json;
using Edulytics.Core.Curriculum;
using Edulytics.Data.Contexts;
using Edulytics.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class
    Phase29CommonCoreHighSchoolBlueprintV2Tests
{
    private static readonly Dictionary<
        string,
        (int Units, int Lessons, int Mappings)>
        Matrix =
        new(StringComparer.Ordinal)
        {
            ["ALG1"] = (7, 132, 284),
            ["GEO"] = (8, 124, 237),
            ["ALG2"] = (7, 120, 200),
            ["TRAD-SUPPLEMENT"] = (4, 11, 13),
            ["ADV-ALG-FUNC"] = (1, 2, 3),
            ["ADV-TRIG-GEO"] = (1, 5, 6),
            ["COMPLEX"] = (1, 1, 4),
            ["PROB-DECISION"] = (1, 5, 5),
            ["VECTOR-MATRIX"] = (1, 5, 12)
        };

    [Fact]
    public void RegistryLocksExactHighSchoolV2Graph()
    {
        var all =
            PedagogicalLessonBlueprintRegistry
                .LoadEmbeddedDocuments();

        var commonCore =
            all.Where(
                    x =>
                        x.PackCode ==
                        MathematicsCurriculumPackRegistry
                            .CommonCoreCode)
                .ToArray();

        Assert.Equal(
            17,
            commonCore.Length);

        Assert.Equal(
            8,
            commonCore.Count(
                x =>
                    x.SchemaVersion == 1));

        var hs =
            commonCore
                .Where(
                    x =>
                        x.SchemaVersion == 2)
                .ToArray();

        Assert.Equal(
            9,
            hs.Length);

        Assert.Equal(
            Matrix.Keys.OrderBy(x => x),
            hs.Select(
                    x => x.CourseCode)
                .OrderBy(x => x));

        Assert.All(
            hs,
            document =>
            {
                Assert.Equal(
                    0,
                    document.LogicalLevel);

                Assert.Equal(
                    10,
                    document.LogicalLevelFrom);

                Assert.Equal(
                    13,
                    document.LogicalLevelTo);

                Assert.True(
                    document
                        .SuppressOutcomeFallbackForLogicalRange);

                Assert.NotEmpty(
                    document.Sources);

                Assert.All(
                    document.Lessons,
                    lesson =>
                        Assert.Empty(
                            lesson.OutcomeCodes));
            });

        foreach (var document in hs)
        {
            var expected =
                Matrix[
                    document.CourseCode];

            Assert.Equal(
                expected.Units,
                document.Units.Count);

            Assert.Equal(
                expected.Lessons,
                document.Lessons.Count);

            Assert.Equal(
                expected.Mappings,
                document.Lessons.Sum(
                    x =>
                        x.FormalTargets.Count));
        }

        Assert.Equal(
            31,
            hs.Sum(
                x => x.Units.Count));

        Assert.Equal(
            405,
            hs.Sum(
                x => x.Lessons.Count));

        var targets =
            hs.SelectMany(
                    x => x.Lessons)
                .SelectMany(
                    x => x.FormalTargets)
                .ToArray();

        Assert.Equal(
            764,
            targets.Length);

        Assert.Equal(
            721,
            targets.Count(
                x =>
                    x.EvidenceKind ==
                    "PublisherAddressing"));

        Assert.Equal(
            35,
            targets.Count(
                x =>
                    x.EvidenceKind ==
                    "PrimarySourceExplicitStandardAlignment"));

        Assert.Equal(
            8,
            targets.Count(
                x =>
                    x.EvidenceKind ==
                    "VerifiedContentCoverage"));
    }

    [Fact]
    public void V2ContractRejectsProvenanceCorruption()
    {
        var documents =
            PedagogicalLessonBlueprintRegistry
                .LoadEmbeddedDocuments()
                .Where(
                    x =>
                        x.SchemaVersion == 2)
                .ToArray();

        var probability =
            documents.Single(
                x =>
                    x.CourseCode ==
                    "PROB-DECISION");

        var legacyLevel =
            Clone(
                probability);

        legacyLevel.LogicalLevel = 10;

        Assert.Throws<
            InvalidOperationException>(
            () =>
                PedagogicalLessonBlueprintContract
                    .Validate(
                        legacyLevel));

        var falsePublisher =
            Clone(
                probability);

        var verified =
            falsePublisher
                .Lessons
                .SelectMany(
                    x => x.FormalTargets)
                .First(
                    x =>
                        x.EvidenceKind ==
                        "VerifiedContentCoverage");

        verified.PublisherSuppliedAlignment =
            true;

        Assert.Throws<
            InvalidOperationException>(
            () =>
                PedagogicalLessonBlueprintContract
                    .Validate(
                        falsePublisher));

        var missingEvidence =
            Clone(
                probability);

        missingEvidence
            .Lessons
            .SelectMany(
                x => x.FormalTargets)
            .First(
                x =>
                    x.EvidenceKind ==
                    "VerifiedContentCoverage")
            .EvidenceReferences
            .Clear();

        Assert.Throws<
            InvalidOperationException>(
            () =>
                PedagogicalLessonBlueprintContract
                    .Validate(
                        missingEvidence));
    }

    [Fact]
    public async Task SeederCreatesExactHighSchoolGraphAndIsIdempotent()
    {
        await using var db =
            CreateDb();

        await new MathematicsCurriculumPackSeeder(
                db)
            .SeedAsync();

        var seeder =
            new MathematicsPedagogicalLessonSeeder(
                db);

        await seeder.SeedAsync();

        var versionId =
            await db
                .CurriculumPackImportStates
                .Where(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry
                            .CommonCoreCode)
                .Select(
                    x => x.FrameworkVersionId)
                .SingleAsync();

        var hs =
            await db
                .CurriculumPedagogicalLessons
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                            versionId &&
                        x.LogicalLevelFrom == 10 &&
                        x.LogicalLevelTo == 13)
                .ToArrayAsync();

        Assert.Equal(
            405,
            hs.Length);

        var ids =
            hs.Select(
                    x => x.Id)
                .ToArray();

        var mappings =
            await db
                .CurriculumPedagogicalLessonOutcomes
                .Where(
                    x =>
                        ids.Contains(
                            x.PedagogicalLessonId))
                .ToArrayAsync();

        Assert.Equal(
            764,
            mappings.Length);

        var core =
            hs.Where(
                    x =>
                        x.Code.StartsWith(
                            "PED:US-CCSS-MATH:HS:TRAD:ALG1:",
                            StringComparison.Ordinal) ||
                        x.Code.StartsWith(
                            "PED:US-CCSS-MATH:HS:TRAD:GEO:",
                            StringComparison.Ordinal) ||
                        x.Code.StartsWith(
                            "PED:US-CCSS-MATH:HS:TRAD:ALG2:",
                            StringComparison.Ordinal))
                .ToArray();

        Assert.Equal(
            376,
            core.Length);

        var coreIds =
            core
                .Select(
                    x => x.Id)
                .ToHashSet();

        Assert.Equal(
            721,
            mappings.Count(
                x =>
                    coreIds.Contains(
                        x.PedagogicalLessonId)));

        Assert.Equal(
            29,
            hs.Length -
            core.Length);

        Assert.Equal(
            43,
            mappings.Count(
                x =>
                    !coreIds.Contains(
                        x.PedagogicalLessonId)));

        var fallback =
            await db
                .CurriculumPedagogicalLessons
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                            versionId &&
                        x.LogicalLevelFrom ==
                            x.LogicalLevelTo &&
                        x.LogicalLevelFrom >= 10 &&
                        x.LogicalLevelFrom <= 13);

        Assert.Equal(
            0,
            fallback);

        var lessonCount =
            await db
                .CurriculumPedagogicalLessons
                .CountAsync();

        var mappingCount =
            await db
                .CurriculumPedagogicalLessonOutcomes
                .CountAsync();

        await seeder.SeedAsync();

        Assert.Equal(
            lessonCount,
            await db
                .CurriculumPedagogicalLessons
                .CountAsync());

        Assert.Equal(
            mappingCount,
            await db
                .CurriculumPedagogicalLessonOutcomes
                .CountAsync());
    }

    private static
        PedagogicalLessonBlueprintDocument Clone(
            PedagogicalLessonBlueprintDocument source)
    {
        var json =
            JsonSerializer.Serialize(
                source);

        return
            JsonSerializer.Deserialize<
                PedagogicalLessonBlueprintDocument>(
                json)
            ?? throw new InvalidOperationException(
                "Unable to clone blueprint.");
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<
                    EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    "phase29-hs-v2-" +
                    Guid.NewGuid())
                .Options;

        return new EdulyticsDbContext(
            options);
    }
}
