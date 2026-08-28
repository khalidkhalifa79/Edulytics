using Edulytics.Core.Curriculum;
using Edulytics.Data.Contexts;
using Edulytics.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29PedagogicalLessonArchitectureTests
{
    [Fact]
    public async Task SeederCreatesOutcomeBackedLessonsForEveryNonUaePackAndPreservesUae()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db).SeedAsync();

        var seeder = new MathematicsPedagogicalLessonSeeder(db);
        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var states = await db.CurriculumPackImportStates
            .ToDictionaryAsync(x => x.FrameworkCode);

        var uaeVersionId =
            states[MathematicsCurriculumPackRegistry.UaeCode].FrameworkVersionId;

        Assert.Equal(
            42,
            await db.CurriculumPedagogicalLessons.CountAsync(
                x =>
                    x.FrameworkVersionId == uaeVersionId &&
                    x.OfficialLessonNodeId != null));

        Assert.Equal(
            48,
            await db.CurriculumPedagogicalLessonOutcomes.CountAsync(
                x => x.FrameworkVersionId == uaeVersionId));

        var nonUaeLessons = await db.CurriculumPedagogicalLessons
            .Where(x => x.FrameworkVersionId != uaeVersionId)
            .ToArrayAsync();

        Assert.NotEmpty(nonUaeLessons);
        Assert.DoesNotContain(nonUaeLessons, x => x.OfficialLessonNodeId != null);

        var nonUaeLessonIds = nonUaeLessons
            .Select(x => x.Id)
            .ToArray();

        var nonUaeMappings = await db.CurriculumPedagogicalLessonOutcomes
            .Where(x => nonUaeLessonIds.Contains(x.PedagogicalLessonId))
            .ToArrayAsync();

        Assert.Equal(nonUaeLessons.Length, nonUaeMappings.Length);

        foreach (var lesson in nonUaeLessons)
        {
            var mapping = Assert.Single(
                nonUaeMappings,
                x => x.PedagogicalLessonId == lesson.Id);

            Assert.Equal(lesson.FrameworkVersionId, mapping.FrameworkVersionId);

            var official = await db.CurriculumPackContentNodes.SingleAsync(
                x => x.Id == mapping.OutcomeNodeId);

            Assert.True(official.IsOfficial);
            Assert.True(official.IsActive);
            Assert.True(
                official.NodeKind is "Standard" or "Outcome",
                $"Unexpected mapped node kind {official.NodeKind} for {official.Code}.");
            Assert.Equal(lesson.FrameworkVersionId, official.FrameworkVersionId);
            Assert.InRange(
                lesson.LogicalLevelFrom,
                official.LogicalLevelFrom,
                official.LogicalLevelTo);
        }

        foreach (var code in new[]
                 {
                     MathematicsCurriculumPackRegistry.EnglandCode,
                     MathematicsCurriculumPackRegistry.CommonCoreCode,
                     MathematicsCurriculumPackRegistry.PolandCode
                 })
        {
            var versionId = states[code].FrameworkVersionId;
            Assert.Contains(
                nonUaeLessons,
                x => x.FrameworkVersionId == versionId);
            Assert.Contains(
                nonUaeMappings,
                x => x.FrameworkVersionId == versionId);
        }
    }

    [Fact]
    public async Task EnglandYearSixHasRealLessonGranularityAndOfficialAlignment()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db).SeedAsync();
        await new MathematicsPedagogicalLessonSeeder(db).SeedAsync();

        var versionId = await db.CurriculumPackImportStates
            .Where(x => x.FrameworkCode == MathematicsCurriculumPackRegistry.EnglandCode)
            .Select(x => x.FrameworkVersionId)
            .SingleAsync();

        var yearSix = await db.CurriculumPedagogicalLessons
            .Where(x =>
                x.FrameworkVersionId == versionId &&
                x.LogicalLevelFrom == 6 &&
                x.LogicalLevelTo == 6)
            .OrderBy(x => x.SortOrder)
            .ToArrayAsync();

        Assert.True(
            yearSix.Length > 6,
            $"Year 6 must contain more than the six old pseudo-unit shells; got {yearSix.Length}.");

        var numberLessons = yearSix
            .Where(x => x.UnitTitle.Contains("Number", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        Assert.True(
            numberLessons.Length > 1,
            $"Year 6 Number must contain multiple pedagogical lessons; got {numberLessons.Length}.");

        var yearSixIds = yearSix.Select(x => x.Id).ToArray();

        var mappedLessonIds = await db.CurriculumPedagogicalLessonOutcomes
            .Where(x => yearSixIds.Contains(x.PedagogicalLessonId))
            .Select(x => x.PedagogicalLessonId)
            .Distinct()
            .ToArrayAsync();

        Assert.Equal(yearSix.Length, mappedLessonIds.Length);
    }

    [Fact]
    public async Task CommonCoreGradeSixIsLogicalSevenAndPolandIsMapped()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db).SeedAsync();
        await new MathematicsPedagogicalLessonSeeder(db).SeedAsync();

        var states = await db.CurriculumPackImportStates
            .ToDictionaryAsync(x => x.FrameworkCode);

        var us = MathematicsCurriculumPackRegistry.All.Single(
            x => x.Code == MathematicsCurriculumPackRegistry.CommonCoreCode);

        var gradeSix = Assert.Single(
            us.Levels,
            x => x.NativeLabel == "Grade 6");

        Assert.Equal(7, gradeSix.LogicalLevel);

        var usVersionId =
            states[MathematicsCurriculumPackRegistry.CommonCoreCode].FrameworkVersionId;

        var usGradeSixLessons = await db.CurriculumPedagogicalLessons
            .Where(x =>
                x.FrameworkVersionId == usVersionId &&
                x.LogicalLevelFrom == 7 &&
                x.LogicalLevelTo == 7 &&
                x.NativeLevel == "Grade 6")
            .ToArrayAsync();

        Assert.True(usGradeSixLessons.Length > 6);

        var usGradeSixIds = usGradeSixLessons.Select(x => x.Id).ToArray();
        Assert.Equal(
            usGradeSixLessons.Length,
            await db.CurriculumPedagogicalLessonOutcomes.CountAsync(
                x => usGradeSixIds.Contains(x.PedagogicalLessonId)));

        var plVersionId =
            states[MathematicsCurriculumPackRegistry.PolandCode].FrameworkVersionId;

        var polishLessons = await db.CurriculumPedagogicalLessons
            .Where(x => x.FrameworkVersionId == plVersionId)
            .ToArrayAsync();

        Assert.NotEmpty(polishLessons);

        var polishIds = polishLessons.Select(x => x.Id).ToArray();
        Assert.Equal(
            polishLessons.Length,
            await db.CurriculumPedagogicalLessonOutcomes.CountAsync(
                x => polishIds.Contains(x.PedagogicalLessonId)));
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
