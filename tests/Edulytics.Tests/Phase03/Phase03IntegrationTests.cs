using System.Net;
using System.Text.RegularExpressions;
using Edulytics.Core.Constants;
using Edulytics.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Edulytics.Tests.Phase03;

public sealed class Phase03IntegrationTests
    : IClassFixture<EdulyticsWebFactory>
{
    private const string TestPassword =
        "Phase03!Test123Aa";

    private readonly EdulyticsWebFactory _factory;

    public Phase03IntegrationTests(
        EdulyticsWebFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Selector_RendersOnlyApprovedEntryContent()
    {
        using var client = CreateClient();

        var response = await client.GetAsync("/");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        Assert.Contains("Edulytics", html);
        Assert.Contains("Polski", html);
        Assert.Contains("English", html);

        Assert.DoesNotContain(">Home<", html);
        Assert.DoesNotContain(">Privacy<", html);
        Assert.DoesNotContain(">Register<", html);
    }

    [Theory]
    [InlineData("en", "Sign in", "Zaloguj się")]
    [InlineData("pl", "Zaloguj się", "Sign in")]
    public async Task Login_RendersOnlySelectedLanguage(
        string culture,
        string expected,
        string forbidden)
    {
        using var client = CreateClient();

        await SelectCultureAsync(
            client,
            culture);

        var response =
            await client.GetAsync("/account/login");

        var html =
            WebUtility.HtmlDecode(
                await response.Content.ReadAsStringAsync());

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Contains(expected, html);
        Assert.DoesNotContain(forbidden, html);
    }

    [Fact]
    public async Task PublicCss_IsAvailableWithoutAuthentication()
    {
        using var client = CreateClient();

        var response =
            await client.GetAsync("/css/site.css");

        var css =
            await response.Content.ReadAsStringAsync();

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Contains("--ed-bg", css);
    }

    [Fact]
    public async Task LoginWithoutCulture_RedirectsToSelector()
    {
        using var client = CreateClient();

        var response =
            await client.GetAsync("/account/login");

        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);

        Assert.Equal(
            "/",
            response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task UnsupportedCulture_IsRejected()
    {
        using var client = CreateClient();

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/");

        var response =
            await client.PostAsync(
                "/set-culture",
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["culture"] = "fr",
                        ["__RequestVerificationToken"] =
                            token
                    }));

        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);

        Assert.Equal(
            "/",
            response.Headers.Location?.OriginalString);

        var login =
            await client.GetAsync(
                "/account/login");

        Assert.Equal(
            HttpStatusCode.Redirect,
            login.StatusCode);

        Assert.Equal(
            "/",
            login.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData(
        "en",
        "Email address is required.",
        "Password is required.")]
    [InlineData(
        "pl",
        "Adres e-mail jest wymagany.",
        "Hasło jest wymagane.")]
    public async Task LoginValidation_IsLocalized(
        string culture,
        string emailMessage,
        string passwordMessage)
    {
        using var client = CreateClient();

        await SelectCultureAsync(
            client,
            culture);

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/account/login");

        var response =
            await client.PostAsync(
                "/account/login",
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["Email"] = "",
                        ["Password"] = "",
                        ["__RequestVerificationToken"] =
                            token
                    }));

        var html =
            WebUtility.HtmlDecode(
                await response.Content.ReadAsStringAsync());

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Contains(emailMessage, html);
        Assert.Contains(passwordMessage, html);
    }

    [Fact]
    public async Task ValidPlatformSuperAdmin_CanAccessDashboard()
    {
        var email =
            $"super-{Guid.NewGuid():N}@example.test";

        await CreateUserAsync(
            email,
            schoolId: null,
            addSuperAdminRole: true);

        using var client = CreateClient();

        await LoginAsync(
            client,
            "en",
            email);

        var response =
            await client.GetAsync(
                "/platform/dashboard");

        var html =
            WebUtility.HtmlDecode(
                await response.Content.ReadAsStringAsync());

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);

        Assert.Contains(
            "Platform dashboard",
            html);
    }

    [Fact]
    public async Task SchoolScopedSuperAdminRole_IsRejectedAtLogin()
    {
        var email =
            $"school-super-{Guid.NewGuid():N}@example.test";

        await CreateUserAsync(
            email,
            Guid.NewGuid(),
            addSuperAdminRole: true);

        using var client = CreateClient();

        await LoginRejectedAsync(
            client,
            "en",
            email);

        var response =
            await client.GetAsync(
                "/platform/dashboard");

        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);

        Assert.NotNull(response.Headers.Location);

        var loginPath =
            response.Headers.Location.IsAbsoluteUri
                ? response.Headers.Location.AbsolutePath
                : response.Headers.Location.OriginalString
                    .Split('?', 2)[0];

        Assert.Equal(
            "/account/login",
            loginPath);
    }

    [Fact]
    public async Task SchoolScopedUserWithoutTenantRole_IsRejectedAtLogin()
    {
        var email =
            $"teacher-{Guid.NewGuid():N}@example.test";

        await CreateUserAsync(
            email,
            Guid.NewGuid(),
            addSuperAdminRole: false);

        using var client = CreateClient();

        await LoginRejectedAsync(
            client,
            "en",
            email);

        var response =
            await client.GetAsync(
                "/platform/dashboard");

        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);

        Assert.NotNull(response.Headers.Location);

        var loginPath =
            response.Headers.Location.IsAbsoluteUri
                ? response.Headers.Location.AbsolutePath
                : response.Headers.Location.OriginalString
                    .Split('?', 2)[0];

        Assert.Equal(
            "/account/login",
            loginPath);
    }

    [Fact]
    public async Task Logout_ClearsAuthAndCultureFlow()
    {
        var email =
            $"logout-{Guid.NewGuid():N}@example.test";

        await CreateUserAsync(
            email,
            schoolId: null,
            addSuperAdminRole: true);

        using var client = CreateClient();

        await LoginAsync(
            client,
            "en",
            email);

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/platform/dashboard");

        var logout =
            await client.PostAsync(
                "/account/logout",
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["__RequestVerificationToken"] =
                            token
                    }));

        Assert.Equal(
            HttpStatusCode.Redirect,
            logout.StatusCode);

        Assert.Equal(
            "/",
            logout.Headers.Location?.OriginalString);

        var login =
            await client.GetAsync(
                "/account/login");

        Assert.Equal(
            HttpStatusCode.Redirect,
            login.StatusCode);

        Assert.Equal(
            "/",
            login.Headers.Location?.OriginalString);
    }

    [Fact]
    public void RegistrationAction_DoesNotExist()
    {
        var registerActions =
            typeof(Edulytics.Web.Controllers.AccountController)
                .GetMethods()
                .Where(method =>
                    string.Equals(
                        method.Name,
                        "Register",
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

        Assert.Empty(registerActions);
    }

    private HttpClient CreateClient()
    {
        return _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = true
            });
    }

    private static async Task SelectCultureAsync(
        HttpClient client,
        string culture)
    {
        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/");

        var response =
            await client.PostAsync(
                "/set-culture",
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["culture"] = culture,
                        ["__RequestVerificationToken"] =
                            token
                    }));

        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);

        Assert.Equal(
            "/account/login",
            response.Headers.Location?.OriginalString);
    }

    private static async Task LoginAsync(
        HttpClient client,
        string culture,
        string email)
    {
        await SelectCultureAsync(
            client,
            culture);

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/account/login");

        var response =
            await client.PostAsync(
                "/account/login",
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["Email"] = email,
                        ["Password"] = TestPassword,
                        ["__RequestVerificationToken"] =
                            token
                    }));

        Assert.Equal(
            HttpStatusCode.Redirect,
            response.StatusCode);
    }

    private static async Task LoginRejectedAsync(
        HttpClient client,
        string culture,
        string email)
    {
        await SelectCultureAsync(
            client,
            culture);

        var token =
            await GetAntiforgeryTokenAsync(
                client,
                "/account/login");

        var response =
            await client.PostAsync(
                "/account/login",
                new FormUrlEncodedContent(
                    new Dictionary<string, string>
                    {
                        ["Email"] = email,
                        ["Password"] = TestPassword,
                        ["__RequestVerificationToken"] =
                            token
                    }));

        Assert.Equal(
            HttpStatusCode.OK,
            response.StatusCode);
    }

    private async Task CreateUserAsync(
        string email,
        Guid? schoolId,
        bool addSuperAdminRole)
    {
        using var scope =
            _factory.Services.CreateScope();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<
                    RoleManager<ApplicationRole>>();

        if (addSuperAdminRole &&
            !await roleManager.RoleExistsAsync(
                RoleNames.SuperAdmin))
        {
            var roleResult =
                await roleManager.CreateAsync(
                    new ApplicationRole
                    {
                        Name = RoleNames.SuperAdmin
                    });

            Assert.True(
                roleResult.Succeeded);
        }

        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = email,
                UserName = email,
                EmailConfirmed = true,
                IsActive = true,
                SchoolId = schoolId,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            };

        var create =
            await userManager.CreateAsync(
                user,
                TestPassword);

        Assert.True(
            create.Succeeded,
            string.Join(
                "; ",
                create.Errors.Select(
                    x => x.Description)));

        if (addSuperAdminRole)
        {
            var role =
                await userManager.AddToRoleAsync(
                    user,
                    RoleNames.SuperAdmin);

            Assert.True(
                role.Succeeded);
        }
    }

    private static async Task<string>
        GetAntiforgeryTokenAsync(
            HttpClient client,
            string path)
    {
        var response =
            await client.GetAsync(path);

        var html =
            WebUtility.HtmlDecode(
                await response.Content.ReadAsStringAsync());

        var match =
            Regex.Match(
                html,
                "name=\"__RequestVerificationToken\"" +
                "[^>]*value=\"([^\"]+)\"",
                RegexOptions.IgnoreCase);

        Assert.True(
            match.Success,
            $"Anti-forgery token not found on {path}.");

        return WebUtility.HtmlDecode(
            match.Groups[1].Value);
    }
}
