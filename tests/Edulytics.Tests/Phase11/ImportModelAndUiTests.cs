using System.Reflection;
using System.Xml.Linq;
using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase11;

public sealed class ImportModelAndUiTests
{
    [Fact]
    public void ImportBatch_HasConcurrencyAndIdempotencyIndex()
    {
        using var db =
            CreateDb();

        var entity =
            db.Model.FindEntityType(
                typeof(ImportBatch));

        Assert.NotNull(entity);

        var rowVersion =
            entity!.FindProperty(
                nameof(
                    ImportBatch.RowVersion));

        Assert.NotNull(rowVersion);

        Assert.True(
            rowVersion!.IsConcurrencyToken);

        Assert.Contains(
            entity.GetIndexes(),
            x =>
                x.IsUnique &&
                x.Properties
                    .Select(p => p.Name)
                    .SequenceEqual(
                        new[]
                        {
                            nameof(
                                ImportBatch.SchoolId),
                            nameof(
                                ImportBatch.UploadedByUserId),
                            nameof(
                                ImportBatch.ImportType),
                            nameof(
                                ImportBatch.FileHash)
                        }));
    }

    [Fact]
    public void ValidationError_IsMapped()
    {
        using var db =
            CreateDb();

        Assert.NotNull(
            db.Model.FindEntityType(
                typeof(
                    ImportValidationError)));
    }

    [Fact]
    public void Controller_RequiresDataImportPolicy()
    {
        var attribute =
            typeof(ImportsController)
                .GetCustomAttributes<
                    AuthorizeAttribute>()
                .Single();

        Assert.Equal(
            "DataImport",
            attribute.Policy);
    }

    [Fact]
    public void StateChangingImportActions_UseAntiForgery()
    {
        var actions =
            typeof(ImportsController)
                .GetMethods(
                    BindingFlags.Public |
                    BindingFlags.Instance)
                .Where(x =>
                    x.GetCustomAttributes<
                        HttpPostAttribute>()
                        .Any())
                .ToArray();

        Assert.Equal(
            2,
            actions.Length);

        Assert.All(
            actions,
            action =>
                Assert.True(
                    action
                        .GetCustomAttributes<
                            ValidateAntiForgeryTokenAttribute>()
                        .Any()));
    }

    [Fact]
    public void ImportController_HasNoDbContext()
    {
        var root =
            Root();

        var source =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/"
                    + "Controllers/"
                    + "ImportsController.cs"));

        Assert.DoesNotContain(
            "EdulyticsDbContext",
            source);

        Assert.DoesNotContain(
            "DbContext",
            source);
    }

    [Fact]
    public void ImportResources_HaveExactEnPlParity()
    {
        var root =
            Root();

        var en =
            Keys(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/"
                    + "Resources/"
                    + "ImportResource.resx"));

        var pl =
            Keys(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/"
                    + "Resources/"
                    + "ImportResource.pl.resx"));

        Assert.Equal(
            en,
            pl);

        Assert.NotEmpty(en);
    }

    [Fact]
    public void ImportUi_HasResponsiveContracts()
    {
        var root =
            Root();

        var css =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src/Edulytics.Web/"
                    + "wwwroot/css/site.css"));

        Assert.Contains(
            ".import-page",
            css);

        Assert.Contains(
            ".import-table",
            css);

        Assert.Contains(
            "@media (max-width: 767px)",
            css);

        Assert.Contains(
            "@media (max-width: 420px)",
            css);
    }

    private static string[] Keys(
        string path) =>
        XDocument.Load(path)
            .Root!
            .Elements("data")
            .Select(x =>
                (string?)x.Attribute(
                    "name")
                ?? string.Empty)
            .OrderBy(x => x)
            .ToArray();

    private static EdulyticsDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<
                EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    $"p11-{Guid.NewGuid():N}")
                .Options;

        return new EdulyticsDbContext(
            options);
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

        throw new DirectoryNotFoundException();
    }
}
