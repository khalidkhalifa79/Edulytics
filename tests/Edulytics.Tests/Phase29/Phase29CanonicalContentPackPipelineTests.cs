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
    public void PublishedContentRequiresAcademicLanguageAndReviewEvidence()
    {
        var document = ValidDocument();

        document.Lessons[0].Translations.Clear();

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
    public void PreviousOfficialFallbackRequiresExplicitReason()
    {
        var document = ValidDocument();

        document.SourceResolution =
            CurriculumSourceResolutionStatus.PreviousOfficialFallback;

        document.SourceCurriculumPeriod = "2025-2026";
        document.TargetCurriculumPeriod = "2026-2027";
        document.FallbackReason = string.Empty;

        var error =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CanonicalLessonContentPackContract.Validate(
                        document));

        Assert.Contains(
            "FallbackReason",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PreviousOfficialFallbackIsValidWhenFullyTraceable()
    {
        var document = ValidDocument();

        document.SourceResolution =
            CurriculumSourceResolutionStatus.PreviousOfficialFallback;

        document.TargetCurriculumPeriod = "2026-2027";
        document.SourceCurriculumPeriod = "2025-2026";
        document.SourceVersionLabel =
            "Previous official mathematics curriculum";
        document.FallbackReason =
            "The intended newer official source is not yet available or cannot be verified reliably.";

        CanonicalLessonContentPackContract.Validate(
            document);
    }

    [Fact]
    public void CurrentOfficialCannotCarryFallbackReason()
    {
        var document = ValidDocument();

        document.SourceResolution =
            CurriculumSourceResolutionStatus.CurrentOfficial;

        document.FallbackReason =
            "This must not be present.";

        var error =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CanonicalLessonContentPackContract.Validate(
                        document));

        Assert.Contains(
            "FallbackReason",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void PublishedContentRequiresTraceableReviewMethod()
    {
        var document = ValidDocument();

        document.ReviewMethod = string.Empty;

        var error =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CanonicalLessonContentPackContract.Validate(
                        document));

        Assert.Contains(
            "ReviewMethod",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SourcePolicyV2_RejectsGenericLessonShellTitle()
    {
        var document = ValidDocument();

        document.Lessons[0]
            .Translations
            .Single(x => x.CultureCode == "en")
            .Title =
                "6.EE — Lesson 01";

        var error =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CanonicalLessonContentPackContract.Validate(
                        document));

        Assert.Contains(
            "generic synthetic title",
            error.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SourcePolicyV2_PublisherFallbackRequiresSelectionEvidence()
    {
        var document = ValidDocument();

        document.PedagogicalSourceType =
            PedagogicalSourceType
                .WidelyUsedPublisherTextbook;

        document.PedagogicalSourceTitle =
            "Publisher mathematics textbook";

        document.PedagogicalSourcePublisher =
            "Publisher";

        document.PedagogicalSourceSelectionReason =
            "No school-adopted or current official textbook is available.";

        document.PedagogicalSourceSelectionEvidence =
            string.Empty;

        var error =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CanonicalLessonContentPackContract.Validate(
                        document));

        Assert.Contains(
            "PedagogicalSourceSelectionEvidence",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SourcePolicyV2_SchoolTextbookRequiresAdoptionEvidence()
    {
        var document = ValidDocument();

        document.PedagogicalSourceType =
            PedagogicalSourceType
                .SchoolAdoptedTextbook;

        document.PedagogicalSourceSelectionReason =
            "The school selected this textbook for the target year.";

        document.PedagogicalSourceSelectionEvidence =
            string.Empty;

        var error =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CanonicalLessonContentPackContract.Validate(
                        document));

        Assert.Contains(
            "PedagogicalSourceSelectionEvidence",
            error.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void SourcePolicyV2_RequiresTraceableLessonTitleOrigin()
    {
        var document = ValidDocument();

        document.Lessons[0]
            .TitleProvenance =
                LessonTitleProvenance.LegacyUnspecified;

        var error =
            Assert.Throws<InvalidOperationException>(
                () =>
                    CanonicalLessonContentPackContract.Validate(
                        document));

        Assert.Contains(
            "TitleProvenance",
            error.Message,
            StringComparison.Ordinal);
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
                        MathematicsCurriculumPackRegistry.CommonCoreCode);

        var fixture =
            await SelectSingleMappedLessonFixtureAsync(
                db,
                state.FrameworkVersionId);

        var document =
            ValidDocument(
                state.VersionCode,
                fixture.LessonCode,
                fixture.OutcomeCode);

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
            fixture.LessonId,
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

        Assert.Equal(
            2,
            translations.Length);

        Assert.Equal(
            new[] { "en", "pl" },
            translations.Select(
                x =>
                    x.CultureCode));
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
                        MathematicsCurriculumPackRegistry.CommonCoreCode);

        var fixture =
            await SelectSingleMappedLessonFixtureAsync(
                db,
                state.FrameworkVersionId);

        var document =
            ValidDocument(
                state.VersionCode,
                fixture.LessonCode,
                "NOT-A-REAL-OFFICIAL-OUTCOME");

        var seeder =
            new MathematicsCanonicalLessonContentSeeder(
                db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () =>
                seeder.SeedDocumentsAsync(
                    [document]));

        Assert.Empty(
            await db.CurriculumLessonContents
                .ToArrayAsync());
    }

    [Fact]
    public void EmbeddedUaePilotIsPublishedReviewedAndExactlyMapped()
    {
        var documents =
            MathematicsCanonicalLessonContentSeeder
                .LoadEmbeddedDocuments();

        var uae =
            Assert.Single(documents, x =>
                        x.PackCode ==
                        MathematicsCurriculumPackRegistry.UaeCode);

        Assert.Equal(
            "MOE-2026-2027-T1",
            uae.VersionCode);

        Assert.Equal(
            CurriculumSourceResolutionStatus.CurrentOfficial,
            uae.SourceResolution);

        Assert.Equal(
            2,
            uae.SourcePolicyVersion);

        Assert.Equal(
            PedagogicalSourceType.CurrentOfficialTextbook,
            uae.PedagogicalSourceType);

        Assert.False(
            string.IsNullOrWhiteSpace(
                uae.PedagogicalSourceRightsNote));

        Assert.Equal(
            CanonicalLessonContentStatus.Published,
            uae.Status);

        Assert.Equal(
            "Edulytics Curriculum Review",
            uae.ReviewedBy);

        var lesson =
            Assert.Single(uae.Lessons);

        Assert.Equal(
            "PED:UAE:G9:ADV:T1:L1-2",
            lesson.LessonCode);

        Assert.Equal(
            LessonTitleProvenance.PedagogicalSource,
            lesson.TitleProvenance);

        Assert.False(
            string.IsNullOrWhiteSpace(
                lesson.TitleSourceReference));

        Assert.Equal(
            new[]
            {
                "UAE:STD:MAT.2.02.01",
                "UAE:STD:MAT.2.02.02"
            },
            lesson.OutcomeCodes);

        Assert.Equal(
            new[] { "en", "pl" },
            lesson.Translations
                .Select(x => x.CultureCode)
                .OrderBy(x => x)
                .ToArray());
    }

    [Fact]
    public async Task EmbeddedUaePilotSeedsThroughFullCurriculumChain()
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
                        MathematicsCurriculumPackRegistry.UaeCode);

        var lesson =
            await db.CurriculumPedagogicalLessons
                .SingleAsync(
                    x =>
                        x.FrameworkVersionId ==
                            state.FrameworkVersionId &&
                        x.Code ==
                            "PED:UAE:G9:ADV:T1:L1-2");

        var mappedOutcomeCodes =
            await (
                from mapping in
                    db.CurriculumPedagogicalLessonOutcomes
                join node in
                    db.CurriculumPackContentNodes
                    on mapping.OutcomeNodeId equals node.Id
                where
                    mapping.FrameworkVersionId ==
                        state.FrameworkVersionId &&
                    mapping.PedagogicalLessonId ==
                        lesson.Id
                orderby node.Code
                select node.Code)
            .ToArrayAsync();

        Assert.Equal(
            new[]
            {
                "UAE:STD:MAT.2.02.01",
                "UAE:STD:MAT.2.02.02"
            },
            mappedOutcomeCodes);

        var canonicalSeeder =
            new MathematicsCanonicalLessonContentSeeder(db);

        await canonicalSeeder.SeedAsync();

        var content =
            await db.CurriculumLessonContents
                .SingleAsync(
                    x =>
                        x.PedagogicalLessonId ==
                            lesson.Id);

        Assert.Equal(
            CanonicalLessonContentStatus.Published,
            content.Status);

        Assert.Equal(
            "uae-g9-adv-t1-pilot-v1",
            content.ContentVersion);

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
            translations
                .Select(x => x.CultureCode)
                .ToArray());

        Assert.All(
            translations,
            x =>
            {
                Assert.False(
                    string.IsNullOrWhiteSpace(
                        x.Explanation));

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        x.KeyConceptsAndRules));

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        x.WorkedExamples));

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        x.StepByStepSolutions));

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        x.CommonMistakes));

                Assert.False(
                    string.IsNullOrWhiteSpace(
                        x.QuickSummary));
            });
    }

    private static async Task<(
        Guid LessonId,
        string LessonCode,
        string OutcomeCode)>
        SelectSingleMappedLessonFixtureAsync(
            EdulyticsDbContext db,
            Guid frameworkVersionId)
    {
        var mappings =
            await db.CurriculumPedagogicalLessonOutcomes
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        frameworkVersionId)
                .ToArrayAsync();

        var mappedOnce =
            mappings
                .GroupBy(
                    x =>
                        x.PedagogicalLessonId)
                .Where(
                    group =>
                        group.Count() == 1)
                .Select(
                    group =>
                        group.Key)
                .ToHashSet();

        if (mappedOnce.Count == 0)
        {
            throw new InvalidOperationException(
                "Fixture requires a lesson with exactly one formal outcome mapping.");
        }

        var lessons =
            await db.CurriculumPedagogicalLessons
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        frameworkVersionId)
                .OrderBy(
                    x =>
                        x.SortOrder)
                .ThenBy(
                    x =>
                        x.Code)
                .ToArrayAsync();

        var lesson =
            lessons.First(
                x =>
                    mappedOnce.Contains(
                        x.Id));

        var mapping =
            mappings.Single(
                x =>
                    x.PedagogicalLessonId ==
                    lesson.Id);

        var outcomeCode =
            await db.CurriculumPackContentNodes
                .Where(
                    x =>
                        x.Id ==
                        mapping.OutcomeNodeId)
                .Select(
                    x =>
                        x.Code)
                .SingleAsync();

        return (
            lesson.Id,
            lesson.Code,
            outcomeCode);
    }

    private static CanonicalLessonContentPackDocument ValidDocument(
        string versionCode = "TEST-VERSION",
        string lessonCode = "PED:TEST:LESSON",
        string outcomeCode = "TEST:OUTCOME") =>
        new()
        {
            PackCode =
                MathematicsCurriculumPackRegistry.CommonCoreCode,

            VersionCode = versionCode,
            ContentVersion = "reviewed-v1",
            AcademicLanguage = "en",
            CurriculumTranslationRequired = false,
            SourcePolicyVersion = 2,

            TargetCurriculumPeriod = "2026-2027",
            SourceCurriculumPeriod = "2026-2027",
            SourceVersionLabel = "Official source fixture",
            SourceAuthority = "Official curriculum authority",
            SourceUrl = "https://example.gov/official-curriculum",
            SourceCheckedAtUtc = "2026-08-29T00:00:00Z",
            SourceResolution =
                CurriculumSourceResolutionStatus.CurrentOfficial,
            FallbackReason = string.Empty,

            PedagogicalSourceType =
                PedagogicalSourceType.CurrentOfficialTextbook,

            PedagogicalSourceTitle =
                "Current official mathematics textbook",

            PedagogicalSourcePublisher =
                "Official curriculum authority",

            PedagogicalSourceEdition =
                "2026-2027",

            PedagogicalSourceUrl =
                "https://example.gov/official-mathematics-textbook",

            PedagogicalSourceCheckedAtUtc =
                "2026-08-29T00:00:00Z",

            PedagogicalSourceSelectionReason =
                "Current official target-year pedagogical material is available.",

            PedagogicalSourceSelectionEvidence =
                string.Empty,

            PedagogicalSourceRightsNote =
                "Reference-only fixture; Edulytics content is independently authored.",

            ReviewMethod =
                "Official-source alignment plus mathematical and pedagogical verification.",

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

                    TitleProvenance =
                        LessonTitleProvenance
                            .EdulyticsDerivedFromOfficialOutcome,

                    TitleSourceReference =
                        outcomeCode,

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
