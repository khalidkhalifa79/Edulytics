using Edulytics.Web.Middleware;
using Edulytics.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Web.Controllers;

[AllowAnonymous]
[Route("system")]
public sealed class SystemStatusController
    : Controller
{
    [HttpGet("error")]
    [ResponseCache(
        Duration = 0,
        Location =
            ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        Response.StatusCode =
            StatusCodes
                .Status500InternalServerError;

        return ErrorView(
            StatusCodes
                .Status500InternalServerError);
    }

    [HttpGet("status/{code:int}")]
    [ResponseCache(
        Duration = 0,
        Location =
            ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Status(
        int code)
    {
        if (code is < 400 or > 599)
        {
            code =
                StatusCodes
                    .Status500InternalServerError;
        }

        Response.StatusCode =
            code;

        return ErrorView(
            code);
    }

    private ViewResult ErrorView(
        int statusCode)
    {
        return View(
            "~/Views/Shared/Error.cshtml",
            new ErrorViewModel
            {
                StatusCode =
                    statusCode,

                RequestId =
                    CorrelationIdMiddleware
                        .GetCorrelationId(
                            HttpContext)
            });
    }
}
