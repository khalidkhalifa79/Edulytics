using System.Text.Json;
using Edulytics.Core.Curriculum;
using Edulytics.Data.Contexts;
using Edulytics.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29CommonCoreB3RepairTests
{
    private static EdulyticsDbContext Db(
        string name) =>
        new(
            new DbContextOptionsBuilder<
                    EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    name)
                .Options);

    private static JsonDocument
        LoadCommonCoreIntegrityManifest()
    {
        var assembly =
            typeof(
                MathematicsCurriculumPackRegistry)
                .Assembly;

        var name =
            assembly
                .GetManifestResourceNames()
                .Single(
                    x =>
                        x.EndsWith(
                            "us-ccss-math.integrity-manifest.json",
                            StringComparison.OrdinalIgnoreCase));

        using var stream =
            assembly
                .GetManifestResourceStream(
                    name)
            ?? throw new InvalidOperationException(
                "Common Core integrity manifest not found.");

        return JsonDocument.Parse(
            stream);
    }

    [Fact]
    public async Task
        AcceptedV13_458_392_UpgradesExactlyToV14_459_393()
    {
        await using var db =
            Db(
                "p29-ccss-b3-v14-" +
                Guid.NewGuid()
                    .ToString("N"));

        var seeder =
            new MathematicsCurriculumPackSeeder(
                db);

        await seeder.SeedAsync();

        var state =
            await db
                .CurriculumPackImportStates
                .SingleAsync(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry
                            .CommonCoreCode);

        Assert.Equal(
            459,
            state.NodeCount);

        Assert.Equal(
            393,
            state.OfficialNodeCount);

        var versionId =
            state.FrameworkVersionId;

        using var manifest =
            LoadCommonCoreIntegrityManifest();

        var previous =
            manifest
                .RootElement
                .GetProperty(
                    "PreviousCorrected");

        var previousHashes =
            previous
                .GetProperty(
                    "ChangedNodeContentHashes")
                .EnumerateObject()
                .ToDictionary(
                    x => x.Name,
                    x =>
                        x.Value.GetString()
                        ?? string.Empty,
                    StringComparer.Ordinal);

        Assert.Equal(
            439,
            previousHashes.Count);

        var b3 =
            await db
                .CurriculumPackContentNodes
                .SingleAsync(
                    x =>
                        x.FrameworkVersionId ==
                            versionId &&
                        x.Code ==
                            "CCSS:1.OA.B.3");

        Assert.Equal(
            20,
            b3.SortOrder);

        db.CurriculumPackContentNodes.Remove(
            b3);

        var shiftedCodes =
            previousHashes
                .Keys
                .ToArray();

        var shiftedNodes =
            await db
                .CurriculumPackContentNodes
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                            versionId &&
                        shiftedCodes.Contains(
                            x.Code))
                .ToArrayAsync();

        Assert.Equal(
            439,
            shiftedNodes.Length);

        foreach (var row in shiftedNodes)
        {
            row.SortOrder--;

            row.ContentHash =
                previousHashes[
                    row.Code];
        }

        state.SourceDigest =
            previous
                .GetProperty(
                    "SourceDigest")
                .GetString()
            ?? string.Empty;

        state.ContentDigest =
            previous
                .GetProperty(
                    "ContentDigest")
                .GetString()
            ?? string.Empty;

        state.NodeCount =
            previous
                .GetProperty(
                    "NodeCount")
                .GetInt32();

        state.OfficialNodeCount =
            previous
                .GetProperty(
                    "OfficialNodeCount")
                .GetInt32();

        state.UnitCount =
            previous
                .GetProperty(
                    "UnitCount")
                .GetInt32();

        state.LessonCount =
            previous
                .GetProperty(
                    "LessonCount")
                .GetInt32();

        state.LinkCount =
            previous
                .GetProperty(
                    "LinkCount")
                .GetInt32();

        state.IsComplete =
            true;

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        Assert.Equal(
            458,
            await db
                .CurriculumPackContentNodes
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                        versionId));

        Assert.False(
            await db
                .CurriculumPackContentNodes
                .AnyAsync(
                    x =>
                        x.FrameworkVersionId ==
                            versionId &&
                        x.Code ==
                            "CCSS:1.OA.B.3"));

        await seeder.SeedAsync();

        db.ChangeTracker.Clear();

        var repairedState =
            await db
                .CurriculumPackImportStates
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry
                            .CommonCoreCode);

        Assert.Equal(
            459,
            repairedState.NodeCount);

        Assert.Equal(
            393,
            repairedState.OfficialNodeCount);

        var repaired =
            await db
                .CurriculumPackContentNodes
                .AsNoTracking()
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        versionId)
                .OrderBy(
                    x =>
                        x.SortOrder)
                .ToArrayAsync();

        Assert.Equal(
            459,
            repaired.Length);

        Assert.Equal(
            Enumerable.Range(
                1,
                459),
            repaired.Select(
                x =>
                    x.SortOrder));

        Assert.Equal(
            20,
            repaired
                .Single(
                    x =>
                        x.Code ==
                        "CCSS:1.OA.B.3")
                .SortOrder);

        Assert.Equal(
            21,
            repaired
                .Single(
                    x =>
                        x.Code ==
                        "CCSS:1.OA.B.4")
                .SortOrder);

        await seeder.SeedAsync();

        Assert.Equal(
            459,
            await db
                .CurriculumPackContentNodes
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                        versionId));
    }
}
