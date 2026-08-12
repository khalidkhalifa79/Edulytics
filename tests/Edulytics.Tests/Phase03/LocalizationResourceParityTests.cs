using System.Xml.Linq;

namespace Edulytics.Tests.Phase03;

public sealed class LocalizationResourceParityTests
{
    [Theory]
    [InlineData("SharedResource")]
    [InlineData("AccountResource")]
    [InlineData("PlatformResource")]
    [InlineData("ValidationResource")]
    public void EnglishAndPolishResources_HaveMatchingKeys(
        string resourceName)
    {
        var root = FindRepositoryRoot();

        var directory =
            Path.Combine(
                root,
                "src",
                "Edulytics.Web",
                "Resources");

        var english =
            ReadKeys(
                Path.Combine(
                    directory,
                    $"{resourceName}.resx"));

        var polish =
            ReadKeys(
                Path.Combine(
                    directory,
                    $"{resourceName}.pl.resx"));

        Assert.Equal(
            english.OrderBy(x => x),
            polish.OrderBy(x => x));
    }

    private static HashSet<string> ReadKeys(
        string path)
    {
        var document = XDocument.Load(path);

        return document
            .Root!
            .Elements("data")
            .Select(
                element =>
                    element.Attribute("name")!.Value)
            .ToHashSet(
                StringComparer.Ordinal);
    }

    private static string FindRepositoryRoot()
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

        throw new DirectoryNotFoundException(
            "Could not locate Edulytics repository root.");
    }
}
