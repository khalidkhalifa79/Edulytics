using System.Xml.Linq;

namespace Edulytics.Tests.Phase08;

public sealed class AssessmentLocalizationTests
{
    [Fact]
    public void EnglishAndPolishKeys_Match()
    {
        var root = FindRoot();
        var en = Keys(Path.Combine(root, "src/Edulytics.Web/Resources/AssessmentResource.resx"));
        var pl = Keys(Path.Combine(root, "src/Edulytics.Web/Resources/AssessmentResource.pl.resx"));

        Assert.Equal(en, pl);
        Assert.NotEmpty(en);
    }

    private static string[] Keys(string path) =>
        XDocument.Load(path).Root!.Elements("data")
            .Select(x => (string?)x.Attribute("name") ?? string.Empty)
            .OrderBy(x => x)
            .ToArray();

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
