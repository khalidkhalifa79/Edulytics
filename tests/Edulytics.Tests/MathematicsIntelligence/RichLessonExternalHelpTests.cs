using Edulytics.Core.Curriculum;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class RichLessonExternalHelpTests
{
    [Fact]
    public void Phase8A_ApprovedResourceRegistry_IsValid()
    {
        RichLessonExternalHelpRegistry.Validate();
    }

    [Fact]
    public void Phase8A_EquivalentFractionsLesson_ResolvesOnlyApprovedResources()
    {
        var help = RichLessonExternalHelpRegistry.Resolve(
            "PED:US-CCSS-MATH:G4:U02:L07",
            "Equivalent fractions",
            "en");

        Assert.NotEmpty(help.ApprovedResources);
        Assert.All(help.ApprovedResources, resource =>
        {
            Assert.Equal(
                RichLessonExternalResourceReviewStatus.Approved,
                resource.ReviewStatus);
            Assert.StartsWith(
                "https://",
                resource.Url,
                StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(resource.WhyRecommended));
        });
    }

    [Fact]
    public void Phase8A_SearchHelp_IsDeterministicAndContainsNoLearnerIdentity()
    {
        var first = RichLessonExternalHelpRegistry.Resolve(
            "PED:US-CCSS-MATH:G4:U02:L07",
            "Equivalent fractions",
            "en");

        var second = RichLessonExternalHelpRegistry.Resolve(
            "PED:US-CCSS-MATH:G4:U02:L07",
            "Equivalent fractions",
            "en");

        Assert.Equal(first.SearchSuggestions, second.SearchSuggestions);
        Assert.Single(first.SearchSuggestions);

        Assert.Contains(
            first.SearchSuggestions,
            x => x.Provider == "YouTube" &&
                 x.Url.StartsWith(
                     "https://www.youtube.com/results?search_query=",
                     StringComparison.Ordinal));

        var serialized = string.Join(
            " ",
            first.SearchSuggestions.SelectMany(x =>
                new[] { x.Label, x.Query, x.Url }));

        Assert.DoesNotContain(
            "student",
            serialized,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "schoolId",
            serialized,
            StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(
            "userId",
            serialized,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Phase8A_UnknownLesson_FailsClosedForCuratedResources()
    {
        var help = RichLessonExternalHelpRegistry.Resolve(
            "PED:DOES-NOT-EXIST",
            "Unknown mathematics lesson",
            "en");

        Assert.Empty(help.ApprovedResources);
        Assert.Empty(help.ApprovedVideos);
        Assert.Single(help.SearchSuggestions);
    }

    [Fact]
    public void Phase8A_SearchSuggestions_LocalizeTheHelpIntent()
    {
        var polish = RichLessonExternalHelpRegistry.Resolve(
            "PED:US-CCSS-MATH:G4:U02:L07",
            "Ułamki równoważne",
            "pl-PL");
        var arabic = RichLessonExternalHelpRegistry.Resolve(
            "PED:US-CCSS-MATH:G4:U02:L07",
            "الكسور المتكافئة",
            "ar");

        Assert.Contains(
            polish.SearchSuggestions,
            x => x.Query.Contains(
                "wyjaśnienie krok po kroku",
                StringComparison.Ordinal));
        Assert.Contains(
            arabic.SearchSuggestions,
            x => x.Query.Contains(
                "شرح خطوة بخطوة",
                StringComparison.Ordinal));
    }

    [Fact]
    public void Phase8A_RoundingLesson_ResolvesReviewedEmbeddedVideo()
    {
        var help = RichLessonExternalHelpRegistry.Resolve(
            "PED:UAE-MOE-MATH:L4:COMMON:02:04",
            "Round Multi-Digit Numbers",
            "en");

        var video = Assert.Single(help.ApprovedVideos);
        Assert.Equal("YouTube", video.Provider);
        Assert.Equal("fd-E18EqSVk", video.VideoId);
        Assert.Equal(
            RichLessonExternalResourceReviewStatus.Approved,
            video.ReviewStatus);
    }

    [Fact]
    public void Phase8A_StudentYouTubeStudio_OwnsEmbedAndYouTubeOnlySearch()
    {
        var root = FindRoot();

        var richPartial = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Views/Shared/_RichLessonContentV2.cshtml"));

        var studioPartial = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Views/Shared/_LessonYouTubeStudio.cshtml"));

        var reviewedResourcesPartial = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Views/Shared/_LessonReviewedResources.cshtml"));

        var studioScript = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/wwwroot/js/lesson-youtube-studio.js"));

        var lessonView = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Views/StudentPortal/Lesson.cshtml"));

        Assert.Contains(
            "suppressLegacyLessonVideos",
            richPartial,
            StringComparison.Ordinal);
        Assert.Contains(
            "youtube-nocookie.com/embed/",
            richPartial,
            StringComparison.Ordinal);

        Assert.Contains(
            "data-youtube-studio",
            studioPartial,
            StringComparison.Ordinal);
        Assert.Contains(
            "youtube-nocookie.com/embed/",
            studioPartial,
            StringComparison.Ordinal);
        Assert.Contains(
            "www.youtube.com/results?search_query=",
            studioPartial,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "google.com/search",
            studioPartial,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "YouTubeLessonChannelPolicy.Resolve",
            studioPartial,
            StringComparison.Ordinal);
        Assert.Contains(
            "load(\"\");",
            studioScript,
            StringComparison.Ordinal);

        Assert.Contains(
            "ApprovedResources",
            reviewedResourcesPartial,
            StringComparison.Ordinal);
        Assert.Contains(
            "08",
            reviewedResourcesPartial,
            StringComparison.Ordinal);

        Assert.Contains(
            "ViewData[\"UseYouTubeStudioV2\"] = true",
            lessonView,
            StringComparison.Ordinal);
        Assert.Contains(
            "_LessonYouTubeStudio",
            lessonView,
            StringComparison.Ordinal);
        Assert.Contains(
            "_LessonReviewedResources",
            lessonView,
            StringComparison.Ordinal);
        Assert.Contains(
            "lesson-youtube-studio.js",
            lessonView,
            StringComparison.Ordinal);
        Assert.Contains(
            "restoreFallbackFeature",
            studioScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "No embeddable video passed the current lesson-match checks.",
            studioScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "YouTube discovery could not load right now. Use the lesson-scoped YouTube links instead.",
            studioScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "if (resultCount) resultCount.textContent = \"0\";",
            studioScript,
            StringComparison.Ordinal);
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "Edulytics.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Edulytics solution root not found.");
    }
}
