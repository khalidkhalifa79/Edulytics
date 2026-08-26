using System.Reflection;
using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Tests.Phase08;

public sealed class AssessmentUiContractTests
{
    [Fact]
    public void Controller_UsesPolicy_AndAllPostsUseAntiForgery()
    {
        var auth = typeof(AssessmentsController)
            .GetCustomAttributes<AuthorizeAttribute>()
            .Single();

        Assert.Equal(
            "AssessmentManagement",
            auth.Policy);

        var posts = typeof(AssessmentsController)
            .GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public)
            .Where(
                x =>
                    x.GetCustomAttributes<HttpPostAttribute>()
                        .Any())
            .ToArray();

        Assert.NotEmpty(posts);

        Assert.All(
            posts,
            method =>
            {
                Assert.True(
                    method
                        .GetCustomAttributes<
                            ValidateAntiForgeryTokenAttribute>()
                        .Any(),
                    method.Name);

                var mutationAuthorization =
                    method
                        .GetCustomAttributes<AuthorizeAttribute>()
                        .Single();

                Assert.Equal(
                    "Teacher",
                    mutationAuthorization.Roles);
            });
    }

    [Fact]
    public void SchoolDashboard_ExposesAssessmentMonitoring_ToAllAcademicRoles()
    {
        var root = FindRoot();

        var controller =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/Controllers/SchoolHomeController.cs"));

        var dashboard =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/Views/SchoolHome/Dashboard.cshtml"));

        Assert.Contains(
            "CanManageAssessments =",
            controller);

        Assert.Contains(
            "context.Role == RoleNames.Teacher",
            controller);

        Assert.Contains(
            "RoleNames.SchoolAdmin",
            dashboard);

        Assert.Contains(
            "RoleNames.SubjectSupervisor",
            dashboard);

        Assert.Contains(
            "RoleNames.Teacher",
            dashboard);

        Assert.Contains(
            "asp-controller=\"Assessments\"",
            dashboard);
    }


    [Fact]
    public void DraftAssessmentUi_IntegratesOutcomesAndExposesDeleteActions()
    {
        var root = FindRoot();

        var details = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Assessments/Details.cshtml"));

        var editQuestion = File.ReadAllText(
            Path.Combine(
                root,
                "src/Edulytics.Web/Views/Assessments/EditQuestion.cshtml"));

        Assert.Contains(
            "asp-action=\"DeleteAssessment\"",
            details);

        Assert.Contains(
            "asp-action=\"DeleteQuestion\"",
            details);

        Assert.Contains(
            "name=\"outcomeIds\"",
            details);

        Assert.Contains(
            "name=\"outcomeIds\"",
            editQuestion);

        Assert.DoesNotContain(
            "asp-action=\"MapOutcome\"",
            details);

        Assert.DoesNotContain(
            "asp-action=\"UnmapOutcome\"",
            details);
    }

    [Fact]
    public void ResponsiveCssContractExists()
    {
        var root = FindRoot();
        var css = File.ReadAllText(
            Path.Combine(root, "src/Edulytics.Web/wwwroot/css/site.css"));

        Assert.Contains(".assessment-page", css);
        Assert.Contains(".assessment-results-grid", css);
        Assert.Contains(".assessment-score-row", css);
        Assert.Contains("@media (max-width: 767px)", css);
        Assert.Contains("@media (max-width: 420px)", css);
    }

    private static string FindRoot()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null)
        {
            if (File.Exists(Path.Combine(d.FullName, "Edulytics.sln")))
                return d.FullName;
            d = d.Parent;
        }
        throw new DirectoryNotFoundException();
    }
}
