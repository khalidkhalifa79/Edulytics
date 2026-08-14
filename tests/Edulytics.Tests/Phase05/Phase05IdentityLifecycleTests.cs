using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Users;
using Edulytics.Data.Contexts;
using Edulytics.Data.Identity;
using Edulytics.Data.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Edulytics.Tests.Phase05;

public sealed class Phase05IdentityLifecycleTests
{
    private const string PasswordOne =
        "Acceptance@Test2026";

    private const string PasswordTwo =
        "Changed@Test2026";

    [Fact]
    public async Task CompleteUserLifecycle_WorksWithRealIdentity()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var services = scope.ServiceProvider;

        var context =
            services.GetRequiredService<
                EdulyticsDbContext>();

        var roleManager =
            services.GetRequiredService<
                RoleManager<ApplicationRole>>();

        var userManager =
            services.GetRequiredService<
                UserManager<ApplicationUser>>();

        await EnsureRolesAsync(roleManager);

        var school = NewSchool();

        context.Schools.Add(school);
        await context.SaveChangesAsync();

        var repository =
            new IdentitySchoolUserRepository(
                userManager,
                roleManager,
                context);

        var create =
            await repository.CreateAsync(
                school.Id,
                "lifecycle@example.com",
                RoleNames.Teacher);

        Assert.True(create.Succeeded);
        Assert.NotNull(create.User);
        Assert.False(
            string.IsNullOrWhiteSpace(
                create.PasswordSetupToken));

        var userId = create.User!.Id;

        var user =
            await userManager.FindByIdAsync(
                userId.ToString());

        Assert.NotNull(user);
        Assert.False(
            await userManager.HasPasswordAsync(
                user!));

        var setup =
            await repository
                .CompletePasswordSetupAsync(
                    userId,
                    create.PasswordSetupToken!,
                    PasswordOne);

        Assert.True(setup.Succeeded);

        user =
            await userManager.FindByIdAsync(
                userId.ToString());

        Assert.NotNull(user);

        Assert.True(
            await userManager.CheckPasswordAsync(
                user!,
                PasswordOne));

        var roleChange =
            await repository.SetRoleAsync(
                school.Id,
                userId,
                RoleNames.SubjectSupervisor);

        Assert.True(roleChange.Succeeded);

        user =
            await userManager.FindByIdAsync(
                userId.ToString());

        Assert.NotNull(user);

        var roles =
            await userManager.GetRolesAsync(user!);

        Assert.Equal(
            RoleNames.SubjectSupervisor,
            Assert.Single(roles));

        var deactivate =
            await repository.SetActiveAsync(
                school.Id,
                userId,
                false);

        Assert.True(deactivate.Succeeded);
        Assert.False(
            deactivate.User!.IsActive);

        var activate =
            await repository.SetActiveAsync(
                school.Id,
                userId,
                true);

        Assert.True(activate.Succeeded);
        Assert.True(
            activate.User!.IsActive);

        var lockResult =
            await repository.SetLockedAsync(
                school.Id,
                userId,
                true);

        Assert.True(lockResult.Succeeded);
        Assert.True(
            lockResult.User!.IsLocked);

        var unlockResult =
            await repository.SetLockedAsync(
                school.Id,
                userId,
                false);

        Assert.True(unlockResult.Succeeded);
        Assert.False(
            unlockResult.User!.IsLocked);

        var reset =
            await repository
                .GeneratePasswordSetupAsync(
                    school.Id,
                    userId);

        Assert.True(reset.Succeeded);
        Assert.False(
            string.IsNullOrWhiteSpace(
                reset.PasswordSetupToken));

        user =
            await userManager.FindByIdAsync(
                userId.ToString());

        Assert.NotNull(user);

        Assert.True(
            await userManager.HasPasswordAsync(
                user!));

        Assert.True(
            await userManager.CheckPasswordAsync(
                user!,
                PasswordOne));

        var setNewPassword =
            await repository
                .CompletePasswordSetupAsync(
                    userId,
                    reset.PasswordSetupToken!,
                    PasswordTwo);

        Assert.True(setNewPassword.Succeeded);

        user =
            await userManager.FindByIdAsync(
                userId.ToString());

        Assert.NotNull(user);

        Assert.True(
            await userManager.CheckPasswordAsync(
                user!,
                PasswordTwo));

        Assert.False(
            await userManager.CheckPasswordAsync(
                user!,
                PasswordOne));
    }

    [Fact]
    public async Task DuplicateEmail_IsRejectedByIdentityPersistence()
    {
        using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        var services = scope.ServiceProvider;

        var context =
            services.GetRequiredService<
                EdulyticsDbContext>();

        var roleManager =
            services.GetRequiredService<
                RoleManager<ApplicationRole>>();

        var userManager =
            services.GetRequiredService<
                UserManager<ApplicationUser>>();

        await EnsureRolesAsync(roleManager);

        var school = NewSchool();

        context.Schools.Add(school);
        await context.SaveChangesAsync();

        var repository =
            new IdentitySchoolUserRepository(
                userManager,
                roleManager,
                context);

        var first =
            await repository.CreateAsync(
                school.Id,
                "duplicate@example.com",
                RoleNames.Teacher);

        Assert.True(first.Succeeded);

        var duplicate =
            await repository.CreateAsync(
                school.Id,
                "DUPLICATE@example.com",
                RoleNames.Student);

        Assert.False(duplicate.Succeeded);

        Assert.Equal(
            SchoolUserPersistenceError.DuplicateEmail,
            duplicate.Error);
    }

    private static ServiceProvider BuildProvider()
    {
        var services =
            new ServiceCollection();

        services.AddLogging();
        services.AddDataProtection();

        services.AddDbContext<
            EdulyticsDbContext>(
            options =>
                options.UseInMemoryDatabase(
                    Guid.NewGuid()
                        .ToString("N")));

        services
            .AddIdentityCore<ApplicationUser>(
                options =>
                {
                    options.User.RequireUniqueEmail =
                        true;

                    options.Password.RequiredLength =
                        12;

                    options.Password
                        .RequiredUniqueChars =
                        4;

                    options.Password.RequireDigit =
                        true;

                    options.Password.RequireLowercase =
                        true;

                    options.Password.RequireUppercase =
                        true;

                    options.Password
                        .RequireNonAlphanumeric =
                        true;

                    options.Lockout
                        .AllowedForNewUsers =
                        true;
                })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<
                EdulyticsDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }

    private static async Task EnsureRolesAsync(
        RoleManager<ApplicationRole> manager)
    {
        foreach (var role in new[]
                 {
                     RoleNames.SchoolAdmin,
                     RoleNames.SubjectSupervisor,
                     RoleNames.Teacher,
                     RoleNames.Student
                 })
        {
            if (!await manager.RoleExistsAsync(role))
            {
                var result =
                    await manager.CreateAsync(
                        new ApplicationRole
                        {
                            Name = role
                        });

                Assert.True(result.Succeeded);
            }
        }
    }

    private static School NewSchool()
    {
        var code =
            $"ACC-{Guid.NewGuid():N}"[..12]
                .ToUpperInvariant();

        return new School
        {
            Id = Guid.NewGuid(),
            Name = "Acceptance School",
            SchoolCode = code,
            NormalizedSchoolCode = code,
            Status = SchoolStatus.Active,
            CountryCode = "PL",
            City = "Warsaw",
            ContactEmail =
                $"{Guid.NewGuid():N}@example.com",
            DefaultCulture = "en",
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion =
                BitConverter.GetBytes(1L)
        };
    }
}
