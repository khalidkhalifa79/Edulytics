namespace Edulytics.Tests.Phase25C;

public sealed class Phase25CArchiveOperationalGuardTests
{
    [Fact]
    public void ArchivedStudent_CannotReceiveNewEnrollment()
    {
        var source = Read(
            "src/Edulytics.Services/Academics/AcademicStructureService.cs");

        Assert.Contains("if (profile.IsArchived)", source);
        Assert.Contains(
            "AcademicStructureErrorCode.StudentAlreadyArchived",
            source);
    }

    [Fact]
    public void ArchivedStudent_CannotReceiveNewAssessmentResultWrite()
    {
        var source = Read(
            "src/Edulytics.Services/Assessments/AssessmentService.Commands.cs");

        Assert.Contains("student.IsArchived", source);
    }

    private static string Read(string relative) =>
        File.ReadAllText(
            Path.Combine(FindRepositoryRoot(), relative));

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(
                    Path.Combine(current.FullName, "Edulytics.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root not found.");
    }
}
