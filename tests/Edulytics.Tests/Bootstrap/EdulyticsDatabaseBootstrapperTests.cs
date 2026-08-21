using Edulytics.Core.Constants;
using Edulytics.Data.Contexts;
using Edulytics.Data.Identity;
using Edulytics.Web.Bootstrap;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Edulytics.Tests.Bootstrap;

public class EdulyticsDatabaseBootstrapperTests
{
    [Fact]
    public async Task InitializeAsync_CreatesRequiredRolesAndSuperAdmin()
    {
        var services = BuildServices();
        var bootstrapper = CreateBootstrapper(services, "superadmin@example.com", "P@ssw0rd!123");

        await bootstrapper.InitializeAsync();

        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        Assert.True(await roleManager.RoleExistsAsync(RoleNames.SuperAdmin));
        Assert.True(await roleManager.RoleExistsAsync(RoleNames.SchoolAdmin));
        Assert.True(await roleManager.RoleExistsAsync(RoleNames.SubjectSupervisor));
        Assert.True(await roleManager.RoleExistsAsync(RoleNames.Teacher));
        Assert.True(await roleManager.RoleExistsAsync(RoleNames.Student));

        var user = await userManager.FindByEmailAsync("superadmin@example.com");

        Assert.NotNull(user);
        Assert.Null(user!.SchoolId);
        Assert.True(await userManager.IsInRoleAsync(user, RoleNames.SuperAdmin));
    }

    [Fact]
    public async Task InitializeAsync_IsIdempotent_WhenRunTwice()
    {
        var services = BuildServices();
        var bootstrapper = CreateBootstrapper(services, "superadmin@example.com", "P@ssw0rd!123");

        await bootstrapper.InitializeAsync();
        await bootstrapper.InitializeAsync();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        Assert.Single(await userManager.GetUsersInRoleAsync(RoleNames.SuperAdmin));
        Assert.Equal(5, await roleManager.Roles.CountAsync());
    }

    [Fact]
    public async Task InitializeAsync_Throws_WhenConfiguredEmailBelongsToSchoolUser()
    {
        var services = BuildServices();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var schoolUser = new ApplicationUser
        {
            Email = "superadmin@example.com",
            UserName = "superadmin@example.com",
            SchoolId = Guid.NewGuid(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            IsActive = true
        };

        var createResult = await userManager.CreateAsync(schoolUser, "P@ssw0rd!123");
        Assert.True(createResult.Succeeded);

        var bootstrapper = CreateBootstrapper(services, "superadmin@example.com", "P@ssw0rd!123");

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => bootstrapper.InitializeAsync());

        Assert.Contains("school-scoped user", exception.Message, StringComparison.OrdinalIgnoreCase);

        var persistedUser = await userManager.FindByEmailAsync("superadmin@example.com");
        Assert.NotNull(persistedUser);
        Assert.NotNull(persistedUser!.SchoolId);
        Assert.False(await userManager.IsInRoleAsync(persistedUser, RoleNames.SuperAdmin));
    }

    [Fact]
    public async Task InitializeAsync_SkipsBootstrap_WhenCredentialsNotConfigured()
    {
        var services = BuildServices();
        var bootstrapper = CreateBootstrapper(services, null, null);

        // Should not throw; bootstrap is optional if credentials are not configured
        await bootstrapper.InitializeAsync();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

        // Roles are always created, but SuperAdmin user is not
        Assert.Equal(5, await roleManager.Roles.CountAsync());
        Assert.Empty(await userManager.Users.ToListAsync());
    }

    [Fact]
    public async Task InitializeAsync_AllowsNormalStartup_AfterSuperAdminCreatedAndCredentialsRemoved()
    {
        // First bootstrap: create SuperAdmin with credentials
        var services = BuildServices();
        var bootstrapper1 = CreateBootstrapper(services, "superadmin@example.com", "P@ssw0rd!123");
        await bootstrapper1.InitializeAsync();

        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var superAdminUser = await userManager.FindByEmailAsync("superadmin@example.com");
        Assert.NotNull(superAdminUser);

        // Second startup: credentials removed (simulating removing from secrets/environment)
        var bootstrapper2 = CreateBootstrapper(services, null, null);

        // Should not throw; bootstrap is idempotent and optional
        await bootstrapper2.InitializeAsync();

        // SuperAdmin should still exist with same properties
        var persistedUser = await userManager.FindByEmailAsync("superadmin@example.com");
        Assert.NotNull(persistedUser);
        Assert.Null(persistedUser!.SchoolId);
        Assert.True(await userManager.IsInRoleAsync(persistedUser, RoleNames.SuperAdmin));
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddDbContext<EdulyticsDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<EdulyticsDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }

    private static EdulyticsDatabaseBootstrapper CreateBootstrapper(ServiceProvider services, string? email, string? password)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Edulytics:SuperAdmin:Email"] = email,
                ["Edulytics:SuperAdmin:Password"] = password
            })
            .Build();

        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var db = services.GetRequiredService<EdulyticsDbContext>();

        return new EdulyticsDatabaseBootstrapper(
            roleManager,
            userManager,
            configuration,
            db);
    }
}
