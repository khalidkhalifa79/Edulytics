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
    [Fact] public void CanonicalLessonContentIsNotSchoolScoped()
    { Assert.Null(typeof(CurriculumLessonContent).GetProperty("SchoolId"));Assert.Null(typeof(CurriculumLessonContentTranslation).GetProperty("SchoolId")); }

    [Fact] public void CanonicalContentUsesOfficialLessonNodeIdentity()
    { Assert.NotNull(typeof(CurriculumLessonContent).GetProperty(nameof(CurriculumLessonContent.LessonNodeId)));Assert.NotNull(typeof(CurriculumLessonContent).GetProperty(nameof(CurriculumLessonContent.FrameworkVersionId))); }

    [Fact] public void EfModelMapsCanonicalTables()
    { using var db=CreateDb();Assert.Equal("CurriculumLessonContents",db.Model.FindEntityType(typeof(CurriculumLessonContent))!.GetTableName());Assert.Equal("CurriculumLessonContentTranslations",db.Model.FindEntityType(typeof(CurriculumLessonContentTranslation))!.GetTableName()); }

    [Fact] public void EfModelUsesUniqueCanonicalLessonNode()
    { using var db=CreateDb();var e=db.Model.FindEntityType(typeof(CurriculumLessonContent))!;Assert.Contains(e.GetIndexes(),x=>x.IsUnique&&x.Properties.Count==1&&x.Properties[0].Name==nameof(CurriculumLessonContent.LessonNodeId)); }

    [Fact] public void CanonicalContentUsesConcurrencyTokens()
    { using var db=CreateDb();Assert.True(db.Model.FindEntityType(typeof(CurriculumLessonContent))!.FindProperty(nameof(CurriculumLessonContent.RowVersion))!.IsConcurrencyToken);Assert.True(db.Model.FindEntityType(typeof(CurriculumLessonContentTranslation))!.FindProperty(nameof(CurriculumLessonContentTranslation.RowVersion))!.IsConcurrencyToken); }

    [Fact] public void CanonicalLifecycleIsCentral()
    { Assert.Equal(new[]{CanonicalLessonContentStatus.Draft,CanonicalLessonContentStatus.Verified,CanonicalLessonContentStatus.Published},Enum.GetValues<CanonicalLessonContentStatus>()); }

    [Fact] public void AllThreeSchoolStaffRolesCanRead()
    { Assert.True(LessonContentPolicy.CanReadStaff([RoleNames.SchoolAdmin]));Assert.True(LessonContentPolicy.CanReadStaff([RoleNames.SubjectSupervisor]));Assert.True(LessonContentPolicy.CanReadStaff([RoleNames.Teacher])); }

    [Fact] public void SchoolRolesHaveNoCanonicalAuthoringPolicy()
    { var s=File.ReadAllText(RepoPath("src/Edulytics.Services/LessonContent/LessonContentPolicy.cs"));Assert.DoesNotContain("CanAuthor",s,StringComparison.Ordinal);Assert.DoesNotContain("CanTransition",s,StringComparison.Ordinal); }

    [Fact] public void SchoolLessonControllerIsReadOnlyGetSurface()
    { var s=File.ReadAllText(RepoPath("src/Edulytics.Web/Controllers/LessonContentController.cs"));Assert.DoesNotContain("[HttpPost",s,StringComparison.Ordinal);Assert.DoesNotContain("CreateAsync",s,StringComparison.Ordinal);Assert.DoesNotContain("PublishAsync",s,StringComparison.Ordinal); }

    [Fact] public void StaffViewsExposeNoAuthoringActions()
    { var root=RepoPath("src/Edulytics.Web/Views/LessonContent");var text=string.Join("\n",Directory.GetFiles(root,"*.cshtml").Select(File.ReadAllText));Assert.DoesNotContain("AddLesson",text,StringComparison.Ordinal);Assert.DoesNotContain("SaveDraft",text,StringComparison.Ordinal);Assert.DoesNotContain("SubmitForReview",text,StringComparison.Ordinal);Assert.DoesNotContain("asp-action=\"Publish\"",text,StringComparison.Ordinal); }

    [Fact] public void CanonicalRepositoryUsesPrimaryActiveAdoption()
    { var s=File.ReadAllText(RepoPath("src/Edulytics.Data/Repositories/LessonContentRepository.cs"));Assert.Contains("x.IsActive&&x.IsPrimary",s,StringComparison.Ordinal); }

    [Fact] public void CanonicalRepositoryReadsOfficialPackLessonNodes()
    { var s=File.ReadAllText(RepoPath("src/Edulytics.Data/Repositories/LessonContentRepository.cs"));Assert.Contains("CurriculumPackContentNodes",s,StringComparison.Ordinal);Assert.Contains("x.NodeKind==\"Lesson\"",s,StringComparison.Ordinal); }

    [Fact] public void OutcomeAlignmentComesFromOfficialPackLinks()
    { var s=File.ReadAllText(RepoPath("src/Edulytics.Data/Repositories/LessonContentRepository.cs"));Assert.Contains("CurriculumPackNodeLinks",s,StringComparison.Ordinal);Assert.Contains("LessonStandardAlignment",s,StringComparison.Ordinal); }

    [Fact] public void StudentAccessIsEnrollmentAndAdoptionGated()
    { var s=File.ReadAllText(RepoPath("src/Edulytics.Data/Repositories/LessonContentRepository.cs"));Assert.Contains("StudentProfiles",s,StringComparison.Ordinal);Assert.Contains("StudentEnrollments",s,StringComparison.Ordinal);Assert.Contains("ClassGroups",s,StringComparison.Ordinal);Assert.Contains("SchoolCurriculumAdoptions",s,StringComparison.Ordinal); }

    [Fact] public void StudentOnlyReceivesPublishedCanonicalBody()
    { Assert.True(LessonContentPolicy.CanExposeCanonicalBody(CanonicalLessonContentStatus.Published));Assert.False(LessonContentPolicy.CanExposeCanonicalBody(CanonicalLessonContentStatus.Draft));Assert.False(LessonContentPolicy.CanExposeCanonicalBody(CanonicalLessonContentStatus.Verified)); }

    [Fact] public void StudentViewRemainsEncodedAndProtected()
    { var view=File.ReadAllText(RepoPath("src/Edulytics.Web/Views/StudentPortal/Lesson.cshtml"));var controller=File.ReadAllText(RepoPath("src/Edulytics.Web/Controllers/StudentPortalController.cs"));Assert.DoesNotContain("Html.Raw",view,StringComparison.OrdinalIgnoreCase);Assert.Contains("[Authorize(Policy = \"StudentPortal\")]",controller,StringComparison.Ordinal); }

    [Fact] public void ControllersDoNotUseDbContextAndNoPaidAiWasAdded()
    { var staff=File.ReadAllText(RepoPath("src/Edulytics.Web/Controllers/LessonContentController.cs"));var student=File.ReadAllText(RepoPath("src/Edulytics.Web/Controllers/StudentPortalController.cs"));var service=File.ReadAllText(RepoPath("src/Edulytics.Services/LessonContent/LessonContentService.cs"));Assert.DoesNotContain("EdulyticsDbContext",staff,StringComparison.Ordinal);Assert.DoesNotContain("EdulyticsDbContext",student,StringComparison.Ordinal);Assert.DoesNotContain("OpenAI",service,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("Anthropic",service,StringComparison.OrdinalIgnoreCase); }

    private static EdulyticsDbContext CreateDb(){var options=new DbContextOptionsBuilder<EdulyticsDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;return new EdulyticsDbContext(options);}
    private static string RepoPath(string relative){var current=new DirectoryInfo(AppContext.BaseDirectory);while(current is not null&&!File.Exists(Path.Combine(current.FullName,"Edulytics.sln")))current=current.Parent;Assert.NotNull(current);return Path.Combine(current!.FullName,relative.Replace('/',Path.DirectorySeparatorChar));}
}
