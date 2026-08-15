using System.Xml.Linq;

namespace Edulytics.Tests.Phase09;

public sealed class AnalyticsLocalizationTests
{
    [Fact]
    public void EnglishAndPolishResourceKeys_Match()
    {
        var root = FindRoot();

        var en = Keys(
            Path.Combine(
                root,
                "src/Edulytics.Web/Resources/"
                + "AnalyticsResource.resx"));

        var pl = Keys(
            Path.Combine(
                root,
                "src/Edulytics.Web/Resources/"
                + "AnalyticsResource.pl.resx"));

        Assert.Equal(en, pl);
        Assert.NotEmpty(en);
    }

    [Fact]
    public void PolishAnalyticsResource_DoesNotContainEnglishTitle()
    {
        var root = FindRoot();

        var values = XDocument.Load(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/Resources/"
                    + "AnalyticsResource.pl.resx"))
            .Root!
            .Elements("data")
            .Select(
                x =>
                    x.Element("value")?.Value ??
                    string.Empty);

        Assert.DoesNotContain(
            values,
            x =>
                x.Contains(
                    "Analytics and mastery",
                    StringComparison.Ordinal));
    }

    private static string[] Keys(string path) =>
        XDocument.Load(path)
            .Root!
            .Elements("data")
            .Select(
                x =>
                    (string?)x.Attribute("name") ??
                    string.Empty)
            .OrderBy(x => x)
            .ToArray();

    private static string FindRoot()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

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

        throw new DirectoryNotFoundException();
    }
}
