using Edulytics.Web;
using Edulytics.Data.Identity;
using Edulytics.Web.Localization;
using Edulytics.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

public sealed class AccountController : Controller
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IStringLocalizer<AccountResource> _localizer;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IStringLocalizer<AccountResource> localizer)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _localizer = localizer;
    }

    [AllowAnonymous]
    [HttpGet("/account/login")]
    public IActionResult Login(string? returnUrl = null)
    {
        if (!CultureCookie.TryRead(Request, out _))
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost("/account/login")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel model,
        string? returnUrl = null)
    {
        if (!CultureCookie.TryRead(Request, out _))
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _userManager.FindByEmailAsync(model.Email!);

        if (user is null || !user.IsActive)
        {
            AddGenericAuthenticationError();
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(
            user,
            model.Password!,
            isPersistent: false,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            AddGenericAuthenticationError();
            return View(model);
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) &&
            Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction(
            "Dashboard",
            "Platform");
    }

    [Authorize]
    [HttpPost("/account/logout")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();

        Response.Cookies.Delete(
            CultureCookie.Name,
            new CookieOptions
            {
                Path = "/"
            });

        return RedirectToAction(
            "Index",
            "Home");
    }

    private void AddGenericAuthenticationError()
    {
        ModelState.AddModelError(
            string.Empty,
            _localizer["InvalidCredentials"]);
    }
}
