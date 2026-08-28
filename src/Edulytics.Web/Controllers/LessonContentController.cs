using System.Globalization;
using System.Security.Claims;
using Edulytics.Core.Constants;
using Edulytics.Services.LessonContent;
using Edulytics.Web.ViewModels.LessonContent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Edulytics.Web.Controllers;

[Authorize(Roles=RoleNames.SchoolAdmin+","+RoleNames.SubjectSupervisor+","+RoleNames.Teacher)]
[Route("lesson-content")]
public sealed class LessonContentController : Controller
{
    private readonly ILessonContentService _lessons;
    public LessonContentController(ILessonContentService lessons)=>_lessons=lessons;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if(!TryActor(out var actorId))return Forbid();
        var r=await _lessons.GetDashboardAsync(actorId,cancellationToken);
        return r.Value is null?HandleError(r.Error):View(new LessonContentIndexViewModel(r.Value));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id,CancellationToken cancellationToken)
    {
        if(!TryActor(out var actorId))return Forbid();
        var r=await _lessons.GetStaffLessonAsync(actorId,id,CultureInfo.CurrentUICulture.Name,cancellationToken);
        return r.Value is null?HandleError(r.Error):View(new LessonContentDetailViewModel(r.Value));
    }

    private IActionResult HandleError(LessonContentErrorCode? error)=>
        error is LessonContentErrorCode.AccessDenied or LessonContentErrorCode.SchoolNotActive?Forbid():NotFound();

    private bool TryActor(out Guid actorUserId)=>Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier),out actorUserId);
}
