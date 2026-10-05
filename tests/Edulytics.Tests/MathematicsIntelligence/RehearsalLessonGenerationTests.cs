using System.Reflection;
using System.Data.Common;
using System.Text.Json;
using Edulytics.Core.Assessments;
using Edulytics.Core.Constants;
using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Mathematics.Practice;
using Edulytics.Core.Users;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Edulytics.Data.Seeding;
using Edulytics.Services.Assessments;
using Edulytics.Services.Mathematics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Edulytics.Tests.MathematicsIntelligence;

public class RehearsalLessonGenerationTests
{
    [Fact]
    public async Task Existing_rehearsal_content_accepts_mapping_update_without_changing_bodies_or_versions()
    {
        await using var db = new EdulyticsDbContext(new DbContextOptionsBuilder<EdulyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await new MathematicsCurriculumPackSeeder(db).SeedAsync();
        await new MathematicsPedagogicalLessonSeeder(db).SeedAsync();
        var documents = MathematicsCanonicalLessonContentSeeder.LoadEmbeddedDocuments()
            .Where(d => d.Lessons.Any(l => IsRehearsalLesson(l.LessonCode))).ToArray();
        Assert.Equal(525, documents.Sum(d => d.Lessons.Count));
        var codes = documents.SelectMany(d => d.Lessons).Select(l => l.LessonCode).ToArray();
        var ids = await db.CurriculumPedagogicalLessons.Where(l => codes.Contains(l.Code)).Select(l => l.Id).ToArrayAsync();
        var links = await db.CurriculumPedagogicalLessonOutcomes.Where(l => ids.Contains(l.PedagogicalLessonId)).ToArrayAsync();
        db.CurriculumPedagogicalLessonOutcomes.RemoveRange(links);
        await db.SaveChangesAsync();
        var prior = JsonSerializer.Deserialize<CanonicalLessonContentPackDocument[]>(JsonSerializer.Serialize(documents))!;
        foreach (var document in prior)
        {
            document.ContentVersion = document.ContentVersion.Replace("-official-map-v1", "-v1", StringComparison.Ordinal);
            foreach (var lesson in document.Lessons)
            {
                lesson.OutcomeCodes.Clear();
                lesson.OfficialReferenceCode = null;
                lesson.IsSupporting = true;
            }
        }
        var seeder = new MathematicsCanonicalLessonContentSeeder(db);
        await seeder.SeedDocumentsAsync(prior);
        var before = await StoredBodies(db);
        var versions = await db.CurriculumLessonContents.ToDictionaryAsync(x => x.Id, x => x.ContentVersion);
        db.CurriculumPedagogicalLessonOutcomes.AddRange(links);
        await db.SaveChangesAsync();
        await seeder.SeedDocumentsAsync(documents);
        Assert.Equal(before, await StoredBodies(db));
        Assert.Equal(versions, await db.CurriculumLessonContents.ToDictionaryAsync(x => x.Id, x => x.ContentVersion));
    }

    private static async Task<Dictionary<Guid, string>> StoredBodies(EdulyticsDbContext db) =>
        (await db.CurriculumLessonContentTranslations.AsNoTracking().ToArrayAsync()).ToDictionary(x => x.Id,
            x => JsonSerializer.Serialize(new { x.CultureCode, x.Title, x.Explanation, x.KeyConceptsAndRules,
                x.WorkedExamples, x.StepByStepSolutions, x.CommonMistakes, x.QuickSummary }));

    private static bool IsRehearsalLesson(string code) => new[]
    {
        "PED:CAMBRIDGE-INTL-MATH:L12:", "PED:CAMBRIDGE-INTL-MATH:L13:",
        "PED:UAE-MOE-MATH:L3:COMMON:", "PED:UAE-MOE-MATH:L4:COMMON:",
        "PED:UAE-MOE-MATH:L7:ADVANCED:", "PED:UAE-MOE-MATH:L8:ADVANCED:",
        "PED:UAE-MOE-MATH:L11:ADVANCED:", "PED:UAE-MOE-MATH:L12:ADVANCED:"
    }.Any(prefix => code.StartsWith(prefix, StringComparison.Ordinal));

    [Fact]
    public async Task Official_outcome_projection_translates_on_Postgres_before_opening_a_connection()
    {
        await using var db = new EdulyticsDbContext(new DbContextOptionsBuilder<EdulyticsDbContext>()
            .UseNpgsql("Host=127.0.0.1;Port=1;Database=translation_test;Username=unused;Password=unused")
            .AddInterceptors(new TranslationProbe()).Options);
        await Assert.ThrowsAsync<TranslationReachedDatabaseException>(() => OfficialCurriculumOutcomeMaterializer.EnsureAsync(db,
            new SchoolCurriculumAdoption { IsActive = true, CurriculumLogicalLevel = 12,
                CurriculumLevelKey = "CAMBRIDGE-INTL-MATH:L12:SHARED", FrameworkVersionId = Guid.NewGuid() }));
    }

    private sealed class TranslationReachedDatabaseException : Exception;
    private sealed class TranslationProbe : DbConnectionInterceptor
    {
        public override ValueTask<InterceptionResult> ConnectionOpeningAsync(DbConnection connection,
            ConnectionEventData eventData, InterceptionResult result, CancellationToken cancellationToken = default) =>
            throw new TranslationReachedDatabaseException();
    }

    [Fact]
    public async Task Current_uae_pack_is_idempotent_and_preserves_node_ids()
    {
        await using var db = new EdulyticsDbContext(
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        var seeder = new MathematicsCurriculumPackSeeder(db);
        await seeder.SeedAsync();

        var state = await db.CurriculumPackImportStates.SingleAsync(
            x => x.FrameworkCode == MathematicsCurriculumPackRegistry.UaeCode);
        var first = await db.CurriculumPackContentNodes
            .Where(x => x.FrameworkVersionId == state.FrameworkVersionId)
            .ToDictionaryAsync(x => x.Code, x => x.Id, StringComparer.Ordinal);

        await seeder.SeedAsync();
        await seeder.SeedAsync();

        var current = await db.CurriculumPackImportStates.SingleAsync(
            x => x.FrameworkCode == MathematicsCurriculumPackRegistry.UaeCode);
        var second = await db.CurriculumPackContentNodes
            .Where(x => x.FrameworkVersionId == current.FrameworkVersionId)
            .ToDictionaryAsync(x => x.Code, x => x.Id, StringComparer.Ordinal);

        Assert.Equal(state.FrameworkVersionId, current.FrameworkVersionId);
        Assert.Equal(1532, current.NodeCount);
        Assert.Equal(1420, current.OfficialNodeCount);
        Assert.Equal(1373, second.Keys.Count(x =>
            x.StartsWith("UAE:REF:TEXTBOOK:", StringComparison.Ordinal)));
        Assert.Equal(first, second);
        Assert.Equal(
            48,
            await db.CurriculumPackNodeLinks.CountAsync(
                x => x.FrameworkVersionId == current.FrameworkVersionId));
    }

    [Fact]
    public async Task Current_uae_reference_catalog_excludes_removed_grade5_and_grade6_advanced_paths()
    {
        await using var db = new EdulyticsDbContext(
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);

        await new MathematicsCurriculumPackSeeder(db).SeedAsync();

        var state = await db.CurriculumPackImportStates.SingleAsync(
            x => x.FrameworkCode == MathematicsCurriculumPackRegistry.UaeCode);
        var nodes = await db.CurriculumPackContentNodes
            .Where(x => x.FrameworkVersionId == state.FrameworkVersionId)
            .ToArrayAsync();

        Assert.Equal(1532, nodes.Length);
        Assert.Equal(
            1373,
            nodes.Count(x =>
                x.NodeKind == "Reference" &&
                x.IsOfficial &&
                x.Code.StartsWith("UAE:REF:TEXTBOOK:", StringComparison.Ordinal)));

        Assert.DoesNotContain(
            nodes,
            x =>
                x.Code.StartsWith("UAE:CATALOG:G5:ADVANCED:", StringComparison.Ordinal) ||
                x.Code.StartsWith("UAE:CATALOG:G6:ADVANCED:", StringComparison.Ordinal) ||
                x.Code.StartsWith("UAE:REF:TEXTBOOK:G5:ADVANCED:", StringComparison.Ordinal) ||
                x.Code.StartsWith("UAE:REF:TEXTBOOK:G6:ADVANCED:", StringComparison.Ordinal));
    }

    [Fact]
    public void Official_mapping_preserves_previously_reviewed_effective_lesson_bodies()
    {
        var lessons = MathematicsCanonicalLessonContentSeeder.LoadEmbeddedDocuments()
            .SelectMany(document => document.Lessons.Select(lesson => (document, lesson)))
            .Where(x => x.lesson.OutcomeCodes.Count > 0 &&
                IsRehearsalLesson(x.lesson.LessonCode))
            .ToArray();
        Assert.Equal(57, lessons.Length);
        foreach (var (document, lesson) in lessons)
        {
            var mappedBody = JsonSerializer.Serialize(lesson.Translations);
            var version = CanonicalLessonContentMaterializer.GetEffectiveContentVersion(document, lesson);
            lesson.OutcomeCodes.Clear();
            SupportingLessonPracticeContentCorrections.ApplyApprovedCorrections(document);
            Assert.Equal(mappedBody, JsonSerializer.Serialize(lesson.Translations));
            Assert.Equal(version, CanonicalLessonContentMaterializer.GetEffectiveContentVersion(document, lesson));
        }
    }

    [Fact]
    public async Task All_target_lessons_generate_practice_exam_worksheet_and_homework_with_exact_alignment()
    {
        await using var db = new EdulyticsDbContext(new DbContextOptionsBuilder<EdulyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        await new MathematicsCurriculumPackSeeder(db).SeedAsync();
        await new MathematicsPedagogicalLessonSeeder(db).SeedAsync();

        var documents = PedagogicalLessonBlueprintRegistry.LoadEmbeddedDocuments().Where(d =>
            d.BlueprintCode.StartsWith("CAMBRIDGE-AS-LEVEL-9709-", StringComparison.Ordinal) ||
            d.BlueprintCode.StartsWith("CAMBRIDGE-A-LEVEL-9709-", StringComparison.Ordinal) ||
            (d.PackCode == "UAE-MOE-MATH" && new[] { 3, 4, 7, 8, 11, 12 }.Contains(d.LogicalLevel) &&
             d.Pathway == (d.LogicalLevel < 5 ? "Common" : "Advanced"))).ToArray();
        Assert.Equal(8, documents.Length);
        Assert.Equal(525, documents.Sum(d => d.Lessons.Count));
        var school = Guid.NewGuid();
        var teacher = Guid.NewGuid();
        var users = Proxy<ISchoolUserRepository>((method, _) => method.Name == "GetActorAsync"
            ? Task.FromResult<SchoolUserRecord?>(new(teacher, school, "teacher@example.test", true, false,
                DateTime.UtcNow, DateTime.UtcNow, [RoleNames.Teacher]))
            : throw new InvalidOperationException(method.Name));
        var unusedRepository = Proxy<IAssessmentRepository>((method, _) => throw new InvalidOperationException(method.Name));
        var seed = 340000;
        foreach (var document in documents)
        {
            var state = await db.CurriculumPackImportStates.SingleAsync(x => x.FrameworkCode == document.PackCode);
            var adoption = new SchoolCurriculumAdoption
            {
                Id = Guid.NewGuid(), SchoolId = school, FrameworkVersionId = state.FrameworkVersionId,
                AcademicProgramId = Guid.NewGuid(), SubjectId = Guid.NewGuid(), GradeLevelId = Guid.NewGuid(),
                CurriculumLogicalLevel = document.LogicalLevel, CurriculumPathway = document.Pathway,
                CurriculumLevelKey = $"{document.PackCode}:L{document.LogicalLevel:D2}:{document.Pathway}", IsActive = true
            };
            await OfficialCurriculumOutcomeMaterializer.EnsureAsync(db, adoption);
            var outcomes = await db.LearningOutcomes.Where(x => x.CurriculumAdoptionId == adoption.Id).ToArrayAsync();
            Assert.NotEmpty(outcomes);
            var codes = document.Lessons.Select(x => x.LessonCode).ToArray();
            var lessons = await db.CurriculumPedagogicalLessons.Where(x => codes.Contains(x.Code)).ToArrayAsync();
            Assert.Equal(codes.Length, lessons.Length);
            var lessonIds = lessons.Select(x => x.Id).ToArray();
            var links = await db.CurriculumPedagogicalLessonOutcomes.Where(x => lessonIds.Contains(x.PedagogicalLessonId)).ToArrayAsync();

            foreach (var lesson in lessons)
            {
                Assert.True(LessonPracticeCapabilityResolver.TryResolve(lesson.Code, out var contract), lesson.Code);
                Assert.NotNull(contract);
                foreach (var question in new ExactSkillContractQuestionEngine().Generate("rehearsal-practice", lesson.Code,
                             contract!.AllowedQuestionFamilies, ExactSkillQuestionDifficulty.Standard, 5, seed++, []))
                    Assert.True(ExactSkillContractQuestionEngine.Verify(question.Family, question.Parameters, question.CorrectAnswer), lesson.Code);

                var expectedOutcomes = outcomes.Where(x => links.Any(link => link.PedagogicalLessonId == lesson.Id &&
                    link.OutcomeNodeId == x.OfficialContentNodeId)).Select(x => x.Id).OrderBy(x => x).ToArray();
                Assert.Equal(document.Lessons.Single(x => x.LessonCode == lesson.Code).OutcomeCodes.Count, expectedOutcomes.Length);
                foreach (var type in new[] { AssessmentType.Exam, AssessmentType.Worksheet, AssessmentType.Homework })
                {
                    var assessment = new Assessment
                    {
                        Id = Guid.NewGuid(), SchoolId = school, AssessmentType = type, Status = AssessmentStatus.Draft,
                        MaxScore = type == AssessmentType.Exam ? 5 : 0, DifficultyBand = AssessmentDifficultyBand.AtClassLevel
                    };
                    var details = new AssessmentDetails(new AssessmentListItem(assessment.Id, adoption.SubjectId, Guid.NewGuid(),
                        Guid.NewGuid(), Guid.NewGuid(), "Rehearsal", DateOnly.FromDateTime(DateTime.UtcNow), assessment.MaxScore,
                        AssessmentStatus.Draft, []) { AssessmentType = type }, [],
                        outcomes.Select(x => new AssessmentOutcomeItem(x.Id, x.Code, x.Description)).ToArray(), [], [], []);
                    var context = new AssessmentBuilderPersistenceContext(assessment, new ClassGroup(), adoption, [], [], [], [],
                        outcomes, [], lessons, links);
                    var bundles = new List<AssessmentBuilderQuestionBundle>();
                    var repository = Proxy<IAssessmentBuilderRepository>((method, args) => method.Name switch
                    {
                        "GetContextAsync" => Task.FromResult<AssessmentBuilderPersistenceContext?>(context),
                        "AddBundle" => Add(bundles, (AssessmentBuilderQuestionBundle)args![0]!),
                        "SaveAsync" => Task.FromResult(AssessmentPersistenceResult.Success()),
                        _ => throw new InvalidOperationException(method.Name)
                    });
                    var assessments = Proxy<IAssessmentService>((method, _) => method.Name == "GetDetailsAsync"
                        ? Task.FromResult(AssessmentQueryResult<AssessmentDetails>.Success(details))
                        : throw new InvalidOperationException(method.Name));
                    var result = await new AssessmentBuilderService(assessments, repository, unusedRepository, users)
                        .GenerateQuestionsAsync(teacher, new GenerateBuilderQuestionsRequest(assessment.Id, 5,
                            type == AssessmentType.Exam ? 1 : 0, AssessmentBuilderDifficulty.AtClassLevel, [], [], seed++)
                        { ScopeType = AssessmentGenerationScopeType.Lessons, LessonIds = [lesson.Id] });
                    Assert.True(result.Succeeded, $"{lesson.Code}/{type}: {result.Error}");
                    Assert.Equal(5, bundles.Count);
                    foreach (var bundle in bundles)
                    {
                        Assert.Equal(lesson.Id, bundle.Item.CurriculumPedagogicalLessonId);
                        Assert.Equal(expectedOutcomes, bundle.ItemOutcomeMappings.Select(x => x.LearningOutcomeId).OrderBy(x => x));
                        Assert.Equal(type == AssessmentType.Exam ? 1m : 0m, bundle.Question.MaxScore);
                        using var parameters = JsonDocument.Parse(bundle.Item.GenerationParametersJson!);
                        var values = parameters.RootElement.GetProperty("parameters").EnumerateObject()
                            .ToDictionary(x => x.Name, x => x.Value.GetInt32());
                        Assert.True(ExactSkillContractQuestionEngine.Verify(bundle.Item.GenerationFamily!, values, bundle.Item.CorrectAnswer));
                        using var validation = JsonDocument.Parse(bundle.Item.ValidationMetadataJson!);
                        Assert.True(validation.RootElement.GetProperty("solverVerified").GetBoolean());
                        Assert.False(validation.RootElement.GetProperty("broadFallbackUsed").GetBoolean());
                        Assert.Equal(expectedOutcomes.Length == 0, validation.RootElement.GetProperty("supportingLesson").GetBoolean());
                    }
                }
            }
        }
    }

    private static object? Add(List<AssessmentBuilderQuestionBundle> bundles, AssessmentBuilderQuestionBundle bundle)
    {
        bundles.Add(bundle);
        return null;
    }

    private static T Proxy<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, RepositoryProxy>();
        ((RepositoryProxy)(object)proxy).Handler = handler;
        return proxy;
    }

    public class RepositoryProxy : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }
}
