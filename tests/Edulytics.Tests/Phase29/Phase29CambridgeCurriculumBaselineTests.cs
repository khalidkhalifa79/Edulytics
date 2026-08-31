using System.Text.Json;
using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Edulytics.Data.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29CambridgeCurriculumBaselineTests
{
    private static EdulyticsDbContext Db(
        string name) =>
        new(
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(name)
                .Options);

    private static JsonDocument LoadManifest()
    {
        var assembly =
            typeof(MathematicsCurriculumPackRegistry)
                .Assembly;

        var name =
            assembly.GetManifestResourceNames()
                .Single(
                    x =>
                        x.EndsWith(
                            "cambridge-intl-math.integrity-manifest.json",
                            StringComparison.OrdinalIgnoreCase));

        using var stream =
            assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(
                "Cambridge manifest resource missing.");

        return JsonDocument.Parse(stream);
    }

    private static JsonDocument LoadPack()
    {
        var assembly =
            typeof(MathematicsCurriculumPackRegistry)
                .Assembly;

        var name =
            assembly.GetManifestResourceNames()
                .Single(
                    x =>
                        x.EndsWith(
                            "cambridge-intl-math.curriculum-pack.json",
                            StringComparison.OrdinalIgnoreCase));

        using var stream =
            assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException(
                "Cambridge pack resource missing.");

        return JsonDocument.Parse(stream);
    }

    [Fact]
    public void RegistryUsesCambridgeInsteadOfEngland()
    {
        MathematicsCurriculumPackRegistry.Validate();
        MathematicsLessonBlueprintRegistry.Validate();

        Assert.Equal(
            4,
            MathematicsCurriculumPackRegistry.All.Count);

        Assert.DoesNotContain(
            MathematicsCurriculumPackRegistry.All,
            x =>
                x.Code ==
                "UK-" + "NC-ENG-MATH");

        var cambridge =
            Assert.Single(
                MathematicsCurriculumPackRegistry.All,
                x =>
                    x.Code ==
                    MathematicsCurriculumPackRegistry.CambridgeCode);

        Assert.Equal(
            CurriculumReuseBasis.CopyrightedOfficialSourceReference,
            cambridge.ReuseBasis);

        Assert.Equal(
            CurriculumTextMode.OfficialSourceLinked,
            cambridge.TextMode);

        Assert.Equal(
            "en",
            cambridge.AcademicLanguage);

        Assert.Equal(
            Enumerable.Range(1, 13),
            cambridge.Levels
                .Select(x => x.LogicalLevel)
                .Distinct()
                .OrderBy(x => x));

        Assert.Contains(
            cambridge.Levels,
            x =>
                x.LogicalLevel == 10 &&
                x.Pathway == "Core");

        Assert.Contains(
            cambridge.Levels,
            x =>
                x.LogicalLevel == 10 &&
                x.Pathway == "Extended");

        Assert.DoesNotContain(
            MathematicsLessonBlueprintRegistry.CreateBlueprints(),
            x =>
                x.PackCode ==
                MathematicsCurriculumPackRegistry.CambridgeCode);
    }

    [Fact]
    public void EmbeddedCambridgeGraphMatchesAcceptedSourceGate()
    {
        using var pack =
            LoadPack();

        using var manifest =
            LoadManifest();

        var root =
            pack.RootElement;

        Assert.Equal(
            16,
            root.GetProperty("SchemaVersion").GetInt32());

        Assert.Equal(
            "CAMBRIDGE-INTL-MATH",
            root.GetProperty("PackCode").GetString());

        Assert.Equal(
            "CAMBRIDGE-PATHWAY-2026",
            root.GetProperty("VersionCode").GetString());

        Assert.Equal(
            779,
            root.GetProperty("OfficialNodeCount").GetInt32());

        Assert.Equal(
            888,
            root.GetProperty("NodeCount").GetInt32());

        Assert.Equal(
            0,
            root.GetProperty("UnitCount").GetInt32());

        Assert.Equal(
            0,
            root.GetProperty("LessonCount").GetInt32());

        Assert.Equal(
            0,
            root.GetProperty("LinkCount").GetInt32());

        var nodes =
            root.GetProperty("Nodes")
                .EnumerateArray()
                .ToArray();

        Assert.Equal(
            888,
            nodes.Length);

        Assert.All(
            nodes,
            node =>
                Assert.Equal(
                    JsonValueKind.Null,
                    node.GetProperty("OfficialText").ValueKind));

        var counts =
            manifest.RootElement
                .GetProperty("Counts");

        Assert.Equal(
            296,
            counts.GetProperty("Primary0096").GetInt32());

        Assert.Equal(
            187,
            counts.GetProperty("Lower0862").GetInt32());

        Assert.Equal(
            53,
            counts.GetProperty("Igcse0580CoreSections").GetInt32());

        Assert.Equal(
            100,
            counts.GetProperty("Igcse0580CoreLeafReferences").GetInt32());

        Assert.Equal(
            72,
            counts.GetProperty("Igcse0580ExtendedSections").GetInt32());

        Assert.Equal(
            158,
            counts.GetProperty("Igcse0580ExtendedLeafReferences").GetInt32());

        Assert.Equal(
            38,
            counts.GetProperty("Alevel9709TopicReferences").GetInt32());

        Assert.Equal(
            779,
            counts.GetProperty("OfficialReferenceIdentifiers").GetInt32());

        Assert.Equal(
            888,
            counts.GetProperty("Nodes").GetInt32());
    }

    [Fact]
    public async Task CambridgeSeederIsIdempotentAndCreatesOnlyReviewedStageOneLessons()
    {
        await using var db =
            Db(
                "cambridge-v2-" +
                Guid.NewGuid().ToString("N"));

        var packSeeder =
            new MathematicsCurriculumPackSeeder(db);

        await packSeeder.SeedAsync();
        await packSeeder.SeedAsync();

        var state =
            await db.CurriculumPackImportStates
                .SingleAsync(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry.CambridgeCode);

        Assert.Equal(
            779,
            state.OfficialNodeCount);

        Assert.Equal(
            888,
            state.NodeCount);

        Assert.Equal(
            888,
            await db.CurriculumPackContentNodes
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                        state.FrameworkVersionId));

        Assert.False(
            await db.CurriculumPackContentNodes
                .AnyAsync(
                    x =>
                        x.FrameworkVersionId ==
                            state.FrameworkVersionId &&
                        x.OfficialText != null));

        var pedagogical =
            new MathematicsPedagogicalLessonSeeder(db);

        await pedagogical.SeedAsync();
        await pedagogical.SeedAsync();

        var lessons =
            await db.CurriculumPedagogicalLessons
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        state.FrameworkVersionId)
                .OrderBy(x => x.SortOrder)
                .ToArrayAsync();

        Assert.Equal(
            27,
            lessons.Length);

        Assert.All(
            lessons,
            lesson =>
            {
                Assert.Equal(
                    1,
                    lesson.LogicalLevelFrom);

                Assert.Equal(
                    1,
                    lesson.LogicalLevelTo);

                Assert.Equal(
                    "Cambridge Primary Stage 1",
                    lesson.NativeLevel);

                Assert.StartsWith(
                    "PED:CAMBRIDGE-INTL-MATH:S1:",
                    lesson.Code,
                    StringComparison.Ordinal);
            });

        var lessonIds =
            lessons
                .Select(x => x.Id)
                .ToArray();

        var mappings =
            await (
                from mapping in
                    db.CurriculumPedagogicalLessonOutcomes
                join node in
                    db.CurriculumPackContentNodes
                    on mapping.OutcomeNodeId equals node.Id
                where
                    mapping.FrameworkVersionId ==
                        state.FrameworkVersionId &&
                    lessonIds.Contains(
                        mapping.PedagogicalLessonId)
                select node.Code)
                .ToArrayAsync();

        Assert.Equal(
            36,
            mappings.Length);

        Assert.Equal(
            36,
            mappings
                .Distinct(StringComparer.Ordinal)
                .Count());

        Assert.All(
            mappings,
            code =>
                Assert.StartsWith(
                    "CAM:OUT:0096:1",
                    code,
                    StringComparison.Ordinal));

        Assert.False(
            await db.CurriculumPedagogicalLessons
                .AnyAsync(
                    x =>
                        x.FrameworkVersionId ==
                            state.FrameworkVersionId &&
                        (
                            x.LogicalLevelFrom != 1 ||
                            x.LogicalLevelTo != 1
                        )));
    }

    [Fact]
    public async Task SeederRetiresHistoricalEnglandWithoutConvertingItsIdentity()
    {
        await using var db =
            Db(
                "cambridge-retire-england-" +
                Guid.NewGuid().ToString("N"));

        var now =
            DateTime.UtcNow;

        var framework =
            new CurriculumFramework
            {
                Id = Guid.NewGuid(),
                OwnerSchoolId = null,
                Code = "UK-" + "NC-ENG-MATH",
                NormalizedCode = "UK-" + "NC-ENG-MATH",
                Name = "Historical England Mathematics",
                CountryCode = "GB",
                ProviderName = "Historical authority",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

        var version =
            new CurriculumFrameworkVersion
            {
                Id = Guid.NewGuid(),
                FrameworkId = framework.Id,
                VersionCode = "HISTORICAL-TEST",
                NormalizedVersionCode = "HISTORICAL-TEST",
                Name = "Historical England test version",
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

        db.CurriculumFrameworks.Add(framework);
        db.CurriculumFrameworkVersions.Add(version);

        await db.SaveChangesAsync();

        await new MathematicsCurriculumPackSeeder(db)
            .SeedAsync();

        db.ChangeTracker.Clear();

        var historicalFramework =
            await db.CurriculumFrameworks
                .SingleAsync(
                    x =>
                        x.Id ==
                        framework.Id);

        var historicalVersion =
            await db.CurriculumFrameworkVersions
                .SingleAsync(
                    x =>
                        x.Id ==
                        version.Id);

        Assert.False(
            historicalFramework.IsActive);

        Assert.False(
            historicalVersion.IsActive);

        Assert.Equal(
            "UK-" + "NC-ENG-MATH",
            historicalFramework.Code);

        Assert.True(
            await db.CurriculumFrameworks
                .AnyAsync(
                    x =>
                        x.Code ==
                        MathematicsCurriculumPackRegistry.CambridgeCode &&
                        x.IsActive));
    }
}
