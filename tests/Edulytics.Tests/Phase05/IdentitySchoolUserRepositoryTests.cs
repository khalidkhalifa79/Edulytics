using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Data.Contexts;
using Edulytics.Data.Identity;
using Edulytics.Data.Repositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Edulytics.Tests.Phase05;

public sealed class IdentitySchoolUserRepositoryTests
{
    [Fact]
    public async Task CreateAsync_PersistsSchoolRoleAndSetupToken()
    {
        using var provider = BuildProvider();

        using var scope =
            provider.CreateScope();

        var services =
            scope.ServiceProvider;

        var context =
            services.GetRequiredService<
                EdulyticsDbContext>();

        var roleManager =
            services.GetRequiredService<
                RoleManager<ApplicationRole>>();

        var userManager =
            services.GetRequiredService<
                UserManager<ApplicationUser>>();

        foreach (var role in new[]
                 {
                     RoleNames.SchoolAdmin,
                     RoleNames.SubjectSupervisor,
                     RoleNames.Teacher,
                     RoleNames.Student
                 })
        {
            await roleManager.CreateAsync(
                new ApplicationRole
                {
                    Name = role
                });
        }

        var school = NewSchool();

        context.Schools.Add(school);
        await context.SaveChangesAsync();

        var repository =
            new IdentitySchoolUserRepository(
                userManager,
                roleManager,
                context);

        var result =
            await repository.CreateAsync(
                school.Id,
                "teacher@example.com",
                RoleNames.Teacher);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.User);

        Assert.Equal(
            school.Id,
            result.User!.SchoolId);

        Assert.Equal(
            RoleNames.Teacher,
            Assert.Single(result.User.Roles));

        Assert.False(
            string.IsNullOrWhiteSpace(
                result.PasswordSetupToken));

        var persisted =
            await userManager.FindByEmailAsync(
                "teacher@example.com");

        Assert.NotNull(persisted);
        Assert.False(
            await userManager.HasPasswordAsync(
                persisted!));
    }

    [Fact]
    public async Task GetBySchoolAndIdAsync_DoesNotCrossTenant()
    {
        using var provider = BuildProvider();

        using var scope =
            provider.CreateScope();

        var services =
            scope.ServiceProvider;

        var context =
            services.GetRequiredService<
                EdulyticsDbContext>();

        var roleManager =
            services.GetRequiredService<
                RoleManager<ApplicationRole>>();

        var userManager =
            services.GetRequiredService<
                UserManager<ApplicationUser>>();

        await roleManager.CreateAsync(
            new ApplicationRole
            {
                Name = RoleNames.Teacher
            });

        var schoolA = NewSchool();
        var schoolB = NewSchool();

        context.Schools.AddRange(
            schoolA,
            schoolB);

        await context.SaveChangesAsync();

        var repository =
            new IdentitySchoolUserRepository(
                userManager,
                roleManager,
                context);

        var created =
            await repository.CreateAsync(
                schoolA.Id,
                "teacher@example.com",
                RoleNames.Teacher);

        Assert.NotNull(created.User);

        var crossTenant =
            await repository.GetBySchoolAndIdAsync(
                schoolB.Id,
                created.User!.Id);

        Assert.Null(crossTenant);
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
                        .RequireNonAlphanumeric =
                        true;

                    options.Password.RequireDigit =
                        true;

                    options.Password.RequireLowercase =
                        true;

                    options.Password.RequireUppercase =
                        true;
                })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<
                EdulyticsDbContext>()
            .AddDefaultTokenProviders();

        return services.BuildServiceProvider();
    }

    private static School NewSchool() =>
        new()
        {
            Id = Guid.NewGuid(),
            Name = "Test School",
            SchoolCode =
                Guid.NewGuid()
                    .ToString("N")[..8]
                    .ToUpperInvariant(),
            NormalizedSchoolCode =
                Guid.NewGuid()
                    .ToString("N")[..8]
                    .ToUpperInvariant(),
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
