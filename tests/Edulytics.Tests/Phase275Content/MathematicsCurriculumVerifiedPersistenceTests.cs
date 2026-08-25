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

    [Fact]
    public async Task AcceptedPacks_SeedWithExactVerifiedCounts_AndNoSyntheticLessonShells()
    {
        await using var db = Db("p275-v19-" + Guid.NewGuid().ToString("N"));
        var seeder = new MathematicsCurriculumPackSeeder(db);
        await seeder.SeedAsync();

        var states = await db.CurriculumPackImportStates.AsNoTracking().ToListAsync();
        Assert.Equal(4, states.Count);
        Assert.Equal(436, states.Single(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.EnglandCode).OfficialNodeCount);
        Assert.Equal(360, states.Single(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.CommonCoreCode).OfficialNodeCount);
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
    public void Registry_ExposesCurrentUae2026_2027SourceWithoutExposingHistoricalYearAsDisplayVersion()
    {
        var uae = MathematicsCurriculumPackRegistry.All.Single(x => x.Code == MathematicsCurriculumPackRegistry.UaeCode);
        Assert.Contains(uae.Sources, x => x.Url == "https://minhaji.moe.gov.ae/" && x.VersionLabel.Contains("2026-2027", StringComparison.Ordinal));
        Assert.Contains("2026-2027", uae.EvidenceNote, StringComparison.Ordinal);
        Assert.Contains("Historical", uae.EvidenceNote, StringComparison.Ordinal);
    }
}
