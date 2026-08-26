using System.Reflection;
using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Tests.Phase07;

public sealed class CurriculumUiContractTests
{
    [Fact]
    public void Controller_UsesAcademicAdministrationPolicy()
    {
        var attribute = typeof(CurriculumController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();

        Assert.Equal(
            "SchoolAccess",
            attribute.Policy);
    }

    [Fact]
    public void EveryPost_UsesAntiForgery()
    {
        var posts = typeof(CurriculumController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Where(x => x.GetCustomAttributes<HttpPostAttribute>().Any())
            .ToArray();

        Assert.NotEmpty(posts);

        Assert.All(
            posts,
            method =>
            {
                Assert.True(
                    method.GetCustomAttributes<
                        ValidateAntiForgeryTokenAttribute>().Any(),
                    method.Name);

                var authorization = method
                    .GetCustomAttributes<AuthorizeAttribute>()
                    .Single();

                Assert.Equal(
                    "SubjectSupervisor",
                    authorization.Roles);
            });
    }


    [Fact]
    public void Dashboard_RequiresVerifiedFrameworkSelectionBeforeTopicAuthoring()
    {
        var root = FindRepositoryRoot();
        var view = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Views/Curriculum/Index.cshtml"));

        Assert.Contains(
            "asp-action=\"SelectFramework\"",
            view);
        Assert.Contains(
            "name=\"frameworkCode\"",
            view);
        Assert.Contains(
            "@foreach (var framework in Model.Frameworks)",
            view);
        Assert.Contains(
            "TopicRequiresFramework",
            view);
    }

    [Fact]
    public void OfficialOutcome_IsSelected_AndOfficialFieldsAreReadOnly()
    {
        var root = FindRepositoryRoot();
        var view = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Views/Curriculum/Index.cshtml"));

        Assert.Contains(
            "asp-action=\"CreateOfficialOutcome\"",
            view);
        Assert.Contains(
            "name=\"selectionKey\"",
            view);
        Assert.Contains(
            "data-code=\"@outcome.Code\"",
            view);
        Assert.Contains(
            "id=\"officialCode\" readonly",
            view);
        Assert.Contains(
            "id=\"officialDescription\"",
            view);
        Assert.Contains(
            "@topic.FrameworkDisplayName",
            view);
        Assert.DoesNotContain(
            "AddCustomOutcome",
            view);
        Assert.DoesNotContain(
            "asp-action=\"CreateOutcome\"",
            view);
        Assert.DoesNotContain(
            "SchoolOutcomeCode",
            view);
        Assert.DoesNotContain(
            "name=\"weight\"",
            view);
    }

    [Fact]
    public void CurriculumViews_DoNotExposeOutcomeWeight()
    {
        var root = FindRepositoryRoot();

        foreach (var name in new[]
        {
            "Index.cshtml",
            "EditOutcome.cshtml"
        })
        {
            var view = File.ReadAllText(Path.Combine(
                root,
                "src/Edulytics.Web/Views/Curriculum",
                name));

            Assert.DoesNotContain(
                "name=\"weight\"",
                view);
            Assert.DoesNotContain(
                "@C[\"WeightHelp\"]",
                view);
            Assert.DoesNotContain(
                "outcome.Weight",
                view);
        }
    }

    [Fact]
    public void ResponsiveCssContractExists()
    {
        var root = FindRepositoryRoot();
        var css = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/wwwroot/css/site.css"));

        Assert.Contains(".curriculum-page", css);
        Assert.Contains(".curriculum-create-grid", css);
        Assert.Contains(".curriculum-table", css);
        Assert.Contains("@media (max-width: 767px)", css);
        Assert.Contains("@media (max-width: 420px)", css);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Edulytics.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException();
    }
}
