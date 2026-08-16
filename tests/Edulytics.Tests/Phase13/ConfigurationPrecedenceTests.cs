namespace Edulytics.Tests.Phase13;

public sealed class ConfigurationPrecedenceTests
{
    [Fact]
    public void Program_DoesNotReAddUserSecretsAfterDefaultConfiguration()
    {
        var root =
            Root();

        var program =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Program.cs"));

        Assert.DoesNotContain(
            ".AddUserSecrets<Program>",
            program);
    }

    [Fact]
    public void DevelopmentSettings_DoNotContainDatabaseConnection()
    {
        var root =
            Root();

        var settings =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "appsettings.Development.json"));

        Assert.DoesNotContain(
            "ConnectionStrings",
            settings);

        Assert.DoesNotContain(
            "Server=",
            settings);

        Assert.DoesNotContain(
            "Encrypt=",
            settings);
    }

    private static string Root()
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

            directory =
                directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Edulytics repository root not found.");
    }
}
