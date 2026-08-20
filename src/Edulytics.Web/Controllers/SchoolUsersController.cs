using System.Security.Claims;
using Edulytics.Core.Constants;
using Edulytics.Services.Users;
using Edulytics.Web.Email;
using Edulytics.Web.ViewModels.SchoolUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Timeouts;
using Edulytics.Web.Resilience;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Localization;

namespace Edulytics.Web.Controllers;

[Authorize(Policy = "UserManagement")]
[Route("School/Users")]
public sealed class SchoolUsersController : Controller
{
    private readonly ISchoolUserManagementService _users;
    private readonly IStringLocalizer<PlatformResource> _text;
    private readonly IUserInvitationDeliveryService _invitations;

    public SchoolUsersController(
        ISchoolUserManagementService users,
        IStringLocalizer<PlatformResource> text,
        IUserInvitationDeliveryService invitations)
    {
        _users = users;
        _text = text;
        _invitations = invitations;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(
        Guid? schoolId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Forbid();
        }

        var result = await _users.ListAsync(
            actorUserId,
            schoolId,
            cancellationToken);

        if (result.Value is null)
        {
            return QueryFailure(result.Error);
        }

        return View(
            new SchoolUserListViewModel
            {
                Context = result.Value.Context,
                Users = result.Value.Users
            });
    }

    [HttpGet("Create")]
    public async Task<IActionResult> Create(
        Guid? schoolId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Forbid();
        }

        var context =
            await _users.GetManagementContextAsync(
                actorUserId,
                schoolId,
                cancellationToken);

        if (context.Value is null)
        {
            return QueryFailure(context.Error);
        }

        if (!context.Value.CanMutate)
        {
            TempData["SchoolUserError"] =
                _text["UserSchoolArchived"].Value;

            return RedirectToAction(
                nameof(Index),
                new
                {
                    schoolId =
                        context.Value.SchoolId
                });
        }

        return View(
            new SchoolUserCreateViewModel
            {
                SchoolId =
                    context.Value.SchoolId,
                Role =
                    RoleNames.Teacher,
                RoleOptions =
                    BuildRoleOptions()
            });
    }

    [HttpPost("Create")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("SchoolUserCreate")]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    public async Task<IActionResult> Create(
        SchoolUserCreateViewModel model,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Forbid();
        }

        model.RoleOptions = BuildRoleOptions();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = await _users.CreateAsync(
            actorUserId,
            model.SchoolId,
            new CreateSchoolUserRequest(
                model.Email,
                model.Role),
            cancellationToken);

        if (!result.Succeeded)
        {
            if (TryReturnSecurityFailure(
                    result.Errors,
                    out var failure))
            {
                return failure;
            }

            AddErrors(result.Errors);

            return View(model);
        }

        var recipientEmail =
            model.Email?.Trim() ?? string.Empty;

        var invitationCulture =
            GetInvitationCulture();

        var link = BuildPasswordSetupLink(
            result.UserId,
            result.PasswordSetupToken,
            invitationCulture);

        var context =
            await _users.GetManagementContextAsync(
                actorUserId,
                model.SchoolId,
                cancellationToken);

        var delivered = false;

        if (link is not null &&
            context.Value is not null)
        {
            var delivery =
                await _invitations.SendAsync(
                    new UserInvitationDeliveryRequest(
                        recipientEmail,
                        context.Value.SchoolName,
                        invitationCulture,
                        link,
                        "initial"),
                    cancellationToken);

            delivered = delivery.Succeeded;
        }

        if (delivered)
        {
            TempData["SchoolUserSuccess"] =
                _text[
                    "CreateUserInvitationSentSuccess",
                    recipientEmail
                ].Value;
        }
        else
        {
            TempData["SchoolUserSuccess"] =
                _text["CreateUserSuccess"].Value;

            TempData["SchoolUserError"] =
                _text[
                    "InvitationDeliveryFailed"
                ].Value;
        }

        return RedirectToAction(
            nameof(Details),
            new
            {
                id = result.UserId,
                schoolId = model.SchoolId
            });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Details(
        Guid id,
        Guid? schoolId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Forbid();
        }

        var result = await _users.GetAsync(
            actorUserId,
            schoolId,
            id,
            cancellationToken);

        if (result.Value is null)
        {
            return QueryFailure(result.Error);
        }

        return View(
            new SchoolUserDetailsViewModel
            {
                User = result.Value,
                RoleOptions =
                    BuildRoleOptions()
            });
    }

    [HttpPost("{id:guid}/Active")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetActive(
        Guid id,
        Guid schoolId,
        bool isActive,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Forbid();
        }

        var result =
            await _users.SetActiveAsync(
                actorUserId,
                schoolId,
                id,
                isActive,
                cancellationToken);

        if (!result.Succeeded)
        {
            return CommandFailure(
                result.Errors,
                id,
                schoolId);
        }

        TempData["SchoolUserSuccess"] =
            _text[
                isActive
                    ? "ActivateUserSuccess"
                    : "DeactivateUserSuccess"
            ].Value;

        return RedirectToAction(
            nameof(Details),
            new
            {
                id,
                schoolId
            });
    }

    [HttpPost("{id:guid}/Lock")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetLocked(
        Guid id,
        Guid schoolId,
        bool isLocked,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Forbid();
        }

        var result =
            await _users.SetLockedAsync(
                actorUserId,
                schoolId,
                id,
                isLocked,
                cancellationToken);

        if (!result.Succeeded)
        {
            return CommandFailure(
                result.Errors,
                id,
                schoolId);
        }

        TempData["SchoolUserSuccess"] =
            _text[
                isLocked
                    ? "LockUserSuccess"
                    : "UnlockUserSuccess"
            ].Value;

        return RedirectToAction(
            nameof(Details),
            new
            {
                id,
                schoolId
            });
    }

    [HttpPost("{id:guid}/Role")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeRole(
        Guid id,
        Guid schoolId,
        string role,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Forbid();
        }

        var result =
            await _users.ChangeRoleAsync(
                actorUserId,
                schoolId,
                id,
                role,
                cancellationToken);

        if (!result.Succeeded)
        {
            return CommandFailure(
                result.Errors,
                id,
                schoolId);
        }

        TempData["SchoolUserSuccess"] =
            _text["RoleChangeSuccess"].Value;

        return RedirectToAction(
            nameof(Details),
            new
            {
                id,
                schoolId
            });
    }

    [HttpPost("{id:guid}/Password-Link")]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("InvitationResend")]
    [RequestTimeout(BackendResiliencePolicyNames.InteractiveWrite)]
    public async Task<IActionResult> PasswordLink(
        Guid id,
        Guid schoolId,
        CancellationToken cancellationToken)
    {
        if (!TryGetActorId(out var actorUserId))
        {
            return Forbid();
        }

        var target =
            await _users.GetAsync(
                actorUserId,
                schoolId,
                id,
                cancellationToken);

        if (target.Value is null)
        {
            return QueryFailure(
                target.Error);
        }

        var result =
            await _users.GeneratePasswordSetupAsync(
                actorUserId,
                schoolId,
                id,
                cancellationToken);

        if (!result.Succeeded)
        {
            return CommandFailure(
                result.Errors,
                id,
                schoolId);
        }

        var invitationCulture =
            GetInvitationCulture();

        var link = BuildPasswordSetupLink(
            result.UserId,
            result.PasswordSetupToken,
            invitationCulture);

        UserInvitationDeliveryResult? delivery =
            null;

        if (link is not null)
        {
            delivery =
                await _invitations.SendAsync(
                    new UserInvitationDeliveryRequest(
                        target.Value.Email,
                        target.Value.Context.SchoolName,
                        invitationCulture,
                        link,
                        "resend"),
                    cancellationToken);
        }

        if (delivery?.Succeeded == true)
        {
            TempData["SchoolUserSuccess"] =
                _text[
                    "InvitationResentSuccess"
                ].Value;
        }
        else
        {
            TempData["SchoolUserError"] =
                _text[
                    "InvitationDeliveryFailed"
                ].Value;
        }

        return RedirectToAction(
            nameof(Details),
            new
            {
                id,
                schoolId
            });
    }

    private static string GetInvitationCulture()
    {
        var culture =
            System.Globalization.CultureInfo
                .CurrentUICulture
                .TwoLetterISOLanguageName;

        return string.Equals(
            culture,
            "pl",
            StringComparison.OrdinalIgnoreCase)
                ? "pl"
                : "en";
    }

    private string? BuildPasswordSetupLink(
        Guid? userId,
        string? token,
        string? culture)
    {
        if (!userId.HasValue ||
            userId.Value == Guid.Empty ||
            string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        return Url.Action(
            "SetPassword",
            "Account",
            new
            {
                userId = userId.Value,
                token,
                culture =
                    string.IsNullOrWhiteSpace(culture)
                        ? "en"
                        : culture
            },
            Request.Scheme);
    }

    private IActionResult CommandFailure(
        IReadOnlyList<SchoolUserError> errors,
        Guid userId,
        Guid schoolId)
    {
        if (TryReturnSecurityFailure(
                errors,
                out var failure))
        {
            return failure;
        }

        var first = errors.FirstOrDefault();

        TempData["SchoolUserError"] =
            first is null
                ? _text["UserPersistenceError"].Value
                : _text[first.Code.ToString()].Value;

        return RedirectToAction(
            nameof(Details),
            new
            {
                id = userId,
                schoolId
            });
    }

    private IActionResult QueryFailure(
        SchoolUserErrorCode? error) =>
        error switch
        {
            SchoolUserErrorCode.SchoolNotFound =>
                NotFound(),

            SchoolUserErrorCode.UserNotFound =>
                NotFound(),

            _ =>
                Forbid()
        };

    private bool TryReturnSecurityFailure(
        IReadOnlyList<SchoolUserError> errors,
        out IActionResult result)
    {
        var error =
            errors.FirstOrDefault()?.Code;

        if (error ==
            SchoolUserErrorCode.UserAccessDenied)
        {
            result = Forbid();
            return true;
        }

        if (error ==
                SchoolUserErrorCode.SchoolNotFound ||
            error ==
                SchoolUserErrorCode.UserNotFound)
        {
            result = NotFound();
            return true;
        }

        result = null!;
        return false;
    }

    private void AddErrors(
        IReadOnlyList<SchoolUserError> errors)
    {
        foreach (var error in errors)
        {
            ModelState.AddModelError(
                error.Field,
                _text[error.Code.ToString()].Value);
        }
    }

    private bool TryGetActorId(
        out Guid actorUserId) =>
        Guid.TryParse(
            User.FindFirstValue(
                ClaimTypes.NameIdentifier),
            out actorUserId);

    private static IReadOnlyList<
        SchoolUserRoleOptionViewModel>
        BuildRoleOptions() =>
        [
            new(
                RoleNames.SchoolAdmin,
                "RoleSchoolAdmin"),

            new(
                RoleNames.SubjectSupervisor,
                "RoleSubjectSupervisor"),

            new(
                RoleNames.Teacher,
                "RoleTeacher"),

            new(
                RoleNames.Student,
                "RoleStudent")
        ];
}
