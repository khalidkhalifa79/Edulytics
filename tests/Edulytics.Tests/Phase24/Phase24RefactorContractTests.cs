namespace Edulytics.Tests.Phase24;

public sealed class Phase24RefactorContractTests
{
    [Fact]
    public void AssessmentService_IsSplitByResponsibility()
    {
        var root = FindRepositoryRoot();

        var queryPath =
            Path.Combine(
                root,
                "src/Edulytics.Services/Assessments/"
                + "AssessmentService.cs");

        var commandPath =
            Path.Combine(
                root,
                "src/Edulytics.Services/Assessments/"
                + "AssessmentService.Commands.cs");

        var supportPath =
            Path.Combine(
                root,
                "src/Edulytics.Services/Assessments/"
                + "AssessmentService.Support.cs");

        Assert.True(File.Exists(queryPath));
        Assert.True(File.Exists(commandPath));
        Assert.True(File.Exists(supportPath));

        var query = File.ReadAllText(queryPath);
        var commands = File.ReadAllText(commandPath);
        var support = File.ReadAllText(supportPath);

        Assert.Contains(
            "partial class AssessmentService",
            query,
            StringComparison.Ordinal);

        Assert.Contains(
            "GetWorkspaceAsync",
            query,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "CreateAssessmentAsync",
            query,
            StringComparison.Ordinal);

        Assert.Contains(
            "CreateAssessmentAsync",
            commands,
            StringComparison.Ordinal);

        Assert.Contains(
            "SaveStudentResultAsync",
            commands,
            StringComparison.Ordinal);

        Assert.Contains(
            "QueueAuditAsync",
            support,
            StringComparison.Ordinal);

        Assert.Contains(
            "ResolveScopeAsync",
            support,
            StringComparison.Ordinal);

        Assert.True(
            File.ReadLines(queryPath).Count() < 300,
            "Assessment query shell should stay focused.");
    }

    [Fact]
    public void AnalyticsIndex_UsesResultsPartial()
    {
        var root = FindRepositoryRoot();

        var indexPath =
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Analytics/"
                + "Index.cshtml");

        var partialPath =
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Analytics/"
                + "_AnalyticsDashboardResults.cshtml");

        Assert.True(File.Exists(partialPath));

        var index = File.ReadAllText(indexPath);
        var partial = File.ReadAllText(partialPath);

        Assert.Contains(
            "_AnalyticsDashboardResults",
            index,
            StringComparison.Ordinal);

        Assert.Contains(
            "analytics-metrics",
            partial,
            StringComparison.Ordinal);

        Assert.Contains(
            "ClassHeatmap",
            partial,
            StringComparison.Ordinal);

        Assert.Contains(
            "TopicAnalysis",
            partial,
            StringComparison.Ordinal);

        Assert.Contains(
            "ProgressOverTime",
            partial,
            StringComparison.Ordinal);

        Assert.Contains(
            "RiskStudents",
            partial,
            StringComparison.Ordinal);

        Assert.Contains(
            "MasteryLegend",
            partial,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "<script",
            partial,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "signalr.min.js",
            index,
            StringComparison.Ordinal);

        Assert.True(
            File.ReadLines(indexPath).Count() < 220,
            "Analytics parent view should stay focused.");
    }

    private static string FindRepositoryRoot()
    {
        var current =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        current.FullName,
                        "Edulytics.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root not found.");
    }
}
