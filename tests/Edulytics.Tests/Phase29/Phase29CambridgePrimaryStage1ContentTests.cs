using System.Text.RegularExpressions;
using Edulytics.Core.Curriculum;
using Edulytics.Core.Enums;
using Edulytics.Data.Contexts;
using Edulytics.Data.Seeding;
using Edulytics.Web.Presentation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class
    Phase29CambridgePrimaryStage1ContentTests
{
    private static readonly HashSet<string>
        ExpectedStageOneCodes =
        new(
            new[]
            {
                "CAM:OUT:0096:1Nc.01","CAM:OUT:0096:1Nc.02","CAM:OUT:0096:1Nc.03",
                "CAM:OUT:0096:1Nc.04","CAM:OUT:0096:1Nc.05","CAM:OUT:0096:1Nc.06",

                "CAM:OUT:0096:1Ni.01","CAM:OUT:0096:1Ni.02","CAM:OUT:0096:1Ni.03",
                "CAM:OUT:0096:1Ni.04","CAM:OUT:0096:1Ni.05","CAM:OUT:0096:1Ni.06",

                "CAM:OUT:0096:1Nm.01",

                "CAM:OUT:0096:1Np.01","CAM:OUT:0096:1Np.02",
                "CAM:OUT:0096:1Np.03","CAM:OUT:0096:1Np.04",

                "CAM:OUT:0096:1Nf.01","CAM:OUT:0096:1Nf.02",
                "CAM:OUT:0096:1Nf.03","CAM:OUT:0096:1Nf.04",

                "CAM:OUT:0096:1Gt.01","CAM:OUT:0096:1Gt.02","CAM:OUT:0096:1Gt.03",

                "CAM:OUT:0096:1Gg.01","CAM:OUT:0096:1Gg.02","CAM:OUT:0096:1Gg.03",
                "CAM:OUT:0096:1Gg.04","CAM:OUT:0096:1Gg.05","CAM:OUT:0096:1Gg.06",
                "CAM:OUT:0096:1Gg.07","CAM:OUT:0096:1Gg.08",

                "CAM:OUT:0096:1Gp.01",

                "CAM:OUT:0096:1Ss.01","CAM:OUT:0096:1Ss.02","CAM:OUT:0096:1Ss.03"
            },
            StringComparer.Ordinal);

    [Fact]
    public void
        StageOneBlueprintIsExplicitLawfulAndExactlyMapped()
    {
        var blueprint =
            Assert.Single(
                PedagogicalLessonBlueprintRegistry
                    .LoadEmbeddedDocuments(),
                x =>
                    x.PackCode ==
                        MathematicsCurriculumPackRegistry
                            .CambridgeCode &&
                    x.BlueprintCode ==
                        "CAMBRIDGE-PRIMARY-S1:DFE-OGL-V1");

        Assert.Equal(
            2,
            blueprint.SchemaVersion);

        Assert.Equal(
            "CAMBRIDGE-PATHWAY-2026",
            blueprint.VersionCode);

        Assert.Equal(
            "CAMBRIDGE-PRIMARY-S1",
            blueprint.CourseCode);

        Assert.Equal(
            "Cambridge Primary Stage 1",
            blueprint.NativeLevel);

        Assert.Equal(
            1,
            blueprint.LogicalLevelFrom);

        Assert.Equal(
            1,
            blueprint.LogicalLevelTo);

        Assert.Equal(
            "EdulyticsOwnedSequence",
            blueprint.SequenceAuthority);

        Assert.True(
            blueprint
                .SuppressOutcomeFallbackForLogicalRange);

        Assert.Equal(
            "Open Government Licence v3.0",
            blueprint.SourceLicense);

        Assert.True(
            PedagogicalSourceLicensePolicy
                .IsApproved(
                    "Open Government Licence v3.0"));

        Assert.False(
            PedagogicalSourceLicensePolicy
                .IsApproved(
                    "CC BY-NC 4.0"));

        Assert.Equal(
            6,
            blueprint.Units.Count);

        Assert.Equal(
            27,
            blueprint.Lessons.Count);

        Assert.All(
            blueprint.Sources,
            source =>
            {
                Assert.Equal(
                    "Open Government Licence v3.0",
                    source.License);

                Assert.Contains(
                    "Contains public sector information",
                    source.RequiredDigitalAttribution,
                    StringComparison.Ordinal);

                Assert.Contains(
                    "Department for Education",
                    source.Publisher,
                    StringComparison.Ordinal);
            });

        var targets =
            blueprint.Lessons
                .SelectMany(
                    x =>
                        x.FormalTargets)
                .ToArray();

        Assert.Equal(
            36,
            targets.Length);

        Assert.Equal(
            36,
            targets
                .Select(
                    x =>
                        x.OutcomeCode)
                .Distinct(
                    StringComparer.Ordinal)
                .Count());

        Assert.True(
            ExpectedStageOneCodes
                .SetEquals(
                    targets.Select(
                        x =>
                            x.OutcomeCode)));

        Assert.DoesNotContain(
            targets,
            target =>
                target.OutcomeCode.StartsWith(
                    "TWM.",
                    StringComparison.Ordinal));

        Assert.All(
            targets,
            target =>
            {
                Assert.Equal(
                    "VerifiedContentCoverage",
                    target.EvidenceKind);

                Assert.False(
                    target.PublisherSuppliedAlignment);

                Assert.True(
                    target.EvidenceReferences.Count >= 2);

                Assert.Contains(
                    target.EvidenceReferences,
                    evidence =>
                        evidence.SourceFamily.Contains(
                            "Cambridge Primary Mathematics 0096",
                            StringComparison.Ordinal));

                Assert.Contains(
                    target.EvidenceReferences,
                    evidence =>
                        evidence.License ==
                        "Open Government Licence v3.0");
            });
    }

    [Fact]
    public void
        StageOneCanonicalContentIsPublishedEnglishLearnerFacingAndExact()
    {
        var document =
            Assert.Single(
                MathematicsCanonicalLessonContentSeeder
                    .LoadEmbeddedDocuments(),
                x =>
                    x.PackCode ==
                        MathematicsCurriculumPackRegistry
                            .CambridgeCode &&
                    x.ContentVersion ==
                        "phase29-cambridge-primary-stage1-dfe-ogl-v1");

        Assert.Equal(
            "CAMBRIDGE-PATHWAY-2026",
            document.VersionCode);

        Assert.Equal(
            "en",
            document.AcademicLanguage);

        Assert.False(
            document.CurriculumTranslationRequired);

        Assert.Equal(
            PedagogicalSourceType
                .OpenEducationalResource,
            document.PedagogicalSourceType);

        Assert.Equal(
            CanonicalLessonContentStatus.Published,
            document.Status);

        Assert.Equal(
            27,
            document.Lessons.Count);

        var mapped =
            document.Lessons
                .SelectMany(
                    x =>
                        x.OutcomeCodes)
                .ToArray();

        Assert.Equal(
            36,
            mapped.Length);

        Assert.Equal(
            36,
            mapped
                .Distinct(
                    StringComparer.Ordinal)
                .Count());

        Assert.True(
            ExpectedStageOneCodes
                .SetEquals(
                    mapped));

        Assert.All(
            document.Lessons,
            lesson =>
            {
                Assert.False(
                    lesson.IsSupporting);

                Assert.NotEmpty(
                    lesson.OutcomeCodes);

                Assert.Matches(
                    "^[0-9a-f]{64}$",
                    lesson.SourceSha256);

                Assert.Matches(
                    "^[0-9a-f]{64}$",
                    lesson.CanonicalBodySha256);

                var translation =
                    Assert.Single(
                        lesson.Translations);

                Assert.Equal(
                    "en",
                    translation.CultureCode);

                var body =
                    string.Join(
                        "\n",
                        translation.Title,
                        translation.Explanation,
                        translation.KeyConceptsAndRules,
                        translation.WorkedExamples,
                        translation.StepByStepSolutions,
                        translation.CommonMistakes,
                        translation.QuickSummary);

                Assert.False(
                    Regex.IsMatch(
                        body,
                        "<[^>]+>",
                        RegexOptions.CultureInvariant));

                Assert.DoesNotContain(
                    "Student Facing",
                    body,
                    StringComparison.OrdinalIgnoreCase);

                Assert.DoesNotContain(
                    "Ask students",
                    body,
                    StringComparison.OrdinalIgnoreCase);

                Assert.DoesNotContain(
                    "Give students",
                    body,
                    StringComparison.OrdinalIgnoreCase);

                Assert.DoesNotContain(
                    "Select 2",
                    body,
                    StringComparison.OrdinalIgnoreCase);

                Assert.DoesNotContain(
                    "Cambridge Primary Mathematics 0096 Curriculum Framework:",
                    body,
                    StringComparison.Ordinal);
            });
    }

    [Fact]
    public void
        EveryStageOneLessonProducesAFreeLocalInstructionalVisual()
    {
        var document =
            Assert.Single(
                MathematicsCanonicalLessonContentSeeder
                    .LoadEmbeddedDocuments(),
                x =>
                    x.PackCode ==
                        MathematicsCurriculumPackRegistry
                            .CambridgeCode &&
                    x.ContentVersion ==
                        "phase29-cambridge-primary-stage1-dfe-ogl-v1");

        var visualLessons =
            document.Lessons
                .Count(
                    lesson =>
                    {
                        var translation =
                            Assert.Single(
                                lesson.Translations);

                        return
                            LessonPresentationParser
                                .Parse(
                                    translation.WorkedExamples,
                                    sectionKind:
                                        "examples")
                                .Any(
                                    item =>
                                        item.IsVisual);
                    });

        Assert.Equal(
            27,
            visualLessons);
    }

    [Fact]
    public async Task
        StageOneSeedsExactlyAndDoesNotCreateCambridgeFallbackBeyondStageOne()
    {
        await using var db =
            CreateDb();

        await new MathematicsCurriculumPackSeeder(
                db)
            .SeedAsync();

        var pedagogy =
            new MathematicsPedagogicalLessonSeeder(
                db);

        await pedagogy
            .SeedAsync();

        var canonical =
            new MathematicsCanonicalLessonContentSeeder(
                db);

        await canonical
            .SeedAsync();

        var versionId =
            await db.CurriculumPackImportStates
                .Where(
                    x =>
                        x.FrameworkCode ==
                        MathematicsCurriculumPackRegistry
                            .CambridgeCode)
                .Select(
                    x =>
                        x.FrameworkVersionId)
                .SingleAsync();

        var lessons =
            await db.CurriculumPedagogicalLessons
                .Where(
                    x =>
                        x.FrameworkVersionId ==
                        versionId)
                .OrderBy(
                    x =>
                        x.SortOrder)
                .ToArrayAsync();

        Assert.Equal(
            27,
            lessons.Length);

        Assert.All(
            lessons,
            x =>
            {
                Assert.Equal(
                    1,
                    x.LogicalLevelFrom);

                Assert.Equal(
                    1,
                    x.LogicalLevelTo);

                Assert.Equal(
                    "Cambridge Primary Stage 1",
                    x.NativeLevel);
            });

        var lessonIds =
            lessons
                .Select(
                    x =>
                        x.Id)
                .ToArray();

        var mappings =
            await (
                from mapping in
                    db.CurriculumPedagogicalLessonOutcomes
                join node in
                    db.CurriculumPackContentNodes
                    on mapping.OutcomeNodeId
                    equals node.Id
                where
                    mapping.FrameworkVersionId ==
                        versionId &&
                    lessonIds.Contains(
                        mapping.PedagogicalLessonId)
                select node.Code)
                .ToArrayAsync();

        Assert.Equal(
            36,
            mappings.Length);

        Assert.True(
            ExpectedStageOneCodes
                .SetEquals(
                    mappings));

        Assert.DoesNotContain(
            mappings,
            x =>
                x.StartsWith(
                    "TWM.",
                    StringComparison.Ordinal));

        Assert.False(
            await db.CurriculumPedagogicalLessons
                .AnyAsync(
                    x =>
                        x.FrameworkVersionId ==
                            versionId &&
                        (
                            x.LogicalLevelFrom != 1 ||
                            x.LogicalLevelTo != 1
                        )));

        Assert.Equal(
            27,
            await db.CurriculumLessonContents
                .CountAsync(
                    x =>
                        lessonIds.Contains(
                            x.PedagogicalLessonId)));

        var contentIds =
            await db.CurriculumLessonContents
                .Where(
                    x =>
                        lessonIds.Contains(
                            x.PedagogicalLessonId))
                .Select(
                    x =>
                        x.Id)
                .ToArrayAsync();

        Assert.Equal(
            27,
            await db
                .CurriculumLessonContentTranslations
                .CountAsync(
                    x =>
                        contentIds.Contains(
                            x.CurriculumLessonContentId) &&
                        x.CultureCode == "en"));

        var lessonCount =
            await db.CurriculumPedagogicalLessons
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                        versionId);

        var mappingCount =
            await db.CurriculumPedagogicalLessonOutcomes
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                        versionId);

        var contentCount =
            await db.CurriculumLessonContents
                .CountAsync(
                    x =>
                        lessonIds.Contains(
                            x.PedagogicalLessonId));

        await pedagogy
            .SeedAsync();

        await canonical
            .SeedAsync();

        Assert.Equal(
            lessonCount,
            await db.CurriculumPedagogicalLessons
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                        versionId));

        Assert.Equal(
            mappingCount,
            await db.CurriculumPedagogicalLessonOutcomes
                .CountAsync(
                    x =>
                        x.FrameworkVersionId ==
                        versionId));

        Assert.Equal(
            contentCount,
            await db.CurriculumLessonContents
                .CountAsync(
                    x =>
                        lessonIds.Contains(
                            x.PedagogicalLessonId)));
    }

    [Fact]
    public void
        CentralAttributionPageContainsRequiredOglAttribution()
    {
        var root =
            FindRepositoryRoot();

        var page =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "wwwroot",
                    "content-sources-licenses.html"));

        Assert.Contains(
            "Contains public sector information licensed under the Open Government Licence v3.0.",
            page,
            StringComparison.Ordinal);

        Assert.Contains(
            "DfE-00110-2020",
            page,
            StringComparison.Ordinal);

        Assert.Contains(
            "Cambridge Primary Mathematics official curriculum page",
            page,
            StringComparison.Ordinal);

        foreach (var layout in
                 new[]
                 {
                     "_AppLayout.cshtml",
                     "_StudentLayout.cshtml"
                 })
        {
            var text =
                File.ReadAllText(
                    Path.Combine(
                        root,
                        "src",
                        "Edulytics.Web",
                        "Views",
                        "Shared",
                        layout));

            Assert.Contains(
                "/content-sources-licenses.html",
                text,
                StringComparison.Ordinal);
        }
    }

    private static EdulyticsDbContext
        CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<
                    EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    "phase29-cambridge-stage1-"
                    + Guid.NewGuid().ToString("N"))
                .Options;

        return new EdulyticsDbContext(
            options);
    }

    private static string
        FindRepositoryRoot()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "Edulytics.sln")))
            {
                return directory.FullName;
            }

            directory =
                directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root not found.");
    }
}
