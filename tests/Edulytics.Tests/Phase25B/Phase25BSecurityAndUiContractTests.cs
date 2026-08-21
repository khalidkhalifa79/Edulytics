using System.Reflection;
using Edulytics.Core.Enums;
using Edulytics.Web.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Edulytics.Tests.Phase25B;

public sealed class Phase25BSecurityAndUiContractTests
{
    [Fact]
    public void PublicDemoPost_HasAntiForgeryAndRequestDemoRateLimit()
    {
        var method = typeof(OnboardingController).GetMethods()
            .Single(x => x.Name == "Index" && x.GetCustomAttributes<HttpPostAttribute>().Any());
        Assert.True(method.GetCustomAttributes<AllowAnonymousAttribute>().Any());
        Assert.True(method.GetCustomAttributes<ValidateAntiForgeryTokenAttribute>().Any());
        Assert.Contains(method.GetCustomAttributes<EnableRateLimitingAttribute>(), x => x.PolicyName == "RequestDemo");
    }

    [Fact]
    public void PlatformOnboarding_RequiresPlatformAdministration()
    {
        var attribute = typeof(CustomerOnboardingController)
            .GetCustomAttributes<AuthorizeAttribute>().Single();
        Assert.Equal("PlatformAdministration", attribute.Policy);
    }

    [Fact]
    public void Controllers_DoNotUseDbContext()
    {
        foreach (var file in new[]
        {
            "src/Edulytics.Web/Controllers/OnboardingController.cs",
            "src/Edulytics.Web/Controllers/CustomerOnboardingController.cs"
        })
            Assert.DoesNotContain("EdulyticsDbContext", ReadSource(file), StringComparison.Ordinal);
    }

    [Fact]
    public void RequestDemoRateLimit_IsLocalAndDistributed()
    {
        var local = ReadSource("src/Edulytics.Web/Extensions/BackendResilienceRegistrationExtensions.cs");
        var distributed = ReadSource("src/Edulytics.Web/Scale/DistributedSensitiveRateLimitMiddleware.cs");
        Assert.Contains("\"RequestDemo\"", local, StringComparison.Ordinal);
        Assert.Contains("PermitLimit = 5", local, StringComparison.Ordinal);
        Assert.Contains("\"RequestDemo\" =>", distributed, StringComparison.Ordinal);
        Assert.Contains("TimeSpan.FromHours(1)", distributed, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionProvisioning_IsSuspendedPendingPaymentActivation()
    {
        var source = ReadSource("src/Edulytics.Data/Repositories/CustomerOnboardingRepository.cs");
        Assert.Contains("Status = SchoolStatus.Suspended", source, StringComparison.Ordinal);
        Assert.Contains("school.Status = SchoolStatus.Suspended", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DemoExpiryAndRevocation_AreEnforcedAtSignIn()
    {
        var source = ReadSource("src/Edulytics.Services/Users/SchoolUserManagementService.cs");
        Assert.Contains("GetDemoAccessBySchoolAsync", source, StringComparison.Ordinal);
        Assert.Contains("demo.ExpiresAtUtc <= DateTime.UtcNow", source, StringComparison.Ordinal);
        Assert.Contains("demo.RevokedAtUtc.HasValue", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PublicRegistration_RemainsAbsent()
    {
        var root = FindRoot();
        foreach (var path in Directory.GetFiles(Path.Combine(root, "src/Edulytics.Web/Controllers"), "*.cs"))
        {
            var text = File.ReadAllText(path);
            Assert.DoesNotContain("[HttpGet(\"/account/register", text, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("[HttpPost(\"/account/register", text, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Pipeline_ContainsAllLockedStates()
    {
        Assert.Equal(new[]
        {
            DemoRequestStatus.New,
            DemoRequestStatus.Contacted,
            DemoRequestStatus.DemoScheduled,
            DemoRequestStatus.DemoCompleted,
            DemoRequestStatus.Qualified,
            DemoRequestStatus.Won,
            DemoRequestStatus.Lost
        }, Enum.GetValues<DemoRequestStatus>());
    }

    private static string ReadSource(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRoot(), relativePath));

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "Edulytics.sln")))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException();
    }
}
