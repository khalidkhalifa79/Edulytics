using Edulytics.Core.Constants;
using System.Globalization;
using Edulytics.Data.Identity;
using Edulytics.Services.Users;
using Edulytics.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

public sealed class AccountController : Controller
{
    private const string CultureCookieName =
        "Edulytics.Culture";

    private static readonly HashSet<string>
        SupportedCultures =
        new(
            ["en", "pl"],
            StringComparer.Ordinal);

    private readonly SignInManager<ApplicationUser>
        _signInManager;

    private readonly UserManager<ApplicationUser>
        _userManager;

    private readonly ISchoolUserManagementService
        _schoolUsers;

    private readonly IStringLocalizer<PlatformResource>
        _text;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ISchoolUserManagementService schoolUsers,
        IStringLocalizer<PlatformResource> text)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _schoolUsers = schoolUsers;
        _text = text;
    }

    [AllowAnonymous]
    [HttpGet("/account/login")]
    public IActionResult Login(
        string? returnUrl = null)
    {
        var culture =
            Request.Cookies[CultureCookieName];

        if (string.IsNullOrEmpty(culture))
        {
            return RedirectToAction(
                "Index",
                "Home");
        }

        ViewData["ReturnUrl"] =
            returnUrl;

        return View(
            new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost("/account/login")]
    [EnableRateLimiting("Login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        var culture =
            Request.Cookies[CultureCookieName];

        if (string.IsNullOrEmpty(culture))
        {
            return RedirectToAction(
                "Index",
                "Home");
        }

        ViewData["ReturnUrl"] =
            returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user =
            await _userManager.FindByEmailAsync(
                model.Email!);

        if (user is null)
        {
            AddInvalidCredentials();
            return View(model);
        }

        var access =
            await _schoolUsers
                .EvaluateSignInAsync(user.Id);

        if (!access.Allowed)
        {
            AddInvalidCredentials();
            return View(model);
        }

        var result =
            await _signInManager
                .PasswordSignInAsync(
                    user,
                    model.Password!,
                    isPersistent: false,
                    lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            AddInvalidCredentials();
            return View(model);
        }

        if (Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl!);
        }

        if (access.IsPlatformAdministrator)
        {
            return RedirectToAction(
                "Dashboard",
                "Platform");
        }

        if (string.Equals(
                access.Role,
                RoleNames.Student,
                StringComparison.Ordinal))
        {
            return RedirectToAction(
                "Dashboard",
                "StudentPortal");
        }

        return RedirectToAction(
            "Dashboard",
            "SchoolHome");
    }

    [AllowAnonymous]
    [HttpGet("/account/set-password")]
    public IActionResult SetPassword(
        Guid userId,
        string token,
        string culture)
    {
        culture = ApplySetupCulture(culture);

        if (userId == Guid.Empty ||
            string.IsNullOrWhiteSpace(token))
        {
            return View(
                new SetPasswordViewModel
                {
                    UserId = userId,
                    Token = token ?? string.Empty,
                    Culture = culture
                });
        }

        return View(
            new SetPasswordViewModel
            {
                UserId = userId,
                Token = token,
                Culture = culture
            });
    }

    [AllowAnonymous]
    [HttpPost("/account/set-password")]
    [IgnoreAntiforgeryToken]
    [EnableRateLimiting("PasswordSetup")]
    public async Task<IActionResult> SetPassword(
        SetPasswordViewModel model,
        CancellationToken cancellationToken)
    {
        model.Culture =
            ApplySetupCulture(model.Culture);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result =
            await _schoolUsers
                .CompletePasswordSetupAsync(
                    model.UserId,
                    model.Token,
                    model.Password,
                    cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    string.Empty,
                    _text[
                        error.Code.ToString()
                    ].Value);
            }

            return View(model);
        }

        return RedirectToAction(
            nameof(PasswordSet),
            new
            {
                culture = model.Culture
            });
    }

    [AllowAnonymous]
    [HttpGet("/account/password-set")]
    public IActionResult PasswordSet(
        string? culture)
    {
        ApplySetupCulture(culture);

        return View();
    }

    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        Response.Cookies.Delete(
            CultureCookieName);

        return RedirectToAction(
            "Index",
            "Home");
    }

    private void AddInvalidCredentials()
    {
        ModelState.AddModelError(
            string.Empty,
            _text["InvalidCredentials"].Value);
    }

    private string ApplySetupCulture(
        string? culture)
    {
        culture =
            culture?.Trim().ToLowerInvariant()
            ?? string.Empty;

        if (!SupportedCultures.Contains(culture))
        {
            var cookieCulture =
                Request.Cookies[
                    CultureCookieName]
                    ?.Trim()
                    .ToLowerInvariant();

            culture =
                !string.IsNullOrWhiteSpace(
                    cookieCulture) &&
                SupportedCultures.Contains(
                    cookieCulture)
                    ? cookieCulture
                    : "en";
        }

        var cultureInfo =
            CultureInfo.GetCultureInfo(culture);

        CultureInfo.CurrentCulture =
            cultureInfo;

        CultureInfo.CurrentUICulture =
            cultureInfo;

        Response.Cookies.Append(
            CultureCookieName,
            culture,
            new CookieOptions
            {
                Expires =
                    DateTimeOffset.UtcNow
                        .AddDays(365),
                IsEssential = true,
                HttpOnly = false,
                SameSite =
                    SameSiteMode.Strict,
                Secure =
                    Request.IsHttps
            });

        return culture;
    }
}
