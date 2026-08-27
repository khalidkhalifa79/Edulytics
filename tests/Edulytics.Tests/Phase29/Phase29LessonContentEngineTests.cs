using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Data.Contexts;
using Edulytics.Services.LessonContent;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29LessonContentEngineTests
{
    [Fact]
    public void SubjectSupervisorCanAuthor()
    {
        Assert.True(LessonContentPolicy.CanAuthor([RoleNames.SubjectSupervisor]));
    }

    [Fact]
    public void SchoolAdminCannotAuthor()
    {
        Assert.False(LessonContentPolicy.CanAuthor([RoleNames.SchoolAdmin]));
        Assert.True(LessonContentPolicy.CanReadStaff([RoleNames.SchoolAdmin]));
    }

    [Fact]
    public void TeacherCannotAuthor()
    {
        Assert.False(LessonContentPolicy.CanAuthor([RoleNames.Teacher]));
        Assert.True(LessonContentPolicy.CanReadStaff([RoleNames.Teacher]));
    }

    [Fact]
    public void DraftCanMoveToReview()
    {
        Assert.True(LessonContentPolicy.CanTransition(
            LearningLessonStatus.Draft,
            LearningLessonStatus.InReview));
    }

    [Fact]
    public void ReviewCanReturnToDraft()
    {
        Assert.True(LessonContentPolicy.CanTransition(
            LearningLessonStatus.InReview,
            LearningLessonStatus.Draft));
    }

    [Fact]
    public void ReviewCanPublish()
    {
        Assert.True(LessonContentPolicy.CanTransition(
            LearningLessonStatus.InReview,
            LearningLessonStatus.Published));
    }

    [Fact]
    public void PublishedCannotMoveBack()
    {
        Assert.False(LessonContentPolicy.CanTransition(
            LearningLessonStatus.Published,
            LearningLessonStatus.Draft));
        Assert.False(LessonContentPolicy.CanTransition(
            LearningLessonStatus.Published,
            LearningLessonStatus.InReview));
    }

    [Fact]
    public void CompleteEnglishContentIsRequiredForWorkflow()
    {
        var complete = new LessonTranslationInput(
            "Title", "Explanation", "Rules", "Examples", "Solutions", "Mistakes", "Summary");
        var incomplete = complete with { QuickSummary = "" };

        Assert.True(LessonContentPolicy.IsComplete(complete));
        Assert.False(LessonContentPolicy.IsComplete(incomplete));
    }

    [Fact]
    public void EfModelHasThreeTenantScopedTables()
    {
        using var db = CreateDb();

        Assert.Equal("LearningLessons", db.Model.FindEntityType(typeof(LearningLesson))!.GetTableName());
        Assert.Equal("LearningLessonOutcomes", db.Model.FindEntityType(typeof(LearningLessonOutcome))!.GetTableName());
        Assert.Equal("LearningLessonTranslations", db.Model.FindEntityType(typeof(LearningLessonTranslation))!.GetTableName());

        Assert.NotNull(db.Model.FindEntityType(typeof(LearningLesson))!.FindProperty(nameof(LearningLesson.SchoolId)));
        Assert.NotNull(db.Model.FindEntityType(typeof(LearningLessonOutcome))!.FindProperty(nameof(LearningLessonOutcome.SchoolId)));
        Assert.NotNull(db.Model.FindEntityType(typeof(LearningLessonTranslation))!.FindProperty(nameof(LearningLessonTranslation.SchoolId)));
    }

    [Fact]
    public void EfModelUsesConcurrencyTokensOnLessonAndTranslation()
    {
        using var db = CreateDb();

        Assert.True(db.Model.FindEntityType(typeof(LearningLesson))!
            .FindProperty(nameof(LearningLesson.RowVersion))!.IsConcurrencyToken);
        Assert.True(db.Model.FindEntityType(typeof(LearningLessonTranslation))!
            .FindProperty(nameof(LearningLessonTranslation.RowVersion))!.IsConcurrencyToken);
    }

    [Fact]
    public void StudentRepositoryRequiresPublishedEnrollmentAndAdoption()
    {
        var source = File.ReadAllText(
            RepoPath("src/Edulytics.Data/Repositories/LessonContentRepository.cs"));

        Assert.Contains("LearningLessonStatus.Published", source, StringComparison.Ordinal);
        Assert.Contains("StudentEnrollments.Any", source, StringComparison.Ordinal);
        Assert.Contains("SchoolCurriculumAdoptions.Any", source, StringComparison.Ordinal);
        Assert.Contains("a.IsPrimary && a.IsActive", source, StringComparison.Ordinal);
        Assert.Contains("a.FrameworkVersionId == topic.FrameworkVersionId", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StudentViewIsEncodedAndControllerKeepsStudentPolicy()
    {
        var view = File.ReadAllText(
            RepoPath("src/Edulytics.Web/Views/StudentPortal/Lesson.cshtml"));
        var controller = File.ReadAllText(
            RepoPath("src/Edulytics.Web/Controllers/StudentPortalController.cs"));

        Assert.DoesNotContain("Html.Raw", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("[Authorize(Policy = \"StudentPortal\")]", controller, StringComparison.Ordinal);
        Assert.Contains("learning/lesson/{id:guid}", controller, StringComparison.Ordinal);
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EdulyticsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new EdulyticsDbContext(options);
    }

    private static string RepoPath(string relative)
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "Edulytics.sln")))
            current = current.Parent;

        Assert.NotNull(current);
        return Path.Combine(current!.FullName, relative.Replace('/', Path.DirectorySeparatorChar));
    }
}
