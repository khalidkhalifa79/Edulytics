using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

using Edulytics.Core.Constants;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Lessons;
using Edulytics.Core.Users;
using Edulytics.Services.LessonContent;
namespace Edulytics.Tests.Phase29;

public sealed class Phase29CanonicalRepositoryCoverageTests
{
    [Fact]
    public async Task RepositoryReadsPedagogicalLessonCanonicalBodyAndOfficialAlignment()
    {
        await using var db = CreateDb();
        var ids = await SeedAsync(db);
        var repository = new LessonContentRepository(db);

        var staff = Assert.Single(
            await repository.ListStaffAdoptionsAsync(ids.SchoolId));

        Assert.Equal(ids.FrameworkVersionId, staff.FrameworkVersionId);
        Assert.Equal(ids.AcademicProgramId, staff.AcademicProgramId);
        Assert.Equal("Test Stream", staff.AcademicProgramName);
        Assert.Equal("TEST-MATH", staff.FrameworkCode);
        Assert.Equal("GRADE 6", staff.GradeName);

        var lesson = Assert.Single(
            await repository.ListPedagogicalLessonsAsync(
                [ids.FrameworkVersionId]));

        Assert.Equal(ids.PedagogicalLessonId, lesson.Id);
        Assert.Null(lesson.OfficialLessonNodeId);
        Assert.Equal("TEST:UNIT:NUMBER", lesson.UnitKey);
        Assert.Equal("Number", lesson.UnitTitle);
        Assert.Equal(1, lesson.OfficialOutcomeCount);

        var content = Assert.Single(
            await repository.ListCanonicalContentsAsync(
                [ids.PedagogicalLessonId]));

        Assert.Equal(ids.PedagogicalLessonId, content.PedagogicalLessonId);
        Assert.Equal(CanonicalLessonContentStatus.Published, content.Status);
        Assert.Equal("Canonical lesson title", Assert.Single(content.Translations).Title);

        var outcome = Assert.Single(
            await repository.ListOfficialOutcomesAsync(
                ids.FrameworkVersionId,
                ids.PedagogicalLessonId));

        Assert.Equal(ids.StandardNodeId, outcome.Id);
        Assert.Equal("TEST:STD:G6:001", outcome.Code);
        Assert.Equal("Reference-only standard fixture", outcome.Description);

        var student = Assert.Single(
            await repository.ListStudentAdoptionsAsync(
                ids.StudentUserId,
                ids.SchoolId));

        Assert.Equal(ids.GradeLevelId, student.GradeLevelId);
        Assert.Equal(ids.AcademicProgramId, student.AcademicProgramId);
        Assert.Equal("Test Stream", student.AcademicProgramName);
        Assert.Equal(ids.FrameworkVersionId, student.FrameworkVersionId);
    }

    [Fact]
    public async Task RepositoryFailsClosedForEmptyAndUnknownScopes()
    {
        await using var db = CreateDb();
        var repository = new LessonContentRepository(db);

        Assert.Empty(await repository.ListPedagogicalLessonsAsync([]));
        Assert.Empty(await repository.ListCanonicalContentsAsync([]));
        Assert.Empty(await repository.ListOfficialOutcomesAsync(Guid.NewGuid(), Guid.NewGuid()));
        Assert.Empty(await repository.ListStaffAdoptionsAsync(Guid.NewGuid()));
        Assert.Empty(await repository.ListStudentAdoptionsAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EdulyticsDbContext>()
            .UseInMemoryDatabase("p29-repository-" + Guid.NewGuid())
            .Options;

        return new EdulyticsDbContext(options);
    }

    private static async Task<SeedIds> SeedAsync(EdulyticsDbContext db)
    {
        var now = DateTime.UtcNow;
        var schoolId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var gradeLevelId = Guid.NewGuid();
        var frameworkId = Guid.NewGuid();
        var frameworkVersionId = Guid.NewGuid();
        var standardNodeId = Guid.NewGuid();
        var pedagogicalLessonId = Guid.NewGuid();
        var canonicalContentId = Guid.NewGuid();
        var studentUserId = Guid.NewGuid();
        var studentProfileId = Guid.NewGuid();
        var classGroupId = Guid.NewGuid();
        var academicYearId = Guid.NewGuid();
        var academicProgramId = Guid.NewGuid();

        db.Schools.Add(new School
        {
            Id = schoolId,
            Name = "Coverage School",
            SchoolCode = "COV",
            NormalizedSchoolCode = "COV",
            Status = SchoolStatus.Active,
            CountryCode = "GB",
            City = "London",
            ContactEmail = "coverage@example.test",
            DefaultCulture = "en",
            TimeZoneId = "UTC",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.Subjects.Add(new Subject
        {
            Id = subjectId,
            SchoolId = schoolId,
            Name = "Mathematics",
            Code = "MATH6",
            NormalizedCode = "MATH6",
            Status = AcademicStructureStatus.Active
        });

        db.GradeLevels.Add(new GradeLevel
        {
            Id = gradeLevelId,
            SchoolId = schoolId,
            Name = "GRADE 6",
            Order = 6
        });

        db.AcademicPrograms.Add(new AcademicProgram
        {
            Id = academicProgramId,
            SchoolId = schoolId,
            Name = "Test Stream",
            Code = "TEST",
            NormalizedCode = "TEST",
            Status = AcademicStructureStatus.Active,
            IsDefault = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.CurriculumFrameworks.Add(new CurriculumFramework
        {
            Id = frameworkId,
            Code = "TEST-MATH",
            NormalizedCode = "TEST-MATH",
            Name = "Repository Test Mathematics",
            CountryCode = "GB",
            ProviderName = "Test Curriculum Authority",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.CurriculumFrameworkVersions.Add(new CurriculumFrameworkVersion
        {
            Id = frameworkVersionId,
            FrameworkId = frameworkId,
            VersionCode = "TEST-2026",
            NormalizedVersionCode = "TEST-2026",
            Name = "Repository Test Mathematics",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.SchoolCurriculumAdoptions.Add(new SchoolCurriculumAdoption
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            AcademicYearId = null,
            AcademicProgramId = academicProgramId,
            GradeLevelId = gradeLevelId,
            SubjectId = subjectId,
            FrameworkVersionId = frameworkVersionId,
            IsPrimary = true,
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.CurriculumPackContentNodes.Add(new CurriculumPackContentNode
        {
            Id = standardNodeId,
            FrameworkVersionId = frameworkVersionId,
            FrameworkCode = "TEST-MATH",
            VersionCode = "TEST-2026",
            NodeKind = "Standard",
            Code = "TEST:STD:G6:001",
            LogicalLevelFrom = 6,
            LogicalLevelTo = 6,
            NativeLevel = "Grade 6",
            Title = "Standard title",
            OfficialText = null,
            AuthorDescription = "Reference-only standard fixture",
            SourceAuthority = "Test Curriculum Authority",
            SourceUrl = "https://example.test/source",
            SourceLocator = "coverage",
            Attribution = "coverage",
            IsOfficial = true,
            IsActive = true,
            SortOrder = 1,
            ContentHash = new string('b', 64),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.CurriculumPedagogicalLessons.Add(new CurriculumPedagogicalLesson
        {
            Id = pedagogicalLessonId,
            FrameworkVersionId = frameworkVersionId,
            OfficialLessonNodeId = null,
            Code = "PED:TEST:MATH:G6:L01",
            UnitKey = "TEST:UNIT:NUMBER",
            UnitTitle = "Number",
            Title = "Repository fixture lesson",
            LogicalLevelFrom = 6,
            LogicalLevelTo = 6,
            NativeLevel = "Grade 6",
            SortOrder = 1,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.CurriculumPedagogicalLessonOutcomes.Add(new CurriculumPedagogicalLessonOutcome
        {
            PedagogicalLessonId = pedagogicalLessonId,
            FrameworkVersionId = frameworkVersionId,
            OutcomeNodeId = standardNodeId,
            SortOrder = 1
        });

        db.CurriculumLessonContents.Add(new CurriculumLessonContent
        {
            Id = canonicalContentId,
            FrameworkVersionId = frameworkVersionId,
            PedagogicalLessonId = pedagogicalLessonId,
            Status = CanonicalLessonContentStatus.Published,
            ContentVersion = "1",
            VerifiedAtUtc = now,
            PublishedAtUtc = now,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.CurriculumLessonContentTranslations.Add(new CurriculumLessonContentTranslation
        {
            Id = Guid.NewGuid(),
            CurriculumLessonContentId = canonicalContentId,
            CultureCode = "en",
            Title = "Canonical lesson title",
            Explanation = "Explanation body",
            KeyConceptsAndRules = "Rules body",
            WorkedExamples = "Examples body",
            StepByStepSolutions = "Solutions body",
            CommonMistakes = "Mistakes body",
            QuickSummary = "Summary body",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.StudentProfiles.Add(new StudentProfile
        {
            Id = studentProfileId,
            SchoolId = schoolId,
            UserId = studentUserId,
            StudentNumber = "S001",
            NormalizedStudentNumber = "S001",
            FirstName = "Coverage",
            LastName = "Student",
            DisplayName = "Coverage Student",
            Status = AcademicStructureStatus.Active,
            IsArchived = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });

        db.ClassGroups.Add(new ClassGroup
        {
            Id = classGroupId,
            SchoolId = schoolId,
            AcademicYearId = academicYearId,
            AcademicProgramId = academicProgramId,
            GradeLevelId = gradeLevelId,
            Name = "A-1",
            Code = "A-1",
            NormalizedCode = "A-1",
            Status = AcademicStructureStatus.Active
        });

        db.StudentEnrollments.Add(new StudentEnrollment
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            StudentProfileId = studentProfileId,
            ClassGroupId = classGroupId,
            AcademicYearId = academicYearId,
            EnrolledAtUtc = now
        });

        await db.SaveChangesAsync();

        return new SeedIds(
            schoolId,
            academicProgramId,
            gradeLevelId,
            frameworkVersionId,
            standardNodeId,
            pedagogicalLessonId,
            studentUserId);
    }

    private sealed record SeedIds(
        Guid SchoolId,
        Guid AcademicProgramId,
        Guid GradeLevelId,
        Guid FrameworkVersionId,
        Guid StandardNodeId,
        Guid PedagogicalLessonId,
        Guid StudentUserId);
}

// PHASE29_SERVICE_BEHAVIOR_COVERAGE_V1
public sealed class Phase29LessonContentServiceCoverageTests
{
    [Fact]
    public async Task DashboardUsesNativeLogicalLevelsAndCountsOnlyPublishedAlignedLessonsAsReady()
    {
        var schoolId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        var repo = new FakeLessonRepository
        {
            StaffContexts =
            [
                Context(versionId, "US-CCSS-MATH", "GRADE 6", 6),
                Context(versionId, "US-CCSS-MATH", "Grade Level 6", 99),
                Context(versionId, "US-CCSS-MATH", "Alpha Cohort", 4)
            ],
            Lessons =
            [
                Lesson(Guid.NewGuid(), versionId, 7, 1, 1),
                Lesson(Guid.NewGuid(), versionId, 7, 2, 0),
                Lesson(Guid.NewGuid(), versionId, 4, 3, 1)
            ]
        };

        repo.Contents =
        [
            Content(versionId, repo.Lessons[0].Id, CanonicalLessonContentStatus.Published, En("Published aligned")),
            Content(versionId, repo.Lessons[1].Id, CanonicalLessonContentStatus.Published, En("Published unaligned")),
            Content(versionId, repo.Lessons[2].Id, CanonicalLessonContentStatus.Published, En("Fallback grade"))
        ];

        var service = Service(
            repo,
            Actor(actorId, schoolId, RoleNames.SubjectSupervisor),
            ActiveSchool(schoolId));

        var result = await service.GetDashboardAsync(actorId);

        Assert.Null(result.Error);
        var dashboard = Assert.IsType<LessonContentDashboard>(result.Value);
        Assert.Equal(schoolId, dashboard.SchoolId);
        Assert.Equal(3, dashboard.Curricula.Count);

        var exact = Assert.Single(dashboard.Curricula, x => x.GradeName == "GRADE 6");
        Assert.Equal(2, exact.TotalLessons);
        Assert.Equal(2, exact.ProductionReadyLessons);
        Assert.Contains(exact.Lessons, x => x.HasOfficialAlignment);
        Assert.Contains(exact.Lessons, x => !x.HasOfficialAlignment);

        var regexMapped = Assert.Single(dashboard.Curricula, x => x.GradeName == "Grade Level 6");
        Assert.Equal(2, regexMapped.TotalLessons);

        var fallback = Assert.Single(dashboard.Curricula, x => x.GradeName == "Alpha Cohort");
        Assert.Single(fallback.Lessons);
        Assert.Equal(3, fallback.Lessons[0].SortOrder);
    }

    [Fact]
    public async Task StaffDetailReturnsCommonCoreAcademicLanguageRegardlessOfUiCulture()
    {
        var schoolId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        var repo = new FakeLessonRepository
        {
            StaffContexts = [Context(versionId, "US-CCSS-MATH", "Grade 6", 7)],
            Lessons = [Lesson(lessonId, versionId, 7, 1, 2)],
            Contents =
            [
                Content(
                    versionId,
                    lessonId,
                    CanonicalLessonContentStatus.Published,
                    En("English canonical title"),
                    Pl("Polski tytuł"))
            ]
        };

        repo.OutcomesByLesson[lessonId] =
        [
            new LessonOutcomeRecord(Guid.NewGuid(), "STD-1", "Outcome one", 1),
            new LessonOutcomeRecord(Guid.NewGuid(), "STD-2", "Outcome two", 2)
        ];

        var service = Service(
            repo,
            Actor(actorId, schoolId, RoleNames.Teacher),
            ActiveSchool(schoolId));

        var fallback = await service.GetStaffLessonAsync(actorId, lessonId, "fr-FR");
        Assert.Null(fallback.Error);
        var detail = Assert.IsType<CanonicalLessonDetail>(fallback.Value);
        Assert.Equal("English canonical title", Assert.IsType<CanonicalLessonTranslationRecord>(detail.Body).Title);
        Assert.Equal(2, detail.Outcomes.Count);

        var polish = await service.GetStaffLessonAsync(actorId, lessonId, "pl-PL");
        Assert.Null(polish.Error);
        Assert.Equal("English canonical title", Assert.IsType<CanonicalLessonTranslationRecord>(polish.Value!.Body).Title);

        repo.Contents =
        [
            Content(versionId, lessonId, CanonicalLessonContentStatus.Verified, En("Verified"))
        ];

        var verified = await service.GetStaffLessonAsync(actorId, lessonId, "en");
        Assert.Null(verified.Error);
        Assert.Null(verified.Value!.Body);
        Assert.Equal(CanonicalLessonContentStatus.Verified, verified.Value.Status);
    }

    [Fact]
    public async Task StudentListIncludesPublishedSupportingAndFiltersDraftAndWrongGrade()
    {
        var schoolId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var versionId = Guid.NewGuid();

        var visible = Guid.NewGuid();
        var unaligned = Guid.NewGuid();
        var draft = Guid.NewGuid();
        var wrongLevel = Guid.NewGuid();

        var repo = new FakeLessonRepository
        {
            StudentContexts = [Context(versionId, "US-CCSS-MATH", "Grade Level 6", 99)],
            Lessons =
            [
                Lesson(visible, versionId, 7, 1, 1),
                Lesson(unaligned, versionId, 7, 2, 0),
                Lesson(draft, versionId, 7, 3, 1),
                Lesson(wrongLevel, versionId, 8, 4, 1)
            ],
            Contents =
            [
                Content(versionId, visible, CanonicalLessonContentStatus.Published, En("Visible")),
                Content(versionId, unaligned, CanonicalLessonContentStatus.Published, En("Unaligned")),
                Content(versionId, draft, CanonicalLessonContentStatus.Draft, En("Draft")),
                Content(versionId, wrongLevel, CanonicalLessonContentStatus.Published, En("Wrong level"))
            ]
        };

        var service = Service(
            repo,
            Actor(actorId, schoolId, RoleNames.Student),
            ActiveSchool(schoolId));

        var result = await service.ListPublishedForStudentAsync(actorId, "");

        Assert.Null(result.Error);
        Assert.Equal(2, result.Value!.Count);
        Assert.Contains(result.Value, x => x.Id == visible && !x.IsSupporting);
        Assert.Contains(result.Value, x => x.Id == unaligned && x.IsSupporting);
    }

    [Fact]
    public async Task StudentDetailReturnsPublishedAlignedBodyAndUpdatedTimestampFallback()
    {
        var schoolId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var updated = DateTime.UtcNow.AddMinutes(-5);

        var repo = new FakeLessonRepository
        {
            StudentContexts = [Context(versionId, "UAE-MOE-MATH", "Grade 6", 6)],
            Lessons = [Lesson(lessonId, versionId, 6, 1, 1)],
            Contents =
            [
                new CanonicalLessonContentRecord(
                    Guid.NewGuid(),
                    versionId,
                    lessonId,
                    CanonicalLessonContentStatus.Published,
                    "1",
                    updated.AddDays(-1),
                    null,
                    updated,
                    [En("Student lesson"), Pl("Lekcja ucznia")])
            ]
        };

        repo.OutcomesByLesson[lessonId] =
        [
            new LessonOutcomeRecord(Guid.NewGuid(), "STD-1", "Official outcome", 1)
        ];

        var service = Service(
            repo,
            Actor(actorId, schoolId, RoleNames.Student),
            ActiveSchool(schoolId));

        var result = await service.GetPublishedForStudentAsync(actorId, lessonId, "pl-PL");

        Assert.Null(result.Error);
        var detail = Assert.IsType<StudentLessonDetail>(result.Value);
        Assert.Equal("Student lesson", detail.Title);
        Assert.Equal(updated, detail.PublishedAtUtc);
        Assert.Single(detail.Outcomes);
    }

    [Fact]
    public async Task StudentDetailFailsClosedAcrossLessonGradeContentAndTranslationBoundaries()
    {
        var schoolId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        var repo = new FakeLessonRepository
        {
            StudentContexts = [Context(versionId, "UAE-MOE-MATH", "Grade 6", 6)]
        };

        var service = Service(
            repo,
            Actor(actorId, schoolId, RoleNames.Student),
            ActiveSchool(schoolId));

        var missingLesson = await service.GetPublishedForStudentAsync(actorId, lessonId, "en");
        Assert.Equal(LessonContentErrorCode.LessonNotFound, missingLesson.Error);

        repo.Lessons = [Lesson(lessonId, versionId, 6, 1, 0)];
        repo.Contents = [Content(versionId, lessonId, CanonicalLessonContentStatus.Published, En("Supporting"))];
        var supporting = await service.GetPublishedForStudentAsync(actorId, lessonId, "pl-PL");
        Assert.Null(supporting.Error);
        Assert.True(supporting.Value!.IsSupporting);
        Assert.Empty(supporting.Value.Outcomes);
        Assert.Equal("Supporting", supporting.Value.Title);

        repo.Lessons = [Lesson(lessonId, versionId, 7, 1, 1)];
        var wrongGrade = await service.GetPublishedForStudentAsync(actorId, lessonId, "en");
        Assert.Equal(LessonContentErrorCode.LessonNotFound, wrongGrade.Error);

        repo.Lessons = [Lesson(lessonId, versionId, 6, 1, 1)];
        repo.Contents = [];
        var noContent = await service.GetPublishedForStudentAsync(actorId, lessonId, "en");
        Assert.Equal(LessonContentErrorCode.LessonNotFound, noContent.Error);

        repo.Contents = [Content(versionId, lessonId, CanonicalLessonContentStatus.Draft, En("Draft"))];
        var draft = await service.GetPublishedForStudentAsync(actorId, lessonId, "en");
        Assert.Equal(LessonContentErrorCode.LessonNotFound, draft.Error);

        repo.Contents =
        [
            new CanonicalLessonContentRecord(
                Guid.NewGuid(),
                versionId,
                lessonId,
                CanonicalLessonContentStatus.Published,
                "1",
                DateTime.UtcNow,
                DateTime.UtcNow,
                DateTime.UtcNow,
                [])
        ];

        var untranslated = await service.GetPublishedForStudentAsync(actorId, lessonId, "en");
        Assert.Equal(LessonContentErrorCode.LessonNotFound, untranslated.Error);
    }

    [Fact]
    public async Task StaffDetailFailsClosedForUnknownLessonAndWrongLogicalLevel()
    {
        var schoolId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();

        var repo = new FakeLessonRepository
        {
            StaffContexts = [Context(versionId, "UAE-MOE-MATH", "Grade 6", 6)]
        };

        var service = Service(
            repo,
            Actor(actorId, schoolId, RoleNames.SchoolAdmin),
            ActiveSchool(schoolId));

        var missing = await service.GetStaffLessonAsync(actorId, lessonId, "en");
        Assert.Equal(LessonContentErrorCode.LessonNotFound, missing.Error);

        repo.Lessons = [Lesson(lessonId, versionId, 7, 1, 1)];
        var wrongGrade = await service.GetStaffLessonAsync(actorId, lessonId, "en");
        Assert.Equal(LessonContentErrorCode.LessonNotFound, wrongGrade.Error);
    }

    [Fact]
    public async Task StaffDetailOpensPublishedSupportingBodyWithoutFabricatedOutcomes()
    {
        var schoolId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var lessonId = Guid.NewGuid();
        var repo = new FakeLessonRepository
        {
            StaffContexts = [Context(versionId, "US-CCSS-MATH", "Grade 6", 7)],
            Lessons = [Lesson(lessonId, versionId, 7, 1, 0)],
            Contents = [Content(versionId, lessonId, CanonicalLessonContentStatus.Published, En("English supporting body"))]
        };

        var service = Service(repo, Actor(actorId, schoolId, RoleNames.Teacher), ActiveSchool(schoolId));
        var result = await service.GetStaffLessonAsync(actorId, lessonId, "pl-PL");

        Assert.Null(result.Error);
        Assert.Equal("English supporting body", result.Value!.Body!.Title);
        Assert.Empty(result.Value.Outcomes);
    }

    [Fact]
    public async Task ScopeAndRoleGuardsFailClosed()
    {
        var schoolId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var emptyRepo = new FakeLessonRepository();

        foreach (var actor in new SchoolUserRecord?[]
        {
            null,
            Actor(actorId, schoolId, RoleNames.Teacher) with { IsActive = false },
            Actor(actorId, schoolId, RoleNames.Teacher) with { IsLocked = true },
            Actor(actorId, null, RoleNames.Teacher)
        })
        {
            var service = Service(emptyRepo, actor, ActiveSchool(schoolId));
            var result = await service.GetDashboardAsync(actorId);
            Assert.Equal(LessonContentErrorCode.AccessDenied, result.Error);
        }

        var missingSchool = Service(
            emptyRepo,
            Actor(actorId, schoolId, RoleNames.Teacher),
            null);

        Assert.Equal(
            LessonContentErrorCode.SchoolNotActive,
            (await missingSchool.GetDashboardAsync(actorId)).Error);

        var suspendedSchool = ActiveSchool(schoolId);
        suspendedSchool.Status = SchoolStatus.Suspended;

        var suspended = Service(
            emptyRepo,
            Actor(actorId, schoolId, RoleNames.Teacher),
            suspendedSchool);

        Assert.Equal(
            LessonContentErrorCode.SchoolNotActive,
            (await suspended.GetDashboardAsync(actorId)).Error);

        var studentOnStaffSurface = Service(
            emptyRepo,
            Actor(actorId, schoolId, RoleNames.Student),
            ActiveSchool(schoolId));

        Assert.Equal(
            LessonContentErrorCode.AccessDenied,
            (await studentOnStaffSurface.GetDashboardAsync(actorId)).Error);

        var teacherOnStudentSurface = Service(
            emptyRepo,
            Actor(actorId, schoolId, RoleNames.Teacher),
            ActiveSchool(schoolId));

        Assert.Equal(
            LessonContentErrorCode.AccessDenied,
            (await teacherOnStudentSurface.ListPublishedForStudentAsync(actorId, "en")).Error);

        Assert.Equal(
            LessonContentErrorCode.AccessDenied,
            (await teacherOnStudentSurface.GetPublishedForStudentAsync(actorId, Guid.NewGuid(), "en")).Error);
    }

    private static LessonContentService Service(
        FakeLessonRepository lessons,
        SchoolUserRecord? actor,
        School? school) =>
        new(
            lessons,
            new FakeSchoolUserRepository(actor),
            new FakeSchoolRepository(school));

    private static SchoolUserRecord Actor(
        Guid id,
        Guid? schoolId,
        string role) =>
        new(
            id,
            schoolId,
            "actor@example.test",
            true,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            [role]);

    private static School ActiveSchool(Guid id) =>
        new()
        {
            Id = id,
            Name = "Coverage School",
            SchoolCode = "COV",
            NormalizedSchoolCode = "COV",
            Status = SchoolStatus.Active,
            CountryCode = "GB",
            City = "London",
            ContactEmail = "coverage@example.test",
            DefaultCulture = "en",
            TimeZoneId = "UTC",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

    private static CanonicalCurriculumContextRecord Context(
        Guid versionId,
        string frameworkCode,
        string gradeName,
        int gradeOrder) =>
        new(
            versionId,
            frameworkCode,
            frameworkCode + " Framework",
            "Version",
            Guid.NewGuid(),
            "Mathematics",
            "MATH",
            Guid.NewGuid(),
            gradeName,
            gradeOrder);

    private static PedagogicalLessonRecord Lesson(
        Guid id,
        Guid versionId,
        int logicalLevel,
        int sortOrder,
        int officialOutcomeCount) =>
        new(
            id,
            versionId,
            null,
            "PED:" + id.ToString("N"),
            "UNIT",
            "Unit",
            "Lesson " + sortOrder,
            null,
            logicalLevel,
            logicalLevel,
            sortOrder,
            officialOutcomeCount);

    private static CanonicalLessonContentRecord Content(
        Guid versionId,
        Guid lessonId,
        CanonicalLessonContentStatus status,
        params CanonicalLessonTranslationRecord[] translations)
    {
        var now = DateTime.UtcNow;

        return new(
            Guid.NewGuid(),
            versionId,
            lessonId,
            status,
            "1",
            status == CanonicalLessonContentStatus.Draft ? null : now,
            status == CanonicalLessonContentStatus.Published ? now : null,
            now,
            translations);
    }

    private static CanonicalLessonTranslationRecord En(string title) =>
        new("en", title, "Explanation", "Rules", "Examples", "Solutions", "Mistakes", "Summary");

    private static CanonicalLessonTranslationRecord Pl(string title) =>
        new("pl", title, "Wyjaśnienie", "Zasady", "Przykłady", "Rozwiązania", "Błędy", "Podsumowanie");

    private sealed class FakeLessonRepository : ILessonContentRepository
    {
        public IReadOnlyList<CanonicalCurriculumContextRecord> StaffContexts { get; set; } = [];
        public IReadOnlyList<CanonicalCurriculumContextRecord> StudentContexts { get; set; } = [];
        public IReadOnlyList<PedagogicalLessonRecord> Lessons { get; set; } = [];
        public IReadOnlyList<CanonicalLessonContentRecord> Contents { get; set; } = [];
        public Dictionary<Guid, IReadOnlyList<LessonOutcomeRecord>> OutcomesByLesson { get; } = [];

        public Task<IReadOnlyList<CanonicalCurriculumContextRecord>> ListStaffAdoptionsAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(StaffContexts);

        public Task<IReadOnlyList<CanonicalCurriculumContextRecord>> ListStudentAdoptionsAsync(
            Guid actorUserId,
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(StudentContexts);

        public Task<IReadOnlyList<PedagogicalLessonRecord>> ListPedagogicalLessonsAsync(
            IReadOnlyCollection<Guid> frameworkVersionIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<PedagogicalLessonRecord>>(
                Lessons
                    .Where(x => frameworkVersionIds.Contains(x.FrameworkVersionId))
                    .ToArray());

        public Task<IReadOnlyList<CanonicalLessonContentRecord>> ListCanonicalContentsAsync(
            IReadOnlyCollection<Guid> pedagogicalLessonIds,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CanonicalLessonContentRecord>>(
                Contents
                    .Where(x => pedagogicalLessonIds.Contains(x.PedagogicalLessonId))
                    .ToArray());

        public Task<IReadOnlyList<LessonOutcomeRecord>> ListOfficialOutcomesAsync(
            Guid frameworkVersionId,
            Guid pedagogicalLessonId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                OutcomesByLesson.GetValueOrDefault(
                    pedagogicalLessonId,
                    Array.Empty<LessonOutcomeRecord>()));
    }

    private sealed class FakeSchoolUserRepository : ISchoolUserRepository
    {
        private readonly SchoolUserRecord? _actor;

        public FakeSchoolUserRepository(SchoolUserRecord? actor) => _actor = actor;

        public Task<SchoolUserRecord?> GetActorAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_actor);

        public Task<IReadOnlyList<SchoolUserRecord>> ListBySchoolAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SchoolUserRecord?> GetBySchoolAndIdAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SchoolUserPersistenceResult> CreateAsync(
            Guid schoolId,
            string email,
            string role,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SchoolUserPersistenceResult> SetActiveAsync(
            Guid schoolId,
            Guid userId,
            bool isActive,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SchoolUserPersistenceResult> SetLockedAsync(
            Guid schoolId,
            Guid userId,
            bool isLocked,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SchoolUserPersistenceResult> SetRoleAsync(
            Guid schoolId,
            Guid userId,
            string role,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SchoolUserPersistenceResult> GeneratePasswordSetupAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SchoolUserPersistenceResult> CompletePasswordSetupAsync(
            Guid userId,
            string token,
            string newPassword,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeSchoolRepository : ISchoolRepository
    {
        private readonly School? _school;

        public FakeSchoolRepository(School? school) => _school = school;

        public Task<IReadOnlyList<School>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<School>>(
                _school is null ? Array.Empty<School>() : [_school]);

        public Task<School?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_school?.Id == id ? _school : null);

        public Task<School?> GetForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistsByNormalizedCodeAsync(
            string normalizedSchoolCode,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddAsync(
            School school,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<SchoolRepositoryWriteResult> SaveAsync(
            School school,
            byte[]? expectedRowVersion,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
