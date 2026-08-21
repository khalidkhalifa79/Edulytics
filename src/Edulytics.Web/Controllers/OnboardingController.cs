using Edulytics.Services.Onboarding;
using Edulytics.Web.Resilience;
using Edulytics.Web.ViewModels.Onboarding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Route("request-demo")]
public sealed class OnboardingController : Controller
{
    private readonly ICustomerOnboardingService _onboarding;
    private readonly IStringLocalizer<OnboardingResource> _text;

    public OnboardingController(
        ICustomerOnboardingService onboarding,
        IStringLocalizer<OnboardingResource> text)
    {
        _onboarding = onboarding;
        _text = text;
    }

    [HttpGet("")]
    [AllowAnonymous]
    public IActionResult Index() =>
        View(new RequestDemoViewModel());

    [HttpPost("")]
    [AllowAnonymous]
    [EnableRateLimiting("RequestDemo")]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(
        RequestDemoViewModel model,
        CancellationToken cancellationToken)
    {
        var result = await _onboarding.SubmitDemoRequestAsync(
            new DemoRequestSubmission(
                model.SchoolName ?? string.Empty,
                model.ContactName ?? string.Empty,
                model.WorkEmail ?? string.Empty,
                model.Phone,
                model.CountryCode ?? string.Empty,
                model.City ?? string.Empty,
                model.EstimatedStudentCount,
                model.Message,
                model.PrivacyAccepted),
            cancellationToken);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(
                    error.Field,
                    _text[$"Error_{error.Code}"].Value);
            }
            return View(model);
        }

        return RedirectToAction(nameof(Thanks));
    }

    [HttpGet("thanks")]
    [AllowAnonymous]
    public IActionResult Thanks() => View();
}
