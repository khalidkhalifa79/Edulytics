namespace Edulytics.Tests.Phase25C;

public sealed class Phase25CFinalContractTests
{
    [Fact]
    public void ImportApply_HasSerializableSubscriptionSeatGuard()
    {
        var source = Read(
            "src/Edulytics.Data/Repositories/ImportRepository.cs");

        Assert.Contains("IsolationLevel.Serializable", source);
        Assert.Contains("SchoolSubscriptions", source);
        Assert.Contains("FOR UPDATE", source);
        Assert.Contains("incomingActiveSeats", source);
        Assert.Contains("ImportPersistenceError.SeatLimit", source);
        Assert.Contains("!x.IsArchived", source);
    }

    [Fact]
    public void CommercialStudentSignIn_RequiresCurrentStudentSeatProfile()
    {
        var source = Read(
            "src/Edulytics.Services/Users/SchoolUserManagementService.cs");

        Assert.Contains(
            "HasActiveStudentProfileForUserAsync",
            source);
        Assert.Contains(
            "role != RoleNames.Student",
            source);
    }

    [Fact]
    public void SubscriptionUi_IsPlatformOnlyAndAntiForgeryProtected()
    {
        var controller = Read(
            "src/Edulytics.Web/Controllers/SubscriptionsController.cs");
        var view = Read(
            "src/Edulytics.Web/Views/Subscriptions/Index.cshtml");

        Assert.Contains(
            "[Authorize(Policy = \"PlatformAdministration\")]",
            controller);

        Assert.True(
            controller.Split("[ValidateAntiForgeryToken]").Length - 1 >= 8);

        Assert.Contains("data-subscription-id", view);
        Assert.Contains("CommittedSeats", view);
    }

    [Fact]
    public void AcademicStudentArchiveRestore_IsAuditedAndConcurrencyGuarded()
    {
        var service = Read(
            "src/Edulytics.Services/Academics/AcademicStructureService.cs");
        var repository = Read(
            "src/Edulytics.Data/Repositories/AcademicStructureRepository.cs");

        Assert.Contains("StudentProfile.Archived", service);
        Assert.Contains("StudentProfile.Restored", service);
        Assert.Contains("expectedRowVersion", repository);
        Assert.Contains("FOR UPDATE", repository);
    }

    [Fact]
    public void Phase25D_BillingModels_AreNotIntroducedInSubscriptionCore()
    {
        var root = FindRepositoryRoot();

        var files =
            Directory.GetFiles(
                Path.Combine(root, "src", "Edulytics.Services", "Subscriptions"),
                "*.cs",
                SearchOption.AllDirectories)
            .Concat(
                Directory.GetFiles(
                    Path.Combine(root, "src", "Edulytics.Core", "Subscriptions"),
                    "*.cs",
                    SearchOption.AllDirectories))
            .ToArray();

        var text = string.Join(
            "\n",
            files.Select(File.ReadAllText));

        Assert.DoesNotContain("Invoice", text, StringComparison.Ordinal);
        Assert.DoesNotContain("BankTransfer", text, StringComparison.Ordinal);
        Assert.DoesNotContain("PaymentProvider", text, StringComparison.Ordinal);
    }

    [Fact]
    public void RenderFreeStaging_AppliesMigrationsBeforeWebStartup()
    {
        var blueprint = Read("render.yaml");
        var entrypoint = Read("docker/render-entrypoint.sh");

        Assert.Contains("plan: free", blueprint);
        Assert.DoesNotContain("preDeployCommand:", blueprint);

        Assert.Contains(
            "ConnectionStrings__MigrationConnection",
            entrypoint);
        Assert.Contains("/app/efbundle", entrypoint);
        Assert.Contains(
            "exec dotnet Edulytics.Web.dll",
            entrypoint);

        var migrationIndex = entrypoint.IndexOf(
            "/app/efbundle",
            StringComparison.Ordinal);
        var webIndex = entrypoint.IndexOf(
            "exec dotnet Edulytics.Web.dll",
            StringComparison.Ordinal);

        Assert.True(migrationIndex >= 0);
        Assert.True(webIndex > migrationIndex);
    }

    private static string Read(string relative) =>
        File.ReadAllText(
            Path.Combine(
                FindRepositoryRoot(),
                relative));

    private static string FindRepositoryRoot()
    {
        var current =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        current.FullName,
                        "Edulytics.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root not found.");
    }
}
