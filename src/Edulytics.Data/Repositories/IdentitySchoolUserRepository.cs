using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;
using Edulytics.Data.Contexts;
using Edulytics.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class IdentitySchoolUserRepository
    : ISchoolUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly EdulyticsDbContext _context;

    public IdentitySchoolUserRepository(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        EdulyticsDbContext context)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _context = context;
    }

    public async Task<SchoolUserRecord?> GetActorAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == userId,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var tracked = await _userManager.FindByIdAsync(
            user.Id.ToString());

        if (tracked is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(
            tracked);

        return ToRecord(
            tracked,
            roles);
    }

    public async Task<IReadOnlyList<SchoolUserRecord>>
        ListBySchoolAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default)
    {
        var users = await _context.Users
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Email)
            .ToArrayAsync(cancellationToken);

        if (users.Length == 0)
        {
            return [];
        }

        var userIds = users
            .Select(x => x.Id)
            .ToArray();

        var roleRows =
            await (
                from userRole in _context.UserRoles
                join role in _context.Roles
                    on userRole.RoleId equals role.Id
                where userIds.Contains(userRole.UserId)
                select new
                {
                    userRole.UserId,
                    RoleName = role.Name!
                })
            .AsNoTracking()
            .ToArrayAsync(cancellationToken);

        var rolesByUser = roleRows
            .GroupBy(x => x.UserId)
            .ToDictionary(
                group => group.Key,
                group => group
                    .Select(x => x.RoleName)
                    .OrderBy(x => x)
                    .ToArray());

        return users
            .Select(
                user => ToRecord(
                    user,
                    rolesByUser.TryGetValue(
                        user.Id,
                        out var roles)
                        ? roles
                        : []))
            .ToArray();
    }

    public async Task<SchoolUserRecord?>
        GetBySchoolAndIdAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        var user = await _userManager.Users
            .SingleOrDefaultAsync(
                x =>
                    x.Id == userId &&
                    x.SchoolId == schoolId,
                cancellationToken);

        if (user is null)
        {
            return null;
        }

        var roles = await _userManager.GetRolesAsync(
            user);

        return ToRecord(
            user,
            roles);
    }

    public async Task<SchoolUserPersistenceResult>
        CreateAsync(
            Guid schoolId,
            string email,
            string role,
            CancellationToken cancellationToken = default)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            return SchoolUserPersistenceResult.Failure(
                SchoolUserPersistenceError.RoleFailure);
        }

        var now = DateTime.UtcNow;

        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Email = email,
            UserName = email,
            IsActive = true,
            LockoutEnabled = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        var create =
            await _userManager.CreateAsync(user);

        if (!create.Succeeded)
        {
            return SchoolUserPersistenceResult.Failure(
                MapIdentityErrors(create));
        }

        var roleResult =
            await _userManager.AddToRoleAsync(
                user,
                role);

        if (!roleResult.Succeeded)
        {
            await _userManager.DeleteAsync(user);

            return SchoolUserPersistenceResult.Failure(
                SchoolUserPersistenceError.RoleFailure);
        }

        var token =
            await _userManager
                .GeneratePasswordResetTokenAsync(user);

        var roles =
            await _userManager.GetRolesAsync(user);

        return SchoolUserPersistenceResult.Success(
            ToRecord(user, roles),
            token);
    }

    public async Task<SchoolUserPersistenceResult>
        SetActiveAsync(
            Guid schoolId,
            Guid userId,
            bool isActive,
            CancellationToken cancellationToken = default)
    {
        var user = await FindTenantUserAsync(
            schoolId,
            userId,
            cancellationToken);

        if (user is null)
        {
            return SchoolUserPersistenceResult.Failure(
                SchoolUserPersistenceError.NotFound);
        }

        user.IsActive = isActive;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var update =
            await _userManager.UpdateAsync(user);

        if (!update.Succeeded)
        {
            return SchoolUserPersistenceResult.Failure(
                MapIdentityErrors(update));
        }

        if (!isActive)
        {
            var stamp =
                await _userManager
                    .UpdateSecurityStampAsync(user);

            if (!stamp.Succeeded)
            {
                return SchoolUserPersistenceResult.Failure(
                    MapIdentityErrors(stamp));
            }
        }

        return await SuccessForUserAsync(user);
    }

    public async Task<SchoolUserPersistenceResult>
        SetLockedAsync(
            Guid schoolId,
            Guid userId,
            bool isLocked,
            CancellationToken cancellationToken = default)
    {
        var user = await FindTenantUserAsync(
            schoolId,
            userId,
            cancellationToken);

        if (user is null)
        {
            return SchoolUserPersistenceResult.Failure(
                SchoolUserPersistenceError.NotFound);
        }

        IdentityResult lockResult;

        if (isLocked)
        {
            lockResult =
                await _userManager.SetLockoutEndDateAsync(
                    user,
                    DateTimeOffset.UtcNow.AddYears(100));
        }
        else
        {
            lockResult =
                await _userManager.SetLockoutEndDateAsync(
                    user,
                    null);

            if (lockResult.Succeeded)
            {
                lockResult =
                    await _userManager
                        .ResetAccessFailedCountAsync(user);
            }
        }

        if (!lockResult.Succeeded)
        {
            return SchoolUserPersistenceResult.Failure(
                MapIdentityErrors(lockResult));
        }

        user.UpdatedAtUtc = DateTime.UtcNow;

        var update =
            await _userManager.UpdateAsync(user);

        if (!update.Succeeded)
        {
            return SchoolUserPersistenceResult.Failure(
                MapIdentityErrors(update));
        }

        if (isLocked)
        {
            var stamp =
                await _userManager
                    .UpdateSecurityStampAsync(user);

            if (!stamp.Succeeded)
            {
                return SchoolUserPersistenceResult.Failure(
                    MapIdentityErrors(stamp));
            }
        }

        return await SuccessForUserAsync(user);
    }

    public async Task<SchoolUserPersistenceResult>
        SetRoleAsync(
            Guid schoolId,
            Guid userId,
            string role,
            CancellationToken cancellationToken = default)
    {
        if (!await _roleManager.RoleExistsAsync(role))
        {
            return SchoolUserPersistenceResult.Failure(
                SchoolUserPersistenceError.RoleFailure);
        }

        var user = await FindTenantUserAsync(
            schoolId,
            userId,
            cancellationToken);

        if (user is null)
        {
            return SchoolUserPersistenceResult.Failure(
                SchoolUserPersistenceError.NotFound);
        }

        var oldRoles =
            await _userManager.GetRolesAsync(user);

        if (oldRoles.Count == 1 &&
            oldRoles[0] == role)
        {
            return SchoolUserPersistenceResult.Success(
                ToRecord(user, oldRoles));
        }

        if (oldRoles.Count > 0)
        {
            var remove =
                await _userManager.RemoveFromRolesAsync(
                    user,
                    oldRoles);

            if (!remove.Succeeded)
            {
                return SchoolUserPersistenceResult.Failure(
                    MapIdentityErrors(remove));
            }
        }

        var add =
            await _userManager.AddToRoleAsync(
                user,
                role);

        if (!add.Succeeded)
        {
            foreach (var oldRole in oldRoles)
            {
                await _userManager.AddToRoleAsync(
                    user,
                    oldRole);
            }

            return SchoolUserPersistenceResult.Failure(
                SchoolUserPersistenceError.RoleFailure);
        }

        user.UpdatedAtUtc = DateTime.UtcNow;

        var update =
            await _userManager.UpdateAsync(user);

        if (!update.Succeeded)
        {
            return SchoolUserPersistenceResult.Failure(
                MapIdentityErrors(update));
        }

        var stamp =
            await _userManager.UpdateSecurityStampAsync(
                user);

        if (!stamp.Succeeded)
        {
            return SchoolUserPersistenceResult.Failure(
                MapIdentityErrors(stamp));
        }

        return await SuccessForUserAsync(user);
    }

    public async Task<SchoolUserPersistenceResult>
        GeneratePasswordSetupAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default)
    {
        var user = await FindTenantUserAsync(
            schoolId,
            userId,
            cancellationToken);

        if (user is null)
        {
            return SchoolUserPersistenceResult.Failure(
                SchoolUserPersistenceError.NotFound);
        }

        // Generating a password setup/reset email must not
        // disable an existing password. The current password
        // remains valid until ResetPasswordAsync succeeds.
        user.UpdatedAtUtc = DateTime.UtcNow;

        var update =
            await _userManager.UpdateAsync(user);

        if (!update.Succeeded)
        {
            return SchoolUserPersistenceResult.Failure(
                MapIdentityErrors(update));
        }

        var token =
            await _userManager
                .GeneratePasswordResetTokenAsync(user);

        var roles =
            await _userManager.GetRolesAsync(user);

        return SchoolUserPersistenceResult.Success(
            ToRecord(user, roles),
            token);
    }

    public async Task<SchoolUserPersistenceResult>
        CompletePasswordSetupAsync(
            Guid userId,
            string token,
            string newPassword,
            CancellationToken cancellationToken = default)
    {
        var user =
            await _userManager.FindByIdAsync(
                userId.ToString());

        if (user is null ||
            !user.SchoolId.HasValue)
        {
            return SchoolUserPersistenceResult.Failure(
                SchoolUserPersistenceError.InvalidToken);
        }

        var reset =
            await _userManager.ResetPasswordAsync(
                user,
                token,
                newPassword);

        if (!reset.Succeeded)
        {
            var invalidToken =
                reset.Errors.Any(
                    x => x.Code.Contains(
                        "InvalidToken",
                        StringComparison.OrdinalIgnoreCase));

            return SchoolUserPersistenceResult.Failure(
                invalidToken
                    ? SchoolUserPersistenceError.InvalidToken
                    : SchoolUserPersistenceError.PasswordPolicy);
        }

        user.UpdatedAtUtc = DateTime.UtcNow;

        var update =
            await _userManager.UpdateAsync(user);

        if (!update.Succeeded)
        {
            return SchoolUserPersistenceResult.Failure(
                MapIdentityErrors(update));
        }

        return await SuccessForUserAsync(user);
    }

    private Task<ApplicationUser?>
        FindTenantUserAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken) =>
        _userManager.Users.SingleOrDefaultAsync(
            x =>
                x.Id == userId &&
                x.SchoolId == schoolId,
            cancellationToken);

    private async Task<SchoolUserPersistenceResult>
        SuccessForUserAsync(
            ApplicationUser user)
    {
        var roles =
            await _userManager.GetRolesAsync(user);

        return SchoolUserPersistenceResult.Success(
            ToRecord(user, roles));
    }

    private static SchoolUserRecord ToRecord(
        ApplicationUser user,
        IEnumerable<string> roles) =>
        new(
            user.Id,
            user.SchoolId,
            user.Email ?? string.Empty,
            user.IsActive,
            user.LockoutEnabled &&
            user.LockoutEnd.HasValue &&
            user.LockoutEnd.Value > DateTimeOffset.UtcNow,
            user.CreatedAtUtc,
            user.UpdatedAtUtc,
            roles.ToArray());

    private static SchoolUserPersistenceError
        MapIdentityErrors(
            IdentityResult result)
    {
        if (result.Errors.Any(
                x =>
                    x.Code.Equals(
                        "DuplicateEmail",
                        StringComparison.OrdinalIgnoreCase) ||
                    x.Code.Equals(
                        "DuplicateUserName",
                        StringComparison.OrdinalIgnoreCase)))
        {
            return SchoolUserPersistenceError.DuplicateEmail;
        }

        if (result.Errors.Any(
                x => x.Code.Equals(
                    "ConcurrencyFailure",
                    StringComparison.OrdinalIgnoreCase)))
        {
            return SchoolUserPersistenceError.Conflict;
        }

        return SchoolUserPersistenceError.IdentityFailure;
    }
}
