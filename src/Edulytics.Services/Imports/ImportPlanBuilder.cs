using System.Globalization;
using System.Text.Json;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Imports;
using Edulytics.Core.Realtime;
using Edulytics.Core.Users;

namespace Edulytics.Services.Imports;

public sealed class ImportPlanBuilder
{
    public ImportApplyPlan Build(
        Guid schoolId,
        Guid actorUserId,
        Guid batchId,
        ImportType type,
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        IReadOnlyList<SchoolUserRecord> schoolUsers,
        DateTime now)
    {
        var plan =
            new ImportApplyPlan();

        var scopes =
            new HashSet<
                ImportDashboardScope>();

        switch (type)
        {
            case ImportType.Students:
                BuildStudents(
                    schoolId,
                    file,
                    snapshot,
                    now,
                    plan);
                break;

            case ImportType.Teachers:
                BuildTeachers(
                    schoolId,
                    file,
                    snapshot,
                    schoolUsers,
                    now,
                    plan,
                    scopes);
                break;

            case ImportType.Classes:
                BuildClasses(
                    schoolId,
                    file,
                    snapshot,
                    plan);
                break;

            case ImportType.Subjects:
                BuildSubjects(
                    schoolId,
                    file,
                    plan);
                break;

            case ImportType.AssessmentResults:
                BuildResults(
                    schoolId,
                    actorUserId,
                    file,
                    snapshot,
                    now,
                    plan,
                    scopes);
                break;

            case ImportType.CurriculumMappings:
                BuildMappings(
                    schoolId,
                    file,
                    snapshot,
                    plan,
                    scopes);
                break;
        }

        var eventId =
            Guid.NewGuid();

        var completed =
            new ImportBatchCompletedEvent(
                eventId,
                schoolId,
                batchId,
                type.ToString(),
                scopes.ToArray(),
                now);

        plan.OutboxMessages.Add(
            new OutboxMessage
            {
                Id = eventId,
                SchoolId = schoolId,
                EventType =
                    RealtimeEventTypes
                        .ImportBatchCompleted,
                PayloadJson =
                    JsonSerializer.Serialize(
                        completed),
                OccurredAtUtc = now,
                AvailableAtUtc = now,
                ProcessingAttempts = 0,
                CorrelationId =
                    $"import-batch:{batchId:N}"
            });

        return plan;
    }

    private static void BuildSubjects(
        Guid schoolId,
        ParsedImportFile file,
        ImportApplyPlan plan)
    {
        foreach (var row in file.Rows)
        {
            var code =
                Code(row, "Code");

            plan.Subjects.Add(
                new Subject
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    Code = code,
                    NormalizedCode = code,
                    Name =
                        Text(row, "Name"),
                    Status =
                        AcademicStructureStatus
                            .Active
                });
        }
    }

    private static void BuildClasses(
        Guid schoolId,
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        ImportApplyPlan plan)
    {
        foreach (var row in file.Rows)
        {
            var year =
                snapshot.AcademicYears.Single(x =>
                    x.Status ==
                        AcademicStructureStatus.Active &&
                    string.Equals(
                        x.Name,
                        Text(
                            row,
                            "AcademicYear"),
                        StringComparison
                            .OrdinalIgnoreCase));

            var grade =
                snapshot.GradeLevels.Single(x =>
                    string.Equals(
                        x.Name,
                        Text(
                            row,
                            "GradeLevel"),
                        StringComparison
                            .OrdinalIgnoreCase));

            plan.AcademicYearGuards.Add(
                new ImportEntityGuard(
                    year.Id,
                    year.RowVersion));

            var code =
                Code(row, "Code");

            plan.Classes.Add(
                new ClassGroup
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    AcademicYearId =
                        year.Id,
                    GradeLevelId =
                        grade.Id,
                    Name =
                        Text(
                            row,
                            "Name"),
                    Code = code,
                    NormalizedCode = code,
                    Status =
                        AcademicStructureStatus
                            .Active
                });
        }
    }

    private static void BuildStudents(
        Guid schoolId,
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        DateTime now,
        ImportApplyPlan plan)
    {
        foreach (var row in file.Rows)
        {
            var year =
                snapshot.AcademicYears.Single(x =>
                    x.Status ==
                        AcademicStructureStatus.Active &&
                    string.Equals(
                        x.Name,
                        Text(
                            row,
                            "AcademicYear"),
                        StringComparison
                            .OrdinalIgnoreCase));

            var classGroup =
                snapshot.ClassGroups.Single(x =>
                    x.AcademicYearId ==
                        year.Id &&
                    x.Status ==
                        AcademicStructureStatus.Active &&
                    x.NormalizedCode ==
                        Code(
                            row,
                            "ClassCode"));

            plan.AcademicYearGuards.Add(
                new ImportEntityGuard(
                    year.Id,
                    year.RowVersion));

            plan.ClassGroupGuards.Add(
                new ImportEntityGuard(
                    classGroup.Id,
                    classGroup.RowVersion));

            var studentId =
                Guid.NewGuid();

            var first =
                Text(
                    row,
                    "FirstName");

            var last =
                Text(
                    row,
                    "LastName");

            var number =
                Code(
                    row,
                    "StudentNumber");

            plan.Students.Add(
                new StudentProfile
                {
                    Id = studentId,
                    SchoolId = schoolId,
                    UserId = null,
                    StudentNumber = number,
                    NormalizedStudentNumber =
                        number,
                    FirstName = first,
                    LastName = last,
                    DisplayName =
                        $"{first} {last}"
                            .Trim(),
                    Status =
                        AcademicStructureStatus
                            .Active,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                });

            plan.Enrollments.Add(
                new StudentEnrollment
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    StudentProfileId =
                        studentId,
                    ClassGroupId =
                        classGroup.Id,
                    AcademicYearId =
                        year.Id,
                    EnrolledAtUtc =
                        now
                });
        }
    }

    private static void BuildTeachers(
        Guid schoolId,
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        IReadOnlyList<SchoolUserRecord> schoolUsers,
        DateTime now,
        ImportApplyPlan plan,
        HashSet<ImportDashboardScope> scopes)
    {
        foreach (var row in file.Rows)
        {
            var teacher =
                schoolUsers.Single(x =>
                    x.Roles.Count == 1 &&
                    x.Roles[0] ==
                        RoleNames.Teacher &&
                    x.IsActive &&
                    !x.IsLocked &&
                    string.Equals(
                        x.Email,
                        Text(
                            row,
                            "Email"),
                        StringComparison
                            .OrdinalIgnoreCase));

            var year =
                snapshot.AcademicYears.Single(x =>
                    x.Status ==
                        AcademicStructureStatus.Active &&
                    string.Equals(
                        x.Name,
                        Text(
                            row,
                            "AcademicYear"),
                        StringComparison
                            .OrdinalIgnoreCase));

            var classGroup =
                snapshot.ClassGroups.Single(x =>
                    x.AcademicYearId ==
                        year.Id &&
                    x.Status ==
                        AcademicStructureStatus.Active &&
                    x.NormalizedCode ==
                        Code(
                            row,
                            "ClassCode"));

            var subject =
                snapshot.Subjects.Single(x =>
                    x.Status ==
                        AcademicStructureStatus.Active &&
                    x.NormalizedCode ==
                        Code(
                            row,
                            "SubjectCode"));

            plan.AcademicYearGuards.Add(
                new ImportEntityGuard(
                    year.Id,
                    year.RowVersion));

            plan.ClassGroupGuards.Add(
                new ImportEntityGuard(
                    classGroup.Id,
                    classGroup.RowVersion));

            plan.SubjectGuards.Add(
                new ImportEntityGuard(
                    subject.Id,
                    subject.RowVersion));

            plan.TeacherAssignments.Add(
                new TeacherAssignment
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    TeacherUserId =
                        teacher.Id,
                    ClassGroupId =
                        classGroup.Id,
                    SubjectId =
                        subject.Id,
                    AcademicYearId =
                        year.Id,
                    CreatedAtUtc =
                        now
                });

            scopes.Add(
                new ImportDashboardScope(
                    classGroup.Id,
                    subject.Id));
        }
    }

    private static void BuildResults(
        Guid schoolId,
        Guid actorUserId,
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        DateTime now,
        ImportApplyPlan plan,
        HashSet<ImportDashboardScope> scopes)
    {
        var rows =
            file.Rows.Select(row =>
            {
                var assessment =
                    snapshot.Assessments
                        .Single(x =>
                            x.Id ==
                                Guid.Parse(
                                    Text(
                                        row,
                                        "AssessmentId")));

                var student =
                    snapshot.StudentProfiles
                        .Single(x =>
                            x.Status ==
                                AcademicStructureStatus.Active &&
                            x.NormalizedStudentNumber ==
                                Code(
                                    row,
                                    "StudentNumber"));

                var order =
                    int.Parse(
                        Text(
                            row,
                            "QuestionOrder"),
                        CultureInfo.InvariantCulture);

                var question =
                    snapshot.AssessmentQuestions
                        .Single(x =>
                            x.AssessmentId ==
                                assessment.Id &&
                            x.Order ==
                                order);

                var score =
                    decimal.Parse(
                        Text(
                            row,
                            "Score"),
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture);

                return new
                {
                    Assessment =
                        assessment,
                    Student =
                        student,
                    Question =
                        question,
                    Score =
                        decimal.Round(
                            score,
                            2,
                            MidpointRounding
                                .AwayFromZero)
                };
            })
            .ToArray();

        foreach (var group in
                 rows.GroupBy(x =>
                     new
                     {
                         AssessmentId =
                             x.Assessment.Id,
                         StudentId =
                             x.Student.Id
                     }))
        {
            var first =
                group.First();

            var assessment =
                first.Assessment;

            var student =
                first.Student;

            var resultId =
                Guid.NewGuid();

            var total =
                decimal.Round(
                    group.Sum(x =>
                        x.Score),
                    2,
                    MidpointRounding
                        .AwayFromZero);

            var percentage =
                decimal.Round(
                    total /
                    assessment.MaxScore *
                    100m,
                    2,
                    MidpointRounding
                        .AwayFromZero);

            plan.AssessmentResults.Add(
                new AssessmentResult
                {
                    Id = resultId,
                    SchoolId = schoolId,
                    AssessmentId =
                        assessment.Id,
                    StudentProfileId =
                        student.Id,
                    Score = total,
                    Percentage =
                        percentage,
                    EnteredByUserId =
                        actorUserId,
                    EnteredAtUtc = now,
                    UpdatedAtUtc = now
                });

            foreach (var item in group)
            {
                plan.StudentAnswers.Add(
                    new StudentAnswer
                    {
                        Id =
                            Guid.NewGuid(),
                        SchoolId =
                            schoolId,
                        AssessmentResultId =
                            resultId,
                        AssessmentQuestionId =
                            item.Question.Id,
                        Score =
                            item.Score,
                        UpdatedAtUtc =
                            now
                    });
            }

            plan.AssessmentGuards.Add(
                new ImportAssessmentGuard(
                    assessment.Id,
                    assessment.RowVersion,
                    AssessmentStatus.Open));

            scopes.Add(
                new ImportDashboardScope(
                    assessment.ClassGroupId,
                    assessment.SubjectId));
        }
    }

    private static void BuildMappings(
        Guid schoolId,
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        ImportApplyPlan plan,
        HashSet<ImportDashboardScope> scopes)
    {
        foreach (var row in file.Rows)
        {
            var assessment =
                snapshot.Assessments
                    .Single(x =>
                        x.Id ==
                            Guid.Parse(
                                Text(
                                    row,
                                    "AssessmentId")));

            var questionOrder =
                int.Parse(
                    Text(
                        row,
                        "QuestionOrder"),
                    CultureInfo.InvariantCulture);

            var question =
                snapshot.AssessmentQuestions
                    .Single(x =>
                        x.AssessmentId ==
                            assessment.Id &&
                        x.Order ==
                            questionOrder);

            var classGroup =
                snapshot.ClassGroups
                    .Single(x =>
                        x.Id ==
                            assessment.ClassGroupId);

            var versions =
                ImportValidationEngine
                    .ResolveEligibleFrameworkVersionIds(
                        snapshot,
                        assessment.AcademicYearId,
                        classGroup.GradeLevelId,
                        assessment.SubjectId);

            var outcome =
                snapshot.LearningOutcomes
                    .Single(x =>
                        x.SubjectId ==
                            assessment.SubjectId &&
                        x.GradeLevelId ==
                            classGroup.GradeLevelId &&
                        versions.Contains(
                            x.FrameworkVersionId) &&
                        string.Equals(
                            x.Code,
                            Code(
                                row,
                                "OutcomeCode"),
                            StringComparison
                                .OrdinalIgnoreCase));

            plan.CurriculumMappings.Add(
                new QuestionLearningOutcome
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    AssessmentQuestionId =
                        question.Id,
                    LearningOutcomeId =
                        outcome.Id
                });

            plan.AssessmentGuards.Add(
                new ImportAssessmentGuard(
                    assessment.Id,
                    assessment.RowVersion,
                    AssessmentStatus.Draft));

            plan.AssessmentsToTouch.Add(
                assessment.Id);

            scopes.Add(
                new ImportDashboardScope(
                    assessment.ClassGroupId,
                    assessment.SubjectId));
        }
    }

    private static string Text(
        ImportFileRow row,
        string column) =>
        ImportValidationEngine
            .Value(
                row,
                column)
            .Trim();

    private static string Code(
        ImportFileRow row,
        string column) =>
        ImportValidationEngine
            .NormalizeCode(
                Text(
                    row,
                    column));
}
