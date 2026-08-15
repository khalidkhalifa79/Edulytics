using System.Globalization;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Imports;
using Edulytics.Core.Users;

namespace Edulytics.Services.Imports;

public sealed record ImportValidationIssue(
    int RowNumber,
    string ColumnName,
    string Code,
    string? RawValue);

public sealed class ImportValidationEngine
{
    private static readonly Regex CodePattern =
        new(
            "^[A-Z0-9._-]+$",
            RegexOptions.Compiled |
            RegexOptions.CultureInvariant);

    private static readonly IReadOnlyDictionary<
        ImportType,
        string[]> Schemas =
        new Dictionary<
            ImportType,
            string[]>
        {
            [ImportType.Students] =
                [
                    "StudentNumber",
                    "FirstName",
                    "LastName",
                    "AcademicYear",
                    "ClassCode"
                ],

            [ImportType.Teachers] =
                [
                    "Email",
                    "AcademicYear",
                    "ClassCode",
                    "SubjectCode"
                ],

            [ImportType.Classes] =
                [
                    "AcademicYear",
                    "GradeLevel",
                    "Code",
                    "Name"
                ],

            [ImportType.Subjects] =
                [
                    "Code",
                    "Name"
                ],

            [ImportType.AssessmentResults] =
                [
                    "AssessmentId",
                    "StudentNumber",
                    "QuestionOrder",
                    "Score"
                ],

            [ImportType.CurriculumMappings] =
                [
                    "AssessmentId",
                    "QuestionOrder",
                    "OutcomeCode"
                ]
        };

    public IReadOnlyList<string> RequiredHeaders(
        ImportType type) =>
        Schemas[type];

    public IReadOnlyList<ImportValidationIssue> Validate(
        ImportType type,
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        IReadOnlyList<SchoolUserRecord> schoolUsers,
        Guid actorUserId,
        string actorRole)
    {
        var errors =
            ValidateHeaders(
                type,
                file.Headers)
            .ToList();

        if (errors.Count > 0)
            return errors;

        switch (type)
        {
            case ImportType.Students:
                ValidateStudents(
                    file,
                    snapshot,
                    errors);
                break;

            case ImportType.Teachers:
                ValidateTeachers(
                    file,
                    snapshot,
                    schoolUsers,
                    errors);
                break;

            case ImportType.Classes:
                ValidateClasses(
                    file,
                    snapshot,
                    errors);
                break;

            case ImportType.Subjects:
                ValidateSubjects(
                    file,
                    snapshot,
                    errors);
                break;

            case ImportType.AssessmentResults:
                ValidateAssessmentResults(
                    file,
                    snapshot,
                    actorUserId,
                    actorRole,
                    errors);
                break;

            case ImportType.CurriculumMappings:
                ValidateMappings(
                    file,
                    snapshot,
                    errors);
                break;
        }

        return errors;
    }

    private IEnumerable<ImportValidationIssue>
        ValidateHeaders(
            ImportType type,
            IReadOnlyList<string> headers)
    {
        var actual =
            headers.ToHashSet(
                StringComparer
                    .OrdinalIgnoreCase);

        foreach (var required in
                 Schemas[type])
        {
            if (!actual.Contains(required))
            {
                yield return new(
                    1,
                    required,
                    "MissingColumn",
                    null);
            }
        }
    }

    private static void ValidateSubjects(
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        List<ImportValidationIssue> errors)
    {
        var seen =
            new HashSet<string>(
                StringComparer.Ordinal);

        var existing =
            snapshot.Subjects
                .Select(x =>
                    x.NormalizedCode)
                .ToHashSet(
                    StringComparer.Ordinal);

        foreach (var row in file.Rows)
        {
            var code =
                NormalizeCode(
                    Value(
                        row,
                        "Code"));

            var name =
                Value(
                    row,
                    "Name").Trim();

            Require(
                row,
                "Code",
                code,
                errors);

            Require(
                row,
                "Name",
                name,
                errors);

            ValidateCode(
                row,
                "Code",
                code,
                errors);

            if (name.Length > 150)
            {
                Add(
                    row,
                    "Name",
                    "InvalidType",
                    name,
                    errors);
            }

            if (code.Length > 0 &&
                !seen.Add(code))
            {
                Add(
                    row,
                    "Code",
                    "DuplicateRow",
                    code,
                    errors);
            }

            if (code.Length > 0 &&
                existing.Contains(code))
            {
                Add(
                    row,
                    "Code",
                    "ExistingConflict",
                    code,
                    errors);
            }
        }
    }

    private static void ValidateClasses(
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        List<ImportValidationIssue> errors)
    {
        var seen =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var row in file.Rows)
        {
            var yearName =
                Value(
                    row,
                    "AcademicYear")
                .Trim();

            var gradeName =
                Value(
                    row,
                    "GradeLevel")
                .Trim();

            var code =
                NormalizeCode(
                    Value(
                        row,
                        "Code"));

            var name =
                Value(
                    row,
                    "Name")
                .Trim();

            Require(
                row,
                "AcademicYear",
                yearName,
                errors);

            Require(
                row,
                "GradeLevel",
                gradeName,
                errors);

            Require(
                row,
                "Code",
                code,
                errors);

            Require(
                row,
                "Name",
                name,
                errors);

            ValidateCode(
                row,
                "Code",
                code,
                errors);

            if (name.Length > 150)
            {
                Add(
                    row,
                    "Name",
                    "InvalidType",
                    name,
                    errors);
            }

            var year =
                snapshot.AcademicYears
                    .FirstOrDefault(x =>
                        x.Status ==
                            AcademicStructureStatus.Active &&
                        string.Equals(
                            x.Name,
                            yearName,
                            StringComparison
                                .OrdinalIgnoreCase));

            if (yearName.Length > 0 &&
                year is null)
            {
                Add(
                    row,
                    "AcademicYear",
                    "UnknownReference",
                    yearName,
                    errors);
            }

            var grade =
                snapshot.GradeLevels
                    .FirstOrDefault(x =>
                        string.Equals(
                            x.Name,
                            gradeName,
                            StringComparison
                                .OrdinalIgnoreCase));

            if (gradeName.Length > 0 &&
                grade is null)
            {
                Add(
                    row,
                    "GradeLevel",
                    "UnknownReference",
                    gradeName,
                    errors);
            }

            if (year is not null &&
                code.Length > 0)
            {
                var logical =
                    $"{year.Id:N}:{code}";

                if (!seen.Add(logical))
                {
                    Add(
                        row,
                        "Code",
                        "DuplicateRow",
                        code,
                        errors);
                }

                if (snapshot.ClassGroups
                    .Any(x =>
                        x.AcademicYearId ==
                            year.Id &&
                        x.NormalizedCode ==
                            code))
                {
                    Add(
                        row,
                        "Code",
                        "ExistingConflict",
                        code,
                        errors);
                }
            }
        }
    }

    private static void ValidateStudents(
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        List<ImportValidationIssue> errors)
    {
        var seen =
            new HashSet<string>(
                StringComparer.Ordinal);

        var existing =
            snapshot.StudentProfiles
                .Select(x =>
                    x.NormalizedStudentNumber)
                .ToHashSet(
                    StringComparer.Ordinal);

        foreach (var row in file.Rows)
        {
            var studentNumber =
                NormalizeCode(
                    Value(
                        row,
                        "StudentNumber"));

            var firstName =
                Value(
                    row,
                    "FirstName")
                .Trim();

            var lastName =
                Value(
                    row,
                    "LastName")
                .Trim();

            var yearName =
                Value(
                    row,
                    "AcademicYear")
                .Trim();

            var classCode =
                NormalizeCode(
                    Value(
                        row,
                        "ClassCode"));

            foreach (var required in
                     new[]
                     {
                         ("StudentNumber", studentNumber),
                         ("FirstName", firstName),
                         ("LastName", lastName),
                         ("AcademicYear", yearName),
                         ("ClassCode", classCode)
                     })
            {
                Require(
                    row,
                    required.Item1,
                    required.Item2,
                    errors);
            }

            ValidateCode(
                row,
                "StudentNumber",
                studentNumber,
                errors);

            if (firstName.Length > 100)
            {
                Add(
                    row,
                    "FirstName",
                    "InvalidType",
                    firstName,
                    errors);
            }

            if (lastName.Length > 100)
            {
                Add(
                    row,
                    "LastName",
                    "InvalidType",
                    lastName,
                    errors);
            }

            if (studentNumber.Length > 0 &&
                !seen.Add(studentNumber))
            {
                Add(
                    row,
                    "StudentNumber",
                    "DuplicateRow",
                    studentNumber,
                    errors);
            }

            if (studentNumber.Length > 0 &&
                existing.Contains(
                    studentNumber))
            {
                Add(
                    row,
                    "StudentNumber",
                    "ExistingConflict",
                    studentNumber,
                    errors);
            }

            var year =
                snapshot.AcademicYears
                    .FirstOrDefault(x =>
                        x.Status ==
                            AcademicStructureStatus.Active &&
                        string.Equals(
                            x.Name,
                            yearName,
                            StringComparison
                                .OrdinalIgnoreCase));

            if (yearName.Length > 0 &&
                year is null)
            {
                Add(
                    row,
                    "AcademicYear",
                    "UnknownReference",
                    yearName,
                    errors);
            }

            if (year is not null &&
                classCode.Length > 0)
            {
                var classGroup =
                    snapshot.ClassGroups
                        .FirstOrDefault(x =>
                            x.AcademicYearId ==
                                year.Id &&
                            x.NormalizedCode ==
                                classCode &&
                            x.Status ==
                                AcademicStructureStatus
                                    .Active);

                if (classGroup is null)
                {
                    Add(
                        row,
                        "ClassCode",
                        "UnknownReference",
                        classCode,
                        errors);
                }
            }
        }
    }

    private static void ValidateTeachers(
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        IReadOnlyList<SchoolUserRecord> users,
        List<ImportValidationIssue> errors)
    {
        var seen =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var row in file.Rows)
        {
            var email =
                Value(
                    row,
                    "Email")
                .Trim();

            var yearName =
                Value(
                    row,
                    "AcademicYear")
                .Trim();

            var classCode =
                NormalizeCode(
                    Value(
                        row,
                        "ClassCode"));

            var subjectCode =
                NormalizeCode(
                    Value(
                        row,
                        "SubjectCode"));

            foreach (var required in
                     new[]
                     {
                         ("Email", email),
                         ("AcademicYear", yearName),
                         ("ClassCode", classCode),
                         ("SubjectCode", subjectCode)
                     })
            {
                Require(
                    row,
                    required.Item1,
                    required.Item2,
                    errors);
            }

            if (email.Length > 0 &&
                !ValidEmail(email))
            {
                Add(
                    row,
                    "Email",
                    "InvalidType",
                    email,
                    errors);
            }

            var teacher =
                users.FirstOrDefault(x =>
                    x.IsActive &&
                    !x.IsLocked &&
                    x.Roles.Count == 1 &&
                    x.Roles[0] ==
                        RoleNames.Teacher &&
                    string.Equals(
                        x.Email,
                        email,
                        StringComparison
                            .OrdinalIgnoreCase));

            if (email.Length > 0 &&
                teacher is null)
            {
                Add(
                    row,
                    "Email",
                    "UnknownReference",
                    email,
                    errors);
            }

            var year =
                snapshot.AcademicYears
                    .FirstOrDefault(x =>
                        x.Status ==
                            AcademicStructureStatus.Active &&
                        string.Equals(
                            x.Name,
                            yearName,
                            StringComparison
                                .OrdinalIgnoreCase));

            if (yearName.Length > 0 &&
                year is null)
            {
                Add(
                    row,
                    "AcademicYear",
                    "UnknownReference",
                    yearName,
                    errors);
            }

            ClassGroup? classGroup =
                null;

            if (year is not null &&
                classCode.Length > 0)
            {
                classGroup =
                    snapshot.ClassGroups
                        .FirstOrDefault(x =>
                            x.AcademicYearId ==
                                year.Id &&
                            x.NormalizedCode ==
                                classCode &&
                            x.Status ==
                                AcademicStructureStatus
                                    .Active);

                if (classGroup is null)
                {
                    Add(
                        row,
                        "ClassCode",
                        "UnknownReference",
                        classCode,
                        errors);
                }
            }

            var subject =
                snapshot.Subjects
                    .FirstOrDefault(x =>
                        x.NormalizedCode ==
                            subjectCode &&
                        x.Status ==
                            AcademicStructureStatus
                                .Active);

            if (subjectCode.Length > 0 &&
                subject is null)
            {
                Add(
                    row,
                    "SubjectCode",
                    "UnknownReference",
                    subjectCode,
                    errors);
            }

            if (teacher is not null &&
                year is not null &&
                classGroup is not null &&
                subject is not null)
            {
                var logical =
                    $"{teacher.Id:N}:"
                    + $"{classGroup.Id:N}:"
                    + $"{subject.Id:N}";

                if (!seen.Add(logical))
                {
                    Add(
                        row,
                        "Email",
                        "DuplicateRow",
                        email,
                        errors);
                }

                if (snapshot
                    .TeacherAssignments
                    .Any(x =>
                        x.TeacherUserId ==
                            teacher.Id &&
                        x.ClassGroupId ==
                            classGroup.Id &&
                        x.SubjectId ==
                            subject.Id))
                {
                    Add(
                        row,
                        "Email",
                        "ExistingConflict",
                        email,
                        errors);
                }
            }
        }
    }

    private static void ValidateAssessmentResults(
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        Guid actorUserId,
        string actorRole,
        List<ImportValidationIssue> errors)
    {
        var validRows =
            new List<ValidatedResultRow>();

        var seen =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var row in file.Rows)
        {
            var assessmentValue =
                Value(
                    row,
                    "AssessmentId")
                .Trim();

            var studentNumber =
                NormalizeCode(
                    Value(
                        row,
                        "StudentNumber"));

            var orderValue =
                Value(
                    row,
                    "QuestionOrder")
                .Trim();

            var scoreValue =
                Value(
                    row,
                    "Score")
                .Trim();

            foreach (var required in
                     new[]
                     {
                         ("AssessmentId", assessmentValue),
                         ("StudentNumber", studentNumber),
                         ("QuestionOrder", orderValue),
                         ("Score", scoreValue)
                     })
            {
                Require(
                    row,
                    required.Item1,
                    required.Item2,
                    errors);
            }

            if (!Guid.TryParse(
                    assessmentValue,
                    out var assessmentId))
            {
                if (assessmentValue.Length > 0)
                {
                    Add(
                        row,
                        "AssessmentId",
                        "InvalidType",
                        assessmentValue,
                        errors);
                }

                continue;
            }

            var assessment =
                snapshot.Assessments
                    .FirstOrDefault(x =>
                        x.Id ==
                            assessmentId);

            if (assessment is null)
            {
                Add(
                    row,
                    "AssessmentId",
                    "UnknownReference",
                    assessmentValue,
                    errors);

                continue;
            }

            if (assessment.Status !=
                AssessmentStatus.Open)
            {
                Add(
                    row,
                    "AssessmentId",
                    "ExistingConflict",
                    assessmentValue,
                    errors);
            }

            if (actorRole ==
                    RoleNames.Teacher &&
                !snapshot
                    .TeacherAssignments
                    .Any(x =>
                        x.TeacherUserId ==
                            actorUserId &&
                        x.ClassGroupId ==
                            assessment.ClassGroupId &&
                        x.SubjectId ==
                            assessment.SubjectId &&
                        x.AcademicYearId ==
                            assessment.AcademicYearId))
            {
                Add(
                    row,
                    "AssessmentId",
                    "AccessDenied",
                    assessmentValue,
                    errors);
            }

            var student =
                snapshot.StudentProfiles
                    .FirstOrDefault(x =>
                        x.NormalizedStudentNumber ==
                            studentNumber &&
                        x.Status ==
                            AcademicStructureStatus
                                .Active);

            if (student is null)
            {
                Add(
                    row,
                    "StudentNumber",
                    "UnknownReference",
                    studentNumber,
                    errors);

                continue;
            }

            if (!snapshot.StudentEnrollments
                .Any(x =>
                    x.StudentProfileId ==
                        student.Id &&
                    x.AcademicYearId ==
                        assessment.AcademicYearId &&
                    x.ClassGroupId ==
                        assessment.ClassGroupId))
            {
                Add(
                    row,
                    "StudentNumber",
                    "CrossSchoolReference",
                    studentNumber,
                    errors);
            }

            if (snapshot.AssessmentResults
                .Any(x =>
                    x.AssessmentId ==
                        assessment.Id &&
                    x.StudentProfileId ==
                        student.Id))
            {
                Add(
                    row,
                    "StudentNumber",
                    "ExistingConflict",
                    studentNumber,
                    errors);
            }

            if (!int.TryParse(
                    orderValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var order) ||
                order <= 0)
            {
                Add(
                    row,
                    "QuestionOrder",
                    "InvalidType",
                    orderValue,
                    errors);

                continue;
            }

            var question =
                snapshot.AssessmentQuestions
                    .FirstOrDefault(x =>
                        x.AssessmentId ==
                            assessment.Id &&
                        x.Order == order);

            if (question is null)
            {
                Add(
                    row,
                    "QuestionOrder",
                    "UnknownReference",
                    orderValue,
                    errors);

                continue;
            }

            if (!decimal.TryParse(
                    scoreValue,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out var score))
            {
                Add(
                    row,
                    "Score",
                    "InvalidType",
                    scoreValue,
                    errors);

                continue;
            }

            if (score < 0m ||
                score > question.MaxScore)
            {
                Add(
                    row,
                    "Score",
                    "InvalidScore",
                    scoreValue,
                    errors);
            }

            var logical =
                $"{assessment.Id:N}:"
                + $"{student.Id:N}:"
                + $"{question.Id:N}";

            if (!seen.Add(logical))
            {
                Add(
                    row,
                    "QuestionOrder",
                    "DuplicateRow",
                    orderValue,
                    errors);
            }

            validRows.Add(
                new ValidatedResultRow(
                    row,
                    assessment,
                    student.Id,
                    question.Id));
        }

        foreach (var group in
                 validRows.GroupBy(x =>
                     new
                     {
                         AssessmentId =
                             x.Assessment.Id,
                         x.StudentId
                     }))
        {
            var expected =
                snapshot.AssessmentQuestions
                    .Where(x =>
                        x.AssessmentId ==
                            group.Key
                                .AssessmentId)
                    .Select(x => x.Id)
                    .ToHashSet();

            var actual =
                group
                    .Select(x =>
                        x.QuestionId)
                    .ToHashSet();

            if (!expected.SetEquals(actual) ||
                group.Count() !=
                    expected.Count)
            {
                Add(
                    group.First().Row,
                    "QuestionOrder",
                    "IncompleteResult",
                    null,
                    errors);
            }
        }
    }

    private static void ValidateMappings(
        ParsedImportFile file,
        ImportDataSnapshot snapshot,
        List<ImportValidationIssue> errors)
    {
        var seen =
            new HashSet<string>(
                StringComparer.Ordinal);

        foreach (var row in file.Rows)
        {
            var assessmentValue =
                Value(
                    row,
                    "AssessmentId")
                .Trim();

            var orderValue =
                Value(
                    row,
                    "QuestionOrder")
                .Trim();

            var outcomeCode =
                NormalizeCode(
                    Value(
                        row,
                        "OutcomeCode"));

            foreach (var required in
                     new[]
                     {
                         ("AssessmentId", assessmentValue),
                         ("QuestionOrder", orderValue),
                         ("OutcomeCode", outcomeCode)
                     })
            {
                Require(
                    row,
                    required.Item1,
                    required.Item2,
                    errors);
            }

            if (!Guid.TryParse(
                    assessmentValue,
                    out var assessmentId))
            {
                if (assessmentValue.Length > 0)
                {
                    Add(
                        row,
                        "AssessmentId",
                        "InvalidType",
                        assessmentValue,
                        errors);
                }

                continue;
            }

            var assessment =
                snapshot.Assessments
                    .FirstOrDefault(x =>
                        x.Id ==
                            assessmentId);

            if (assessment is null)
            {
                Add(
                    row,
                    "AssessmentId",
                    "UnknownReference",
                    assessmentValue,
                    errors);

                continue;
            }

            if (assessment.Status !=
                AssessmentStatus.Draft)
            {
                Add(
                    row,
                    "AssessmentId",
                    "ExistingConflict",
                    assessmentValue,
                    errors);
            }

            if (!int.TryParse(
                    orderValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var order) ||
                order <= 0)
            {
                Add(
                    row,
                    "QuestionOrder",
                    "InvalidType",
                    orderValue,
                    errors);

                continue;
            }

            var question =
                snapshot.AssessmentQuestions
                    .FirstOrDefault(x =>
                        x.AssessmentId ==
                            assessment.Id &&
                        x.Order == order);

            if (question is null)
            {
                Add(
                    row,
                    "QuestionOrder",
                    "UnknownReference",
                    orderValue,
                    errors);

                continue;
            }

            var classGroup =
                snapshot.ClassGroups
                    .FirstOrDefault(x =>
                        x.Id ==
                            assessment.ClassGroupId);

            if (classGroup is null)
            {
                Add(
                    row,
                    "AssessmentId",
                    "UnknownReference",
                    assessmentValue,
                    errors);

                continue;
            }

            var versions =
                ResolveEligibleFrameworkVersionIds(
                    snapshot,
                    assessment.AcademicYearId,
                    classGroup.GradeLevelId,
                    assessment.SubjectId);

            var outcomes =
                snapshot.LearningOutcomes
                    .Where(x =>
                        x.SubjectId ==
                            assessment.SubjectId &&
                        x.GradeLevelId ==
                            classGroup.GradeLevelId &&
                        versions.Contains(
                            x.FrameworkVersionId) &&
                        string.Equals(
                            x.Code,
                            outcomeCode,
                            StringComparison
                                .OrdinalIgnoreCase))
                    .ToArray();

            if (outcomes.Length == 0)
            {
                Add(
                    row,
                    "OutcomeCode",
                    "UnknownReference",
                    outcomeCode,
                    errors);

                continue;
            }

            if (outcomes.Length > 1)
            {
                Add(
                    row,
                    "OutcomeCode",
                    "AmbiguousReference",
                    outcomeCode,
                    errors);

                continue;
            }

            var outcome =
                outcomes[0];

            var logical =
                $"{question.Id:N}:"
                + $"{outcome.Id:N}";

            if (!seen.Add(logical))
            {
                Add(
                    row,
                    "OutcomeCode",
                    "DuplicateRow",
                    outcomeCode,
                    errors);
            }

            if (snapshot.OutcomeMappings
                .Any(x =>
                    x.AssessmentQuestionId ==
                        question.Id &&
                    x.LearningOutcomeId ==
                        outcome.Id))
            {
                Add(
                    row,
                    "OutcomeCode",
                    "ExistingConflict",
                    outcomeCode,
                    errors);
            }
        }
    }

    public static HashSet<Guid>
        ResolveEligibleFrameworkVersionIds(
            ImportDataSnapshot snapshot,
            Guid academicYearId,
            Guid gradeLevelId,
            Guid subjectId)
    {
        var activeVersions =
            snapshot.FrameworkVersions
                .Where(x => x.IsActive)
                .Select(x => x.Id)
                .ToHashSet();

        var yearSpecific =
            snapshot.CurriculumAdoptions
                .Where(x =>
                    x.IsActive &&
                    x.AcademicYearId ==
                        academicYearId &&
                    x.GradeLevelId ==
                        gradeLevelId &&
                    x.SubjectId ==
                        subjectId &&
                    activeVersions.Contains(
                        x.FrameworkVersionId))
                .ToArray();

        var selected =
            yearSpecific.Length > 0
                ? yearSpecific
                : snapshot
                    .CurriculumAdoptions
                    .Where(x =>
                        x.IsActive &&
                        x.AcademicYearId is null &&
                        x.GradeLevelId ==
                            gradeLevelId &&
                        x.SubjectId ==
                            subjectId &&
                        activeVersions.Contains(
                            x.FrameworkVersionId))
                    .ToArray();

        return selected
            .Select(x =>
                x.FrameworkVersionId)
            .ToHashSet();
    }

    public static string Value(
        ImportFileRow row,
        string column) =>
        row.Values.TryGetValue(
            column,
            out var value)
            ? value
            : string.Empty;

    public static string NormalizeCode(
        string? value) =>
        (value ?? string.Empty)
            .Trim()
            .ToUpperInvariant();

    private static void ValidateCode(
        ImportFileRow row,
        string column,
        string value,
        List<ImportValidationIssue> errors)
    {
        if (value.Length == 0)
            return;

        if (value.Length > 50 ||
            !CodePattern.IsMatch(value))
        {
            Add(
                row,
                column,
                "InvalidType",
                value,
                errors);
        }
    }

    private static void Require(
        ImportFileRow row,
        string column,
        string value,
        List<ImportValidationIssue> errors)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            Add(
                row,
                column,
                "Required",
                value,
                errors);
        }
    }

    private static bool ValidEmail(
        string value)
    {
        try
        {
            var address =
                new MailAddress(value);

            return string.Equals(
                address.Address,
                value,
                StringComparison
                    .OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static void Add(
        ImportFileRow row,
        string column,
        string code,
        string? value,
        List<ImportValidationIssue> errors) =>
        errors.Add(
            new ImportValidationIssue(
                row.RowNumber,
                column,
                code,
                value));

    private sealed record ValidatedResultRow(
        ImportFileRow Row,
        Edulytics.Core.Entities.Assessment Assessment,
        Guid StudentId,
        Guid QuestionId);
}
