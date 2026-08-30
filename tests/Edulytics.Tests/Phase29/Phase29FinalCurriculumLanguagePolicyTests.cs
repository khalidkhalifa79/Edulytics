using Edulytics.Core.Curriculum;
using Edulytics.Data.Seeding;
using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29FinalCurriculumLanguagePolicyTests
{
    [Fact]
    public void CommonCoreCanonicalPackHasFinalEnglishCoverageAndProvenance()
    {
        var documents = MathematicsCanonicalLessonContentSeeder.LoadEmbeddedDocuments()
            .Where(x => x.PackCode == MathematicsCurriculumPackRegistry.CommonCoreCode).ToArray();
        Assert.All(documents, x =>
        {
            Assert.Equal("en", x.AcademicLanguage);
            Assert.False(x.CurriculumTranslationRequired);
        });
        var lessons = documents.SelectMany(x => x.Lessons)
            .ToDictionary(x => x.LessonCode, StringComparer.Ordinal);

        Assert.Equal(1560, lessons.Count);
        Assert.Equal(1466, lessons.Values.Count(x => !x.IsSupporting));
        Assert.Equal(94, lessons.Values.Count(x => x.IsSupporting));
        Assert.All(lessons.Values.Where(x => x.IsSupporting), x => Assert.Empty(x.OutcomeCodes));

        foreach (var lesson in lessons.Values)
        {
            var translation = Assert.Single(lesson.Translations);
            Assert.Equal("en", translation.CultureCode);
            Assert.False(string.IsNullOrWhiteSpace(translation.Title));
            Assert.False(string.IsNullOrWhiteSpace(translation.Explanation));
            Assert.False(string.IsNullOrWhiteSpace(translation.KeyConceptsAndRules));
            Assert.False(string.IsNullOrWhiteSpace(translation.WorkedExamples));
            Assert.False(string.IsNullOrWhiteSpace(translation.StepByStepSolutions));
            Assert.False(string.IsNullOrWhiteSpace(translation.CommonMistakes));
            Assert.False(string.IsNullOrWhiteSpace(translation.QuickSummary));
            Assert.Matches("^[0-9a-f]{64}$", lesson.CanonicalBodySha256);
            Assert.Matches("^[0-9a-f]{64}$", lesson.SourceSha256);
            Assert.False(string.IsNullOrWhiteSpace(lesson.SourceUrl));
            Assert.False(string.IsNullOrWhiteSpace(lesson.SourceLocator));
            Assert.False(string.IsNullOrWhiteSpace(lesson.SourceTitle));
            Assert.False(string.IsNullOrWhiteSpace(lesson.SourcePublisher));
            Assert.False(string.IsNullOrWhiteSpace(lesson.SourceRights));
            Assert.False(string.IsNullOrWhiteSpace(lesson.SourceVerifiedAtUtc));
            Assert.False(string.IsNullOrWhiteSpace(lesson.RetrievalUrl));
            Assert.False(string.IsNullOrWhiteSpace(lesson.RetrievalChannel));
            Assert.StartsWith("SOURCE_FAITHFUL_", lesson.AdaptationStatus, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void CurriculumAcademicLanguageIsIndependentFromUiLocale()
    {
        var commonCore = Assert.Single(MathematicsCurriculumPackRegistry.All,
            x => x.Code == MathematicsCurriculumPackRegistry.CommonCoreCode);
        Assert.Equal("en", commonCore.AcademicLanguage);
        Assert.Equal(new[] { "en", "pl" }, SupportedUiCultures());

        var service = File.ReadAllText(RepoPath("src/Edulytics.Services/LessonContent/LessonContentService.cs"));
        Assert.Contains("SelectAcademicContent", service, StringComparison.Ordinal);
        Assert.Contains(".AcademicLanguage", service, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectTranslation(content.Translations,cultureCode)", service, StringComparison.Ordinal);
    }

    [Fact]
    public void ContentSourcesRouteIsCentralAndPublic()
    {
        var method = typeof(HomeController).GetMethod(nameof(HomeController.ContentSources));
        Assert.NotNull(method);
        Assert.Contains(method!.GetCustomAttributes(false), x => x is HttpGetAttribute route && route.Template == "/legal/content-sources");
        Assert.Contains(method.GetCustomAttributes(false), x => x.GetType().Name == "AllowAnonymousAttribute");
        Assert.True(File.Exists(RepoPath("src/Edulytics.Web/Views/Home/ContentSources.cshtml")));
        Assert.DoesNotContain("SourceSha256", File.ReadAllText(RepoPath("src/Edulytics.Web/Views/StudentPortal/Lesson.cshtml")), StringComparison.Ordinal);
    }

    private static string[] SupportedUiCultures() => ["en", "pl"];

    private static string RepoPath(string relative)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Edulytics.sln")))
            directory = directory.Parent;
        return Path.Combine(directory?.FullName ?? throw new InvalidOperationException("Repository root not found."), relative);
    }
}
