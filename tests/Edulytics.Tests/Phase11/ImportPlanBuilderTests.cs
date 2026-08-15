using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Imports;
using Edulytics.Core.Realtime;
using Edulytics.Core.Users;
using Edulytics.Services.Imports;

namespace Edulytics.Tests.Phase11;

public sealed class ImportPlanBuilderTests
{
    [Fact]
    public void Builder_ProducesAllSixImportPlans()
    {
        var fixture = CreateFixture();

        var builder =
            new ImportPlanBuilder();

        var now =
            DateTime.UtcNow;

        var subjects =
            builder.Build(
                fixture.SchoolId,
                fixture.AdminId,
                Guid.NewGuid(),
                ImportType.Subjects,
                File(
                    ["Code", "Name"],
                    ("Code", "SCI"),
                    ("Name", "Science")),
                fixture.Snapshot,
                fixture.Users,
                now);

        Assert.Single(
            subjects.Subjects);

        AssertCompletionEvent(
            subjects);

        var classes =
            builder.Build(
                fixture.SchoolId,
                fixture.AdminId,
                Guid.NewGuid(),
                ImportType.Classes,
                File(
                    [
                        "AcademicYear",
                        "GradeLevel",
                        "Code",
                        "Name"
                    ],
                    ("AcademicYear", fixture.Year.Name),
                    ("GradeLevel", fixture.Grade.Name),
                    ("Code", "6B"),
                    ("Name", "Class 6B")),
                fixture.Snapshot,
                fixture.Users,
                now);

        Assert.Single(
            classes.Classes);

        Assert.Single(
            classes.AcademicYearGuards);

        AssertCompletionEvent(
            classes);

        var students =
            builder.Build(
                fixture.SchoolId,
                fixture.AdminId,
                Guid.NewGuid(),
                ImportType.Students,
                File(
                    [
                        "StudentNumber",
                        "FirstName",
                        "LastName",
                        "AcademicYear",
                        "ClassCode"
                    ],
                    ("StudentNumber", "S002"),
                    ("FirstName", "Jan"),
                    ("LastName", "Kowalski"),
                    ("AcademicYear", fixture.Year.Name),
                    ("ClassCode", fixture.Class.Code)),
                fixture.Snapshot,
                fixture.Users,
                now);

        Assert.Single(
            students.Students);

        Assert.Single(
            students.Enrollments);

        AssertCompletionEvent(
            students);

        var teachers =
            builder.Build(
                fixture.SchoolId,
                fixture.AdminId,
                Guid.NewGuid(),
                ImportType.Teachers,
                File(
                    [
                        "Email",
                        "AcademicYear",
                        "ClassCode",
                        "SubjectCode"
                    ],
                    ("Email", fixture.Teacher.Email),
                    ("AcademicYear", fixture.Year.Name),
                    ("ClassCode", fixture.Class.Code),
                    ("SubjectCode", fixture.Subject.Code)),
                new ImportDataSnapshot
                {
                    AcademicYears = fixture.Snapshot.AcademicYears,
                    GradeLevels = fixture.Snapshot.GradeLevels,
                    ClassGroups = fixture.Snapshot.ClassGroups,
                    Subjects = fixture.Snapshot.Subjects,
                    StudentProfiles = fixture.Snapshot.StudentProfiles,
                    StudentEnrollments = fixture.Snapshot.StudentEnrollments,
                    TeacherAssignments = [],
                    LearningOutcomes = fixture.Snapshot.LearningOutcomes,
                    CurriculumAdoptions = fixture.Snapshot.CurriculumAdoptions,
                    FrameworkVersions = fixture.Snapshot.FrameworkVersions,
                    Assessments = fixture.Snapshot.Assessments,
                    AssessmentQuestions = fixture.Snapshot.AssessmentQuestions,
                    OutcomeMappings = fixture.Snapshot.OutcomeMappings,
                    AssessmentResults = fixture.Snapshot.AssessmentResults
                },
                fixture.Users,
                now);

        Assert.Single(
            teachers.TeacherAssignments);

        AssertCompletionEvent(
            teachers);

        var results =
            builder.Build(
                fixture.SchoolId,
                fixture.Teacher.Id,
                Guid.NewGuid(),
                ImportType.AssessmentResults,
                File(
                    [
                        "AssessmentId",
                        "StudentNumber",
                        "QuestionOrder",
                        "Score"
                    ],
                    ("AssessmentId", fixture.OpenAssessment.Id.ToString()),
                    ("StudentNumber", fixture.Student.StudentNumber),
                    ("QuestionOrder", "1"),
                    ("Score", "8")),
                fixture.Snapshot,
                fixture.Users,
                now);

        Assert.Single(
            results.AssessmentResults);

        Assert.Single(
            results.StudentAnswers);

        Assert.Equal(
            80m,
            results.AssessmentResults[0]
                .Percentage);

        Assert.Single(
            results.AssessmentGuards);

        AssertCompletionEvent(
            results);

        var mappings =
            builder.Build(
                fixture.SchoolId,
                fixture.AdminId,
                Guid.NewGuid(),
                ImportType.CurriculumMappings,
                File(
                    [
                        "AssessmentId",
                        "QuestionOrder",
                        "OutcomeCode"
                    ],
                    ("AssessmentId", fixture.DraftAssessment.Id.ToString()),
                    ("QuestionOrder", "1"),
                    ("OutcomeCode", fixture.Outcome.Code)),
                fixture.Snapshot,
                fixture.Users,
                now);

        Assert.Single(
            mappings.CurriculumMappings);

        Assert.Single(
            mappings.AssessmentGuards);

        Assert.Single(
            mappings.AssessmentsToTouch);

        AssertCompletionEvent(
            mappings);
    }

    private static void AssertCompletionEvent(
        ImportApplyPlan plan)
    {
        var message =
            Assert.Single(
                plan.OutboxMessages);

        Assert.Equal(
            RealtimeEventTypes
                .ImportBatchCompleted,
            message.EventType);

        Assert.StartsWith(
            "import-batch:",
            message.CorrelationId);
    }

    private static ParsedImportFile File(
        string[] headers,
        params (string Key, string Value)[] values) =>
        new(
            headers,
            [
                new ImportFileRow(
                    2,
                    values.ToDictionary(
                        x => x.Key,
                        x => x.Value,
                        StringComparer.OrdinalIgnoreCase))
            ]);

    private static Fixture CreateFixture()
    {
        var schoolId =
            Guid.NewGuid();

        var adminId =
            Guid.NewGuid();

        var teacherId =
            Guid.NewGuid();

        var year =
            new AcademicYear
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                Name = "2026/2027",
                Status =
                    AcademicStructureStatus.Active,
                RowVersion = [1]
            };

        var grade =
            new GradeLevel
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                Name = "Grade 6",
                Order = 6
            };

        var classGroup =
            new ClassGroup
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                AcademicYearId = year.Id,
                GradeLevelId = grade.Id,
                Name = "Class 6A",
                Code = "6A",
                NormalizedCode = "6A",
                Status =
                    AcademicStructureStatus.Active,
                RowVersion = [2]
            };

        var subject =
            new Subject
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                Name = "Mathematics",
                Code = "MATH",
                NormalizedCode = "MATH",
                Status =
                    AcademicStructureStatus.Active,
                RowVersion = [3]
            };

        var student =
            new StudentProfile
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                StudentNumber = "S001",
                NormalizedStudentNumber =
                    "S001",
                FirstName = "Maja",
                LastName = "Nowak",
                DisplayName = "Maja Nowak",
                Status =
                    AcademicStructureStatus.Active
            };

        var teacher =
            new SchoolUserRecord(
                teacherId,
                schoolId,
                "teacher@example.test",
                true,
                false,
                DateTime.UtcNow,
                DateTime.UtcNow,
                [RoleNames.Teacher]);

        var version =
            new CurriculumFrameworkVersion
            {
                Id = Guid.NewGuid(),
                FrameworkId = Guid.NewGuid(),
                VersionCode = "2026",
                NormalizedVersionCode = "2026",
                Name = "2026",
                IsActive = true
            };

        var outcome =
            new LearningOutcome
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                FrameworkVersionId =
                    version.Id,
                SubjectId = subject.Id,
                GradeLevelId = grade.Id,
                TopicId = Guid.NewGuid(),
                Code = "N.1",
                Description = "Outcome",
                Weight = 1m,
                Order = 1
            };

        var openAssessment =
            new Assessment
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                SubjectId = subject.Id,
                ClassGroupId =
                    classGroup.Id,
                AcademicYearId = year.Id,
                MaxScore = 10m,
                Status =
                    AssessmentStatus.Open,
                RowVersion = [4]
            };

        var openQuestion =
            new AssessmentQuestion
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                AssessmentId =
                    openAssessment.Id,
                MaxScore = 10m,
                Order = 1
            };

        var draftAssessment =
            new Assessment
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                SubjectId = subject.Id,
                ClassGroupId =
                    classGroup.Id,
                AcademicYearId = year.Id,
                MaxScore = 10m,
                Status =
                    AssessmentStatus.Draft,
                RowVersion = [5]
            };

        var draftQuestion =
            new AssessmentQuestion
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                AssessmentId =
                    draftAssessment.Id,
                MaxScore = 10m,
                Order = 1
            };

        var snapshot =
            new ImportDataSnapshot
            {
                AcademicYears =
                    [year],

                GradeLevels =
                    [grade],

                ClassGroups =
                    [classGroup],

                Subjects =
                    [subject],

                StudentProfiles =
                    [student],

                StudentEnrollments =
                    [
                        new StudentEnrollment
                        {
                            Id = Guid.NewGuid(),
                            SchoolId =
                                schoolId,
                            StudentProfileId =
                                student.Id,
                            ClassGroupId =
                                classGroup.Id,
                            AcademicYearId =
                                year.Id
                        }
                    ],

                TeacherAssignments =
                    [
                        new TeacherAssignment
                        {
                            Id = Guid.NewGuid(),
                            SchoolId =
                                schoolId,
                            TeacherUserId =
                                teacherId,
                            ClassGroupId =
                                classGroup.Id,
                            SubjectId =
                                subject.Id,
                            AcademicYearId =
                                year.Id
                        }
                    ],

                FrameworkVersions =
                    [version],

                CurriculumAdoptions =
                    [
                        new SchoolCurriculumAdoption
                        {
                            Id = Guid.NewGuid(),
                            SchoolId =
                                schoolId,
                            AcademicYearId =
                                year.Id,
                            GradeLevelId =
                                grade.Id,
                            SubjectId =
                                subject.Id,
                            FrameworkVersionId =
                                version.Id,
                            IsActive = true
                        }
                    ],

                LearningOutcomes =
                    [outcome],

                Assessments =
                    [
                        openAssessment,
                        draftAssessment
                    ],

                AssessmentQuestions =
                    [
                        openQuestion,
                        draftQuestion
                    ]
            };

        return new Fixture(
            schoolId,
            adminId,
            teacher,
            year,
            grade,
            classGroup,
            subject,
            student,
            outcome,
            openAssessment,
            draftAssessment,
            snapshot,
            [teacher]);
    }

    private sealed record Fixture(
        Guid SchoolId,
        Guid AdminId,
        SchoolUserRecord Teacher,
        AcademicYear Year,
        GradeLevel Grade,
        ClassGroup Class,
        Subject Subject,
        StudentProfile Student,
        LearningOutcome Outcome,
        Assessment OpenAssessment,
        Assessment DraftAssessment,
        ImportDataSnapshot Snapshot,
        IReadOnlyList<SchoolUserRecord> Users);
}
