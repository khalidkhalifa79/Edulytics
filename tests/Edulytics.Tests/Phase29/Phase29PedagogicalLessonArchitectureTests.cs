using System.Text.RegularExpressions;
using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Edulytics.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29PedagogicalLessonArchitectureTests
{
    private const string GradeSixSemanticGraphSha =
        "edae65cc700ae2b2f3a5a7828275a3ff" +
        "dded4fbf07759489801e7c4e5059e0e9";

    [Fact]
    public void CommonCoreGradeSixBlueprintLocksExactSourceSemantics()
    {
        var blueprint =
            Assert.Single(
                PedagogicalLessonBlueprintRegistry
                    .LoadEmbeddedDocuments(),
                x =>
                    x.PackCode ==
                        MathematicsCurriculumPackRegistry.CommonCoreCode &&
                    x.VersionCode ==
                        "CCSSM-2010" &&
                    x.LogicalLevel ==
                        7 &&
                    x.NativeLevel ==
                        "Grade 6");

        Assert.Equal(
            "US-CCSS-MATH:G6:OUR-IM-2017",
            blueprint.BlueprintCode);

        Assert.Equal(
            "CC BY 4.0",
            blueprint.SourceLicense);

        Assert.Equal(
            GradeSixSemanticGraphSha,
            blueprint.SemanticGraphSha256);

        Assert.Equal(
            9,
            blueprint.Units.Count);

        Assert.Equal(
            147,
            blueprint.Lessons.Count);

        Assert.Equal(
            new[]
            {
                19, 17, 17, 17, 15,
                19, 19, 18, 6
            },
            blueprint.Units
                .OrderBy(x => x.Number)
                .Select(x => x.LessonCount)
                .ToArray());

        Assert.Equal(
            Enumerable.Range(1, 147).ToArray(),
            blueprint.Lessons
                .OrderBy(x => x.SortOrder)
                .Select(x => x.SortOrder)
                .ToArray());

        Assert.Equal(
            147,
            blueprint.Lessons
                .Select(x => x.SourceLessonCode)
                .Distinct(StringComparer.Ordinal)
                .Count());

        Assert.Equal(
            147,
            blueprint.Lessons
                .Select(x => x.LessonCode)
                .Distinct(StringComparer.Ordinal)
                .Count());

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
            });

        Assert.Equal(
            208,
            blueprint.Lessons.Sum(
                x =>
                    x.OutcomeCodes.Count));

        Assert.Equal(
            17,
            blueprint.Lessons.Count(
                x =>
                    x.OutcomeCodes.Count == 0));

        var formalCoverage =
            blueprint.Lessons
                .SelectMany(
                    x => x.OutcomeCodes)
                .Distinct(
                    StringComparer.Ordinal)
                .OrderBy(x => x)
                .ToArray();

        Assert.Equal(
            29,
            formalCoverage.Length);

        Assert.All(
            formalCoverage,
            code =>
                Assert.Matches(
                    @"^CCSS:6\.(RP|NS|EE|G|SP)\.[A-Z]\.\d+$",
                    code));

        var noNumberedGradeSix =
            blueprint.Lessons
                .Where(
                    lesson =>
                        !lesson.Alignments.Any(
                            x =>
                                IsGradeSixNumberedReference(
                                    x.ReferenceCode)))
                .Select(
                    x => x.SourceLessonCode)
                .OrderBy(x => x)
                .ToArray();

        Assert.Equal(
            new[]
            {
                "6.3.17",
                "6.4.1",
                "6.4.2",
                "6.5.1",
                "6.5.5",
                "6.5.6",
                "6.5.9",
                "6.9.1",
                "6.9.2"
            },
            noNumberedGradeSix);

        var multiStandard =
            blueprint.Lessons.Count(
                lesson =>
                    lesson.Alignments
                        .Where(
                            x =>
                                IsGradeSixNumberedReference(
                                    x.ReferenceCode))
                        .Select(
                            x =>
                                NormalizeAcceptedParent(
                                    x.ReferenceCode))
                        .Distinct(
                            StringComparer.Ordinal)
                        .Count() > 1);

        Assert.Equal(
            60,
            multiStandard);

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

    [Fact]
    public async Task SeederCreatesExactGradeSixBlueprintAndPreservesFallbacks()
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

        var blueprint =
            Assert.Single(
                PedagogicalLessonBlueprintRegistry
                    .LoadEmbeddedDocuments(),
                x =>
                    x.PackCode ==
                        MathematicsCurriculumPackRegistry.CommonCoreCode &&
                    x.NativeLevel ==
                        "Grade 6");

        var states =
            await db.CurriculumPackImportStates
                .ToDictionaryAsync(
                    x => x.FrameworkCode);

        var commonCoreVersionId =
            states[
                MathematicsCurriculumPackRegistry
                    .CommonCoreCode]
                .FrameworkVersionId;

        var gradeSixLessons =
            await db.CurriculumPedagogicalLessons
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                            commonCoreVersionId &&
                        x.LogicalLevelFrom == 7 &&
                        x.LogicalLevelTo == 7 &&
                        x.NativeLevel == "Grade 6")
                .OrderBy(
                    x => x.SortOrder)
                .ToArrayAsync();

        Assert.Equal(
            147,
            gradeSixLessons.Length);

        var byCode =
            gradeSixLessons.ToDictionary(
                x => x.Code,
                StringComparer.Ordinal);

        var gradeSixIds =
            gradeSixLessons
                .Select(x => x.Id)
                .ToArray();

        var mappings =
            await db.CurriculumPedagogicalLessonOutcomes
                .Where(
                    x =>
                        gradeSixIds.Contains(
                            x.PedagogicalLessonId))
                .OrderBy(
                    x => x.SortOrder)
                .ToArrayAsync();

        Assert.Equal(
            208,
            mappings.Length);

        var mappedNodeIds =
            mappings
                .Select(x => x.OutcomeNodeId)
                .Distinct()
                .ToArray();

        var nodeCodes =
            await db.CurriculumPackContentNodes
                .Where(
                    x =>
                        mappedNodeIds.Contains(
                            x.Id))
                .ToDictionaryAsync(
                    x => x.Id,
                    x => x.Code);

        Assert.Equal(
            29,
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

        Assert.Equal(
            17,
            gradeSixLessons.Count(
                lesson =>
                    !mappings.Any(
                        x =>
                            x.PedagogicalLessonId ==
                            lesson.Id)));

        var uaeVersionId =
            states[
                MathematicsCurriculumPackRegistry
                    .UaeCode]
                .FrameworkVersionId;

        Assert.Equal(
            42,
            await db.CurriculumPedagogicalLessons
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                            uaeVersionId &&
                        x.OfficialLessonNodeId != null &&
                        db.CurriculumPackContentNodes.Any(
                            node =>
                                node.Id ==
                                    x.OfficialLessonNodeId &&
                                node.NodeKind == "Lesson")));

        Assert.Equal(
            1373,
            await db.CurriculumPedagogicalLessons
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                            uaeVersionId &&
                        x.OfficialLessonNodeId != null &&
                        db.CurriculumPackContentNodes.Any(
                            node =>
                                node.Id ==
                                    x.OfficialLessonNodeId &&
                                node.NodeKind == "Reference")));

        Assert.Equal(
            48,
            await db.CurriculumPedagogicalLessonOutcomes
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                            uaeVersionId));

        var blueprintLessonCodes =
            PedagogicalLessonBlueprintRegistry
                .LoadEmbeddedDocuments()
                .SelectMany(
                    x => x.Lessons)
                .Select(
                    x => x.LessonCode)
                .Distinct(
                    StringComparer.Ordinal)
                .ToArray();

        var fallbackLessons =
            await db.CurriculumPedagogicalLessons
                .Where(
                    x =>
                        x.FrameworkVersionId !=
                            uaeVersionId &&
                        !blueprintLessonCodes.Contains(
                            x.Code))
                .ToArrayAsync();

        Assert.NotEmpty(
            fallbackLessons);

        var fallbackIds =
            fallbackLessons
                .Select(x => x.Id)
                .ToArray();

        var fallbackMappings =
            await db.CurriculumPedagogicalLessonOutcomes
                .Where(
                    x =>
                        fallbackIds.Contains(
                            x.PedagogicalLessonId))
                .ToArrayAsync();

        Assert.Equal(
            fallbackLessons.Length,
            fallbackMappings.Length);

        Assert.All(
            fallbackLessons,
            lesson =>
                Assert.Single(
                    fallbackMappings,
                    x =>
                        x.PedagogicalLessonId ==
                        lesson.Id));

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

    [Fact]
    public async Task CambridgeCreatesReviewedPrimaryBlueprintsWithoutSyntheticOutcomeFallback()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db).SeedAsync();
        await new MathematicsPedagogicalLessonSeeder(db).SeedAsync();

        var versionId = await db.CurriculumPackImportStates
            .Where(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.CambridgeCode)
            .Select(x => x.FrameworkVersionId)
            .SingleAsync();

        var lessons = await db.CurriculumPedagogicalLessons
            .Where(x => x.FrameworkVersionId == versionId)
            .OrderBy(x => x.SortOrder)
            .ToArrayAsync();

        Assert.Equal(566, lessons.Length);

        var stageOne = lessons.Where(x => x.LogicalLevelFrom == 1 && x.LogicalLevelTo == 1).ToArray();
        var supportingPrimary = lessons.Where(x => x.LogicalLevelFrom >= 2 && x.LogicalLevelFrom <= 6).ToArray();
        var supportingLater = lessons.Where(x => x.LogicalLevelFrom >= 7 && x.LogicalLevelFrom <= 13).ToArray();

        Assert.Equal(27, stageOne.Length);
        Assert.Equal(142, supportingPrimary.Length);
        Assert.Equal(397, supportingLater.Length);
        Assert.All(stageOne, x => Assert.Equal("Cambridge Primary Stage 1", x.NativeLevel));
        Assert.All(stageOne, x => Assert.StartsWith("PED:CAMBRIDGE-INTL-MATH:S1:", x.Code, StringComparison.Ordinal));

        foreach (var stage in Enumerable.Range(2, 5))
        {
            var scoped = supportingPrimary.Where(x => x.LogicalLevelFrom == stage && x.LogicalLevelTo == stage).ToArray();
            Assert.NotEmpty(scoped);
            Assert.All(scoped, x => Assert.Equal($"Cambridge Primary Stage {stage}", x.NativeLevel));
            Assert.All(scoped, x => Assert.StartsWith($"PED:CAMBRIDGE-INTL-MATH:S{stage}:", x.Code, StringComparison.Ordinal));
        }

        var stageOneIds = stageOne.Select(x => x.Id).ToArray();
        var non9709SupportingIds = supportingPrimary
            .Concat(supportingLater.Where(x => x.LogicalLevelFrom <= 11))
            .Select(x => x.Id)
            .ToArray();
        var advanced9709Ids = supportingLater
            .Where(x => x.LogicalLevelFrom is 12 or 13)
            .Select(x => x.Id)
            .ToArray();

        Assert.Contains(supportingLater, x => x.LogicalLevelFrom == 7);
        Assert.Contains(supportingLater, x => x.LogicalLevelFrom == 8);
        Assert.Contains(supportingLater, x => x.LogicalLevelFrom == 9);
        Assert.Contains(supportingLater, x => x.LogicalLevelFrom == 10 && x.Pathway == "Core");
        Assert.Contains(supportingLater, x => x.LogicalLevelFrom == 10 && x.Pathway == "Extended");
        Assert.Contains(supportingLater, x => x.LogicalLevelFrom == 11 && x.Pathway == "Core");
        Assert.Contains(supportingLater, x => x.LogicalLevelFrom == 11 && x.Pathway == "Extended");
        Assert.Contains(supportingLater, x => x.LogicalLevelFrom == 12 && x.Pathway == "Component/route structure preserved in reference graph");
        Assert.Contains(supportingLater, x => x.LogicalLevelFrom == 13 && x.Pathway == "Component/route structure preserved in reference graph");

        Assert.Equal(
            36,
            await db.CurriculumPedagogicalLessonOutcomes.CountAsync(
                x => x.FrameworkVersionId == versionId && stageOneIds.Contains(x.PedagogicalLessonId)));

        Assert.Equal(
            non9709SupportingIds.Length,
            await db.CurriculumPedagogicalLessonOutcomes
                .Where(x => x.FrameworkVersionId == versionId && non9709SupportingIds.Contains(x.PedagogicalLessonId))
                .Select(x => x.PedagogicalLessonId)
                .Distinct()
                .CountAsync());

        var advancedMappings = await (
            from mapping in db.CurriculumPedagogicalLessonOutcomes
            join node in db.CurriculumPackContentNodes on mapping.OutcomeNodeId equals node.Id
            where mapping.FrameworkVersionId == versionId &&
                  advanced9709Ids.Contains(mapping.PedagogicalLessonId)
            select node.Code).ToArrayAsync();
        Assert.Equal(61, advancedMappings.Length);
        Assert.All(advancedMappings, code => Assert.StartsWith("CAM:REF:9709:", code, StringComparison.Ordinal));

        Assert.False(
            await db.CurriculumPedagogicalLessons.AnyAsync(
                x => x.FrameworkVersionId == versionId &&
                     (x.LogicalLevelFrom < 1 || x.LogicalLevelTo > 13)));
    }

    [Fact]
    public async Task SeederRemovesUnreferencedObsoletePseudoLesson()
    {
        await using var db =
            CreateDb();

        await new MathematicsCurriculumPackSeeder(
                db)
            .SeedAsync();

        var state =
            await db.CurriculumPackImportStates
                .SingleAsync(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry
                            .CommonCoreCode);

        var official =
            await db.CurriculumPackContentNodes
                .SingleAsync(
                    x =>
                        x.FrameworkVersionId ==
                            state.FrameworkVersionId &&
                        x.Code ==
                            "CCSS:6.RP.A.1");

        var id =
            Guid.NewGuid();

        db.CurriculumPedagogicalLessons.Add(
            new CurriculumPedagogicalLesson
            {
                Id = id,
                FrameworkVersionId =
                    state.FrameworkVersionId,
                OfficialLessonNodeId =
                    null,
                Code =
                    "PED:US-CCSS-MATH:"
                    + "LEGACY-G6-PSEUDO-TEST",
                UnitKey =
                    "LEGACY",
                UnitTitle =
                    "Ratios",
                Title =
                    "Ratios — Lesson 99",
                LogicalLevelFrom =
                    7,
                LogicalLevelTo =
                    7,
                NativeLevel =
                    "Grade 6",
                Pathway =
                    null,
                SortOrder =
                    9999,
                CreatedAtUtc =
                    DateTime.UtcNow,
                UpdatedAtUtc =
                    DateTime.UtcNow
            });

        db.CurriculumPedagogicalLessonOutcomes.Add(
            new CurriculumPedagogicalLessonOutcome
            {
                PedagogicalLessonId =
                    id,
                FrameworkVersionId =
                    state.FrameworkVersionId,
                OutcomeNodeId =
                    official.Id,
                SortOrder =
                    1
            });

        await db.SaveChangesAsync();

        await new MathematicsPedagogicalLessonSeeder(
                db)
            .SeedAsync();

        Assert.False(
            await db.CurriculumPedagogicalLessons
                .AnyAsync(
                    x => x.Id == id));

        Assert.False(
            await db.CurriculumPedagogicalLessonOutcomes
                .AnyAsync(
                    x =>
                        x.PedagogicalLessonId ==
                        id));
    }

    [Fact]
    public async Task SeederReattachesUaeCanonicalContentByExactFrameworkAndCodeIdentity()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db).SeedAsync();
        var seeder = new MathematicsPedagogicalLessonSeeder(db);
        await seeder.SeedAsync();

        var uaeVersionId = await db.CurriculumPackImportStates
            .Where(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.UaeCode)
            .Select(x => x.FrameworkVersionId)
            .SingleAsync();

        var target = await db.CurriculumPedagogicalLessons
            .Where(x =>
                x.FrameworkVersionId == uaeVersionId &&
                x.Code.StartsWith("PED:UAE-MOE-MATH:"))
            .OrderBy(x => x.Code)
            .FirstAsync();

        var staleId = Guid.NewGuid();
        db.CurriculumPedagogicalLessons.Add(
            new CurriculumPedagogicalLesson
            {
                Id = staleId,
                FrameworkVersionId = target.FrameworkVersionId,
                OfficialLessonNodeId = target.OfficialLessonNodeId,
                Code = target.Code,
                UnitKey = target.UnitKey,
                UnitTitle = target.UnitTitle,
                Title = target.Title,
                LogicalLevelFrom = target.LogicalLevelFrom,
                LogicalLevelTo = target.LogicalLevelTo,
                NativeLevel = target.NativeLevel,
                Pathway = target.Pathway,
                SortOrder = target.SortOrder,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

        var contentId = Guid.NewGuid();
        db.CurriculumLessonContents.Add(
            new CurriculumLessonContent
            {
                Id = contentId,
                FrameworkVersionId = uaeVersionId,
                PedagogicalLessonId = staleId,
                Status = Edulytics.Core.Enums.CanonicalLessonContentStatus.Draft,
                ContentVersion = "legacy-uae-test",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                RowVersion = []
            });

        await db.SaveChangesAsync();
        await seeder.SeedAsync();

        var content = await db.CurriculumLessonContents.SingleAsync(x => x.Id == contentId);
        Assert.Equal(target.Id, content.PedagogicalLessonId);
        Assert.False(await db.CurriculumPedagogicalLessons.AnyAsync(x => x.Id == staleId));
    }

    [Fact]
    public async Task CurrentUaeCanonicalContentCodesAreAllInTheExpectedPedagogicalGraph()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db).SeedAsync();
        await new MathematicsPedagogicalLessonSeeder(db).SeedAsync();

        var uaeVersionId = await db.CurriculumPackImportStates
            .Where(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.UaeCode)
            .Select(x => x.FrameworkVersionId)
            .SingleAsync();

        var pedagogicalCodes = await db.CurriculumPedagogicalLessons
            .Where(x => x.FrameworkVersionId == uaeVersionId)
            .Select(x => x.Code)
            .ToHashSetAsync();

        var canonicalCodes = MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .Where(x => x.PackCode == MathematicsCurriculumPackRegistry.UaeCode)
            .SelectMany(x => x.Lessons)
            .Select(x => x.LessonCode)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(canonicalCodes);
        Assert.All(
            canonicalCodes,
            code => Assert.Contains(code, pedagogicalCodes));
    }

    [Fact]
    public async Task SeederRetiresUaeCanonicalContentOnlyWhenCodeWasRemovedFromCurrentPacks()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db).SeedAsync();
        var seeder = new MathematicsPedagogicalLessonSeeder(db);
        await seeder.SeedAsync();

        var uaeVersionId = await db.CurriculumPackImportStates
            .Where(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.UaeCode)
            .Select(x => x.FrameworkVersionId)
            .SingleAsync();

        var staleId = Guid.NewGuid();
        const string retiredCode =
            "PED:UAE-MOE-MATH:L10:ADVANCED:03:07:PYTHAGORAS-THEOREM";

        Assert.DoesNotContain(
            MathematicsCanonicalLessonContentSeeder
                .LoadEmbeddedDocuments()
                .Where(x => x.PackCode == MathematicsCurriculumPackRegistry.UaeCode)
                .SelectMany(x => x.Lessons)
                .Select(x => x.LessonCode),
            x => string.Equals(x, retiredCode, StringComparison.Ordinal));

        db.CurriculumPedagogicalLessons.Add(
            new CurriculumPedagogicalLesson
            {
                Id = staleId,
                FrameworkVersionId = uaeVersionId,
                OfficialLessonNodeId = staleId,
                Code = retiredCode,
                UnitKey = "legacy-uae",
                UnitTitle = "Legacy UAE",
                Title = "Pythagoras theorem — advanced reasoning",
                LogicalLevelFrom = 10,
                LogicalLevelTo = 10,
                NativeLevel = "Grade 10",
                Pathway = "Advanced",
                SortOrder = 999,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

        var contentId = Guid.NewGuid();
        db.CurriculumLessonContents.Add(
            new CurriculumLessonContent
            {
                Id = contentId,
                FrameworkVersionId = uaeVersionId,
                PedagogicalLessonId = staleId,
                Status = Edulytics.Core.Enums.CanonicalLessonContentStatus.Draft,
                ContentVersion = "retired-uae-test",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                RowVersion = []
            });

        await db.SaveChangesAsync();
        await seeder.SeedAsync();

        Assert.False(await db.CurriculumLessonContents.AnyAsync(x => x.Id == contentId));
        Assert.False(await db.CurriculumPedagogicalLessons.AnyAsync(x => x.Id == staleId));
    }

    [Fact]
    public async Task SeederDetachesPracticeReferencesBeforeRetiringRemovedUaeLesson()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db).SeedAsync();
        var seeder = new MathematicsPedagogicalLessonSeeder(db);
        await seeder.SeedAsync();

        var uaeVersionId = await db.CurriculumPackImportStates
            .Where(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.UaeCode)
            .Select(x => x.FrameworkVersionId)
            .SingleAsync();

        var staleId = Guid.NewGuid();
        db.CurriculumPedagogicalLessons.Add(
            new CurriculumPedagogicalLesson
            {
                Id = staleId,
                FrameworkVersionId = uaeVersionId,
                OfficialLessonNodeId = staleId,
                Code = "PED:UAE-MOE-MATH:L10:ADVANCED:99:99:RETIRED-PRACTICE-TARGET",
                UnitKey = "legacy-uae",
                UnitTitle = "Legacy UAE",
                Title = "Retired practice target",
                LogicalLevelFrom = 10,
                LogicalLevelTo = 10,
                NativeLevel = "Grade 10",
                Pathway = "Advanced",
                SortOrder = 999,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

        var schoolId = Guid.NewGuid();
        var adoptionId = Guid.NewGuid();
        var item = new AssessmentItem
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            CurriculumAdoptionId = adoptionId,
            CurriculumPedagogicalLessonId = staleId,
            Prompt = "legacy",
            CorrectAnswer = "legacy",
            Solution = "legacy",
            ExposureFingerprint = "legacy",
            CreatedAtUtc = DateTime.UtcNow,
            RowVersion = []
        };
        db.AssessmentItems.Add(item);

        await db.SaveChangesAsync();
        await seeder.SeedAsync();

        Assert.Null(item.CurriculumPedagogicalLessonId);
        Assert.False(await db.CurriculumPedagogicalLessons.AnyAsync(x => x.Id == staleId));
    }

    [Fact]
    public async Task SeederRefusesCanonicalContentReconciliationOutsideUae()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db).SeedAsync();
        var seeder = new MathematicsPedagogicalLessonSeeder(db);
        await seeder.SeedAsync();

        var commonCoreVersionId = await db.CurriculumPackImportStates
            .Where(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.CommonCoreCode)
            .Select(x => x.FrameworkVersionId)
            .SingleAsync();

        var target = await db.CurriculumPedagogicalLessons
            .Where(x => x.FrameworkVersionId == commonCoreVersionId)
            .OrderBy(x => x.Code)
            .FirstAsync();

        var staleId = Guid.NewGuid();
        db.CurriculumPedagogicalLessons.Add(
            new CurriculumPedagogicalLesson
            {
                Id = staleId,
                FrameworkVersionId = target.FrameworkVersionId,
                OfficialLessonNodeId = target.OfficialLessonNodeId,
                Code = target.Code,
                UnitKey = target.UnitKey,
                UnitTitle = target.UnitTitle,
                Title = target.Title,
                LogicalLevelFrom = target.LogicalLevelFrom,
                LogicalLevelTo = target.LogicalLevelTo,
                NativeLevel = target.NativeLevel,
                Pathway = target.Pathway,
                SortOrder = target.SortOrder,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

        db.CurriculumLessonContents.Add(
            new CurriculumLessonContent
            {
                Id = Guid.NewGuid(),
                FrameworkVersionId = commonCoreVersionId,
                PedagogicalLessonId = staleId,
                Status = Edulytics.Core.Enums.CanonicalLessonContentStatus.Draft,
                ContentVersion = "non-uae-legacy-test",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                RowVersion = []
            });

        await db.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => seeder.SeedAsync());

        Assert.Contains(
            "outside the UAE migration scope",
            error.Message,
            StringComparison.Ordinal);
        Assert.True(await db.CurriculumPedagogicalLessons.AnyAsync(x => x.Id == staleId));
    }

    [Fact]
    public void CanonicalContentForeignKeyTargetsPedagogicalLesson()
    {
        using var db =
            CreateDb();

        var entity =
            db.Model.FindEntityType(
                "Edulytics.Core.Entities."
                + "CurriculumLessonContent");

        Assert.NotNull(
            entity);

        var property =
            entity!.FindProperty(
                "PedagogicalLessonId");

        Assert.NotNull(
            property);

        Assert.Equal(
            "LessonNodeId",
            property!.GetColumnName());

        var fk =
            Assert.Single(
                entity.GetForeignKeys(),
                x =>
                    x.Properties.Any(
                        p =>
                            p.Name ==
                            "PedagogicalLessonId"));

        Assert.Equal(
            "Edulytics.Core.Entities."
            + "CurriculumPedagogicalLesson",
            fk.PrincipalEntityType.Name);
    }

    private static bool IsGradeSixNumberedReference(
        string reference) =>
        Regex.IsMatch(
            reference,
            @"^6\.(RP|NS|EE|G|SP)\.[A-Z]\.\d+(?:\.[a-z])?$",
            RegexOptions.CultureInvariant);

    private static string NormalizeAcceptedParent(
        string reference) =>
        Regex.Replace(
            reference,
            @"\.[a-z]$",
            string.Empty,
            RegexOptions.CultureInvariant);

    private static EdulyticsDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<
                    EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    "phase29-" +
                    Guid.NewGuid())
                .Options;

        return new EdulyticsDbContext(
            options);
    }
}
