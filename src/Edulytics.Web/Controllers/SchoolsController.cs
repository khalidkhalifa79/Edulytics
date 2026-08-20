using Edulytics.Core.Enums;
using Edulytics.Services.Schools;
using Edulytics.Web.Localization;
using Edulytics.Web.ViewModels.Schools;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "PlatformAdministration")]
[Route("Platform/Schools")]
public sealed class SchoolsController : Controller
{
    private readonly ISchoolManagementService _schoolService;
    private readonly IStringLocalizer<Edulytics.Web.PlatformResource> _text;

    public SchoolsController(
        ISchoolManagementService schoolService,
        IStringLocalizer<Edulytics.Web.PlatformResource> text)
    {
        _schoolService = schoolService;
        _text = text;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        var schools = await _schoolService.ListAsync(cancellationToken);

        var model = new SchoolListViewModel
        {
            Schools = schools
                .Select(SchoolListRowViewModel.FromService)
                .ToArray()
        };

        return View(model);
    }

    [HttpGet("Create")]
    public IActionResult Create()
    {
        var culture =
            System.Globalization.CultureInfo
                .CurrentUICulture
                .TwoLetterISOLanguageName;

        var model = new SchoolFormViewModel
        {
            CountryCode = "PL",
            DefaultCulture =
                culture is "pl" ? "pl" : "en",
            TimeZoneId = "Europe/Warsaw"
        };

        return View(model);
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        SchoolFormViewModel model,
        CancellationToken cancellationToken)
    {
        var result = await _schoolService.CreateAsync(
            new CreateSchoolRequest(
                model.Name ?? string.Empty,
                model.SchoolCode ?? string.Empty,
                model.CountryCode ?? string.Empty,
                model.City ?? string.Empty,
                model.ContactEmail ?? string.Empty,
                model.DefaultCulture ?? string.Empty,
                model.TimeZoneId ?? string.Empty),
            cancellationToken);

        if (!result.Succeeded)
        {
            AddErrors(result);
            return View(model);
        }

        TempData["SchoolSuccess"] = _text["CreateSuccess"].Value;

        return RedirectToAction(
            nameof(Details),
            new { id = result.SchoolId });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(
        Guid id,
        CancellationToken cancellationToken)
    {
        var school = await _schoolService.GetAsync(
            id,
            cancellationToken);

        if (school is null)
        {
            return NotFound();
        }

        return View(
            SchoolDetailsViewModel.FromService(school));
    }

    [HttpGet("{id:guid}/Edit")]
    public async Task<IActionResult> Edit(
        Guid id,
        CancellationToken cancellationToken)
    {
        var school = await _schoolService.GetAsync(
            id,
            cancellationToken);

        if (school is null)
        {
            return NotFound();
        }

        if (!school.CanEdit)
        {
            TempData["SchoolError"] =
                _text["ArchivedCannotEdit"].Value;

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var model = new SchoolFormViewModel
        {
            Id = school.Id,
            Name = school.Name,
            SchoolCode = school.SchoolCode,
            CountryCode = school.CountryCode,
            City = school.City,
            ContactEmail = school.ContactEmail,
            DefaultCulture = school.DefaultCulture,
            TimeZoneId = school.TimeZoneId,
            RowVersionBase64 =
                Convert.ToBase64String(school.RowVersion)
        };

        return View(model);
    }

    [HttpPost("{id:guid}/Edit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        Guid id,
        SchoolFormViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                model.RowVersionBase64,
                out var rowVersion))
        {
            ModelState.AddModelError(
                string.Empty,
                _text["ConcurrencyConflict"].Value);

            model.Id = id;
            return View(model);
        }

        var result = await _schoolService.UpdateAsync(
            new UpdateSchoolRequest(
                id,
                model.Name ?? string.Empty,
                model.CountryCode ?? string.Empty,
                model.City ?? string.Empty,
                model.ContactEmail ?? string.Empty,
                model.DefaultCulture ?? string.Empty,
                model.TimeZoneId ?? string.Empty,
                rowVersion),
            cancellationToken);

        if (!result.Succeeded)
        {
            AddErrors(result);
            model.Id = id;
            return View(model);
        }

        TempData["SchoolSuccess"] = _text["UpdateSuccess"].Value;

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    [HttpPost("{id:guid}/Suspend")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Suspend(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ChangeStatus(
            id,
            rowVersion,
            SchoolStatus.Suspended,
            "SuspendSuccess",
            cancellationToken);

    [HttpPost("{id:guid}/Reactivate")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Reactivate(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ChangeStatus(
            id,
            rowVersion,
            SchoolStatus.Active,
            "ReactivateSuccess",
            cancellationToken);

    [HttpPost("{id:guid}/Archive")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Archive(
        Guid id,
        string rowVersion,
        CancellationToken cancellationToken) =>
        ChangeStatus(
            id,
            rowVersion,
            SchoolStatus.Archived,
            "ArchiveSuccess",
            cancellationToken);

    private async Task<IActionResult> ChangeStatus(
        Guid id,
        string rowVersionValue,
        SchoolStatus targetStatus,
        string successKey,
        CancellationToken cancellationToken)
    {
        if (!TryDecodeRowVersion(
                rowVersionValue,
                out var rowVersion))
        {
            TempData["SchoolError"] =
                _text["ConcurrencyConflict"].Value;

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        var result = await _schoolService.ChangeStatusAsync(
            new SchoolStatusChangeRequest(
                id,
                targetStatus,
                rowVersion),
            cancellationToken);

        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault();

            if (firstError?.Code == SchoolErrorCode.SchoolNotFound)
            {
                return NotFound();
            }

            TempData["SchoolError"] =
                firstError is null
                    ? _text["PersistenceError"].Value
                    : _text[firstError.Code.ToString()].Value;

            return RedirectToAction(
                nameof(Details),
                new { id });
        }

        TempData["SchoolSuccess"] = _text[successKey].Value;

        return RedirectToAction(
            nameof(Details),
            new { id });
    }

    private void AddErrors(SchoolCommandResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(
                error.Field,
                _text[error.Code.ToString()].Value);
        }
    }

    private static bool TryDecodeRowVersion(
        string? value,
        out byte[] rowVersion)
    {
        rowVersion = Array.Empty<byte>();

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        try
        {
            rowVersion = Convert.FromBase64String(value);
            return rowVersion.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
