using System.Net.Mail;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;
using Edulytics.Services.Auditing;

namespace Edulytics.Services.Users;

public sealed class SchoolUserManagementService
    : ISchoolUserManagementService
{
    private const int MaxEmailLength = 256;

    private static readonly HashSet<string> TenantRoles =
        new(
            [
                RoleNames.SchoolAdmin,
                RoleNames.SubjectSupervisor,
                RoleNames.Teacher,
                RoleNames.Student
            ],
            StringComparer.Ordinal);

    private readonly ISchoolUserRepository _users;
    private readonly ISchoolRepository _schools;
    private readonly IAuditService? _audit;
    private readonly IApplicationTransactionManager? _transactions;
    private readonly ICustomerOnboardingRepository? _onboarding;
    private readonly ISchoolSubscriptionRepository? _subscriptions;

    public SchoolUserManagementService(
        ISchoolUserRepository users,
        ISchoolRepository schools,
        IAuditService? audit = null,
        IApplicationTransactionManager? transactions = null,
        ICustomerOnboardingRepository? onboarding = null,
        ISchoolSubscriptionRepository? subscriptions = null)
    {
        _users = users;
        _schools = schools;
        _audit = audit;
        _transactions = transactions;
        _onboarding = onboarding;
        _subscriptions = subscriptions;
    }

    public async Task<SchoolUserQueryResult<SchoolUserListData>>
        ListAsync(
            Guid actorUserId,
            Guid? requestedSchoolId,
            CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            requestedSchoolId,
            forMutation: false,
            cancellationToken);

        if (!scope.Succeeded)
        {
            return SchoolUserQueryResult<SchoolUserListData>
                .Failure(scope.Error!.Value);
        }

        var users = await _users.ListBySchoolAsync(
            scope.School!.Id,
            cancellationToken);

        var items = users
            .Select(
                user => new SchoolUserListItem(
                    user.Id,
                    user.Email,
                    GetSingleRole(user.Roles) ?? string.Empty,
                    user.IsActive,
                    user.IsLocked,
                    user.Id == scope.Actor!.Id,
                    user.CreatedAtUtc,
                    user.UpdatedAtUtc))
            .ToArray();

        return SchoolUserQueryResult<SchoolUserListData>
            .Success(
                new SchoolUserListData(
                    BuildContext(scope),
                    items));
    }

    public async Task<
        SchoolUserQueryResult<SchoolUserManagementContext>>
        GetManagementContextAsync(
            Guid actorUserId,
            Guid? requestedSchoolId,
            CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            requestedSchoolId,
            forMutation: false,
            cancellationToken);

        return scope.Succeeded
            ? SchoolUserQueryResult<SchoolUserManagementContext>
                .Success(BuildContext(scope))
            : SchoolUserQueryResult<SchoolUserManagementContext>
                .Failure(scope.Error!.Value);
    }

    public async Task<SchoolUserQueryResult<SchoolUserDetails>>
        GetAsync(
            Guid actorUserId,
            Guid? requestedSchoolId,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            requestedSchoolId,
            forMutation: false,
            cancellationToken);

        if (!scope.Succeeded)
        {
            return SchoolUserQueryResult<SchoolUserDetails>
                .Failure(scope.Error!.Value);
        }

        var user = await _users.GetBySchoolAndIdAsync(
            scope.School!.Id,
            userId,
            cancellationToken);

        if (user is null)
        {
            return SchoolUserQueryResult<SchoolUserDetails>
                .Failure(SchoolUserErrorCode.UserNotFound);
        }

        var isSelf = user.Id == scope.Actor!.Id;

        var schoolAdminSelf =
            !scope.IsPlatformActor &&
            isSelf;

        var canModify =
            scope.School.Status != SchoolStatus.Archived &&
            !schoolAdminSelf;

        return SchoolUserQueryResult<SchoolUserDetails>
            .Success(
                new SchoolUserDetails(
                    BuildContext(scope),
                    user.Id,
                    user.Email,
                    GetSingleRole(user.Roles) ?? string.Empty,
                    user.IsActive,
                    user.IsLocked,
                    isSelf,
                    canModify,
                    user.CreatedAtUtc,
                    user.UpdatedAtUtc));
    }

    public async Task<SchoolUserCreateResult> CreateAsync(
        Guid actorUserId,
        Guid? requestedSchoolId,
        CreateSchoolUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            requestedSchoolId,
            forMutation: true,
            cancellationToken);

        if (!scope.Succeeded)
        {
            return SchoolUserCreateResult.Failure(
                string.Empty,
                scope.Error!.Value);
        }

        var email = request.Email?.Trim() ?? string.Empty;
        var role = request.Role?.Trim() ?? string.Empty;

        if (email.Length == 0)
        {
            return SchoolUserCreateResult.Failure(
                nameof(request.Email),
                SchoolUserErrorCode.UserRequiredEmail);
        }

        if (email.Length > MaxEmailLength)
        {
            return SchoolUserCreateResult.Failure(
                nameof(request.Email),
                SchoolUserErrorCode.UserEmailTooLong);
        }

        if (!IsValidEmail(email))
        {
            return SchoolUserCreateResult.Failure(
                nameof(request.Email),
                SchoolUserErrorCode.UserInvalidEmail);
        }

        if (!TenantRoles.Contains(role))
        {
            return SchoolUserCreateResult.Failure(
                nameof(request.Role),
                SchoolUserErrorCode.UserInvalidRole);
        }

        await using var auditTransaction =
            await BeginAuditTransactionAsync(
                cancellationToken);

        var write = await _users.CreateAsync(
            scope.School!.Id,
            email,
            role,
            cancellationToken);

        if (!write.Succeeded ||
            write.User is null ||
            string.IsNullOrWhiteSpace(write.PasswordSetupToken))
        {
            var mapped = MapPersistenceError(write.Error);

            return SchoolUserCreateResult.Failure(
                mapped == SchoolUserErrorCode.UserDuplicateEmail
                    ? nameof(request.Email)
                    : string.Empty,
                mapped);
        }

        if (_audit is not null)
        {
            await _audit.RecordAsync(
                new AuditEvent(
                    SchoolId: scope.School.Id,
                    Action: "SchoolUser.Created",
                    EntityType: "ApplicationUser",
                    EntityId:
                        write.User.Id.ToString("D"),
                    Feature: "UserManagement",
                    NewValues:
                        new Dictionary<string, object?>
                        {
                            ["email"] =
                                write.User.Email,
                            ["role"] =
                                GetSingleRole(
                                    write.User.Roles)
                                ?? role,
                            ["isActive"] =
                                write.User.IsActive,
                            ["isLocked"] =
                                write.User.IsLocked
                        },
                    ResultSummary:
                        "School user created.",
                    ActorUserIdOverride:
                        actorUserId,
                    ActorRoleOverride:
                        GetSingleRole(
                            scope.Actor!.Roles)
                        ?? string.Empty),
                cancellationToken);
        }

        await CommitAuditTransactionAsync(
            auditTransaction,
            cancellationToken);

        return SchoolUserCreateResult.Success(
            write.User.Id,
            write.PasswordSetupToken,
            scope.School.DefaultCulture);
    }

    public Task<SchoolUserCommandResult> SetActiveAsync(
        Guid actorUserId,
        Guid? requestedSchoolId,
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default) =>
        MutateUserAsync(
            actorUserId,
            requestedSchoolId,
            userId,
            (schoolId, token) =>
                _users.SetActiveAsync(
                    schoolId,
                    userId,
                    isActive,
                    token),
            isActive
                ? "SchoolUser.Activated"
                : "SchoolUser.Deactivated",
            isActive
                ? "School user activated."
                : "School user deactivated.",
            cancellationToken);

    public Task<SchoolUserCommandResult> SetLockedAsync(
        Guid actorUserId,
        Guid? requestedSchoolId,
        Guid userId,
        bool isLocked,
        CancellationToken cancellationToken = default) =>
        MutateUserAsync(
            actorUserId,
            requestedSchoolId,
            userId,
            (schoolId, token) =>
                _users.SetLockedAsync(
                    schoolId,
                    userId,
                    isLocked,
                    token),
            isLocked
                ? "SchoolUser.Locked"
                : "SchoolUser.Unlocked",
            isLocked
                ? "School user locked."
                : "School user unlocked.",
            cancellationToken);

    public async Task<SchoolUserCommandResult> ChangeRoleAsync(
        Guid actorUserId,
        Guid? requestedSchoolId,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default)
    {
        role = role?.Trim() ?? string.Empty;

        if (!TenantRoles.Contains(role))
        {
            return SchoolUserCommandResult.Failure(
                nameof(role),
                SchoolUserErrorCode.UserInvalidRole);
        }

        return await MutateUserAsync(
            actorUserId,
            requestedSchoolId,
            userId,
            (schoolId, token) =>
                _users.SetRoleAsync(
                    schoolId,
                    userId,
                    role,
                    token),
            "SchoolUser.RoleChanged",
            "School user role changed.",
            cancellationToken);
    }

    public async Task<SchoolUserPasswordLinkResult>
        GeneratePasswordSetupAsync(
            Guid actorUserId,
            Guid? requestedSchoolId,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            requestedSchoolId,
            forMutation: true,
            cancellationToken);

        if (!scope.Succeeded)
        {
            return SchoolUserPasswordLinkResult.Failure(
                string.Empty,
                scope.Error!.Value);
        }

        var target = await _users.GetBySchoolAndIdAsync(
            scope.School!.Id,
            userId,
            cancellationToken);

        if (target is null)
        {
            return SchoolUserPasswordLinkResult.Failure(
                string.Empty,
                SchoolUserErrorCode.UserNotFound);
        }

        if (!scope.IsPlatformActor &&
            target.Id == scope.Actor!.Id)
        {
            return SchoolUserPasswordLinkResult.Failure(
                string.Empty,
                SchoolUserErrorCode.UserCannotManageSelf);
        }

        await using var auditTransaction =
            await BeginAuditTransactionAsync(
                cancellationToken);

        var write =
            await _users.GeneratePasswordSetupAsync(
                scope.School.Id,
                userId,
                cancellationToken);

        if (!write.Succeeded ||
            write.User is null ||
            string.IsNullOrWhiteSpace(write.PasswordSetupToken))
        {
            return SchoolUserPasswordLinkResult.Failure(
                string.Empty,
                MapPersistenceError(write.Error));
        }

        if (_audit is not null)
        {
            await _audit.RecordAsync(
                new AuditEvent(
                    SchoolId: scope.School.Id,
                    Action:
                        "SchoolUser.PasswordSetupIssued",
                    EntityType:
                        "ApplicationUser",
                    EntityId:
                        write.User.Id.ToString("D"),
                    Feature:
                        "UserManagement",
                    NewValues:
                        new Dictionary<string, object?>
                        {
                            ["email"] =
                                write.User.Email,
                            ["invitationGenerated"] =
                                true
                        },
                    ResultSummary:
                        "Password setup invitation generated.",
                    ActorUserIdOverride:
                        actorUserId,
                    ActorRoleOverride:
                        GetSingleRole(
                            scope.Actor!.Roles)
                        ?? string.Empty),
                cancellationToken);
        }

        await CommitAuditTransactionAsync(
            auditTransaction,
            cancellationToken);

        return SchoolUserPasswordLinkResult.Success(
            write.User.Id,
            write.PasswordSetupToken,
            scope.School.DefaultCulture);
    }

    public async Task<SchoolUserCommandResult>
        CompletePasswordSetupAsync(
            Guid userId,
            string token,
            string newPassword,
            CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty ||
            string.IsNullOrWhiteSpace(token))
        {
            return SchoolUserCommandResult.Failure(
                string.Empty,
                SchoolUserErrorCode.UserInvalidPasswordSetup);
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return SchoolUserCommandResult.Failure(
                nameof(newPassword),
                SchoolUserErrorCode.UserPasswordPolicy);
        }

        await using var auditTransaction =
            await BeginAuditTransactionAsync(
                cancellationToken);

        var write =
            await _users.CompletePasswordSetupAsync(
                userId,
                token,
                newPassword,
                cancellationToken);

        if (!write.Succeeded)
        {
            return SchoolUserCommandResult.Failure(
                string.Empty,
                MapPersistenceError(write.Error));
        }

        if (_audit is not null &&
            write.User is not null &&
            write.User.SchoolId.HasValue)
        {
            await _audit.RecordAsync(
                new AuditEvent(
                    SchoolId:
                        write.User.SchoolId.Value,
                    Action:
                        "SchoolUser.PasswordSetupCompleted",
                    EntityType:
                        "ApplicationUser",
                    EntityId:
                        write.User.Id.ToString("D"),
                    Feature:
                        "UserManagement",
                    NewValues:
                        new Dictionary<string, object?>
                        {
                            ["passwordSetupCompleted"] =
                                true
                        },
                    ResultSummary:
                        "Password setup completed.",
                    ActorUserIdOverride:
                        write.User.Id,
                    ActorRoleOverride:
                        GetSingleRole(
                            write.User.Roles)
                        ?? string.Empty),
                cancellationToken);
        }

        await CommitAuditTransactionAsync(
            auditTransaction,
            cancellationToken);

        return SchoolUserCommandResult.Success();
    }

    public async Task<SchoolUserSignInDecision>
        EvaluateSignInAsync(
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        var user = await _users.GetActorAsync(
            userId,
            cancellationToken);

        if (user is null ||
            !user.IsActive ||
            user.IsLocked)
        {
            return Denied();
        }

        var role = GetSingleRole(user.Roles);

        if (user.SchoolId is null)
        {
            return role == RoleNames.SuperAdmin
                ? new SchoolUserSignInDecision(
                    true,
                    true,
                    null,
                    role)
                : Denied();
        }

        if (role is null ||
            !TenantRoles.Contains(role))
        {
            return Denied();
        }

        var school = await _schools.GetByIdAsync(
            user.SchoolId.Value,
            cancellationToken);

        if (school is null ||
            school.Status != SchoolStatus.Active)
        {
            return Denied();
        }

        if (_onboarding is not null)
        {
            var demo = await _onboarding.GetDemoAccessBySchoolAsync(
                school.Id,
                cancellationToken);

            if (demo is not null &&
                demo.ConvertedAtUtc is null &&
                (demo.RevokedAtUtc.HasValue ||
                 demo.StartsAtUtc > DateTime.UtcNow ||
                 demo.ExpiresAtUtc <= DateTime.UtcNow))
            {
                return Denied();
            }
        }

        if (!await HasCommercialOperationalAccessAsync(
                school.Id,
                user.Id,
                role,
                cancellationToken))
        {
            return Denied();
        }

        return new SchoolUserSignInDecision(
            true,
            false,
            school.Id,
            role);
    }

    public async Task<bool> CanManageUsersAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateSignInAsync(
            actorUserId,
            cancellationToken);

        if (!decision.Allowed)
            return false;

        if (decision.IsPlatformAdministrator)
            return true;

        return decision.Role == RoleNames.SchoolAdmin;
    }

    public async Task<SchoolUserActorContext?>
        GetActorContextAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default)
    {
        var decision = await EvaluateSignInAsync(
            actorUserId,
            cancellationToken);

        if (!decision.Allowed ||
            decision.IsPlatformAdministrator ||
            !decision.SchoolId.HasValue ||
            string.IsNullOrWhiteSpace(decision.Role))
        {
            return null;
        }

        var school = await _schools.GetByIdAsync(
            decision.SchoolId.Value,
            cancellationToken);

        if (school is null)
        {
            return null;
        }

        return new SchoolUserActorContext(
            actorUserId,
            school.Id,
            school.Name,
            decision.Role,
            decision.Role == RoleNames.SchoolAdmin);
    }

    private async Task<SchoolUserCommandResult>
        MutateUserAsync(
            Guid actorUserId,
            Guid? requestedSchoolId,
            Guid targetUserId,
            Func<Guid, CancellationToken,
                Task<SchoolUserPersistenceResult>> operation,
            string auditAction,
            string auditResultSummary,
            CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            requestedSchoolId,
            forMutation: true,
            cancellationToken);

        if (!scope.Succeeded)
        {
            return SchoolUserCommandResult.Failure(
                string.Empty,
                scope.Error!.Value);
        }

        var target = await _users.GetBySchoolAndIdAsync(
            scope.School!.Id,
            targetUserId,
            cancellationToken);

        if (target is null)
        {
            return SchoolUserCommandResult.Failure(
                string.Empty,
                SchoolUserErrorCode.UserNotFound);
        }

        if (!scope.IsPlatformActor &&
            target.Id == scope.Actor!.Id)
        {
            return SchoolUserCommandResult.Failure(
                string.Empty,
                SchoolUserErrorCode.UserCannotManageSelf);
        }

        await using var auditTransaction =
            await BeginAuditTransactionAsync(
                cancellationToken);

        var write = await operation(
            scope.School.Id,
            cancellationToken);

        if (!write.Succeeded ||
            write.User is null)
        {
            return SchoolUserCommandResult.Failure(
                string.Empty,
                MapPersistenceError(write.Error));
        }

        if (_audit is not null)
        {
            await _audit.RecordAsync(
                new AuditEvent(
                    SchoolId:
                        scope.School.Id,
                    Action:
                        auditAction,
                    EntityType:
                        "ApplicationUser",
                    EntityId:
                        targetUserId.ToString("D"),
                    Feature:
                        "UserManagement",
                    OldValues:
                        new Dictionary<string, object?>
                        {
                            ["role"] =
                                GetSingleRole(
                                    target.Roles)
                                ?? string.Empty,
                            ["isActive"] =
                                target.IsActive,
                            ["isLocked"] =
                                target.IsLocked
                        },
                    NewValues:
                        new Dictionary<string, object?>
                        {
                            ["role"] =
                                GetSingleRole(
                                    write.User.Roles)
                                ?? string.Empty,
                            ["isActive"] =
                                write.User.IsActive,
                            ["isLocked"] =
                                write.User.IsLocked
                        },
                    ResultSummary:
                        auditResultSummary,
                    ActorUserIdOverride:
                        actorUserId,
                    ActorRoleOverride:
                        GetSingleRole(
                            scope.Actor!.Roles)
                        ?? string.Empty),
                cancellationToken);
        }

        await CommitAuditTransactionAsync(
            auditTransaction,
            cancellationToken);

        return SchoolUserCommandResult.Success();
    }

    private async Task<IApplicationTransaction?>
        BeginAuditTransactionAsync(
            CancellationToken cancellationToken)
    {
        if (_transactions is null)
            return null;

        return await _transactions.BeginAsync(
            cancellationToken);
    }

    private static Task CommitAuditTransactionAsync(
        IApplicationTransaction? transaction,
        CancellationToken cancellationToken) =>
        transaction is null
            ? Task.CompletedTask
            : transaction.CommitAsync(
                cancellationToken);

    private async Task<ScopeResult> ResolveScopeAsync(
        Guid actorUserId,
        Guid? requestedSchoolId,
        bool forMutation,
        CancellationToken cancellationToken)
    {
        var actor = await _users.GetActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked)
        {
            return ScopeResult.Fail(
                SchoolUserErrorCode.UserAccessDenied);
        }

        var actorRole = GetSingleRole(actor.Roles);

        var isPlatformActor =
            actor.SchoolId is null &&
            actorRole == RoleNames.SuperAdmin;

        var isSchoolAdmin =
            actor.SchoolId.HasValue &&
            actorRole == RoleNames.SchoolAdmin;

        Guid targetSchoolId;

        if (isPlatformActor)
        {
            if (!requestedSchoolId.HasValue)
            {
                return ScopeResult.Fail(
                    SchoolUserErrorCode.UserAccessDenied);
            }

            targetSchoolId =
                requestedSchoolId.Value;
        }
        else if (isSchoolAdmin)
        {
            targetSchoolId =
                actor.SchoolId!.Value;

            if (requestedSchoolId.HasValue &&
                requestedSchoolId.Value != targetSchoolId)
            {
                return ScopeResult.Fail(
                    SchoolUserErrorCode.UserAccessDenied);
            }
        }
        else
        {
            return ScopeResult.Fail(
                SchoolUserErrorCode.UserAccessDenied);
        }

        var school = await _schools.GetByIdAsync(
            targetSchoolId,
            cancellationToken);

        if (school is null)
        {
            return ScopeResult.Fail(
                SchoolUserErrorCode.SchoolNotFound);
        }

        if (!isPlatformActor &&
            school.Status != SchoolStatus.Active)
        {
            return ScopeResult.Fail(
                SchoolUserErrorCode.UserAccessDenied);
        }

        if (!isPlatformActor &&
            !await HasCommercialOperationalAccessAsync(
                school.Id,
                actor.Id,
                actorRole!,
                cancellationToken))
        {
            return ScopeResult.Fail(
                SchoolUserErrorCode.UserAccessDenied);
        }

        if (forMutation &&
            school.Status == SchoolStatus.Archived)
        {
            return ScopeResult.Fail(
                SchoolUserErrorCode.UserSchoolArchived);
        }

        return ScopeResult.Ok(
            actor,
            school,
            isPlatformActor);
    }

    private static SchoolUserManagementContext BuildContext(
        ScopeResult scope) =>
        new(
            scope.School!.Id,
            scope.School.Name,
            scope.School.Status,
            scope.School.Status != SchoolStatus.Archived,
            scope.IsPlatformActor);

    private static string? GetSingleRole(
        IReadOnlyList<string> roles) =>
        roles.Count == 1
            ? roles[0]
            : null;

    private static bool IsValidEmail(string email)
    {
        try
        {
            var parsed = new MailAddress(email);

            return string.Equals(
                parsed.Address,
                email,
                StringComparison.OrdinalIgnoreCase);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private async Task<bool> HasCommercialOperationalAccessAsync(
        Guid schoolId,
        Guid userId,
        string role,
        CancellationToken cancellationToken)
    {
        if (_subscriptions is null)
            return true;

        var subscription =
            await _subscriptions.GetBySchoolAsync(
                schoolId,
                cancellationToken);

        if (subscription is null)
            return true;

        var now = DateTime.UtcNow;

        var operational =
            subscription.Status ==
                SubscriptionStatus.Active &&
            subscription.CurrentTermStartsAtUtc.HasValue &&
            subscription.CurrentTermEndsAtUtc.HasValue &&
            subscription.CurrentTermStartsAtUtc.Value <= now &&
            now < subscription.CurrentTermEndsAtUtc.Value;

        if (!operational)
            return false;

        if (role != RoleNames.Student)
            return true;

        return await _subscriptions
            .HasActiveStudentProfileForUserAsync(
                schoolId,
                userId,
                cancellationToken);
    }

    private static SchoolUserErrorCode MapPersistenceError(
        SchoolUserPersistenceError error) =>
        error switch
        {
            SchoolUserPersistenceError.DuplicateEmail =>
                SchoolUserErrorCode.UserDuplicateEmail,

            SchoolUserPersistenceError.NotFound =>
                SchoolUserErrorCode.UserNotFound,

            SchoolUserPersistenceError.InvalidToken =>
                SchoolUserErrorCode.UserInvalidPasswordSetup,

            SchoolUserPersistenceError.PasswordPolicy =>
                SchoolUserErrorCode.UserPasswordPolicy,

            SchoolUserPersistenceError.Conflict =>
                SchoolUserErrorCode.UserIdentityConflict,

            _ =>
                SchoolUserErrorCode.UserPersistenceError
        };

    private static SchoolUserSignInDecision Denied() =>
        new(
            false,
            false,
            null,
            null);

    private sealed record ScopeResult(
        bool Succeeded,
        SchoolUserRecord? Actor,
        School? School,
        bool IsPlatformActor,
        SchoolUserErrorCode? Error)
    {
        public static ScopeResult Ok(
            SchoolUserRecord actor,
            School school,
            bool isPlatformActor) =>
            new(
                true,
                actor,
                school,
                isPlatformActor,
                null);

        public static ScopeResult Fail(
            SchoolUserErrorCode error) =>
            new(
                false,
                null,
                null,
                false,
                error);
    }
}
