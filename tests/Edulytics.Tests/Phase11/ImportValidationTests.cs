using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Imports;
using Edulytics.Services.Imports;

namespace Edulytics.Tests.Phase11;

public sealed class ImportValidationTests
{
    [Fact]
    public void SixImportSchemas_AreExact()
    {
        var validator =
            new ImportValidationEngine();

        Assert.Equal(
            new[]
            {
                "StudentNumber",
                "FirstName",
                "LastName",
                "AcademicYear",
                "ClassCode"
            },
            validator.RequiredHeaders(
                ImportType.Students));

        Assert.Equal(
            new[]
            {
                "Email",
                "AcademicYear",
                "ClassCode",
                "SubjectCode"
            },
            validator.RequiredHeaders(
                ImportType.Teachers));

        Assert.Equal(
            new[]
            {
                "AcademicYear",
                "GradeLevel",
                "Code",
                "Name"
            },
            validator.RequiredHeaders(
                ImportType.Classes));

        Assert.Equal(
            new[]
            {
                "Code",
                "Name"
            },
            validator.RequiredHeaders(
                ImportType.Subjects));

        Assert.Equal(
            new[]
            {
                "AssessmentId",
                "StudentNumber",
                "QuestionOrder",
                "Score"
            },
            validator.RequiredHeaders(
                ImportType.AssessmentResults));

        Assert.Equal(
            new[]
            {
                "AssessmentId",
                "QuestionOrder",
                "OutcomeCode"
            },
            validator.RequiredHeaders(
                ImportType.CurriculumMappings));
    }

    [Fact]
    public void MissingColumn_IsValidationError()
    {
        var file =
            new ParsedImportFile(
                ["Code"],
                [
                    Row(
                        2,
                        ("Code", "MATH"))
                ]);

        var result =
            new ImportValidationEngine()
                .Validate(
                    ImportType.Subjects,
                    file,
                    new ImportDataSnapshot(),
                    [],
                    Guid.NewGuid(),
                    RoleNames.SchoolAdmin);

        Assert.Contains(
            result,
            x =>
                x.Code ==
                    "MissingColumn" &&
                x.ColumnName ==
                    "Name");
    }

    [Fact]
    public void ExistingSubject_IsConflict()
    {
        var snapshot =
            new ImportDataSnapshot
            {
                Subjects =
                    [
                        new Subject
                        {
                            Id =
                                Guid.NewGuid(),
                            SchoolId =
                                Guid.NewGuid(),
                            Name =
                                "Mathematics",
                            Code =
                                "MATH",
                            NormalizedCode =
                                "MATH",
                            Status =
                                AcademicStructureStatus
                                    .Active
                        }
                    ]
            };

        var result =
            new ImportValidationEngine()
                .Validate(
                    ImportType.Subjects,
                    new ParsedImportFile(
                        ["Code", "Name"],
                        [
                            Row(
                                2,
                                ("Code", "MATH"),
                                ("Name", "Math"))
                        ]),
                    snapshot,
                    [],
                    Guid.NewGuid(),
                    RoleNames.SchoolAdmin);

        Assert.Contains(
            result,
            x =>
                x.Code ==
                    "ExistingConflict");
    }

    [Fact]
    public void SubjectSupervisor_OwnsAllSupportedImportTypes()
    {
        foreach (var type in Enum.GetValues<ImportType>())
        {
            Assert.True(
                DataImportService.CanImportType(
                    RoleNames.SubjectSupervisor,
                    type));

            Assert.False(
                DataImportService.CanImportType(
                    RoleNames.SchoolAdmin,
                    type));

            Assert.False(
                DataImportService.CanImportType(
                    RoleNames.Teacher,
                    type));
        }
    }

    private static ImportFileRow Row(
        int number,
        params (string Key, string Value)[] values) =>
        new(
            number,
            values.ToDictionary(
                x => x.Key,
                x => x.Value,
                StringComparer.OrdinalIgnoreCase));
}
