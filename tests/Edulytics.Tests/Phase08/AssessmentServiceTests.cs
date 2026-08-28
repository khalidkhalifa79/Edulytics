using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Edulytics.Services.Assessments;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase08;

public sealed class AssessmentServiceTests
{
    [Fact]
    public async Task AssignedTeacherCanCreate_ButUnassignedTeacherCannot()
    {
        using var assigned = Fixture.Create(assignTeacher: true);
        var ok = await assigned.Service.CreateAssessmentAsync(
            assigned.Teacher.Id,
            assigned.Request());

        Assert.True(ok.Succeeded);

        using var unassigned = Fixture.Create(assignTeacher: false);
        var denied = await unassigned.Service.CreateAssessmentAsync(
            unassigned.Teacher.Id,
            unassigned.Request());

        Assert.False(denied.Succeeded);
        Assert.Equal(AssessmentErrorCode.TeacherNotAssigned, denied.Error);
    }


    [Fact]
    public async Task CreateQuestionRequiresOutcome_AndOpeningUsesIntegratedMapping()
    {
        using var f = Fixture.Create();

        var assessmentId =
            await f.CreateAssessmentAsync();

        var details =
            await f.Service.GetDetailsAsync(
                f.Teacher.Id,
                assessmentId);

        var noOutcome =
            await f.Service.CreateQuestionAsync(
                f.Teacher.Id,
                new CreateAssessmentQuestionRequest(
                    assessmentId,
                    "Only question",
                    10m,
                    1,
                    [],
                    details.Value!.Assessment.RowVersion));

        Assert.False(noOutcome.Succeeded);

        Assert.Equal(
            AssessmentErrorCode.Required,
            noOutcome.Error);

        details =
            await f.Service.GetDetailsAsync(
                f.Teacher.Id,
                assessmentId);

        var question =
            await f.Service.CreateQuestionAsync(
                f.Teacher.Id,
                new CreateAssessmentQuestionRequest(
                    assessmentId,
                    "Only question",
                    10m,
                    1,
                    [f.Outcome.Id],
                    details.Value!.Assessment.RowVersion));

        Assert.True(question.Succeeded);

        details =
            await f.Service.GetDetailsAsync(
                f.Teacher.Id,
                assessmentId);

        var createdQuestion =
            Assert.Single(
                details.Value!.Questions);

        Assert.Contains(
            f.Outcome.Id,
            createdQuestion.OutcomeIds);

        var opened =
            await f.Service.OpenAssessmentAsync(
                f.Teacher.Id,
                assessmentId,
                details.Value.Assessment.RowVersion);

        Assert.True(opened.Succeeded);
    }

    [Fact]
    public async Task ResultScoreAndPercentageAreCalculatedServerSide()
    {
        using var f = Fixture.Create();

        var assessmentId = await f.BuildOpenAssessmentAsync();

        var save = await f.Service.SaveStudentResultAsync(
            f.Teacher.Id,
            new SaveStudentAssessmentResultRequest(
                assessmentId,
                f.Student.Id,
                [f.Question1, f.Question2],
                [3m, 6m],
                null));

        Assert.True(save.Succeeded);

        var results = await f.Service.GetResultsAsync(f.Teacher.Id, assessmentId);
        var student = Assert.Single(results.Value!.Students);

        Assert.Equal(9m, student.Score);
        Assert.Equal(90m, student.Percentage);
    }

    [Fact]
    public async Task ScoreAboveQuestionMaximumIsRejected()
    {
        using var f = Fixture.Create();

        var assessmentId = await f.BuildOpenAssessmentAsync();

        var save = await f.Service.SaveStudentResultAsync(
            f.Teacher.Id,
            new SaveStudentAssessmentResultRequest(
                assessmentId,
                f.Student.Id,
                [f.Question1, f.Question2],
                [5m, 6m],
                null));

        Assert.False(save.Succeeded);
        Assert.Equal(AssessmentErrorCode.InvalidQuestionScore, save.Error);
    }


    [Fact]
    public async Task YearSpecificAdoptionOverridesDefaultAdoption()
    {
        using var f = Fixture.Create();

        var yearFramework = new CurriculumFramework
        {
            Id = Guid.NewGuid(),
            Code = "YEAR-FRAMEWORK",
            NormalizedCode = "YEAR-FRAMEWORK",
            Name = "Year Framework",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var yearVersion = new CurriculumFrameworkVersion
        {
            Id = Guid.NewGuid(),
            FrameworkId = yearFramework.Id,
            VersionCode = "V2",
            NormalizedVersionCode = "V2",
            Name = "Year Version",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        f.Db.CurriculumFrameworks.Add(yearFramework);
        f.Db.CurriculumFrameworkVersions.Add(yearVersion);

        f.Db.SchoolCurriculumAdoptions.Add(
            new SchoolCurriculumAdoption
            {
                Id = Guid.NewGuid(),
                SchoolId = f.School.Id,
                AcademicYearId = f.Year.Id,
                AcademicProgramId = f.Class.AcademicProgramId,
                GradeLevelId = f.Grade.Id,
                SubjectId = f.Subject.Id,
                FrameworkVersionId = yearVersion.Id,
                IsPrimary = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });

        await f.Db.SaveChangesAsync();

        var assessmentId =
            await f.CreateAssessmentAsync();

        var details =
            await f.Service.GetDetailsAsync(
                f.Teacher.Id,
                assessmentId);

        var result =
            await f.Service.CreateQuestionAsync(
                f.Teacher.Id,
                new CreateAssessmentQuestionRequest(
                    assessmentId,
                    "Year-specific precedence question",
                    10m,
                    1,
                    [f.Outcome.Id],
                    details.Value!.Assessment.RowVersion));

        Assert.False(result.Succeeded);

        Assert.Equal(
            AssessmentErrorCode.OutcomeDoesNotMatchAssessment,
            result.Error);
    }


    [Fact]
    public async Task DraftQuestionCanBeDeleted_WithItsMappings()
    {
        using var f = Fixture.Create();

        var assessmentId =
            await f.CreateAssessmentAsync();

        var questionId =
            await f.CreateQuestionAsync(
                assessmentId,
                "Question to delete",
                10m,
                1);

        Assert.True(
            await f.Db.QuestionLearningOutcomes.AnyAsync(
                x => x.AssessmentQuestionId == questionId));

        var details =
            await f.Service.GetDetailsAsync(
                f.Teacher.Id,
                assessmentId);

        var deleted =
            await f.Service.DeleteQuestionAsync(
                f.Teacher.Id,
                new DeleteAssessmentQuestionRequest(
                    questionId,
                    details.Value!.Assessment.RowVersion));

        Assert.True(deleted.Succeeded);

        Assert.False(
            await f.Db.AssessmentQuestions.AnyAsync(
                x => x.Id == questionId));

        Assert.False(
            await f.Db.QuestionLearningOutcomes.AnyAsync(
                x => x.AssessmentQuestionId == questionId));
    }

    [Fact]
    public async Task DraftAssessmentCanBeDeleted_WithQuestionsAndMappings()
    {
        using var f = Fixture.Create();

        var assessmentId =
            await f.CreateAssessmentAsync();

        var questionId =
            await f.CreateQuestionAsync(
                assessmentId,
                "Question inside deleted assessment",
                10m,
                1);

        var details =
            await f.Service.GetDetailsAsync(
                f.Teacher.Id,
                assessmentId);

        var deleted =
            await f.Service.DeleteAssessmentAsync(
                f.Teacher.Id,
                new DeleteAssessmentRequest(
                    assessmentId,
                    details.Value!.Assessment.RowVersion));

        Assert.True(deleted.Succeeded);

        Assert.False(
            await f.Db.Assessments.AnyAsync(
                x => x.Id == assessmentId));

        Assert.False(
            await f.Db.AssessmentQuestions.AnyAsync(
                x => x.Id == questionId));

        Assert.False(
            await f.Db.QuestionLearningOutcomes.AnyAsync(
                x => x.AssessmentQuestionId == questionId));
    }

    [Fact]
    public async Task OpenAssessmentCannotBeDeleted()
    {
        using var f = Fixture.Create();

        var assessmentId =
            await f.BuildOpenAssessmentAsync();

        var details =
            await f.Service.GetDetailsAsync(
                f.Teacher.Id,
                assessmentId);

        var deleted =
            await f.Service.DeleteAssessmentAsync(
                f.Teacher.Id,
                new DeleteAssessmentRequest(
                    assessmentId,
                    details.Value!.Assessment.RowVersion));

        Assert.False(deleted.Succeeded);

        Assert.Equal(
            AssessmentErrorCode.AssessmentNotDraft,
            deleted.Error);
    }

    [Fact]
    public async Task OpenAssessmentQuestionCannotBeDeleted()
    {
        using var f = Fixture.Create();

        var assessmentId =
            await f.BuildOpenAssessmentAsync();

        var details =
            await f.Service.GetDetailsAsync(
                f.Teacher.Id,
                assessmentId);

        var deleted =
            await f.Service.DeleteQuestionAsync(
                f.Teacher.Id,
                new DeleteAssessmentQuestionRequest(
                    f.Question1,
                    details.Value!.Assessment.RowVersion));

        Assert.False(deleted.Succeeded);

        Assert.Equal(
            AssessmentErrorCode.AssessmentNotDraft,
            deleted.Error);
    }

    [Fact]
    public async Task EditQuestionUpdatesOutcomeMappingsAtomically()
    {
        using var f = Fixture.Create();

        var secondOutcome = new LearningOutcome
        {
            Id = Guid.NewGuid(),
            SchoolId = f.School.Id,
            AcademicProgramId = f.Class.AcademicProgramId,
            FrameworkVersionId =
                f.Outcome.FrameworkVersionId,
            SubjectId =
                f.Outcome.SubjectId,
            GradeLevelId =
                f.Outcome.GradeLevelId,
            TopicId =
                f.Outcome.TopicId,
            Code = "G6.N.2",
            Description = "Second number outcome",
            Weight = 1m,
            Order = 2
        };

        f.Db.LearningOutcomes.Add(secondOutcome);
        await f.Db.SaveChangesAsync();

        var assessmentId =
            await f.CreateAssessmentAsync();

        var questionId =
            await f.CreateQuestionAsync(
                assessmentId,
                "Original question",
                10m,
                1);

        var details =
            await f.Service.GetDetailsAsync(
                f.Teacher.Id,
                assessmentId);

        var updated =
            await f.Service.UpdateQuestionAsync(
                f.Teacher.Id,
                new UpdateAssessmentQuestionRequest(
                    questionId,
                    "Updated question",
                    10m,
                    1,
                    [secondOutcome.Id],
                    details.Value!.Assessment.RowVersion));

        Assert.True(updated.Succeeded);

        details =
            await f.Service.GetDetailsAsync(
                f.Teacher.Id,
                assessmentId);

        var question =
            Assert.Single(
                details.Value!.Questions);

        Assert.Single(
            question.OutcomeIds);

        Assert.Equal(
            secondOutcome.Id,
            question.OutcomeIds[0]);
    }


    [Fact]
    public async Task AdminAndSupervisor_CanInspect_ButCannotMutateAssessment()
    {
        using var f = Fixture.Create();
        var assessmentId = await f.CreateAssessmentAsync();

        var teacherDetails = await f.Service.GetDetailsAsync(
            f.Teacher.Id,
            assessmentId);

        Assert.NotNull((await f.Service.GetDetailsAsync(
            f.Admin.Id,
            assessmentId)).Value);

        Assert.NotNull((await f.Service.GetDetailsAsync(
            f.Supervisor.Id,
            assessmentId)).Value);

        foreach (var actorId in new[] { f.Admin.Id, f.Supervisor.Id })
        {
            var denied = await f.Service.DeleteAssessmentAsync(
                actorId,
                new DeleteAssessmentRequest(
                    assessmentId,
                    teacherDetails.Value!.Assessment.RowVersion));

            Assert.False(denied.Succeeded);
            Assert.Equal(AssessmentErrorCode.AccessDenied, denied.Error);
        }
    }

    private sealed class Fixture : IDisposable
    {
        private Fixture(
            EdulyticsDbContext db,
            School school,
            AcademicYear year,
            Term term,
            GradeLevel grade,
            ClassGroup classGroup,
            Subject subject,
            StudentProfile student,
            LearningOutcome outcome,
            SchoolUserRecord admin,
            SchoolUserRecord supervisor,
            SchoolUserRecord teacher,
            AssessmentService service)
        {
            Db = db;
            School = school;
            Year = year;
            Term = term;
            Grade = grade;
            Class = classGroup;
            Subject = subject;
            Student = student;
            Outcome = outcome;
            Admin = admin;
            Supervisor = supervisor;
            Teacher = teacher;
            Service = service;
        }

        public EdulyticsDbContext Db { get; }
        public School School { get; }
        public AcademicYear Year { get; }
        public Term Term { get; }
        public GradeLevel Grade { get; }
        public ClassGroup Class { get; }
        public Subject Subject { get; }
        public StudentProfile Student { get; }
        public LearningOutcome Outcome { get; }
        public SchoolUserRecord Admin { get; }
        public SchoolUserRecord Supervisor { get; }
        public SchoolUserRecord Teacher { get; }
        public AssessmentService Service { get; }
        public Guid Question1 { get; private set; }
        public Guid Question2 { get; private set; }

        public CreateAssessmentRequest Request() =>
            new(
                Class.Id,
                Subject.Id,
                Term.Id,
                "Unit assessment",
                new DateOnly(2026, 9, 15),
                10m);

        public async Task<Guid> CreateAssessmentAsync()
        {
            var result = await Service.CreateAssessmentAsync(Teacher.Id, Request());
            Assert.True(result.Succeeded);
            return result.EntityId!.Value;
        }

        public async Task<Guid> BuildOpenAssessmentAsync()
        {
            var assessmentId = await CreateAssessmentAsync();
            Question1 = await CreateQuestionAsync(assessmentId, "Q1", 4m, 1);
            Question2 = await CreateQuestionAsync(assessmentId, "Q2", 6m, 2);

            var details = await Service.GetDetailsAsync(Teacher.Id, assessmentId);
            var open = await Service.OpenAssessmentAsync(
                Teacher.Id,
                assessmentId,
                details.Value!.Assessment.RowVersion);

            Assert.True(open.Succeeded);
            return assessmentId;
        }

        public async Task<Guid> CreateQuestionAsync(
            Guid assessmentId,
            string prompt,
            decimal maxScore,
            int order)
        {
            var details = await Service.GetDetailsAsync(Teacher.Id, assessmentId);

            var result = await Service.CreateQuestionAsync(
                Teacher.Id,
                new CreateAssessmentQuestionRequest(
                    assessmentId,
                    prompt,
                    maxScore,
                    order,
                    [Outcome.Id],
                    details.Value!.Assessment.RowVersion));

            Assert.True(result.Succeeded);
            return result.EntityId!.Value;
        }



        public static Fixture Create(bool assignTeacher = true)
        {
            var options = new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase($"phase08-{Guid.NewGuid():N}")
                .Options;

            var db = new EdulyticsDbContext(options);
            var school = NewSchool();
            var year = NewYear(school.Id);
            var term = NewTerm(school.Id, year.Id);
            var grade = new GradeLevel
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                Name = "Grade 6",
                Order = 6
            };
            var program = new AcademicProgram
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                Name = "Phase 08 Default Program",
                Code = "MAIN",
                NormalizedCode = "MAIN",
                Status = AcademicStructureStatus.Active,
                IsDefault = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                RowVersion = BitConverter.GetBytes(1L)
            };
            var classGroup = NewClass(
                school.Id,
                year.Id,
                grade.Id,
                program.Id);
            var subject = NewSubject(school.Id);
            var student = NewStudent(school.Id);

            var framework = new CurriculumFramework
            {
                Id = Guid.NewGuid(),
                Code = "TEST-FRAMEWORK",
                NormalizedCode = "TEST-FRAMEWORK",
                Name = "Test Framework",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var frameworkVersion = new CurriculumFrameworkVersion
            {
                Id = Guid.NewGuid(),
                FrameworkId = framework.Id,
                VersionCode = "V1",
                NormalizedVersionCode = "V1",
                Name = "Version 1",
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
            var topic = new CurriculumTopic
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                AcademicProgramId = program.Id,
                FrameworkVersionId = frameworkVersion.Id,
                SubjectId = subject.Id,
                GradeLevelId = grade.Id,
                Name = "Numbers",
                Order = 1
            };
            var outcome = new LearningOutcome
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                AcademicProgramId = program.Id,
                FrameworkVersionId = frameworkVersion.Id,
                SubjectId = subject.Id,
                GradeLevelId = grade.Id,
                TopicId = topic.Id,
                Code = "G6.N.1",
                Description = "Number reasoning",
                Weight = 25m,
                Order = 1
            };
            var adoption = new SchoolCurriculumAdoption
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                AcademicYearId = null,
                AcademicProgramId = program.Id,
                GradeLevelId = grade.Id,
                SubjectId = subject.Id,
                FrameworkVersionId = frameworkVersion.Id,
                IsPrimary = true,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

            var admin = NewUser(school.Id, RoleNames.SchoolAdmin);
            var supervisor = NewUser(
                school.Id,
                RoleNames.SubjectSupervisor);
            var teacher = NewUser(school.Id, RoleNames.Teacher);

            db.Schools.Add(school);
            db.AcademicYears.Add(year);
            db.Terms.Add(term);
            db.GradeLevels.Add(grade);
            db.AcademicPrograms.Add(program);
            db.ClassGroups.Add(classGroup);
            db.Subjects.Add(subject);
            db.StudentProfiles.Add(student);
            db.CurriculumFrameworks.Add(framework);
            db.CurriculumFrameworkVersions.Add(frameworkVersion);
            db.CurriculumTopics.Add(topic);
            db.LearningOutcomes.Add(outcome);
            db.SchoolCurriculumAdoptions.Add(adoption);

            if (assignTeacher)
            {
                db.TeacherAssignments.Add(new TeacherAssignment
                {
                    Id = Guid.NewGuid(),
                    SchoolId = school.Id,
                    TeacherUserId = teacher.Id,
                    ClassGroupId = classGroup.Id,
                    SubjectId = subject.Id,
                    AcademicYearId = year.Id,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }

            db.StudentEnrollments.Add(new StudentEnrollment
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                StudentProfileId = student.Id,
                ClassGroupId = classGroup.Id,
                AcademicYearId = year.Id,
                EnrolledAtUtc = DateTime.UtcNow
            });

            db.SaveChanges();

            var schools = new FakeSchoolRepository();
            schools.Seed(school);

            var users = new FakeUserRepository();
            users.Seed(admin);
            users.Seed(supervisor);
            users.Seed(teacher);

            var service = new AssessmentService(
                new AssessmentRepository(db),
                schools,
                users);

            return new Fixture(
                db,
                school,
                year,
                term,
                grade,
                classGroup,
                subject,
                student,
                outcome,
                admin,
                supervisor,
                teacher,
                service);
        }

        public void Dispose() => Db.Dispose();

        private static School NewSchool()
        {
            var code = $"P8-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

            return new School
            {
                Id = Guid.NewGuid(),
                Name = "Phase 08 School",
                SchoolCode = code,
                NormalizedSchoolCode = code,
                Status = SchoolStatus.Active,
                CountryCode = "PL",
                City = "Warsaw",
                ContactEmail = $"{Guid.NewGuid():N}@example.com",
                DefaultCulture = "en",
                TimeZoneId = "Europe/Warsaw",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                RowVersion = BitConverter.GetBytes(1L)
            };
        }

        private static AcademicYear NewYear(Guid schoolId) =>
            new()
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                Name = "2026/2027",
                StartsOn = new DateOnly(2026, 9, 1),
                EndsOn = new DateOnly(2027, 6, 30),
                Status = AcademicStructureStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow,
                RowVersion = BitConverter.GetBytes(1L)
            };

        private static Term NewTerm(Guid schoolId, Guid yearId) =>
            new()
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                AcademicYearId = yearId,
                Name = "Term 1",
                StartsOn = new DateOnly(2026, 9, 1),
                EndsOn = new DateOnly(2027, 1, 31),
                Status = AcademicStructureStatus.Active
            };

        private static ClassGroup NewClass(
            Guid schoolId,
            Guid yearId,
            Guid gradeId,
            Guid academicProgramId)
        {
            var code = $"C-{Guid.NewGuid():N}"[..10].ToUpperInvariant();
            return new ClassGroup
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                AcademicYearId = yearId,
                AcademicProgramId = academicProgramId,
                GradeLevelId = gradeId,
                Name = "6A",
                Code = code,
                NormalizedCode = code,
                Status = AcademicStructureStatus.Active,
                RowVersion = BitConverter.GetBytes(1L)
            };
        }

        private static Subject NewSubject(Guid schoolId) =>
            new()
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                Name = "Mathematics",
                Code = "MATH",
                NormalizedCode = "MATH",
                Status = AcademicStructureStatus.Active,
                RowVersion = BitConverter.GetBytes(1L)
            };

        private static StudentProfile NewStudent(Guid schoolId)
        {
            var number = $"S-{Guid.NewGuid():N}"[..10].ToUpperInvariant();
            return new StudentProfile
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                StudentNumber = number,
                NormalizedStudentNumber = number,
                FirstName = "Test",
                LastName = "Student",
                DisplayName = "Test Student",
                Status = AcademicStructureStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };
        }

        private static SchoolUserRecord NewUser(Guid schoolId, string role) =>
            new(
                Guid.NewGuid(),
                schoolId,
                $"{Guid.NewGuid():N}@example.com",
                true,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                [role]);
    }

    private sealed class FakeSchoolRepository : ISchoolRepository
    {
        private readonly List<School> _schools = [];
        public void Seed(School school) => _schools.Add(school);

        public Task<IReadOnlyList<School>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<School>>(_schools.ToArray());

        public Task<School?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_schools.SingleOrDefault(x => x.Id == id));

        public Task<School?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
            GetByIdAsync(id, cancellationToken);

        public Task<bool> ExistsByNormalizedCodeAsync(
            string normalizedSchoolCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_schools.Any(x => x.NormalizedSchoolCode == normalizedSchoolCode));

        public Task AddAsync(School school, CancellationToken cancellationToken = default)
        {
            _schools.Add(school);
            return Task.CompletedTask;
        }

        public Task<SchoolRepositoryWriteResult> SaveAsync(
            School school,
            byte[]? expectedRowVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SchoolRepositoryWriteResult.Success);
    }

    private sealed class FakeUserRepository : ISchoolUserRepository
    {
        private readonly Dictionary<Guid, SchoolUserRecord> _users = [];
        public void Seed(SchoolUserRecord user) => _users[user.Id] = user;

        public Task<SchoolUserRecord?> GetActorAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.GetValueOrDefault(userId));

        public Task<IReadOnlyList<SchoolUserRecord>> ListBySchoolAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchoolUserRecord>>(
                _users.Values.Where(x => x.SchoolId == schoolId).ToArray());

        public Task<SchoolUserRecord?> GetBySchoolAndIdAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = _users.GetValueOrDefault(userId);
            return Task.FromResult(user?.SchoolId == schoolId ? user : null);
        }

        public Task<SchoolUserPersistenceResult> CreateAsync(
            Guid schoolId, string email, string role,
            CancellationToken cancellationToken = default) => Failure();

        public Task<SchoolUserPersistenceResult> SetActiveAsync(
            Guid schoolId, Guid userId, bool isActive,
            CancellationToken cancellationToken = default) => Failure();

        public Task<SchoolUserPersistenceResult> SetLockedAsync(
            Guid schoolId, Guid userId, bool isLocked,
            CancellationToken cancellationToken = default) => Failure();

        public Task<SchoolUserPersistenceResult> SetRoleAsync(
            Guid schoolId, Guid userId, string role,
            CancellationToken cancellationToken = default) => Failure();

        public Task<SchoolUserPersistenceResult> GeneratePasswordSetupAsync(
            Guid schoolId, Guid userId,
            CancellationToken cancellationToken = default) => Failure();

        public Task<SchoolUserPersistenceResult> CompletePasswordSetupAsync(
            Guid userId, string token, string newPassword,
            CancellationToken cancellationToken = default) => Failure();

        private static Task<SchoolUserPersistenceResult> Failure() =>
            Task.FromResult(
                SchoolUserPersistenceResult.Failure(
                    SchoolUserPersistenceError.IdentityFailure));
    }
}
