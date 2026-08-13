using System.Collections;
using System.Globalization;
using System.Resources;
using Edulytics.Web;

namespace Edulytics.Tests.Phase04;

public sealed class SchoolLocalizationTests
{
    private static readonly string[] RequiredSchoolKeys =
    [
        "DashboardSchoolsTitle",
        "DashboardSchoolsDescription",
        "ManageSchools",
        "SchoolsTitle",
        "SchoolsSubtitle",
        "CreateSchool",
        "EmptyTitle",
        "EmptyDescription",
        "Name",
        "SchoolCode",
        "Status",
        "CountryCode",
        "City",
        "ContactEmail",
        "DefaultCulture",
        "TimeZoneId",
        "CreatedAt",
        "UpdatedAt",
        "ArchivedAt",
        "Actions",
        "View",
        "Edit",
        "BackToSchools",
        "SaveChanges",
        "Create",
        "Cancel",
        "SchoolDetails",
        "SchoolInformation",
        "StatusActive",
        "StatusSuspended",
        "StatusArchived",
        "Suspend",
        "Reactivate",
        "Archive",
        "ConfirmSuspend",
        "ConfirmReactivate",
        "ConfirmArchive",
        "CreateSuccess",
        "UpdateSuccess",
        "SuspendSuccess",
        "ReactivateSuccess",
        "ArchiveSuccess",
        "RequiredName",
        "RequiredSchoolCode",
        "InvalidSchoolCode",
        "DuplicateSchoolCode",
        "RequiredCountryCode",
        "RequiredCity",
        "RequiredContactEmail",
        "InvalidContactEmail",
        "RequiredDefaultCulture",
        "InvalidDefaultCulture",
        "RequiredTimeZoneId",
        "SchoolNotFound",
        "ArchivedCannotEdit",
        "InvalidStatusTransition",
        "ConcurrencyConflict",
        "PersistenceError",
        "CultureEnglish",
        "CulturePolish",
        "ImmutableCodeHelp",
        "CreateIntro",
        "EditIntro"
    ];

    [Theory]
    [InlineData("")]
    [InlineData("pl")]
    public void SchoolResources_ContainEveryRequiredKey(
        string cultureName)
    {
        var manager = CreateManager();

        var culture = string.IsNullOrEmpty(cultureName)
            ? CultureInfo.InvariantCulture
            : CultureInfo.GetCultureInfo(cultureName);

        using var resourceSet = manager.GetResourceSet(
            culture,
            createIfNotExists: true,
            tryParents: false);

        Assert.NotNull(resourceSet);

        foreach (var key in RequiredSchoolKeys)
        {
            var value = resourceSet!.GetString(key);

            Assert.False(
                string.IsNullOrWhiteSpace(value),
                $"Resource '{key}' is missing or empty for culture '{cultureName}'.");
        }
    }

    [Fact]
    public void CriticalSchoolResources_ResolveToExpectedEnglishValues()
    {
        var manager = CreateManager();

        Assert.Equal(
            "School management",
            manager.GetString(
                "DashboardSchoolsTitle",
                CultureInfo.InvariantCulture));

        Assert.Equal(
            "Manage schools",
            manager.GetString(
                "ManageSchools",
                CultureInfo.InvariantCulture));

        Assert.Equal(
            "Schools",
            manager.GetString(
                "SchoolsTitle",
                CultureInfo.InvariantCulture));

        Assert.Equal(
            "Create school",
            manager.GetString(
                "CreateSchool",
                CultureInfo.InvariantCulture));

        Assert.Equal(
            "No schools yet",
            manager.GetString(
                "EmptyTitle",
                CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CriticalSchoolResources_ResolveToExpectedPolishValues()
    {
        var manager = CreateManager();
        var polish = CultureInfo.GetCultureInfo("pl");

        Assert.Equal(
            "Zarządzanie szkołami",
            manager.GetString(
                "DashboardSchoolsTitle",
                polish));

        Assert.Equal(
            "Zarządzaj szkołami",
            manager.GetString(
                "ManageSchools",
                polish));

        Assert.Equal(
            "Szkoły",
            manager.GetString(
                "SchoolsTitle",
                polish));

        Assert.Equal(
            "Utwórz szkołę",
            manager.GetString(
                "CreateSchool",
                polish));

        Assert.Equal(
            "Brak szkół",
            manager.GetString(
                "EmptyTitle",
                polish));
    }

    [Fact]
    public void EnglishAndPolishPlatformResources_HaveIdenticalKeys()
    {
        var manager = CreateManager();

        using var english = manager.GetResourceSet(
            CultureInfo.InvariantCulture,
            createIfNotExists: true,
            tryParents: false);

        using var polish = manager.GetResourceSet(
            CultureInfo.GetCultureInfo("pl"),
            createIfNotExists: true,
            tryParents: false);

        Assert.NotNull(english);
        Assert.NotNull(polish);

        Assert.Equal(
            GetKeys(english!),
            GetKeys(polish!));
    }

    private static ResourceManager CreateManager() =>
        new(
            "Edulytics.Web.Resources.PlatformResource",
            typeof(PlatformResource).Assembly);

    private static string[] GetKeys(ResourceSet resourceSet) =>
        resourceSet
            .Cast<DictionaryEntry>()
            .Select(entry => (string)entry.Key)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
}
