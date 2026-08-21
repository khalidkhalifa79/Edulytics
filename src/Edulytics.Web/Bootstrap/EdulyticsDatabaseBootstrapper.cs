using Edulytics.Core.Constants;
using Edulytics.Data.Contexts;
using Edulytics.Data.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Web.Bootstrap;

public sealed class EdulyticsDatabaseBootstrapper
{
    private static readonly string[] RequiredRoleNames =
    [
        RoleNames.SuperAdmin,
        RoleNames.SchoolAdmin,
        RoleNames.SubjectSupervisor,
        RoleNames.Teacher,
        RoleNames.Student
    ];

    private const long BootstrapAdvisoryLockKey =
        25000025L;

    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly EdulyticsDbContext _db;

    public EdulyticsDatabaseBootstrapper(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        EdulyticsDbContext db)
    {
        _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
        _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public async Task InitializeAsync()
    {
        if (!_db.Database.IsNpgsql())
        {
            await EnsureRolesExistAsync();
            await EnsureSuperAdminAsync();
            return;
        }

        await _db.Database.OpenConnectionAsync();

        try
        {
            await ExecuteAdvisoryLockAsync(
                acquire: true);

            try
            {
                await EnsureRolesExistAsync();
                await EnsureSuperAdminAsync();
            }
            finally
            {
                await ExecuteAdvisoryLockAsync(
                    acquire: false);
            }
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }
    }

    private async Task ExecuteAdvisoryLockAsync(
        bool acquire)
    {
        await using var command =
            _db.Database
                .GetDbConnection()
                .CreateCommand();

        command.CommandText =
            acquire
                ? $"SELECT pg_advisory_lock({BootstrapAdvisoryLockKey});"
                : $"SELECT pg_advisory_unlock({BootstrapAdvisoryLockKey});";

        _ = await command.ExecuteScalarAsync();
    }

    private async Task EnsureRolesExistAsync()
    {
        foreach (var roleName in RequiredRoleNames)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                var role = new ApplicationRole { Name = roleName };
                var result = await _roleManager.CreateAsync(role);

                if (!result.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to create role '{roleName}': {string.Join("; ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }

    private async Task EnsureSuperAdminAsync()
    {
        var superAdminEmail = _configuration["Edulytics:SuperAdmin:Email"];
        var superAdminPassword = _configuration["Edulytics:SuperAdmin:Password"];

        // If no bootstrap credentials are configured, skip SuperAdmin provisioning.
        // This allows the application to run normally after SuperAdmin has been created once.
        // It also prevents accidental startup failures due to missing environment/secret configuration.
        if (string.IsNullOrWhiteSpace(superAdminEmail) || string.IsNullOrWhiteSpace(superAdminPassword))
        {
            return;
        }

        var existingUser = await _userManager.FindByEmailAsync(superAdminEmail.Trim());

        if (existingUser is not null)
        {
            if (existingUser.SchoolId is not null)
            {
                throw new InvalidOperationException(
                    $"Configured SuperAdmin email '{superAdminEmail}' already belongs to a school-scoped user. Refusing to modify that account or remove SchoolId.");
            }

            if (!await _userManager.IsInRoleAsync(existingUser, RoleNames.SuperAdmin))
            {
                var roleResult = await _userManager.AddToRoleAsync(existingUser, RoleNames.SuperAdmin);
                if (!roleResult.Succeeded)
                {
                    throw new InvalidOperationException($"Failed to add existing SuperAdmin user to role '{RoleNames.SuperAdmin}': {string.Join("; ", roleResult.Errors.Select(e => e.Description))}");
                }
            }

            return;
        }

        // Create SuperAdmin only if email and password are both provided and user does not exist.
        var user = new ApplicationUser
        {
            UserName = superAdminEmail.Trim(),
            Email = superAdminEmail.Trim(),
            EmailConfirmed = true,
            IsActive = true,
            SchoolId = null,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var createResult = await _userManager.CreateAsync(user, superAdminPassword);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Failed to create SuperAdmin user: {string.Join("; ", createResult.Errors.Select(e => e.Description))}");
        }

        var roleResultForNewUser = await _userManager.AddToRoleAsync(user, RoleNames.SuperAdmin);
        if (!roleResultForNewUser.Succeeded)
        {
            throw new InvalidOperationException($"Failed to add created SuperAdmin user to role '{RoleNames.SuperAdmin}': {string.Join("; ", roleResultForNewUser.Errors.Select(e => e.Description))}");
        }
    }
}
