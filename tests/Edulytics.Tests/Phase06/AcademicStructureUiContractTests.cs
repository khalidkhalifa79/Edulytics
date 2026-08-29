using System.Reflection;
using Edulytics.Core.Academics;
using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Tests.Phase06;

public sealed class AcademicStructureUiContractTests
{
    [Fact]
    public void Controller_UsesAcademicAdministrationPolicy()
    {
        var authorize =
            typeof(AcademicStructureController)
                .GetCustomAttributes<AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            "SchoolAccess",
            authorize.Policy);
    }

    [Fact]
    public void EveryStateChangingAction_UsesAntiForgery()
    {
        var posts =
            typeof(AcademicStructureController)
                .GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.GetCustomAttributes<HttpPostAttribute>().Any())
                .ToArray();

        Assert.NotEmpty(posts);

        Assert.All(
            posts,
            method =>
            {
                Assert.True(
                    method.GetCustomAttributes<ValidateAntiForgeryTokenAttribute>().Any(),
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
    public void ResponsiveAcademicCssContractExists()
    {
        var root = FindRepositoryRoot();

        var css = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/wwwroot/css/site.css"));

        Assert.Contains(".academic-page", css);
        Assert.Contains("@media (max-width: 767px)", css);
        Assert.Contains("@media (max-width: 420px)", css);
    }

    [Fact]
    public void AcademicProgramCatalog_IsControlledAndDoesNotExposeMain()
    {
        Assert.Equal(
            new[]
            {
                "BRITISH",
                "AMERICAN",
                "UAE",
                "POLISH"
            },
            AcademicProgramCatalog.All
                .Select(x => x.Code)
                .ToArray());

        Assert.Equal(
            new[]
            {
                "British Stream",
                "American Stream",
                "UAE MoE Stream",
                "Polish Stream"
            },
            AcademicProgramCatalog.All
                .Select(x => x.Name)
                .ToArray());

        Assert.DoesNotContain(
            AcademicProgramCatalog.All,
            x => x.Code == "MAIN");
    }

    [Fact]
    public void CreateAcademicProgram_PostAcceptsChoice_NotUserNameOrCode()
    {
        var method =
            typeof(AcademicStructureController)
                .GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public)
                .Single(
                    x =>
                        x.Name ==
                        nameof(
                            AcademicStructureController
                                .CreateAcademicProgram));

        var parameters =
            method.GetParameters()
                .Select(x => x.Name)
                .ToArray();

        Assert.Contains(
            "academicYearId",
            parameters);

        Assert.Contains(
            "programChoice",
            parameters);

        Assert.DoesNotContain(
            "name",
            parameters);

        Assert.DoesNotContain(
            "code",
            parameters);
    }

    [Fact]
    public void ProgramStreamUi_UsesNameDropdown_AndNeverDisplaysProgramCode()
    {
        var root =
            FindRepositoryRoot();

        var academic =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/Views/" +
                    "AcademicStructure/Index.cshtml"));

        var editClass =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/Views/" +
                    "AcademicStructure/EditClassGroup.cshtml"));

        var curriculum =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/Views/" +
                    "Curriculum/Index.cshtml"));

        Assert.Contains(
            "name=\"programChoice\"",
            academic);

        Assert.Contains(
            "AcademicProgramCatalog.All",
            academic);

        Assert.DoesNotContain(
            "id=\"program-name\"",
            academic);

        Assert.DoesNotContain(
            "id=\"program-code\"",
            academic);

        Assert.DoesNotContain(
            "@program.Name (@program.Code)",
            academic);

        Assert.DoesNotContain(
            "@program.Name (@program.Code)",
            editClass);

        Assert.DoesNotContain(
            "@program.Name (@program.Code)",
            curriculum);

        Assert.DoesNotContain(
            "<td>@item.Code</td><td>@item.Name</td>",
            academic);
    }

    [Fact]
    public void ProgramStreamUi_ContainsAnnualOfferingControls()
    {
        var root =
            FindRepositoryRoot();

        var academic =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/Views/" +
                    "AcademicStructure/Index.cshtml"));

        Assert.Contains(
            "id=\"program-year-filter\"",
            academic);

        Assert.Contains(
            "name=\"academicYearId\"",
            academic);

        Assert.Contains(
            "StopAcademicProgramForYear",
            academic);

        Assert.Contains(
            "data-offered-years",
            academic);

        Assert.Contains(
            "refreshClassPrograms",
            academic);

        Assert.DoesNotContain(
            "id=\"program-status\"",
            academic);
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
