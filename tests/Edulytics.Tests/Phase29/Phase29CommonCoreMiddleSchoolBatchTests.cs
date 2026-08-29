using System.Text.RegularExpressions;
using Edulytics.Core.Curriculum;
using Edulytics.Data.Contexts;
using Edulytics.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29CommonCoreMiddleSchoolBatchTests
{
    private sealed record GradeSpec(
        int Grade,
        int LogicalLevel,
        int LessonCount,
        int FormalMappingCount,
        int OfficialStandardCount,
        int ZeroFormalCount,
        string SemanticSha,
        int[] UnitCounts);

    private static readonly GradeSpec[] Specs =
    [
        new(
            7,
            8,
            145,
            199,
            24,
            7,
            "243f2fc1a433bd8488bb8577579d92d045ac98cb136ae6066b70c659fc85be37",
            [
                13, 15, 11, 16, 17,
                23, 17, 20, 13
            ]),
        new(
            8,
            9,
            131,
            146,
            28,
            22,
            "cfab43f2bb82317c3366e80ddec79128b1777c2bbbe8d947ec627654b5e21d85",
            [
                17, 13, 14, 16, 22,
                11, 16, 16, 6
            ])
    ];

    [Fact]
    public void GradeSevenAndEightBlueprintsLockExactSourceGraphs()
    {
        var documents =
            PedagogicalLessonBlueprintRegistry
                .LoadEmbeddedDocuments();

        // Common Core now has five elementary blueprints plus
        // the already accepted Grade 6-8 middle-school blueprints.
        Assert.Equal(
            8,
            documents.Count(
                x =>
                    x.PackCode ==
                    MathematicsCurriculumPackRegistry.CommonCoreCode &&
                    x.SchemaVersion == 1));

        foreach (var spec in Specs)
        {
            var blueprint =
                Assert.Single(
                    documents,
                    x =>
                        x.PackCode ==
                            MathematicsCurriculumPackRegistry.CommonCoreCode &&
                        x.NativeLevel ==
                            $"Grade {spec.Grade}");

            Assert.Equal(
                $"US-CCSS-MATH:G{spec.Grade}:OUR-IM-2017",
                blueprint.BlueprintCode);

            Assert.Equal(
                spec.LogicalLevel,
                blueprint.LogicalLevel);

            Assert.Equal(
                "CC BY 4.0",
                blueprint.SourceLicense);

            Assert.Equal(
                spec.SemanticSha,
                blueprint.SemanticGraphSha256);

            Assert.Equal(
                9,
                blueprint.Units.Count);

            Assert.Equal(
                spec.LessonCount,
                blueprint.Lessons.Count);

            Assert.Equal(
                spec.UnitCounts,
                blueprint.Units
                    .OrderBy(x => x.Number)
                    .Select(x => x.LessonCount)
                    .ToArray());

            Assert.Equal(
                Enumerable.Range(
                        1,
                        spec.LessonCount)
                    .ToArray(),
                blueprint.Lessons
                    .OrderBy(x => x.SortOrder)
                    .Select(x => x.SortOrder)
                    .ToArray());

            Assert.Equal(
                spec.FormalMappingCount,
                blueprint.Lessons.Sum(
                    x => x.OutcomeCodes.Count));

            Assert.Equal(
                spec.ZeroFormalCount,
                blueprint.Lessons.Count(
                    x => x.OutcomeCodes.Count == 0));

            Assert.Equal(
                spec.OfficialStandardCount,
                blueprint.Lessons
                    .SelectMany(
                        x => x.OutcomeCodes)
                    .Distinct(
                        StringComparer.Ordinal)
                    .Count());

            Assert.Equal(
                spec.OfficialStandardCount,
                blueprint
                    .AcquisitionDiagnostics
                    .EffectiveOfficialStandardCount);

            Assert.Equal(
                spec.OfficialStandardCount,
                blueprint
                    .AcquisitionDiagnostics
                    .AddressingCoverageCount);

            Assert.Equal(
                spec.FormalMappingCount,
                blueprint
                    .AcquisitionDiagnostics
                    .FormalMappingCount);

            Assert.All(
                blueprint.Lessons,
                lesson =>
                {
                    Assert.False(
                        string.IsNullOrWhiteSpace(
                            lesson.Title));

                    Assert.Matches(
                        "^[0-9a-f]{64}$",
                        lesson.SemanticSha256);

                    Assert.False(
                        Regex.IsMatch(
                            lesson.Title,
                            @"(?:^|\s[—-]\s)Lesson\s+\d+\s*$",
                            RegexOptions.IgnoreCase |
                            RegexOptions.CultureInvariant));

                    Assert.All(
                        lesson.OutcomeCodes,
                        code =>
                            Assert.Matches(
                                $@"^CCSS:{spec.Grade}\." +
                                @"(RP|NS|EE|G|SP|F)\.[A-Z]\.\d+$",
                                code));
                });

            Assert.DoesNotContain(
                blueprint.Lessons
                    .SelectMany(
                        x => x.Alignments),
                x =>
                    x.Role != "Addressing" &&
                    !string.IsNullOrWhiteSpace(
                        x.OutcomeCode));

            Assert.DoesNotContain(
                blueprint.Lessons
                    .SelectMany(
                        x => x.Alignments),
                x =>
                    (x.ReferenceKind is
                        "Cluster" or "Domain") &&
                    !string.IsNullOrWhiteSpace(
                        x.OutcomeCode));
        }

        var gradeSeven =
            Assert.Single(
                documents,
                x =>
                    x.PackCode ==
                        MathematicsCurriculumPackRegistry.CommonCoreCode &&
                    x.NativeLevel ==
                        "Grade 7");

        var lesson7623 =
            Assert.Single(
                gradeSeven.Lessons,
                x =>
                    x.SourceLessonCode ==
                    "7.6.23");

        Assert.Empty(
            lesson7623.OutcomeCodes);

        Assert.Contains(
            lesson7623.Alignments,
            x =>
                x.Role == "BuildingOn");

        Assert.DoesNotContain(
            lesson7623.Alignments,
            x =>
                x.Role == "Addressing" &&
                !string.IsNullOrWhiteSpace(
                    x.OutcomeCode));
    }

    [Fact]
    public async Task SeederCreatesExactGradeSevenAndEightGraphsAndIsIdempotent()
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

        var documents =
            PedagogicalLessonBlueprintRegistry
                .LoadEmbeddedDocuments();

        var versionId =
            await db.CurriculumPackImportStates
                .Where(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry.CommonCoreCode)
                .Select(
                    x => x.FrameworkVersionId)
                .SingleAsync();

        foreach (var spec in Specs)
        {
            var blueprint =
                Assert.Single(
                    documents,
                    x =>
                        x.PackCode ==
                            MathematicsCurriculumPackRegistry.CommonCoreCode &&
                        x.NativeLevel ==
                            $"Grade {spec.Grade}");

            var lessons =
                await db.CurriculumPedagogicalLessons
                    .Where(
                        x =>
                            x.FrameworkVersionId ==
                                versionId &&
                            x.LogicalLevelFrom ==
                                spec.LogicalLevel &&
                            x.LogicalLevelTo ==
                                spec.LogicalLevel &&
                            x.NativeLevel ==
                                $"Grade {spec.Grade}")
                    .OrderBy(
                        x => x.SortOrder)
                    .ToArrayAsync();

            Assert.Equal(
                spec.LessonCount,
                lessons.Length);

            var byCode =
                lessons.ToDictionary(
                    x => x.Code,
                    StringComparer.Ordinal);

            var ids =
                lessons
                    .Select(x => x.Id)
                    .ToArray();

            var mappings =
                await db.CurriculumPedagogicalLessonOutcomes
                    .Where(
                        x =>
                            ids.Contains(
                                x.PedagogicalLessonId))
                    .ToArrayAsync();

            Assert.Equal(
                spec.FormalMappingCount,
                mappings.Length);

            Assert.Equal(
                spec.ZeroFormalCount,
                lessons.Count(
                    lesson =>
                        !mappings.Any(
                            x =>
                                x.PedagogicalLessonId ==
                                lesson.Id)));

            var nodeIds =
                mappings
                    .Select(x => x.OutcomeNodeId)
                    .Distinct()
                    .ToArray();

            var nodeCodes =
                await db.CurriculumPackContentNodes
                    .Where(
                        x =>
                            nodeIds.Contains(
                                x.Id))
                    .ToDictionaryAsync(
                        x => x.Id,
                        x => x.Code);

            Assert.Equal(
                spec.OfficialStandardCount,
                nodeCodes.Values
                    .Distinct(
                        StringComparer.Ordinal)
                    .Count());

            foreach (var expected in
                     blueprint.Lessons)
            {
                Assert.True(
                    byCode.TryGetValue(
                        expected.LessonCode,
                        out var actual));

                Assert.Equal(
                    expected.Title,
                    actual!.Title);

                Assert.Equal(
                    expected.UnitTitle,
                    actual.UnitTitle);

                Assert.Equal(
                    expected.SortOrder,
                    actual.SortOrder);

                Assert.Null(
                    actual.OfficialLessonNodeId);

                var actualMappings =
                    mappings
                        .Where(
                            x =>
                                x.PedagogicalLessonId ==
                                actual.Id)
                        .OrderBy(
                            x => x.SortOrder)
                        .Select(
                            x =>
                                nodeCodes[
                                    x.OutcomeNodeId])
                        .ToArray();

                Assert.Equal(
                    expected.OutcomeCodes.ToArray(),
                    actualMappings);
            }
        }

        // Grade 6 remains byte-semantically unchanged at runtime.
        var gradeSixLessons =
            await db.CurriculumPedagogicalLessons
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                            versionId &&
                        x.NativeLevel ==
                            "Grade 6" &&
                        x.LogicalLevelFrom ==
                            7 &&
                        x.LogicalLevelTo ==
                            7)
                .Select(x => x.Id)
                .ToArrayAsync();

        Assert.Equal(
            147,
            gradeSixLessons.Length);

        Assert.Equal(
            208,
            await db.CurriculumPedagogicalLessonOutcomes
                .CountAsync(
                    x =>
                        gradeSixLessons.Contains(
                            x.PedagogicalLessonId)));

        var lessonCount =
            await db.CurriculumPedagogicalLessons
                .CountAsync();

        var mappingCount =
            await db.CurriculumPedagogicalLessonOutcomes
                .CountAsync();

        await seeder.SeedAsync();

        Assert.Equal(
            lessonCount,
            await db.CurriculumPedagogicalLessons
                .CountAsync());

        Assert.Equal(
            mappingCount,
            await db.CurriculumPedagogicalLessonOutcomes
                .CountAsync());
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<
                    EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    "phase29-g7-g8-" +
                    Guid.NewGuid())
                .Options;

        return new EdulyticsDbContext(
            options);
    }
}
