using Edulytics.Core.Curriculum;
using Edulytics.Data.Contexts;
using Edulytics.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29PedagogicalLessonArchitectureTests
{
    [Fact]
    public async Task SeederUsesOfficialUaeLessonsAndEdulyticsBlueprintsForTheOtherThreePacks()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db).SeedAsync();

        var seeder = new MathematicsPedagogicalLessonSeeder(db);
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        Assert.Equal(
            313,
            await db.CurriculumPedagogicalLessons.CountAsync());

        Assert.Equal(
            48,
            await db.CurriculumPedagogicalLessonOutcomes.CountAsync());

        var states = await db.CurriculumPackImportStates
            .ToDictionaryAsync(x => x.FrameworkCode);

        async Task<int> CountLessons(string code) =>
            await db.CurriculumPedagogicalLessons.CountAsync(
                x => x.FrameworkVersionId == states[code].FrameworkVersionId);

        Assert.Equal(
            78,
            await CountLessons(MathematicsCurriculumPackRegistry.EnglandCode));

        Assert.Equal(
            91,
            await CountLessons(MathematicsCurriculumPackRegistry.CommonCoreCode));

        Assert.Equal(
            102,
            await CountLessons(MathematicsCurriculumPackRegistry.PolandCode));

        Assert.Equal(
            42,
            await CountLessons(MathematicsCurriculumPackRegistry.UaeCode));

        Assert.Equal(
            42,
            await db.CurriculumPedagogicalLessons.CountAsync(
                x =>
                    x.FrameworkVersionId ==
                        states[MathematicsCurriculumPackRegistry.UaeCode].FrameworkVersionId &&
                    x.OfficialLessonNodeId != null));

        Assert.Equal(
            0,
            await db.CurriculumPedagogicalLessons.CountAsync(
                x =>
                    x.FrameworkVersionId !=
                        states[MathematicsCurriculumPackRegistry.UaeCode].FrameworkVersionId &&
                    x.OfficialLessonNodeId != null));
    }

    [Fact]
    public async Task NonUaeBlueprintLessonsDoNotInventOfficialOutcomeMappings()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db).SeedAsync();
        await new MathematicsPedagogicalLessonSeeder(db).SeedAsync();

        var uaeVersionId = await db.CurriculumPackImportStates
            .Where(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.UaeCode)
            .Select(x => x.FrameworkVersionId)
            .SingleAsync();

        Assert.DoesNotContain(
            await db.CurriculumPedagogicalLessonOutcomes.ToArrayAsync(),
            x => x.FrameworkVersionId != uaeVersionId);
    }

    [Fact]
    public void CommonCoreGradeSixRemainsLogicalLevelSeven()
    {
        var us = MathematicsCurriculumPackRegistry.All.Single(
            x => x.Code == MathematicsCurriculumPackRegistry.CommonCoreCode);

        var gradeSix = Assert.Single(
            us.Levels,
            x => x.NativeLabel == "Grade 6");

        Assert.Equal(7, gradeSix.LogicalLevel);
    }

    [Fact]
    public void CanonicalContentForeignKeyTargetsPedagogicalLesson()
    {
        using var db = CreateDb();

        var entity = db.Model.FindEntityType(
            "Edulytics.Core.Entities.CurriculumLessonContent");

        Assert.NotNull(entity);

        var property = entity!.FindProperty("PedagogicalLessonId");
        Assert.NotNull(property);
        Assert.Equal("LessonNodeId", property!.GetColumnName());

        var fk = Assert.Single(
            entity.GetForeignKeys(),
            x => x.Properties.Any(
                p => p.Name == "PedagogicalLessonId"));

        Assert.Equal(
            "Edulytics.Core.Entities.CurriculumPedagogicalLesson",
            fk.PrincipalEntityType.Name);
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EdulyticsDbContext>()
            .UseInMemoryDatabase(
                "p29-pedagogical-" + Guid.NewGuid())
            .Options;

        return new EdulyticsDbContext(options);
    }
}
