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
    public void CanonicalLessonContentIsNotSchoolScoped()
    {
        Assert.Null(typeof(CurriculumLessonContent).GetProperty("SchoolId"));
        Assert.Null(typeof(CurriculumLessonContentTranslation).GetProperty("SchoolId"));
        Assert.Null(typeof(CurriculumPedagogicalLesson).GetProperty("SchoolId"));
    }

    [Fact]
    public void CanonicalContentUsesPedagogicalLessonIdentity()
    {
        Assert.NotNull(
            typeof(CurriculumLessonContent).GetProperty(
                nameof(CurriculumLessonContent.PedagogicalLessonId)));

        Assert.Null(
            typeof(CurriculumLessonContent).GetProperty("LessonNodeId"));

        Assert.NotNull(
            typeof(CurriculumLessonContent).GetProperty(
                nameof(CurriculumLessonContent.FrameworkVersionId)));
    }

    [Fact]
    public void OfficialLessonNodeIsOptionalProvenance()
    {
        var property = typeof(CurriculumPedagogicalLesson)
            .GetProperty(nameof(CurriculumPedagogicalLesson.OfficialLessonNodeId));

        Assert.NotNull(property);
        Assert.Equal(typeof(Guid?), property!.PropertyType);
    }

    [Fact]
    public void EfModelMapsPedagogicalAndCanonicalTables()
    {
        using var db = CreateDb();

        Assert.Equal(
            "CurriculumPedagogicalLessons",
            db.Model.FindEntityType(typeof(CurriculumPedagogicalLesson))!.GetTableName());

        Assert.Equal(
            "CurriculumPedagogicalLessonOutcomes",
            db.Model.FindEntityType(typeof(CurriculumPedagogicalLessonOutcome))!.GetTableName());

        Assert.Equal(
            "CurriculumLessonContents",
            db.Model.FindEntityType(typeof(CurriculumLessonContent))!.GetTableName());

        Assert.Equal(
            "CurriculumLessonContentTranslations",
            db.Model.FindEntityType(typeof(CurriculumLessonContentTranslation))!.GetTableName());
    }

    [Fact]
    public void EfModelUsesUniquePedagogicalLessonContentIdentity()
    {
        using var db = CreateDb();

        var entity = db.Model.FindEntityType(typeof(CurriculumLessonContent))!;

        Assert.Contains(
            entity.GetIndexes(),
            x =>
                x.IsUnique &&
                x.Properties.Count == 1 &&
                x.Properties[0].Name ==
                    nameof(CurriculumLessonContent.PedagogicalLessonId));
    }

    [Fact]
    public void CanonicalContentUsesConcurrencyTokens()
    {
        using var db = CreateDb();

        Assert.True(
            db.Model
                .FindEntityType(typeof(CurriculumLessonContent))!
                .FindProperty(nameof(CurriculumLessonContent.RowVersion))!
                .IsConcurrencyToken);

        Assert.True(
            db.Model
                .FindEntityType(typeof(CurriculumLessonContentTranslation))!
                .FindProperty(nameof(CurriculumLessonContentTranslation.RowVersion))!
                .IsConcurrencyToken);

        Assert.True(
            db.Model
                .FindEntityType(typeof(CurriculumPedagogicalLesson))!
                .FindProperty(nameof(CurriculumPedagogicalLesson.RowVersion))!
                .IsConcurrencyToken);
    }

    [Fact]
    public void CanonicalLifecycleIsCentral()
    {
        Assert.Equal(
            new[]
            {
                CanonicalLessonContentStatus.Draft,
                CanonicalLessonContentStatus.Verified,
                CanonicalLessonContentStatus.Published
            },
            Enum.GetValues<CanonicalLessonContentStatus>());
    }

    [Fact]
    public void AllThreeSchoolStaffRolesCanRead()
    {
        Assert.True(LessonContentPolicy.CanReadStaff([RoleNames.SchoolAdmin]));
        Assert.True(LessonContentPolicy.CanReadStaff([RoleNames.SubjectSupervisor]));
        Assert.True(LessonContentPolicy.CanReadStaff([RoleNames.Teacher]));
    }

    [Fact]
    public void SchoolRolesHaveNoCanonicalAuthoringPolicy()
    {
        var source = File.ReadAllText(
            RepoPath("src/Edulytics.Services/LessonContent/LessonContentPolicy.cs"));

        Assert.DoesNotContain("CanAuthor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CanTransition", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SchoolLessonControllerIsReadOnlyGetSurface()
    {
        var source = File.ReadAllText(
            RepoPath("src/Edulytics.Web/Controllers/LessonContentController.cs"));

        Assert.DoesNotContain("[HttpPost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateAsync", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PublishAsync", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StaffViewsExposeNoAuthoringActions()
    {
        var root = RepoPath("src/Edulytics.Web/Views/LessonContent");

        var text = string.Join(
            "\n",
            Directory.GetFiles(root, "*.cshtml")
                .Select(File.ReadAllText));

        Assert.DoesNotContain("AddLesson", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SaveDraft", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SubmitForReview", text, StringComparison.Ordinal);
        Assert.DoesNotContain("asp-action=\"Publish\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalRepositoryUsesPrimaryActiveAdoption()
    {
        var source = File.ReadAllText(
            RepoPath("src/Edulytics.Data/Repositories/LessonContentRepository.cs"));

        Assert.Contains(
            "x.IsActive &&\n                x.IsPrimary",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void CanonicalRepositoryReadsPedagogicalLessons()
    {
        var source = File.ReadAllText(
            RepoPath("src/Edulytics.Data/Repositories/LessonContentRepository.cs"));

        Assert.Contains(
            "CurriculumPedagogicalLessons",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "x.NodeKind == \"Lesson\"",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void OutcomeAlignmentUsesPedagogicalMapping()
    {
        var repository = File.ReadAllText(
            RepoPath("src/Edulytics.Data/Repositories/LessonContentRepository.cs"));

        var seeder = File.ReadAllText(
            RepoPath("src/Edulytics.Data/Seeding/MathematicsPedagogicalLessonSeeder.cs"));

        Assert.Contains(
            "CurriculumPedagogicalLessonOutcomes",
            repository,
            StringComparison.Ordinal);

        Assert.Contains(
            "LessonStandardAlignment",
            seeder,
            StringComparison.Ordinal);
    }

    [Fact]
    public void NonUaeOfficialPacksRemainSyntheticLessonFree()
    {
        var seeder = File.ReadAllText(
            RepoPath("src/Edulytics.Data/Seeding/MathematicsCurriculumPackSeeder.cs"));

        Assert.Contains(
            "Only verified real lessons may be persisted; synthetic teaching shells are forbidden",
            seeder,
            StringComparison.Ordinal);
    }

    [Fact]
    public void StudentAccessIsEnrollmentAndAdoptionGated()
    {
        var source = File.ReadAllText(
            RepoPath("src/Edulytics.Data/Repositories/LessonContentRepository.cs"));

        Assert.Contains("StudentProfiles", source, StringComparison.Ordinal);
        Assert.Contains("StudentEnrollments", source, StringComparison.Ordinal);
        Assert.Contains("ClassGroups", source, StringComparison.Ordinal);
        Assert.Contains("SchoolCurriculumAdoptions", source, StringComparison.Ordinal);
    }

    [Fact]
    public void StudentOnlyReceivesPublishedCanonicalBody()
    {
        Assert.True(
            LessonContentPolicy.CanExposeCanonicalBody(
                CanonicalLessonContentStatus.Published));

        Assert.False(
            LessonContentPolicy.CanExposeCanonicalBody(
                CanonicalLessonContentStatus.Draft));

        Assert.False(
            LessonContentPolicy.CanExposeCanonicalBody(
                CanonicalLessonContentStatus.Verified));
    }

    [Fact]
    public void StudentViewRemainsEncodedAndProtected()
    {
        var view = File.ReadAllText(
            RepoPath("src/Edulytics.Web/Views/StudentPortal/Lesson.cshtml"));

        var controller = File.ReadAllText(
            RepoPath("src/Edulytics.Web/Controllers/StudentPortalController.cs"));

        Assert.DoesNotContain(
            "Html.Raw",
            view,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "[Authorize(Policy = \"StudentPortal\")]",
            controller,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ControllersDoNotUseDbContextAndNoPaidAiWasAdded()
    {
        var staff = File.ReadAllText(
            RepoPath("src/Edulytics.Web/Controllers/LessonContentController.cs"));

        var student = File.ReadAllText(
            RepoPath("src/Edulytics.Web/Controllers/StudentPortalController.cs"));

        var service = File.ReadAllText(
            RepoPath("src/Edulytics.Services/LessonContent/LessonContentService.cs"));

        Assert.DoesNotContain("EdulyticsDbContext", staff, StringComparison.Ordinal);
        Assert.DoesNotContain("EdulyticsDbContext", student, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenAI", service, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Anthropic", service, StringComparison.OrdinalIgnoreCase);
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

        while (current is not null &&
               !File.Exists(Path.Combine(current.FullName, "Edulytics.sln")))
        {
            current = current.Parent;
        }

        Assert.NotNull(current);

        return Path.Combine(
            current!.FullName,
            relative.Replace('/', Path.DirectorySeparatorChar));
    }
}
