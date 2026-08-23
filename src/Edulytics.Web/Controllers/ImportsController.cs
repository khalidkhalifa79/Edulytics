using System.Security.Claims;
using System.Text;
using Edulytics.Core.Enums;
using Edulytics.Services.Imports;
using Edulytics.Web.ViewModels.Imports;
using Edulytics.Web.Resilience;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "DataImport")]
public sealed class ImportsController
    : Controller
{
    private readonly IDataImportService _imports;
    private readonly IStringLocalizer<
        ImportResource> _text;

    public ImportsController(
        IDataImportService imports,
        IStringLocalizer<ImportResource> text)
    {
        _imports = imports;
        _text = text;
    }

    [HttpGet("/school/imports")]
    public async Task<IActionResult> Index(
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result =
            await _imports.GetWorkspaceAsync(
                actorId,
                cancellationToken);

        if (!result.Succeeded)
            return Failure(result.Error);

        return View(
            new ImportIndexViewModel(
                result.Value!));
    }

    [HttpGet("/school/imports/{batchId:guid}")]
    public async Task<IActionResult> Details(
        Guid batchId,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        var result =
            await _imports.GetBatchAsync(
                actorId,
                batchId,
                cancellationToken);

        if (!result.Succeeded)
            return Failure(result.Error);

        return View(
            new ImportDetailsViewModel(
                result.Value!));
    }

    [HttpPost("/school/imports/upload")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(
        ImportFileParser.MaxBytes + 65536)]
    [RequestTimeout(BackendResiliencePolicyNames.Import)]
    [EnableRateLimiting(BackendResiliencePolicyNames.ImportConcurrency)]
    public async Task<IActionResult> Upload(
        ImportType importType,
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        if (!Enum.IsDefined(importType))
            return BadRequest();

        if (file is null ||
            file.Length <= 0)
        {
            TempData["ImportError"] =
                _text["ErrorEmptyFile"].Value;

            return RedirectToAction(
                nameof(Index));
        }

        if (file.Length >
            ImportFileParser.MaxBytes)
        {
            TempData["ImportError"] =
                _text["ErrorFileTooLarge"].Value;

            return RedirectToAction(
                nameof(Index));
        }

        await using var stream =
            new MemoryStream();

        await file.CopyToAsync(
            stream,
            cancellationToken);

        var result =
            await _imports.UploadAsync(
                actorId,
                importType,
                file.FileName,
                stream.ToArray(),
                cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error ==
                ImportErrorCode.AccessDenied)
            {
                return Forbid();
            }

            TempData["ImportError"] =
                _text[
                    ErrorResourceKey(
                        result.Error)]
                    .Value;

            return RedirectToAction(
                nameof(Index));
        }

        TempData["ImportSuccess"] =
            _text["SuccessUploaded"].Value;

        return RedirectToAction(
            nameof(Details),
            new
            {
                batchId =
                    result.Value!.Id
            });
    }

    [HttpPost("/school/imports/{batchId:guid}/confirm")]
    [ValidateAntiForgeryToken]
    [RequestTimeout(BackendResiliencePolicyNames.Import)]
    [EnableRateLimiting(BackendResiliencePolicyNames.ImportConcurrency)]
    public async Task<IActionResult> Confirm(
        Guid batchId,
        string? rowVersion,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        if (!TryRowVersion(
                rowVersion,
                out var bytes))
        {
            TempData["ImportError"] =
                _text[
                    "ErrorConcurrencyConflict"]
                    .Value;

            return RedirectToAction(
                nameof(Details),
                new { batchId });
        }

        var result =
            await _imports.ConfirmAsync(
                actorId,
                batchId,
                bytes,
                cancellationToken);

        if (!result.Succeeded)
        {
            if (result.Error ==
                ImportErrorCode.AccessDenied)
            {
                return Forbid();
            }

            TempData["ImportError"] =
                _text[
                    ErrorResourceKey(
                        result.Error)]
                    .Value;

            return RedirectToAction(
                nameof(Details),
                new { batchId });
        }

        TempData["ImportSuccess"] =
            _text["SuccessCompleted"].Value;

        return RedirectToAction(
            nameof(Details),
            new
            {
                batchId =
                    result.Value!.Id
            });
    }

    [HttpGet("/school/imports/template/{importType}")]
    public async Task<IActionResult> Template(
        ImportType importType,
        CancellationToken cancellationToken)
    {
        if (!TryActor(out var actorId))
            return Forbid();

        if (!Enum.IsDefined(importType))
            return NotFound();

        var workspace =
            await _imports.GetWorkspaceAsync(
                actorId,
                cancellationToken);

        if (!workspace.Succeeded ||
            !workspace.Value!.AllowedTypes
                .Any(x =>
                    x.Type == importType))
        {
            return Forbid();
        }

        var headers =
            _imports.GetTemplateHeaders(
                importType);

        var content =
            string.Join(
                ",",
                headers)
            + Environment.NewLine;

        return File(
            Encoding.UTF8.GetBytes(
                content),
            "text/csv",
            $"edulytics-{importType}.csv");
    }

    private bool TryActor(
        out Guid actorId) =>
        Guid.TryParse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier),
            out actorId);

    private IActionResult Failure(
        ImportErrorCode? error) =>
        error switch
        {
            ImportErrorCode.BatchNotFound =>
                NotFound(),

            _ =>
                Forbid()
        };

    private static bool TryRowVersion(
        string? value,
        out byte[] bytes)
    {
        bytes = [];

        if (string.IsNullOrWhiteSpace(
                value))
        {
            return false;
        }

        try
        {
            bytes =
                Convert.FromBase64String(
                    value);

            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string ErrorResourceKey(
        ImportErrorCode? error) =>
        error switch
        {
            ImportErrorCode.AccessDenied =>
                "ErrorAccessDenied",

            ImportErrorCode.SchoolNotActive =>
                "ErrorSchoolNotActive",

            ImportErrorCode.UnsupportedFile =>
                "ErrorUnsupportedFile",

            ImportErrorCode.InvalidFile =>
                "ErrorInvalidFile",

            ImportErrorCode.FileTooLarge =>
                "ErrorFileTooLarge",

            ImportErrorCode.TooManyRows =>
                "ErrorTooManyRows",

            ImportErrorCode.TooManyColumns =>
                "ErrorTooManyColumns",

            ImportErrorCode.DuplicateHeader =>
                "ErrorDuplicateHeader",

            ImportErrorCode.EmptyFile =>
                "ErrorEmptyFile",

            ImportErrorCode.BatchNotFound =>
                "ErrorBatchNotFound",

            ImportErrorCode.BatchHasErrors =>
                "ErrorBatchHasErrors",

            ImportErrorCode.BatchStateChanged =>
                "ErrorBatchStateChanged",

            ImportErrorCode.ConcurrencyConflict =>
                "ErrorConcurrencyConflict",

            ImportErrorCode.SeatLimitReached =>
                "ErrorSeatLimitReached",

            _ =>
                "ErrorPersistence"
        };
}
