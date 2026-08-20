using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using Edulytics.Web.Middleware;
using Edulytics.Web.ViewModels.Schools;
using Microsoft.AspNetCore.Http;

namespace Edulytics.Tests.Phase23;

public sealed class
    Phase23SecurityPrivacyAccessibilityTests
{
    [Fact]
    public async Task
        SecurityHeaders_UseNonceBasedScriptPolicy()
    {
        var context =
            new DefaultHttpContext();

        context.Request.Scheme =
            "https";

        var middleware =
            new SecurityHeadersMiddleware(
                _ => Task.CompletedTask);

        await middleware.InvokeAsync(
            context);

        var csp =
            context.Response.Headers[
                "Content-Security-Policy"]
                .ToString();

        Assert.Contains(
            "default-src 'self'",
            csp,
            StringComparison.Ordinal);

        Assert.Contains(
            "script-src 'self' 'nonce-",
            csp,
            StringComparison.Ordinal);

        Assert.Contains(
            "script-src-attr 'none'",
            csp,
            StringComparison.Ordinal);

        var scriptDirective =
            csp.Split(';')
                .Single(
                    x =>
                        x.TrimStart()
                            .StartsWith(
                                "script-src ",
                                StringComparison.Ordinal));

        Assert.DoesNotContain(
            "'unsafe-inline'",
            scriptDirective,
            StringComparison.Ordinal);

        Assert.True(
            context.Items.ContainsKey(
                SecurityHeadersMiddleware
                    .CspNonceItemKey));
    }

    [Fact]
    public void
        ProductionAllowedHosts_IsNotWildcard()
    {
        using var document =
            JsonDocument.Parse(
                ReadSource(
                    "src/Edulytics.Web/"
                    + "appsettings.Production.json"));

        var hosts =
            document.RootElement
                .GetProperty(
                    "AllowedHosts")
                .GetString();

        Assert.False(
            string.IsNullOrWhiteSpace(
                hosts));

        Assert.NotEqual(
            "*",
            hosts);
    }

    [Fact]
    public void
        SensitiveAuthenticationAndOperationsAreRateLimited()
    {
        Assert.Contains(
            "EnableRateLimiting(\"Login\")",
            ReadSource(
                "src/Edulytics.Web/Controllers/"
                + "AccountController.cs"),
            StringComparison.Ordinal);

        Assert.Contains(
            "EnableRateLimiting(\"OperationalMutation\")",
            ReadSource(
                "src/Edulytics.Web/Controllers/"
                + "OperationsController.cs"),
            StringComparison.Ordinal);
    }

    [Fact]
    public void
        TenantSignInDecision_IsFailClosed()
    {
        var source =
            ReadSource(
                "src/Edulytics.Services/Users/"
                + "SchoolUserManagementService.cs");

        Assert.Contains(
            "if (user.SchoolId is null)",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "role == RoleNames.SuperAdmin",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "!TenantRoles.Contains(role)",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "school.Status != SchoolStatus.Active",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void
        ApplicationUserUsesIdentityConcurrencyStamp()
    {
        var source =
            ReadSource(
                "src/Edulytics.Data/Configurations/"
                + "ApplicationUserConfiguration.cs");

        Assert.Contains(
            "x => x.ConcurrencyStamp",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            ".IsConcurrencyToken()",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void
        PublicHealthResponse_IsMinimal()
    {
        var source =
            ReadSource(
                "src/Edulytics.Web/Health/"
                + "HealthResponseWriter.cs");

        Assert.Contains(
            "report.Status.ToString()",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "context.TraceIdentifier",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            ".Description",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            ".Data",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "totalDurationMs",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void
        RetentionPhysicallyRemovesSensitiveArtifacts()
    {
        var source =
            ReadSource(
                "src/Edulytics.Data/Repositories/"
                + "SensitiveDataRetentionRepository.cs");

        Assert.Contains(
            "x => x.RowsJson",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "x => x.OriginalFileName",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "x => x.FileContent",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "ExecuteDeleteAsync",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "NotificationDeliveryStatus.Pending",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void
        DocumentLanguageTracksCurrentUICulture()
    {
        var source =
            ReadSource(
                "src/Edulytics.Web/Views/"
                + "Shared/_Layout.cshtml");

        Assert.Contains(
            "CurrentUICulture",
            source,
            StringComparison.Ordinal);

        Assert.Contains(
            "lang=\"@pageLanguage\"",
            source,
            StringComparison.Ordinal);
    }

    [Fact]
    public void
        ViewsContainNoPositiveTabindexOrInlineJsHandlers()
    {
        var root =
            Path.Combine(
                FindRepositoryRoot(),
                "src/Edulytics.Web/Views");

        var positiveTabindex =
            new Regex(
                @"\btabindex\s*=\s*[""']?\s*[1-9]",
                RegexOptions.IgnoreCase);

        var eventHandler =
            new Regex(
                @"\son[a-z]+\s*=",
                RegexOptions.IgnoreCase);

        foreach (var file in
                 Directory.EnumerateFiles(
                     root,
                     "*.cshtml",
                     SearchOption.AllDirectories))
        {
            var source =
                File.ReadAllText(file);

            Assert.DoesNotMatch(
                positiveTabindex,
                source);

            Assert.DoesNotMatch(
                eventHandler,
                source);
        }
    }

    [Fact]
    public void
        EveryImageHasAlternativeTextOrIsDecorative()
    {
        var root =
            Path.Combine(
                FindRepositoryRoot(),
                "src/Edulytics.Web/Views");

        var image =
            new Regex(
                @"<img\b[^>]*>",
                RegexOptions.IgnoreCase |
                RegexOptions.Singleline);

        var alt =
            new Regex(
                @"\balt\s*=",
                RegexOptions.IgnoreCase);

        var decorative =
            new Regex(
                @"\baria-hidden\s*=\s*[""']true[""']",
                RegexOptions.IgnoreCase);

        foreach (var file in
                 Directory.EnumerateFiles(
                     root,
                     "*.cshtml",
                     SearchOption.AllDirectories))
        {
            var source =
                File.ReadAllText(file);

            foreach (Match match in
                     image.Matches(source))
            {
                Assert.True(
                    alt.IsMatch(match.Value) ||
                    decorative.IsMatch(
                        match.Value),
                    $"Image lacks alt/aria-hidden in {file}: "
                    + match.Value);
            }
        }
    }

    [Theory]
    [InlineData(nameof(SchoolFormViewModel.Name))]
    [InlineData(nameof(SchoolFormViewModel.SchoolCode))]
    [InlineData(nameof(SchoolFormViewModel.CountryCode))]
    [InlineData(nameof(SchoolFormViewModel.City))]
    [InlineData(nameof(SchoolFormViewModel.ContactEmail))]
    [InlineData(nameof(SchoolFormViewModel.DefaultCulture))]
    [InlineData(nameof(SchoolFormViewModel.TimeZoneId))]
    [InlineData(nameof(SchoolFormViewModel.RowVersionBase64))]
    public void
        SchoolFormFields_AreNullableSoMvcDoesNotCreateImplicitRequiredErrors(
            string propertyName)
    {
        var property =
            typeof(SchoolFormViewModel)
                .GetProperty(propertyName);

        Assert.NotNull(property);

        var nullability =
            new NullabilityInfoContext()
                .Create(property!);

        Assert.Equal(
            NullabilityState.Nullable,
            nullability.ReadState);
    }

    [Fact]
    public void
        SchoolController_DelegatesMissingValuesToLocalizedServiceValidation()
    {
        var source =
            ReadSource(
                "src/Edulytics.Web/Controllers/"
                + "SchoolsController.cs");

        var requiredCoalesces =
            new[]
            {
                "model.Name ?? string.Empty",
                "model.SchoolCode ?? string.Empty",
                "model.CountryCode ?? string.Empty",
                "model.City ?? string.Empty",
                "model.ContactEmail ?? string.Empty",
                "model.DefaultCulture ?? string.Empty",
                "model.TimeZoneId ?? string.Empty"
            };

        foreach (var contract in requiredCoalesces)
        {
            Assert.Contains(
                contract,
                source,
                StringComparison.Ordinal);
        }

        Assert.DoesNotContain(
            "The SchoolCode field is required.",
            source,
            StringComparison.Ordinal);

        Assert.DoesNotContain(
            "The ContactEmail field is required.",
            source,
            StringComparison.Ordinal);
    }

    private static string ReadSource(
        string relativePath)
    {
        return File.ReadAllText(
            Path.Combine(
                FindRepositoryRoot(),
                relativePath));
    }

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

            current =
                current.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root not found.");
    }
}
