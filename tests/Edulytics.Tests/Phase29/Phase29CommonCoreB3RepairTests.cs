using System.Text.Json;
using Edulytics.Core.Curriculum;
using Edulytics.Data.Contexts;
using Edulytics.Data.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29CommonCoreB3RepairTests
{
    private static readonly string[] AffectedLessonSuffixes =
    [
        ":CCSS-1-OA-B-4",
        ":CCSS-1-OA-C-5",
        ":CCSS-1-OA-C-6",
        ":CCSS-1-OA-D-7",
        ":CCSS-1-OA-D-8"
    ];

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

    private static bool IsAffectedLesson(
        string code) =>
        AffectedLessonSuffixes.Any(
            suffix =>
                code.EndsWith(
                    suffix,
                    StringComparison.Ordinal));

    private static string PreviousLessonTitle(
        string current)
    {
        const string marker =
            " — Lesson ";

        var markerIndex =
            current.LastIndexOf(
                marker,
                StringComparison.Ordinal);

        if (markerIndex < 0)
        {
            throw new InvalidOperationException(
                $"Unexpected lesson title: {current}");
        }

        var numberStart =
            markerIndex +
            marker.Length;

        if (!int.TryParse(
                current[numberStart..],
                out var number) ||
            number <= 1)
        {
            throw new InvalidOperationException(
                $"Unexpected lesson number: {current}");
        }

        return
            current[..numberStart] +
            (number - 1)
                .ToString("D2");
    }

    [Fact]
    public async Task
        AcceptedV13_458_392_UpgradesToV14_459_393_AndRebaselinesOnlyB3AffectedGrade1Lessons()
    {
        var name =
            "p29-ccss-b3-v14-" +
            Guid.NewGuid()
                .ToString("N");

        await using var db =
            Db(name);

        var curriculumSeeder =
            new MathematicsCurriculumPackSeeder(
                db);

        var pedagogicalSeeder =
            new MathematicsPedagogicalLessonSeeder(
                db);

        // Build the clean V14 target first.
        await curriculumSeeder.SeedAsync();
        await pedagogicalSeeder.SeedAsync();

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

        var targetLessons =
            await db
                .CurriculumPedagogicalLessons
                .AsNoTracking()
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        versionId)
                .ToDictionaryAsync(
                    x => x.Code,
                    x => new
                    {
                        x.Id,
                        x.Title,
                        x.SortOrder
                    });

        var targetB3Lesson =
            targetLessons
                .Single(
                    x =>
                        x.Key.EndsWith(
                            ":CCSS-1-OA-B-3",
                            StringComparison.Ordinal));

        // ----------------------------------------------------
        // Downgrade official curriculum to the exact accepted
        // 458/392 V13 state.
        // ----------------------------------------------------

        using var manifest =
            LoadCommonCoreIntegrityManifest();

        var previous =
            manifest.RootElement
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

        var b3Node =
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
            b3Node.SortOrder);

        var b3Mappings =
            await db
                .CurriculumPedagogicalLessonOutcomes
                .Where(
                    x =>
                        x.PedagogicalLessonId ==
                        targetB3Lesson.Value.Id)
                .ToArrayAsync();

        var b3PedagogicalLesson =
            await db
                .CurriculumPedagogicalLessons
                .SingleAsync(
                    x =>
                        x.Id ==
                        targetB3Lesson.Value.Id);

        db.CurriculumPedagogicalLessonOutcomes
            .RemoveRange(
                b3Mappings);

        db.CurriculumPedagogicalLessons
            .Remove(
                b3PedagogicalLesson);

        db.CurriculumPackContentNodes
            .Remove(
                b3Node);

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

        foreach (var row in
                 shiftedNodes)
        {
            row.SortOrder--;

            row.ContentHash =
                previousHashes[
                    row.Code];
        }

        var affectedLessons =
            await db
                .CurriculumPedagogicalLessons
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                            versionId)
                .ToArrayAsync();

        affectedLessons =
            affectedLessons
                .Where(
                    x =>
                        IsAffectedLesson(
                            x.Code))
                .ToArray();

        Assert.Equal(
            5,
            affectedLessons.Length);

        foreach (var row in
                 affectedLessons)
        {
            row.SortOrder--;

            row.Title =
                PreviousLessonTitle(
                    row.Title);
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

        Assert.False(
            await db
                .CurriculumPedagogicalLessons
                .AnyAsync(
                    x =>
                        x.Id ==
                        targetB3Lesson.Value.Id));

        // ----------------------------------------------------
        // Production startup sequence.
        // ----------------------------------------------------

        await curriculumSeeder.SeedAsync();
        await pedagogicalSeeder.SeedAsync();

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

        var repairedNodes =
            await db
                .CurriculumPackContentNodes
                .AsNoTracking()
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        versionId)
                .OrderBy(
                    x => x.SortOrder)
                .ToArrayAsync();

        Assert.Equal(
            459,
            repairedNodes.Length);

        Assert.Equal(
            Enumerable.Range(
                1,
                459),
            repairedNodes.Select(
                x => x.SortOrder));

        var oa =
            repairedNodes.Single(
                x =>
                    x.Code ==
                    "CCSS:DOMAIN:2-2:1.OA");

        var oaChildren =
            repairedNodes
                .Where(
                    x =>
                        x.ParentId ==
                        oa.Id)
                .OrderBy(
                    x => x.SortOrder)
                .Select(
                    x => x.Code)
                .ToArray();

        Assert.Equal(
            [
                "CCSS:1.OA.A.1",
                "CCSS:1.OA.A.2",
                "CCSS:1.OA.B.3",
                "CCSS:1.OA.B.4",
                "CCSS:1.OA.C.5",
                "CCSS:1.OA.C.6",
                "CCSS:1.OA.D.7",
                "CCSS:1.OA.D.8"
            ],
            oaChildren);

        Assert.Equal(
            20,
            repairedNodes
                .Single(
                    x =>
                        x.Code ==
                        "CCSS:1.OA.B.3")
                .SortOrder);

        Assert.Equal(
            21,
            repairedNodes
                .Single(
                    x =>
                        x.Code ==
                        "CCSS:1.OA.B.4")
                .SortOrder);

        var repairedLessons =
            await db
                .CurriculumPedagogicalLessons
                .AsNoTracking()
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        versionId)
                .ToDictionaryAsync(
                    x => x.Code,
                    x => new
                    {
                        x.Id,
                        x.Title,
                        x.SortOrder
                    });

        Assert.Equal(
            targetLessons.Count,
            repairedLessons.Count);

        foreach (var target in
                 targetLessons)
        {
            Assert.True(
                repairedLessons.TryGetValue(
                    target.Key,
                    out var actual));

            Assert.Equal(
                target.Value.Id,
                actual!.Id);

            Assert.Equal(
                target.Value.Title,
                actual.Title);

            Assert.Equal(
                target.Value.SortOrder,
                actual.SortOrder);
        }

        // Prove target state is now idempotent.
        await curriculumSeeder.SeedAsync();
        await pedagogicalSeeder.SeedAsync();

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
