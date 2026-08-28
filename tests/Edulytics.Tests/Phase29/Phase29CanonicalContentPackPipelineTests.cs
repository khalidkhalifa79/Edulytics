using Edulytics.Core.Curriculum;
using Edulytics.Core.Enums;
using Edulytics.Data.Contexts;
using Edulytics.Data.Seeding;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29CanonicalContentPackPipelineTests
{
    [Fact]
    public void PublishedContentRejectsMissingRequiredBodySection()
    {
        var document = ValidDocument();

        document.Lessons[0]
            .Translations[0]
            .Explanation = string.Empty;

        var error =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CanonicalLessonContentPackContract.Validate(
                        document));

        Assert.Contains(
            "Explanation",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PublishedContentRequiresEnglishPolishAndReviewEvidence()
    {
        var document = ValidDocument();

        document.Lessons[0]
            .Translations.RemoveAll(
                x => x.CultureCode == "pl");

        Assert.Throws<InvalidOperationException>(
            () =>
                CanonicalLessonContentPackContract.Validate(
                    document));

        document = ValidDocument();
        document.ReviewedBy = string.Empty;

        Assert.Throws<InvalidOperationException>(
            () =>
                CanonicalLessonContentPackContract.Validate(
                    document));
    }

    [Fact]
    public async Task SeederUsesExactPedagogicalLessonAndOutcomeAndIsIdempotent()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db)
            .SeedAsync();

        await new MathematicsPedagogicalLessonSeeder(db)
            .SeedAsync();

        var state =
            await db.CurriculumPackImportStates
                .SingleAsync(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry.EnglandCode);

        var lesson =
            await db.CurriculumPedagogicalLessons
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        state.FrameworkVersionId)
                .OrderBy(x => x.SortOrder)
                .FirstAsync();

        var mapping =
            await db.CurriculumPedagogicalLessonOutcomes
                .SingleAsync(
                    x =>
                        x.PedagogicalLessonId ==
                        lesson.Id);

        var outcomeCode =
            await db.CurriculumPackContentNodes
                .Where(
                    x => x.Id == mapping.OutcomeNodeId)
                .Select(x => x.Code)
                .SingleAsync();

        var document =
            ValidDocument(
                state.VersionCode,
                lesson.Code,
                outcomeCode);

        var seeder =
            new MathematicsCanonicalLessonContentSeeder(
                db);

        await seeder.SeedDocumentsAsync([document]);
        await seeder.SeedDocumentsAsync([document]);

        var content =
            Assert.Single(
                await db.CurriculumLessonContents
                    .ToArrayAsync());

        Assert.Equal(
            lesson.Id,
            content.PedagogicalLessonId);

        Assert.Equal(
            CanonicalLessonContentStatus.Published,
            content.Status);

        Assert.NotNull(content.VerifiedAtUtc);
        Assert.NotNull(content.PublishedAtUtc);

        var translations =
            await db.CurriculumLessonContentTranslations
                .Where(
                    x =>
                        x.CurriculumLessonContentId ==
                            content.Id)
                .OrderBy(x => x.CultureCode)
                .ToArrayAsync();

        Assert.Equal(2, translations.Length);

        Assert.Equal(
            new[] { "en", "pl" },
            translations.Select(x => x.CultureCode));
    }

    [Fact]
    public async Task SeederRejectsOutcomeCodeThatDoesNotMatchOfficialAlignment()
    {
        await using var db = CreateDb();

        await new MathematicsCurriculumPackSeeder(db)
            .SeedAsync();

        await new MathematicsPedagogicalLessonSeeder(db)
            .SeedAsync();

        var state =
            await db.CurriculumPackImportStates
                .SingleAsync(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry.EnglandCode);

        var lesson =
            await db.CurriculumPedagogicalLessons
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        state.FrameworkVersionId)
                .OrderBy(x => x.SortOrder)
                .FirstAsync();

        var document =
            ValidDocument(
                state.VersionCode,
                lesson.Code,
                "NOT-A-REAL-OFFICIAL-OUTCOME");

        var seeder =
            new MathematicsCanonicalLessonContentSeeder(
                db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                seeder.SeedDocumentsAsync([document]));

        Assert.Empty(
            await db.CurriculumLessonContents
                .ToArrayAsync());
    }

    private static CanonicalLessonContentPackDocument ValidDocument(
        string versionCode = "TEST-VERSION",
        string lessonCode = "PED:TEST:LESSON",
        string outcomeCode = "TEST:OUTCOME") =>
        new()
        {
            PackCode =
                MathematicsCurriculumPackRegistry.EnglandCode,

            VersionCode = versionCode,
            ContentVersion = "reviewed-v1",
            Status =
                CanonicalLessonContentStatus.Published,

            ReviewedBy =
                "Edulytics curriculum review",

            ReviewEvidence =
                "Product-owner approved reviewed content fixture.",

            Lessons =
            [
                new CanonicalLessonContentPackLesson
                {
                    LessonCode = lessonCode,

                    OutcomeCodes =
                    [
                        outcomeCode
                    ],

                    Translations =
                    [
                        Translation(
                            "en",
                            "Reviewed English lesson"),

                        Translation(
                            "pl",
                            "Zweryfikowana lekcja")
                    ]
                }
            ]
        };

    private static CanonicalLessonContentPackTranslation Translation(
        string culture,
        string title) =>
        new()
        {
            CultureCode = culture,
            Title = title,
            Explanation =
                $"Explanation {culture}",
            KeyConceptsAndRules =
                $"Rules {culture}",
            WorkedExamples =
                $"Worked examples {culture}",
            StepByStepSolutions =
                $"Step by step {culture}",
            CommonMistakes =
                $"Common mistakes {culture}",
            QuickSummary =
                $"Summary {culture}"
        };

    private static EdulyticsDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<
                    EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    "p29-canonical-content-" +
                    Guid.NewGuid())
                .Options;

        return new EdulyticsDbContext(options);
    }
}
