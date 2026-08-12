using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "PlatformAdministration")]
public sealed class PlatformController : Controller
{
    [HttpGet("/platform/dashboard")]
    public IActionResult Dashboard()
    {
        return View();
    }
}
