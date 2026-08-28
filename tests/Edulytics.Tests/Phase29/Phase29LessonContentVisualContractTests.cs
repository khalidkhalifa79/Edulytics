using Xunit;
namespace Edulytics.Tests.Phase29;
public sealed class Phase29LessonContentVisualContractTests
{
    [Fact] public void StaffCanonicalLibraryIsReadOnlyAndStyled()
    {
        var root=FindRoot();var index=File.ReadAllText(Path.Combine(root,"src","Edulytics.Web","Views","LessonContent","Index.cshtml"));
        var detail=File.ReadAllText(Path.Combine(root,"src","Edulytics.Web","Views","LessonContent","Detail.cshtml"));
        var css=File.ReadAllText(Path.Combine(root,"src","Edulytics.Web","wwwroot","css","site.css"));
        Assert.Contains("lesson-content-topic-card",index);Assert.Contains("lesson-content-coverage-badge",index);Assert.Contains("lesson-content-panel",detail);
        Assert.DoesNotContain("<form",index,StringComparison.OrdinalIgnoreCase);Assert.DoesNotContain("<form",detail,StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EDULYTICS PHASE 29 CANONICAL CONTENT ARCHITECTURE",css);
    }
    private static string FindRoot(){var d=new DirectoryInfo(AppContext.BaseDirectory);while(d is not null){if(File.Exists(Path.Combine(d.FullName,"Edulytics.sln")))return d.FullName;d=d.Parent;}throw new DirectoryNotFoundException("Repository root not found.");}
}
