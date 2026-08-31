using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Edulytics.Core.Curriculum;
using Edulytics.Data.Contexts;
using Edulytics.Data.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase275Content;

public sealed class MathematicsCurriculumVerifiedPersistenceTests
{
    private static EdulyticsDbContext Db(string name) =>
        new(new DbContextOptionsBuilder<EdulyticsDbContext>()
            .UseInMemoryDatabase(name)
            .Options);


    private static JsonDocument LoadCommonCoreIntegrityManifest()
    {
        var assembly =
            typeof(MathematicsCurriculumPackRegistry)
                .Assembly;

        var name =
            assembly.GetManifestResourceNames()
                .Single(
                    x =>
                        x.EndsWith(
                            "us-ccss-math.integrity-manifest.json",
                            StringComparison.OrdinalIgnoreCase));

        using var stream =
            assembly.GetManifestResourceStream(
                name)
            ?? throw new InvalidOperationException(
                "Common Core integrity manifest not found.");

        return JsonDocument.Parse(
            stream);
    }

    private static string Sha256(
        string value)
    {
        return Convert
            .ToHexString(
                SHA256.HashData(
                    Encoding.UTF8.GetBytes(
                        value)))
            .ToLowerInvariant();
    }

    [Fact]
    public async Task AcceptedPacks_SeedWithExactVerifiedCounts_AndNoSyntheticLessonShells()
    {
        await using var db = Db("p275-v19-" + Guid.NewGuid().ToString("N"));
        var seeder = new MathematicsCurriculumPackSeeder(db);
        await seeder.SeedAsync();

        var states = await db.CurriculumPackImportStates.AsNoTracking().ToListAsync();
        Assert.Equal(4, states.Count);
        Assert.Equal(
            779,
            states.Single(
                x =>
                    x.FrameworkCode ==
                    MathematicsCurriculumPackRegistry.CambridgeCode)
                .OfficialNodeCount);
        Assert.Equal(393, states.Single(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.CommonCoreCode).OfficialNodeCount);
        Assert.Equal(306, states.Single(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.PolandCode).OfficialNodeCount);

        var uae = states.Single(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.UaeCode);
        Assert.Equal("MOE-2026-2027-T1", uae.VersionCode);
        Assert.Equal(22, uae.OfficialNodeCount);
        Assert.Equal(6, uae.UnitCount);
        Assert.Equal(42, uae.LessonCount);
        Assert.Equal(48, uae.LinkCount);

        Assert.Equal(42, await db.CurriculumPackContentNodes.CountAsync(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.UaeCode && x.NodeKind == "Lesson"));
        Assert.Equal(48, await db.CurriculumPackNodeLinks.CountAsync(x => x.LinkKind == "LessonStandardAlignment"));
        Assert.False(await db.CurriculumPackContentNodes.AnyAsync(x => x.Code.StartsWith("EDU:")));
    }

    [Fact]
    public async Task SeedingTwice_IsIdempotent_AndEveryVerifiedUaeLessonIsLinked()
    {
        var name = "p275-v19-idem-" + Guid.NewGuid().ToString("N");
        await using var db = Db(name);
        var seeder = new MathematicsCurriculumPackSeeder(db);
        await seeder.SeedAsync();
        var nodes = await db.CurriculumPackContentNodes.CountAsync();
        var links = await db.CurriculumPackNodeLinks.CountAsync();

        await seeder.SeedAsync();
        Assert.Equal(nodes, await db.CurriculumPackContentNodes.CountAsync());
        Assert.Equal(links, await db.CurriculumPackNodeLinks.CountAsync());

        var uaeVersion = await db.CurriculumPackImportStates
            .Where(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.UaeCode)
            .Select(x => x.FrameworkVersionId)
            .SingleAsync();

        var lessonIds = await db.CurriculumPackContentNodes
            .Where(x => x.FrameworkVersionId == uaeVersion && x.NodeKind == "Lesson")
            .Select(x => x.Id)
            .ToListAsync();

        var linkedIds = await db.CurriculumPackNodeLinks
            .Where(x => x.FrameworkVersionId == uaeVersion && x.LinkKind == "LessonStandardAlignment")
            .Select(x => x.FromNodeId)
            .Distinct()
            .ToListAsync();

        Assert.Equal(42, lessonIds.Count);
        Assert.Equal(lessonIds.OrderBy(x => x), linkedIds.OrderBy(x => x));
    }

    [Fact]
    public async Task CommonCoreCorrectedPack_MatchesAuthoritativeIntegrityManifest()
    {
        var name =
            "p275-ccss-integrity-" +
            Guid.NewGuid().ToString("N");

        await using var db =
            Db(name);

        var seeder =
            new MathematicsCurriculumPackSeeder(
                db);

        await seeder.SeedAsync();

        var state =
            await db.CurriculumPackImportStates
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry.CommonCoreCode);

        Assert.Equal(
            459,
            state.NodeCount);

        Assert.Equal(
            393,
            state.OfficialNodeCount);

        using var manifest =
            LoadCommonCoreIntegrityManifest();

        var root =
            manifest.RootElement;

        Assert.Equal(
            "1dc360aa21390c2860c939f731b693295ee1537cbb2b2e3be2ccd06dcb06898c",
            root.GetProperty(
                    "SourcePdfSha256")
                .GetString());

        Assert.Equal(
            state.ContentDigest,
            root.GetProperty(
                    "CorrectedContentDigest")
                .GetString());

        Assert.Equal(
            385,
            root.GetProperty(
                    "NumberedStandardCount")
                .GetInt32());

        Assert.Equal(
            229,
            root.GetProperty(
                    "K8NumberedStandardCount")
                .GetInt32());

        Assert.Equal(
            156,
            root.GetProperty(
                    "HighSchoolNumberedStandardCount")
                .GetInt32());

        Assert.Equal(
            140,
            root.GetProperty(
                    "TrailingContaminationRepairs")
                .GetInt32());

        var expectedHashes =
            root.GetProperty(
                    "NumberedStandardTextSha256")
                .EnumerateObject()
                .ToDictionary(
                    x => x.Name,
                    x =>
                        x.Value.GetString()
                        ?? string.Empty,
                    StringComparer.Ordinal);

        Assert.Equal(
            385,
            expectedHashes.Count);

        var versionId =
            state.FrameworkVersionId;

        var nodes =
            await db.CurriculumPackContentNodes
                .AsNoTracking()
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        versionId)
                .ToArrayAsync();

        Assert.Equal(
            459,
            nodes.Length);

        Assert.Equal(
            66,
            nodes.Count(
                x =>
                    x.NodeKind ==
                    "Domain"));

        var numbered =
            nodes
                .Where(
                    x =>
                        expectedHashes.ContainsKey(
                            x.Code))
                .ToArray();

        Assert.Equal(
            385,
            numbered.Length);

        foreach (var node in numbered)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(
                    node.OfficialText));

            Assert.Equal(
                expectedHashes[node.Code],
                Sha256(
                    node.OfficialText!));
        }

        Assert.Equal(
            29,
            numbered.Count(
                x =>
                    x.Code.StartsWith(
                        "CCSS:6.",
                        StringComparison.Ordinal)));

        Assert.Equal(
            24,
            numbered.Count(
                x =>
                    x.Code.StartsWith(
                        "CCSS:7.",
                        StringComparison.Ordinal)));

        Assert.Equal(
            28,
            numbered.Count(
                x =>
                    x.Code.StartsWith(
                        "CCSS:8.",
                        StringComparison.Ordinal)));

        Assert.Equal(
            156,
            numbered.Count(
                x =>
                    x.Code.StartsWith(
                        "CCSS:HS",
                        StringComparison.Ordinal)));

        Assert.Equal(
            9,
            numbered.Count(
                x =>
                    x.Code.StartsWith(
                        "CCSS:HSS-CP.",
                        StringComparison.Ordinal)));
    }

    [Fact]
    public async Task CommonCoreLegacy420_360State_RepairsInPlace_AndPreservesExistingPedagogicalLessons()
    {
        var name =
            "p275-ccss-legacy-repair-" +
            Guid.NewGuid().ToString("N");

        await using var db =
            Db(name);

        var curriculumSeeder =
            new MathematicsCurriculumPackSeeder(
                db);

        await curriculumSeeder.SeedAsync();

        var pedagogicalSeeder =
            new MathematicsPedagogicalLessonSeeder(
                db);

        await pedagogicalSeeder.SeedAsync();

        var state =
            await db.CurriculumPackImportStates
                .SingleAsync(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry.CommonCoreCode);

        var versionId =
            state.FrameworkVersionId;

        var before =
            await db.CurriculumPedagogicalLessons
                .AsNoTracking()
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                            versionId)
                .ToDictionaryAsync(
                    x => x.Code,
                    x => new
                    {
                        x.Title,
                        x.SortOrder
                    });

        // The regression intentionally downgrades only the official
        // Common Core pack to the exact historical 420/360 state.
        //
        // The current pedagogical graph already exists before that
        // downgrade. This mirrors the actual preservation invariant:
        // repairing official curriculum data must not rewrite or lose
        // valid current pedagogical lesson identities.
        db.ChangeTracker.Clear();

        state =
            await db.CurriculumPackImportStates
                .SingleAsync(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry.CommonCoreCode);

        using var manifest =
            LoadCommonCoreIntegrityManifest();

        var root =
            manifest.RootElement;

        var legacy =
            root.GetProperty(
                "Legacy");

        var missingCodes =
            legacy.GetProperty(
                    "MissingNodeCodes")
                .EnumerateArray()
                .Select(
                    x =>
                        x.GetString()
                        ?? string.Empty)
                .ToArray();

        var legacyChangedHashes =
            legacy.GetProperty(
                    "ChangedNodeContentHashes")
                .EnumerateObject()
                .ToDictionary(
                    x => x.Name,
                    x =>
                        x.Value.GetString()
                        ?? string.Empty,
                    StringComparer.Ordinal);

        Assert.Equal(
            38,
            missingCodes.Length);

        Assert.Equal(
            140,
            legacyChangedHashes.Count);

        var rowsToRemove =
            await db.CurriculumPackContentNodes
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                            versionId &&
                        (missingCodes.Contains(
                             x.Code) ||
                         x.Code ==
                            "CCSS:1.OA.B.3"))
                .ToArrayAsync();

        Assert.Equal(
            39,
            rowsToRemove.Length);

        db.CurriculumPackContentNodes
            .RemoveRange(
                rowsToRemove);

        await db.SaveChangesAsync();

        // V14 inserted B3 at semantic SortOrder 20.
        // Recreate the exact accepted V13 ordering/hash state
        // before applying the older 420/360 content fingerprint.
        var previousCorrected =
            root.GetProperty(
                "PreviousCorrected");

        var previousChangedHashes =
            previousCorrected.GetProperty(
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
            previousChangedHashes.Count);

        var previousChangedCodes =
            previousChangedHashes
                .Keys
                .ToArray();

        var previousChangedRows =
            await db.CurriculumPackContentNodes
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                            versionId &&
                        previousChangedCodes.Contains(
                            x.Code))
                .ToArrayAsync();

        Assert.Equal(
            401,
            previousChangedRows.Length);

        foreach (var row in
                 previousChangedRows)
        {
            row.SortOrder--;

            row.ContentHash =
                previousChangedHashes[
                    row.Code];
        }

        var changedCodes =
            legacyChangedHashes
                .Keys
                .ToArray();

        var legacyChangedRows =
            await db.CurriculumPackContentNodes
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                            versionId &&
                        changedCodes.Contains(
                            x.Code))
                .ToArrayAsync();

        Assert.Equal(
            140,
            legacyChangedRows.Length);

        foreach (var row in legacyChangedRows)
        {
            row.ContentHash =
                legacyChangedHashes[
                    row.Code];
        }

        var representativeCorruption =
            legacyChangedRows.Single(
                x =>
                    x.Code ==
                    "CCSS:1.G.A.3");

        representativeCorruption.OfficialText =
            "legacy boundary corruption";

        state.SourceDigest =
            legacy.GetProperty(
                    "SourceDigest")
                .GetString()
            ?? string.Empty;

        state.ContentDigest =
            legacy.GetProperty(
                    "ContentDigest")
                .GetString()
            ?? string.Empty;

        state.NodeCount =
            legacy.GetProperty(
                    "NodeCount")
                .GetInt32();

        state.OfficialNodeCount =
            legacy.GetProperty(
                    "OfficialNodeCount")
                .GetInt32();

        state.UnitCount =
            legacy.GetProperty(
                    "UnitCount")
                .GetInt32();

        state.LessonCount =
            legacy.GetProperty(
                    "LessonCount")
                .GetInt32();

        state.LinkCount =
            legacy.GetProperty(
                    "LinkCount")
                .GetInt32();

        state.IsComplete =
            true;

        await db.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // Production startup repairs/verifies the official curriculum pack
        // before seeding the pedagogical graph. Do not ask the new Grade 6
        // source blueprint to resolve against an intentionally incomplete
        // historical 420/360 fixture.
        await curriculumSeeder.SeedAsync();
        await pedagogicalSeeder.SeedAsync();

        db.ChangeTracker.Clear();

        var repairedState =
            await db.CurriculumPackImportStates
                .AsNoTracking()
                .SingleAsync(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry.CommonCoreCode);

        Assert.Equal(
            459,
            repairedState.NodeCount);

        Assert.Equal(
            393,
            repairedState.OfficialNodeCount);

        var repairedNodes =
            await db.CurriculumPackContentNodes
                .AsNoTracking()
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                            versionId)
                .ToArrayAsync();

        Assert.Equal(
            459,
            repairedNodes.Length);

        Assert.Equal(
            66,
            repairedNodes.Count(
                x =>
                    x.NodeKind ==
                    "Domain"));

        Assert.Equal(
            29,
            repairedNodes.Count(
                x =>
                    x.IsOfficial &&
                    x.NodeKind ==
                        "Standard" &&
                    x.Code.StartsWith(
                        "CCSS:6.",
                        StringComparison.Ordinal)));

        Assert.Equal(
            24,
            repairedNodes.Count(
                x =>
                    x.IsOfficial &&
                    x.NodeKind ==
                        "Standard" &&
                    x.Code.StartsWith(
                        "CCSS:7.",
                        StringComparison.Ordinal)));

        Assert.Equal(
            28,
            repairedNodes.Count(
                x =>
                    x.IsOfficial &&
                    x.NodeKind ==
                        "Standard" &&
                    x.Code.StartsWith(
                        "CCSS:8.",
                        StringComparison.Ordinal)));

        Assert.Equal(
            156,
            repairedNodes.Count(
                x =>
                    x.IsOfficial &&
                    x.NodeKind ==
                        "Standard" &&
                    x.Code.StartsWith(
                        "CCSS:HS",
                        StringComparison.Ordinal)));

        Assert.Equal(
            9,
            repairedNodes.Count(
                x =>
                    x.IsOfficial &&
                    x.NodeKind ==
                        "Standard" &&
                    x.Code.StartsWith(
                        "CCSS:HSS-CP.",
                        StringComparison.Ordinal)));

        var repairedRepresentative =
            repairedNodes.Single(
                x =>
                    x.Code ==
                    "CCSS:1.G.A.3");

        Assert.DoesNotContain(
            "legacy boundary corruption",
            repairedRepresentative.OfficialText
                ?? string.Empty);

        var after =
            await db.CurriculumPedagogicalLessons
                .AsNoTracking()
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                            versionId)
                .ToDictionaryAsync(
                    x => x.Code,
                    x => new
                    {
                        x.Title,
                        x.SortOrder
                    });

        // The current pedagogical graph was already present before the
        // official-pack downgrade. Repairing the known legacy 420/360 state
        // must restore official nodes in place while preserving that graph
        // exactly, including the source-driven Grade 6 blueprint.
        Assert.Equal(
            before.Count,
            after.Count);

        foreach (var previous in before)
        {
            Assert.True(
                after.TryGetValue(
                    previous.Key,
                    out var current));

            Assert.Equal(
                previous.Value.Title,
                current!.Title);

            Assert.Equal(
                previous.Value.SortOrder,
                current.SortOrder);
        }

        // Corrected state must now be idempotent.
        await curriculumSeeder.SeedAsync();

        Assert.Equal(
            459,
            await db.CurriculumPackContentNodes
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                            versionId));
    }

    [Fact]
    public void Registry_ExposesCurrentUae2026_2027SourceWithoutExposingHistoricalYearAsDisplayVersion()
    {
        var uae = MathematicsCurriculumPackRegistry.All.Single(x => x.Code == MathematicsCurriculumPackRegistry.UaeCode);
        Assert.Contains(uae.Sources, x => x.Url == "https://minhaji.moe.gov.ae/" && x.VersionLabel.Contains("2026-2027", StringComparison.Ordinal));
        Assert.Contains("2026-2027", uae.EvidenceNote, StringComparison.Ordinal);
        Assert.Contains("Historical", uae.EvidenceNote, StringComparison.Ordinal);
    }
}
