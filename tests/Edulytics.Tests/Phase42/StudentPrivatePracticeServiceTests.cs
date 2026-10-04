using System.Text.Json;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Mathematics.Practice;
using Edulytics.Core.Practice;
using Edulytics.Services.Mathematics;
using Edulytics.Services.Practice;
using Xunit;

namespace Edulytics.Tests.Phase42;

public sealed class StudentPrivatePracticeServiceTests
{
    [Fact]
    public async Task Workspace_projects_curricula_lessons_units_and_history()
    {
        var ids = Ids.Create();
        var repo = new FakeRepository
        {
            Curricula = [new PrivatePracticeCurriculumOption(ids.Adoption, ids.Class, ids.Year, "Grade 1", "1A")],
            Context = BuildContext(ids,
                [Outcome(ids, "CCSS:1.OA.A.1", "Add whole numbers", 1)],
                [
                    Lesson(ids, Guid.NewGuid(), "U1", "Number", "L1", "Addition", 1),
                    Lesson(ids, Guid.NewGuid(), "U1", "Number", "L2", "Subtraction", 2),
                    Lesson(ids, Guid.NewGuid(), "U2", "Fractions", "L3", "Fractions", 3)
                ]),
            Attempts = [new PrivatePracticeAttemptSummary(Guid.NewGuid(), ids.Adoption, null, PracticeAttemptStatus.Submitted, DateTime.UtcNow, DateTime.UtcNow, 4, 5, 80)]
        };

        var service = new StudentPrivatePracticeService(repo);
        var result = await service.GetWorkspaceAsync(ids.User, ids.Adoption);

        Assert.Single(result.Curricula);
        Assert.Equal(3, result.Lessons.Count);
        Assert.Equal(2, result.UnitKeys.Count);
        Assert.Single(result.Attempts);
        Assert.Equal(ids.Adoption, result.SelectedCurriculumAdoptionId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(31)]
    public async Task Generate_rejects_invalid_question_count_before_repository_lookup(int count)
    {
        var repo = new FakeRepository();
        var service = new StudentPrivatePracticeService(repo);
        var result = await service.GenerateAsync(Guid.NewGuid(), new GenerateStudentPrivatePracticeRequest(
            Guid.NewGuid(), StudentPrivatePracticeScope.WholeCurriculum, null, null,
            StudentPrivatePracticeDifficulty.MyLevel, count, 1));

        Assert.Equal(StudentPrivatePracticeError.InvalidQuestionCount, result.Error);
        Assert.Equal(0, repo.ContextCalls);
    }

    [Theory]
    [InlineData(StudentPrivatePracticeScope.Lesson, 6)]
    [InlineData(StudentPrivatePracticeScope.Unit, 11)]
    [InlineData(StudentPrivatePracticeScope.WholeCurriculum, 16)]
    public async Task Generate_enforces_personal_test_question_limits_by_scope(
        StudentPrivatePracticeScope scope,
        int count)
    {
        var repo = new FakeRepository();
        var service = new StudentPrivatePracticeService(repo);

        var result = await service.GenerateAsync(
            Guid.NewGuid(),
            new GenerateStudentPrivatePracticeRequest(
                Guid.NewGuid(),
                scope,
                scope == StudentPrivatePracticeScope.Lesson
                    ? Guid.NewGuid()
                    : null,
                scope == StudentPrivatePracticeScope.Unit
                    ? "U1"
                    : null,
                StudentPrivatePracticeDifficulty.MyLevel,
                count,
                1));

        Assert.Equal(
            StudentPrivatePracticeError.InvalidQuestionCount,
            result.Error);
        Assert.Equal(0, repo.ContextCalls);
    }

    [Fact]
    public async Task Generate_fails_closed_for_unavailable_curriculum()
    {
        var repo = new FakeRepository();
        var result = await new StudentPrivatePracticeService(repo).GenerateAsync(
            Guid.NewGuid(),
            new GenerateStudentPrivatePracticeRequest(Guid.NewGuid(), StudentPrivatePracticeScope.WholeCurriculum,
                null, null, StudentPrivatePracticeDifficulty.MyLevel, 1, 1));

        Assert.Equal(StudentPrivatePracticeError.CurriculumNotAvailable, result.Error);
    }

    [Fact]
    public async Task Lesson_scope_requires_a_lesson()
    {
        var ids = Ids.Create();
        var repo = new FakeRepository { Context = BuildContext(ids, [Outcome(ids, "CCSS:1.OA.A.1", "Add whole numbers", 1)], []) };
        var result = await new StudentPrivatePracticeService(repo).GenerateAsync(
            ids.User,
            new GenerateStudentPrivatePracticeRequest(ids.Adoption, StudentPrivatePracticeScope.Lesson,
                null, null, StudentPrivatePracticeDifficulty.MyLevel, 1, 1));

        Assert.Equal(StudentPrivatePracticeError.InvalidScope, result.Error);
    }

    [Fact]
    public async Task Recognizable_mathematics_outcomes_use_contextual_generation_instead_of_failing_closed()
    {
        var ids = Ids.Create();
        var repo = new FakeRepository
        {
            Context = BuildContext(
                ids,
                [Outcome(ids, "GEO.1", "Identify a geometric shape and reason about its area.", 1)],
                [])
        };
        var result = await new StudentPrivatePracticeService(repo).GenerateAsync(
            ids.User,
            new GenerateStudentPrivatePracticeRequest(ids.Adoption, StudentPrivatePracticeScope.WholeCurriculum,
                null, null, StudentPrivatePracticeDifficulty.AtClassLevel, 1, 7));

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.NotNull(repo.SavedAttempt);
        Assert.Single(repo.SavedItems);
        Assert.Equal("CurriculumContextCheck", repo.SavedItems[0].GenerationFamily);
        Assert.Contains("student-private", repo.SavedItems[0].ValidationMetadataJson, StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(repo.SavedItems[0].CorrectAnswer));
        Assert.False(string.IsNullOrWhiteSpace(repo.SavedItems[0].Solution));
    }

    [Fact]
    public async Task Whole_curriculum_reference_only_scope_uses_lesson_context_instead_of_zero_objectives()
    {
        var ids = Ids.Create();
        var lessons = new[]
        {
            Lesson(
                ids,
                Guid.NewGuid(),
                "ALG",
                "Algebra",
                "PED:REF:G10:FUNCTIONS",
                "Functions — advanced reasoning",
                1),
            Lesson(
                ids,
                Guid.NewGuid(),
                "ALG",
                "Algebra",
                "PED:REF:G10:LINEAR",
                "Linear modelling — advanced reasoning",
                2)
        };

        var repo = new FakeRepository
        {
            Context = BuildContext(ids, [], lessons)
        };

        var result = await new StudentPrivatePracticeService(repo).GenerateAsync(
            ids.User,
            new GenerateStudentPrivatePracticeRequest(
                ids.Adoption,
                StudentPrivatePracticeScope.WholeCurriculum,
                null,
                null,
                StudentPrivatePracticeDifficulty.AtClassLevel,
                2,
                20261001));

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.NotNull(repo.SavedAttempt);
        Assert.True(repo.SavedAttempt!.IsPrivate);
        Assert.Equal(2, repo.SavedItems.Count);
        Assert.Empty(repo.SavedOutcomes);
        Assert.All(
            repo.SavedItems,
            item => Assert.Contains(
                "pedagogical-context-only",
                item.ValidationMetadataJson,
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task Non_mathematics_outcomes_still_fail_closed()
    {
        var ids = Ids.Create();
        var repo = new FakeRepository
        {
            Context = BuildContext(
                ids,
                [Outcome(ids, "HIST.1", "Describe the historical context of a source.", 1)],
                [])
        };
        var result = await new StudentPrivatePracticeService(repo).GenerateAsync(
            ids.User,
            new GenerateStudentPrivatePracticeRequest(ids.Adoption, StudentPrivatePracticeScope.WholeCurriculum,
                null, null, StudentPrivatePracticeDifficulty.AtClassLevel, 1, 7));

        Assert.Equal(StudentPrivatePracticeError.NoSupportedOutcomes, result.Error);
        Assert.Null(repo.SavedAttempt);
        Assert.Empty(repo.SavedItems);
    }

    [Fact]
    public async Task Whole_curriculum_generation_creates_private_native_attempt()
    {
        var ids = Ids.Create();
        var outcome = Outcome(ids, "CCSS:1.OA.A.1", "Add whole numbers", 1);
        var repo = new FakeRepository { Context = BuildContext(ids, [outcome], []) };
        var result = await new StudentPrivatePracticeService(repo).GenerateAsync(
            ids.User,
            new GenerateStudentPrivatePracticeRequest(ids.Adoption, StudentPrivatePracticeScope.WholeCurriculum,
                null, null, StudentPrivatePracticeDifficulty.AtClassLevel, 1, 123));

        Assert.True(result.Succeeded);
        Assert.NotNull(repo.SavedAttempt);
        Assert.True(repo.SavedAttempt!.IsPrivate);
        Assert.Equal(PracticeAttemptStatus.InProgress, repo.SavedAttempt.Status);
        Assert.Single(repo.SavedItems);
        Assert.Single(repo.SavedOutcomes);
        Assert.Equal(outcome.Id, repo.SavedOutcomes[0].LearningOutcomeId);
        Assert.Contains("student-private", repo.SavedItems[0].ValidationMetadataJson, StringComparison.Ordinal);
        Assert.Single(repo.SavedExposures);
    }

    [Fact]
    public async Task Quadratic_lesson_exposes_only_truthfully_supported_standard_difficulties()
    {
        const string lessonCode =
            "PED:UAE-MOE-MATH:L10:ADVANCED:01:04:SOLVING-QUADRATIC-EQUATIONS-BY-FACTORING";

        Assert.True(
            LessonPracticeContractRegistry.TryResolve(
                lessonCode,
                out var contract));
        Assert.NotNull(contract);
        Assert.Equal(
            new[] { "supporting.algebra.quadratic_larger_root" },
            contract!.AllowedQuestionFamilies);

        var ids = Ids.Create();
        var lessonId = Guid.NewGuid();
        var repo = new FakeRepository
        {
            Curricula =
            [
                new PrivatePracticeCurriculumOption(
                    ids.Adoption,
                    ids.Class,
                    ids.Year,
                    "Grade 11 Advanced",
                    "11A")
            ],
            Context = BuildContext(
                ids,
                [],
                [
                    Lesson(
                        ids,
                        lessonId,
                        "ALG",
                        "Algebra",
                        lessonCode,
                        "Quadratic equations — advanced reasoning",
                        1)
                ])
        };

        var service = new StudentPrivatePracticeService(repo);
        var workspace = await service.GetWorkspaceAsync(
            ids.User,
            ids.Adoption);

        var option = Assert.Single(workspace.Lessons);
        Assert.Equal(
            new[]
            {
                StudentPrivatePracticeDifficulty.MyLevel,
                StudentPrivatePracticeDifficulty.AtClassLevel
            },
            option.SupportedDifficulties);

        var result = await service.GenerateAsync(
            ids.User,
            new GenerateStudentPrivatePracticeRequest(
                ids.Adoption,
                StudentPrivatePracticeScope.Lesson,
                lessonId,
                null,
                StudentPrivatePracticeDifficulty.Challenge,
                5,
                20261001));

        Assert.Equal(
            StudentPrivatePracticeError.UnsupportedDifficulty,
            result.Error);
        Assert.Null(repo.SavedAttempt);
    }

    [Fact]
    public async Task Lesson_practice_falls_back_to_honest_standard_composition_when_higher_forms_are_not_supported()
    {
        const string lessonCode =
            "PED:CAMBRIDGE-INTL-MATH:S6:6NPV-1:APPLY";

        Assert.True(
            LessonPracticeContractRegistry.TryResolve(
                lessonCode,
                out var contract));
        Assert.NotNull(contract);
        Assert.Equal(
            new[]
            {
                "supporting.powers10.evaluate",
                "supporting.powers10.multiply",
                "supporting.powers10.divide",
                "supporting.powers10.missing_exponent"
            },
            contract!.AllowedQuestionFamilies);

        var ids = Ids.Create();
        var lessonId = Guid.NewGuid();
        var lesson = Lesson(
            ids,
            lessonId,
            "S6-NPV",
            "Number and Place Value",
            lessonCode,
            "Powers of 10: Reason and Apply",
            1);

        // Seed realistic prior exposure across all four Powers of 10
        // families. These families do not implement distinct Stretch/Challenge
        // cognitive forms, so Lesson Practice must remain Standard rather than
        // attaching unsupported higher-difficulty labels.
        var priorQuestions =
            new ExactSkillContractQuestionEngine().Generate(
                "stage18",
                lessonCode,
                contract.AllowedQuestionFamilies,
                ExactSkillQuestionDifficulty.Standard,
                12,
                42017,
                []);

        Assert.Equal(12, priorQuestions.Count);

        var historicalExposures = priorQuestions
            .Select(question => new StudentItemExposure
            {
                Id = Guid.NewGuid(),
                SchoolId = ids.School,
                StudentProfileId = ids.Student,
                AssessmentItemId = Guid.NewGuid(),
                ExposureFingerprint = question.ExposureFingerprint,
                ExposedAtUtc = DateTime.UtcNow.AddMinutes(-10)
            })
            .ToArray();

        var context = BuildContext(
            ids,
            [],
            [lesson]) with
        {
            Exposures = historicalExposures
        };

        var repo = new FakeRepository { Context = context };
        var result = await new StudentPrivatePracticeService(repo)
            .GenerateAsync(
                ids.User,
                new GenerateStudentPrivatePracticeRequest(
                    ids.Adoption,
                    StudentPrivatePracticeScope.Lesson,
                    lessonId,
                    null,
                    StudentPrivatePracticeDifficulty.MyLevel,
                    8,
                    73031,
                    UseLessonDifficultyProgression: true));

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Equal(8, repo.SavedItems.Count);
        Assert.Equal(
            8,
            repo.SavedItems
                .Select(item => item.ExposureFingerprint)
                .Distinct(StringComparer.Ordinal)
                .Count());

        Assert.Equal(
            new[]
            {
                "supporting.powers10.evaluate",
                "supporting.powers10.multiply",
                "supporting.powers10.divide",
                "supporting.powers10.missing_exponent",
                "supporting.powers10.evaluate",
                "supporting.powers10.multiply",
                "supporting.powers10.divide",
                "supporting.powers10.missing_exponent"
            },
            repo.SavedItems
                .Select(item => item.GenerationFamily)
                .ToArray());

        foreach (var item in repo.SavedItems)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(
                    item.ValidationMetadataJson));

            using var metadata = JsonDocument.Parse(
                item.ValidationMetadataJson!);

            Assert.Equal(
                "practice-assessment-v2",
                metadata.RootElement
                    .GetProperty("composer")
                    .GetString());
            Assert.Equal(
                "Standard",
                metadata.RootElement
                    .GetProperty("cognitiveDifficulty")
                    .GetString());
            Assert.False(
                metadata.RootElement.TryGetProperty(
                    "progressionIndex",
                    out _));
            Assert.Equal(
                AssessmentItemDifficulty.Medium,
                item.Difficulty);
            Assert.Equal(
                "READY_BALANCED",
                metadata.RootElement
                    .GetProperty("sessionReadiness")
                    .GetString());
            Assert.True(
                metadata.RootElement
                    .GetProperty("sessionQualityValidated")
                    .GetBoolean());
        }

        Assert.All(
            repo.SavedItems,
            item => Assert.True(
                Stage18SkillContractPracticeEngine.VerifyPersistedItem(
                    contract.ToLegacyStage18Contract(),
                    item)));
    }

    [Fact]
    public async Task Lesson_progression_contract_rejects_any_count_other_than_eight()
    {
        var ids = Ids.Create();
        var lessonId = Guid.NewGuid();
        var repo = new FakeRepository
        {
            Context = BuildContext(
                ids,
                [],
                [
                    Lesson(
                        ids,
                        lessonId,
                        "S6-NPV",
                        "Number and Place Value",
                        "PED:CAMBRIDGE-INTL-MATH:S6:6NPV-1:APPLY",
                        "Powers of 10: Reason and Apply",
                        1)
                ])
        };

        var result = await new StudentPrivatePracticeService(repo)
            .GenerateAsync(
                ids.User,
                new GenerateStudentPrivatePracticeRequest(
                    ids.Adoption,
                    StudentPrivatePracticeScope.Lesson,
                    lessonId,
                    null,
                    StudentPrivatePracticeDifficulty.MyLevel,
                    10,
                    7,
                    UseLessonDifficultyProgression: true));

        Assert.Equal(
            StudentPrivatePracticeError.InvalidQuestionCount,
            result.Error);
    }

    [Fact]
    public async Task Lesson_personal_practice_uses_genuine_shape_form_diversity_without_padding()
    {
        const string family = "supporting.geometry.shape_dimension";
        var contract = LessonPracticeContractRegistry.All
            .First(x =>
                x.AllowedQuestionFamilies.Count == 1 &&
                string.Equals(
                    x.AllowedQuestionFamilies[0],
                    family,
                    StringComparison.Ordinal));

        var ids = Ids.Create();
        var lessonId = Guid.NewGuid();
        var lesson = Lesson(
            ids,
            lessonId,
            "SHAPES",
            "Geometry",
            contract.LessonCode,
            "2D and 3D shape properties",
            1);

        var repo = new FakeRepository
        {
            Context = BuildContext(ids, [], [lesson])
        };

        var result = await new StudentPrivatePracticeService(repo)
            .GenerateAsync(
                ids.User,
                new GenerateStudentPrivatePracticeRequest(
                    ids.Adoption,
                    StudentPrivatePracticeScope.Lesson,
                    lessonId,
                    null,
                    StudentPrivatePracticeDifficulty.MyLevel,
                    5,
                    20260923));

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.NotNull(repo.SavedAttempt);
        Assert.Equal(5, repo.SavedItems.Count);
        Assert.Equal(5m, repo.SavedAttempt!.MaxScore);

        var semanticPairs = new List<string>();
        var forms = new HashSet<int>();

        foreach (var item in repo.SavedItems)
        {
            using var document = JsonDocument.Parse(
                item.GenerationParametersJson!);
            var parameters = document.RootElement
                .GetProperty("parameters");
            var shape = parameters
                .GetProperty("shape")
                .GetInt32();
            var form = parameters
                .GetProperty("form")
                .GetInt32();

            forms.Add(form);
            semanticPairs.Add($"{form}:{shape}");
        }

        Assert.Equal(
            repo.SavedItems.Count,
            semanticPairs.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            new[] { 0, 1 },
            forms.OrderBy(x => x).ToArray());

        Assert.All(
            repo.SavedItems,
            item =>
            {
                using var metadata = JsonDocument.Parse(
                    item.ValidationMetadataJson!);
                Assert.Equal(
                    "READY_BALANCED",
                    metadata.RootElement
                        .GetProperty("sessionReadiness")
                        .GetString());
                Assert.True(
                    metadata.RootElement
                        .GetProperty("sessionQualityValidated")
                        .GetBoolean());
                Assert.Equal(
                    5,
                    metadata.RootElement
                        .GetProperty("sessionSemanticCount")
                        .GetInt32());
            });
    }

    [Fact]
    public async Task Shape_lesson_progression_uses_forms_that_truthfully_support_each_cognitive_level()
    {
        const string family = "supporting.geometry.shape_dimension";
        var contract = LessonPracticeContractRegistry.All
            .First(x =>
                x.AllowedQuestionFamilies.Count == 1 &&
                string.Equals(
                    x.AllowedQuestionFamilies[0],
                    family,
                    StringComparison.Ordinal));

        var ids = Ids.Create();
        var lessonId = Guid.NewGuid();
        var repo = new FakeRepository
        {
            Context = BuildContext(
                ids,
                [],
                [
                    Lesson(
                        ids,
                        lessonId,
                        "SHAPES",
                        "Geometry",
                        contract.LessonCode,
                        "2D and 3D shape properties",
                        1)
                ])
        };

        var result = await new StudentPrivatePracticeService(repo)
            .GenerateAsync(
                ids.User,
                new GenerateStudentPrivatePracticeRequest(
                    ids.Adoption,
                    StudentPrivatePracticeScope.Lesson,
                    lessonId,
                    null,
                    StudentPrivatePracticeDifficulty.MyLevel,
                    8,
                    20260924,
                    UseLessonDifficultyProgression: true));

        Assert.True(result.Succeeded);
        Assert.Equal(8, repo.SavedItems.Count);

        var expectedDifficulty = new[]
        {
            "Standard", "Standard", "Standard",
            "Stretch", "Stretch", "Stretch",
            "Challenge", "Challenge"
        };
        var semanticKeys = new List<string>();

        for (var index = 0; index < repo.SavedItems.Count; index++)
        {
            var item = repo.SavedItems[index];
            using var metadata = JsonDocument.Parse(
                item.ValidationMetadataJson!);

            Assert.Equal(
                "honest-cognitive-v2",
                metadata.RootElement
                    .GetProperty("progression")
                    .GetString());
            Assert.Equal(
                expectedDifficulty[index],
                metadata.RootElement
                    .GetProperty("difficulty")
                    .GetString());
            Assert.Equal(
                index + 1,
                metadata.RootElement
                    .GetProperty("progressionIndex")
                    .GetInt32());
            Assert.Equal(
                "READY_BALANCED",
                metadata.RootElement
                    .GetProperty("sessionReadiness")
                    .GetString());
            Assert.True(
                metadata.RootElement
                    .GetProperty("sessionQualityValidated")
                    .GetBoolean());

            var form = metadata.RootElement
                .GetProperty("questionForm")
                .GetString();

            if (index < 3)
                Assert.Contains(form, new[] { "Identify", "Classify" });
            else if (index < 6)
                Assert.Contains(form, new[] { "Classify", "ErrorAnalysis" });
            else
                Assert.Contains(form, new[] { "ErrorAnalysis", "Transfer" });

            Assert.Equal(
                index < 3
                    ? AssessmentItemDifficulty.Medium
                    : AssessmentItemDifficulty.Challenging,
                item.Difficulty);

            semanticKeys.Add(
                metadata.RootElement
                    .GetProperty("semanticKey")
                    .GetString()!);
        }

        Assert.Equal(
            semanticKeys.Count,
            semanticKeys.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public async Task Weak_area_generation_prefers_low_official_mastery()
    {
        var ids = Ids.Create();
        var weak = Outcome(ids, "CCSS:1.OA.A.1", "Add whole numbers", 1);
        var strong = Outcome(ids, "CCSS:1.OA.A.2", "Add whole numbers", 2);
        var context = BuildContext(ids, [weak, strong], []);
        context = context with
        {
            OfficialMasteries =
            [
                Mastery(ids, weak.Id, 25m),
                Mastery(ids, strong.Id, 95m)
            ]
        };
        var repo = new FakeRepository { Context = context };
        var result = await new StudentPrivatePracticeService(repo).GenerateAsync(
            ids.User,
            new GenerateStudentPrivatePracticeRequest(ids.Adoption, StudentPrivatePracticeScope.WeakAreas,
                null, null, StudentPrivatePracticeDifficulty.MyLevel, 1, 456));

        Assert.True(result.Succeeded);
        Assert.Single(repo.SavedOutcomes);
        Assert.Equal(weak.Id, repo.SavedOutcomes[0].LearningOutcomeId);
    }

    private static StudentPrivatePracticeContext BuildContext(
        Ids ids,
        IReadOnlyList<LearningOutcome> outcomes,
        IReadOnlyList<CurriculumPedagogicalLesson> lessons) =>
        new(
            new StudentProfile { Id = ids.Student, SchoolId = ids.School, UserId = ids.User, Status = AcademicStructureStatus.Active },
            new SchoolCurriculumAdoption
            {
                Id = ids.Adoption, SchoolId = ids.School, AcademicYearId = ids.Year,
                AcademicProgramId = ids.Program, GradeLevelId = ids.Grade, SubjectId = ids.Subject,
                FrameworkVersionId = ids.Framework, CurriculumLevelKey = "CCSS-G1",
                CurriculumLogicalLevel = 1, CurriculumLevelLabel = "Grade 1", IsActive = true, IsPrimary = true
            },
            new ClassGroup
            {
                Id = ids.Class, SchoolId = ids.School, AcademicYearId = ids.Year,
                AcademicProgramId = ids.Program, GradeLevelId = ids.Grade,
                CurriculumAdoptionId = ids.Adoption, Name = "1A", Code = "1A", Status = AcademicStructureStatus.Active
            },
            new StudentEnrollment
            {
                Id = Guid.NewGuid(), SchoolId = ids.School, StudentProfileId = ids.Student,
                ClassGroupId = ids.Class, AcademicYearId = ids.Year
            },
            outcomes,
            lessons,
            [],
            [],
            []);

    private static LearningOutcome Outcome(Ids ids, string code, string description, int order) => new()
    {
        Id = Guid.NewGuid(), SchoolId = ids.School, AcademicProgramId = ids.Program,
        FrameworkVersionId = ids.Framework, SubjectId = ids.Subject, GradeLevelId = ids.Grade,
        CurriculumAdoptionId = ids.Adoption, TopicId = ids.Topic, Code = code,
        Description = description, Order = order
    };

    private static CurriculumPedagogicalLesson Lesson(Ids ids, Guid id, string unitKey, string unitTitle, string code, string title, int order) => new()
    {
        Id = id, FrameworkVersionId = ids.Framework, Code = code, UnitKey = unitKey,
        UnitTitle = unitTitle, Title = title, LogicalLevelFrom = 1, LogicalLevelTo = 1,
        NativeLevel = "Grade 1", SortOrder = order
    };

    private static StudentOutcomeMastery Mastery(Ids ids, Guid outcomeId, decimal percentage) => new()
    {
        Id = Guid.NewGuid(), SchoolId = ids.School, AcademicYearId = ids.Year,
        ClassGroupId = ids.Class, SubjectId = ids.Subject, StudentProfileId = ids.Student,
        LearningOutcomeId = outcomeId, MasteryPercentage = percentage
    };

    private sealed class FakeRepository : IStudentPrivatePracticeRepository
    {
        public IReadOnlyList<PrivatePracticeCurriculumOption> Curricula { get; init; } = [];
        public StudentPrivatePracticeContext? Context { get; init; }
        public IReadOnlyList<PrivatePracticeAttemptSummary> Attempts { get; init; } = [];
        public int ContextCalls { get; private set; }
        public PracticeAttempt? SavedAttempt { get; private set; }
        public List<AssessmentItem> SavedItems { get; } = [];
        public List<AssessmentItemOutcome> SavedOutcomes { get; } = [];
        public List<StudentItemExposure> SavedExposures { get; } = [];

        public Task<IReadOnlyList<PrivatePracticeCurriculumOption>> ListCurriculaAsync(Guid studentUserId, CancellationToken cancellationToken = default) => Task.FromResult(Curricula);
        public Task<StudentPrivatePracticeContext?> GetContextAsync(Guid studentUserId, Guid curriculumAdoptionId, CancellationToken cancellationToken = default)
        {
            ContextCalls++;
            return Task.FromResult(Context);
        }
        public Task<IReadOnlyList<PrivatePracticeAttemptSummary>> ListPrivateAttemptsAsync(Guid studentUserId, CancellationToken cancellationToken = default) => Task.FromResult(Attempts);
        public Task<IReadOnlyList<PrivatePracticeEvidenceItem>> ListPrivateEvidenceAsync(Guid studentUserId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PrivatePracticeEvidenceItem>>([]);
        public Task AddGeneratedAttemptAsync(IReadOnlyList<AssessmentItem> items, IReadOnlyList<AssessmentItemOutcome> itemOutcomes, PracticeAttempt attempt, IReadOnlyList<PracticeAttemptItem> attemptItems, IReadOnlyList<StudentItemExposure> exposures, CancellationToken cancellationToken = default)
        {
            SavedAttempt = attempt;
            SavedItems.AddRange(items);
            SavedOutcomes.AddRange(itemOutcomes);
            SavedExposures.AddRange(exposures);
            return Task.CompletedTask;
        }
    }

    private sealed record Ids(Guid School, Guid User, Guid Student, Guid Adoption, Guid Class, Guid Year, Guid Program, Guid Grade, Guid Subject, Guid Framework, Guid Topic)
    {
        public static Ids Create() => new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
    }
}
