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
    public void ClassCode_IsInternalAndNotAcceptedFromBrowser()
    {
        var methods =
            typeof(AcademicStructureController)
                .GetMethods(
                    BindingFlags.Instance |
                    BindingFlags.Public);

        var create =
            methods.Single(
                x =>
                    x.Name ==
                        nameof(
                            AcademicStructureController
                                .CreateClassGroup));

        var createParameters =
            create.GetParameters()
                .Select(x => x.Name)
                .ToArray();

        Assert.DoesNotContain(
            "code",
            createParameters);

        var editPost =
            methods.Single(
                x =>
                    x.Name ==
                        nameof(
                            AcademicStructureController
                                .EditClassGroup) &&
                    x.GetCustomAttributes<HttpPostAttribute>()
                        .Any());

        var editParameters =
            editPost.GetParameters()
                .Select(x => x.Name)
                .ToArray();

        Assert.DoesNotContain(
            "code",
            editParameters);
    }

    [Fact]
    public void ClassCode_IsHiddenAcrossAcademicStructureUi()
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

        var classesStart =
            academic.IndexOf(
                "<section id=\"classes\"",
                StringComparison.Ordinal);

        var subjectsStart =
            academic.IndexOf(
                "<section id=\"subjects\"",
                StringComparison.Ordinal);

        Assert.True(
            classesStart >= 0 &&
            subjectsStart > classesStart);

        var classesSection =
            academic[
                classesStart..subjectsStart];

        Assert.DoesNotContain(
            "class-code",
            classesSection);

        Assert.DoesNotContain(
            "@A[\"Code\"]",
            classesSection);

        Assert.DoesNotContain(
            "name=\"code\"",
            editClass);

        Assert.DoesNotContain(
            "ClassCode —",
            academic);

        var teacherClassStart =
            academic.IndexOf(
                "<select id=\"teacher-class\"",
                StringComparison.Ordinal);

        var teacherClassEnd =
            academic.IndexOf(
                "</select>",
                teacherClassStart,
                StringComparison.Ordinal);

        Assert.True(
            teacherClassStart >= 0 &&
            teacherClassEnd > teacherClassStart);

        var teacherClassSelector =
            academic[
                teacherClassStart..(teacherClassEnd + "</select>".Length)];

        Assert.DoesNotContain(
            "@item.Code",
            teacherClassSelector);

        Assert.Contains(
            "@item.Name",
            teacherClassSelector);

        var enrollClassStart =
            academic.IndexOf(
                "<select id=\"enroll-class\"",
                StringComparison.Ordinal);

        var enrollClassEnd =
            academic.IndexOf(
                "</select>",
                enrollClassStart,
                StringComparison.Ordinal);

        Assert.True(
            enrollClassStart >= 0 &&
            enrollClassEnd > enrollClassStart);

        var enrollClassSelector =
            academic[
                enrollClassStart..(enrollClassEnd + "</select>".Length)];

        Assert.DoesNotContain(
            "@item.Code",
            enrollClassSelector);

        Assert.Contains(
            "@item.Name",
            enrollClassSelector);
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
            "academic-programs-group",
            academic);

        Assert.Contains(
            "academic-programs-context",
            academic);

        Assert.Contains(
            "academic-programs-subgroups",
            academic);

        Assert.Contains(
            "class=\"academic-card academic-form academic-programs-subgroup\"",
            academic);

        Assert.Contains(
            "class=\"academic-card academic-programs-subgroup\"",
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

    [Fact]
    public void ProgramStreamActions_UseCompactButtonLabelsAndSizing()
    {
        var root =
            FindRepositoryRoot();

        var academic =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/Views/" +
                    "AcademicStructure/Index.cshtml"));

        var css =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/wwwroot/css/" +
                    "site.css"));

        Assert.Contains(
            "@A[\"AddProgramToAcademicYear\"]",
            academic);

        Assert.Contains(
            "@A[\"AddProgramButton\"]",
            academic);

        Assert.Contains(
            "@A[\"RemoveProgramButton\"]",
            academic);

        Assert.Contains(
            "academic-programs-add-action",
            academic);

        Assert.Contains(
            "academic-programs-remove-action",
            academic);

        Assert.Contains(
            ".academic-programs-add-action",
            css);

        Assert.Contains(
            "justify-self: start;",
            css);

        Assert.Contains(
            "width: fit-content;",
            css);

        Assert.Contains(
            "white-space: nowrap;",
            css);
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
