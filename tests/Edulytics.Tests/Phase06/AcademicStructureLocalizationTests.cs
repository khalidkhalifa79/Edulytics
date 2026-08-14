using System.Xml.Linq;

namespace Edulytics.Tests.Phase06;

public sealed class AcademicStructureLocalizationTests
{
    [Fact]
    public void EnglishAndPolishResourceKeys_Match()
    {
        var root = FindRepositoryRoot();

        var en = LoadKeys(Path.Combine(
            root,
            "src/Edulytics.Web/Resources/AcademicResource.resx"));

        var pl = LoadKeys(Path.Combine(
            root,
            "src/Edulytics.Web/Resources/AcademicResource.pl.resx"));

        Assert.Equal(en, pl);
        Assert.NotEmpty(en);
    }

    private static string[] LoadKeys(string path) =>
        XDocument.Load(path)
            .Root!
            .Elements("data")
            .Select(x => (string?)x.Attribute("name") ?? string.Empty)
            .OrderBy(x => x)
            .ToArray();

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
