using System.Collections;
using System.Globalization;
using System.Resources;
using Edulytics.Web;

namespace Edulytics.Tests.Phase05;

public sealed class SchoolUserLocalizationTests
{
    private static readonly string[] RequiredKeys =
    [
        "ManageSchoolUsers",
        "SchoolUsersTitle",
        "SchoolUsersSubtitle",
        "CreateSchoolUser",
        "NoSchoolUsers",
        "NoSchoolUsersDescription",
        "UserEmail",
        "UserRole",
        "AccountStatus",
        "LockState",
        "UserActive",
        "UserInactive",
        "UserLocked",
        "UserUnlocked",
        "UserDetails",
        "UserInformation",
        "CreateUserSuccess",
        "ActivateUser",
        "DeactivateUser",
        "LockUser",
        "UnlockUser",
        "ChangeRole",
        "ResendInvitation",
        "ResendInvitationDescription",
        "InvitationResentSuccess",
        "InvitationDeliveryFailed",
        "CreateUserInvitationSentSuccess",
        "RoleSchoolAdmin",
        "RoleSubjectSupervisor",
        "RoleTeacher",
        "RoleStudent",
        "UserAccessDenied",
        "UserSchoolArchived",
        "UserNotFound",
        "UserDuplicateEmail",
        "UserCannotManageSelf",
        "UserInvalidPasswordSetup",
        "UserPasswordPolicy",
        "PasswordSetupTitle",
        "NewPassword",
        "ConfirmPassword",
        "SetPassword",
        "PasswordSetTitle",
        "PasswordSetSuccess",
        "SchoolDashboardTitle",
        "YourSchool",
        "YourRole",
        "InvitationEmailSubject",
        "InvitationEmailIntro",
        "InvitationEmailInstruction",
        "InvitationEmailAction",
        "InvitationEmailFallback",
        "InvitationEmailSecurity"
    ];

    [Theory]
    [InlineData("")]
    [InlineData("pl")]
    public void RequiredUserResourcesExist(
        string cultureName)
    {
        var manager = CreateManager();

        var culture =
            string.IsNullOrEmpty(cultureName)
                ? CultureInfo.InvariantCulture
                : CultureInfo.GetCultureInfo(
                    cultureName);

        using var resourceSet =
            manager.GetResourceSet(
                culture,
                createIfNotExists: true,
                tryParents: false);

        Assert.NotNull(resourceSet);

        foreach (var key in RequiredKeys)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(
                    resourceSet!.GetString(key)),
                $"Missing resource {key} for {cultureName}.");
        }
    }

    [Fact]
    public void CriticalEnglishValuesAreCorrect()
    {
        var manager = CreateManager();

        Assert.Equal(
            "School users",
            manager.GetString(
                "SchoolUsersTitle",
                CultureInfo.InvariantCulture));

        Assert.Equal(
            "Create user",
            manager.GetString(
                "CreateSchoolUser",
                CultureInfo.InvariantCulture));

        Assert.Equal(
            "School administrator",
            manager.GetString(
                "RoleSchoolAdmin",
                CultureInfo.InvariantCulture));
    }

    [Fact]
    public void CriticalPolishValuesAreCorrect()
    {
        var manager = CreateManager();

        var culture =
            CultureInfo.GetCultureInfo("pl");

        Assert.Equal(
            "Użytkownicy szkoły",
            manager.GetString(
                "SchoolUsersTitle",
                culture));

        Assert.Equal(
            "Utwórz użytkownika",
            manager.GetString(
                "CreateSchoolUser",
                culture));

        Assert.Equal(
            "Administrator szkoły",
            manager.GetString(
                "RoleSchoolAdmin",
                culture));
    }

    [Fact]
    public void PlatformResourceKeysRemainInParity()
    {
        var manager = CreateManager();

        using var english =
            manager.GetResourceSet(
                CultureInfo.InvariantCulture,
                true,
                false);

        using var polish =
            manager.GetResourceSet(
                CultureInfo.GetCultureInfo("pl"),
                true,
                false);

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

    private static string[] GetKeys(
        ResourceSet resourceSet) =>
        resourceSet
            .Cast<DictionaryEntry>()
            .Select(
                x => (string)x.Key)
            .OrderBy(
                x => x,
                StringComparer.Ordinal)
            .ToArray();
}
