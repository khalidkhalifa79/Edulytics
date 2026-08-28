using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Edulytics.Tests.Phase13;

public sealed class PostgreSqlProviderTests
{
    [Fact]
    public void DataProject_UsesNpgsql_AndNotSqlServer()
    {
        var root = Root();

        var project =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Data",
                    "Edulytics.Data.csproj"));

        Assert.Contains(
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            project);

        Assert.DoesNotContain(
            "Microsoft.EntityFrameworkCore.SqlServer",
            project);
    }

    [Fact]
    public void RuntimeRegistration_UsesNpgsql()
    {
        var root = Root();

        var source =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Extensions",
                    "ServiceCollectionExtensions.cs"));

        Assert.Contains(
            "UseNpgsql",
            source);

        Assert.DoesNotContain(
            "UseSqlServer",
            source);
    }

    [Fact]
    public void RowVersions_AreApplicationManagedConcurrencyTokens()
    {
        using var db =
            CreateDb(
                $"p13-model-{Guid.NewGuid():N}");

        var properties =
            db.Model
                .GetEntityTypes()
                .SelectMany(x =>
                    x.GetProperties())
                .Where(x =>
                    x.Name == "RowVersion")
                .ToArray();

        Assert.NotEmpty(properties);

        Assert.All(
            properties,
            property =>
            {
                Assert.True(
                    property.IsConcurrencyToken);

                Assert.Equal(
                    ValueGenerated.Never,
                    property.ValueGenerated);

                Assert.Equal(
                    typeof(byte[]),
                    property.ClrType);
            });
    }

    [Fact]
    public async Task RowVersion_IsIssued_AndRotates()
    {
        var name =
            $"p13-token-{Guid.NewGuid():N}";

        byte[] first;

        await using (
            var db = CreateDb(name))
        {
            var school =
                NewSchool();

            db.Schools.Add(school);

            await db.SaveChangesAsync();

            Assert.Equal(
                16,
                school.RowVersion.Length);

            first =
                school.RowVersion.ToArray();

            school.Name =
                "Updated school";

            await db.SaveChangesAsync();

            Assert.Equal(
                16,
                school.RowVersion.Length);

            Assert.False(
                first.SequenceEqual(
                    school.RowVersion));
        }
    }

    [Fact]
    public async Task StaleUpdate_IsRejected()
    {
        var name =
            $"p13-concurrency-{Guid.NewGuid():N}";

        var id =
            Guid.NewGuid();

        await using (
            var seed = CreateDb(name))
        {
            var school =
                NewSchool();

            school.Id = id;

            seed.Schools.Add(
                school);

            await seed.SaveChangesAsync();
        }

        await using var first =
            CreateDb(name);

        await using var stale =
            CreateDb(name);

        var firstSchool =
            await first.Schools
                .SingleAsync(x =>
                    x.Id == id);

        var staleSchool =
            await stale.Schools
                .SingleAsync(x =>
                    x.Id == id);

        firstSchool.Name =
            "First update";

        await first.SaveChangesAsync();

        staleSchool.Name =
            "Stale update";

        await Assert.ThrowsAsync<
            DbUpdateConcurrencyException>(
            () =>
                stale.SaveChangesAsync());
    }

    [Fact]
    public void NullableBusinessUniqueIndexes_PreserveOldSemantics()
    {
        using var db =
            CreateDb(
                $"p13-index-{Guid.NewGuid():N}");

        var framework =
            db.Model.FindEntityType(
                typeof(
                    CurriculumFramework))!;

        var frameworkIndex =
            framework.GetIndexes()
                .Single(index =>
                    index.Properties
                        .Select(x => x.Name)
                        .SequenceEqual(
                            new[]
                            {
                                nameof(
                                    CurriculumFramework
                                        .OwnerSchoolId),
                                nameof(
                                    CurriculumFramework
                                        .NormalizedCode)
                            }));

        Assert.True(
            frameworkIndex.IsUnique);

        Assert.False(
            frameworkIndex
                .GetAreNullsDistinct());

        var adoption =
            db.Model.FindEntityType(
                typeof(
                    SchoolCurriculumAdoption))!;

        var nullableIndexes =
            adoption.GetIndexes()
                .Where(index =>
                    index.IsUnique &&
                    index.Properties.Any(
                        p =>
                            p.Name ==
                            nameof(
                                SchoolCurriculumAdoption
                                    .AcademicYearId)))
                .ToArray();

        Assert.Equal(
            2,
            nullableIndexes.Length);

        Assert.All(
            nullableIndexes,
            index =>
                Assert.False(
                    index
                        .GetAreNullsDistinct()));

        var primary =
            nullableIndexes.Single(
                index =>
                    index.Properties.Count ==
                    5);

        Assert.Equal(
            "\"IsPrimary\" = TRUE",
            primary.GetFilter());
    }

    private static EdulyticsDbContext CreateDb(
        string name)
    {
        var options =
            new DbContextOptionsBuilder<
                EdulyticsDbContext>()
                .UseInMemoryDatabase(name)
                .Options;

        return new EdulyticsDbContext(
            options);
    }

    private static School NewSchool() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Phase 13 School",
            SchoolCode = "P13",
            NormalizedSchoolCode = "P13",
            Status = SchoolStatus.Active,
            CountryCode = "PL",
            City = "Warsaw",
            ContactEmail =
                "phase13@example.invalid",
            DefaultCulture = "en",
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

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
            "Edulytics root not found.");
    }
}
