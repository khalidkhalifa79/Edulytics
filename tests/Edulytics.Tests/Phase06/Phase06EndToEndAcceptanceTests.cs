using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Edulytics.Data.Identity;
using Edulytics.Data.Repositories;
using Edulytics.Services.Academics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Edulytics.Tests.Phase06;

public sealed class Phase06EndToEndAcceptanceTests
{
    [Fact]
    public async Task CompleteAcademicStructureWorkflow_IsTenantSafeAndRoleSafe()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddDbContext<EdulyticsDbContext>(
            options =>
                options.UseInMemoryDatabase(
                    $"phase06-e2e-{Guid.NewGuid():N}"));

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<EdulyticsDbContext>();

        await using var provider =
            services.BuildServiceProvider();

        await using var scope =
            provider.CreateAsyncScope();

        var db =
            scope.ServiceProvider
                .GetRequiredService<EdulyticsDbContext>();

        var userManager =
            scope.ServiceProvider
                .GetRequiredService<
                    UserManager<ApplicationUser>>();

        var roleManager =
            scope.ServiceProvider
                .GetRequiredService<
                    RoleManager<ApplicationRole>>();

        await EnsureRoleAsync(
            roleManager,
            RoleNames.SchoolAdmin);

        await EnsureRoleAsync(
            roleManager,
            RoleNames.SubjectSupervisor);

        await EnsureRoleAsync(
            roleManager,
            RoleNames.Teacher);

        await EnsureRoleAsync(
            roleManager,
            RoleNames.Student);

        var schoolA =
            NewSchool("E2E School A");

        var schoolB =
            NewSchool("E2E School B");

        db.Schools.AddRange(
            schoolA,
            schoolB);

        await db.SaveChangesAsync();

        var adminA =
            await CreateUserAsync(
                userManager,
                schoolA.Id,
                RoleNames.SchoolAdmin,
                "admin-a");

        var supervisorA =
            await CreateUserAsync(
                userManager,
                schoolA.Id,
                RoleNames.SubjectSupervisor,
                "supervisor-a");

        var teacherA =
            await CreateUserAsync(
                userManager,
                schoolA.Id,
                RoleNames.Teacher,
                "teacher-a");

        var studentA =
            await CreateUserAsync(
                userManager,
                schoolA.Id,
                RoleNames.Student,
                "student-a");

        var adminB =
            await CreateUserAsync(
                userManager,
                schoolB.Id,
                RoleNames.SchoolAdmin,
                "admin-b");


        var supervisorB =
            await CreateUserAsync(
                userManager,
                schoolB.Id,
                RoleNames.SubjectSupervisor,
                "supervisor-b");

        var schools =
            new SchoolRepository(db);

        var users =
            new IdentitySchoolUserRepository(
                userManager,
                roleManager,
                db);

        var academics =
            new AcademicStructureRepository(db);

        var service =
            new AcademicStructureService(
                academics,
                schools,
                users);

        // -------------------------------------------------
        // Authorization
        // -------------------------------------------------

        var adminDashboard =
            await service.GetDashboardAsync(
                adminA.Id);

        Assert.NotNull(
            adminDashboard.Value);

        Assert.Equal(
            schoolA.Id,
            adminDashboard.Value!.SchoolId);

        var teacherDashboard =
            await service.GetDashboardAsync(
                teacherA.Id);

        Assert.NotNull(
            teacherDashboard.Value);

        Assert.Equal(
            schoolA.Id,
            teacherDashboard.Value!.SchoolId);

        var supervisorDashboard =
            await service.GetDashboardAsync(
                supervisorA.Id);

        Assert.NotNull(supervisorDashboard.Value);

        var studentDashboard =
            await service.GetDashboardAsync(
                studentA.Id);

        Assert.Null(
            studentDashboard.Value);

        Assert.Equal(
            AcademicStructureErrorCode.AccessDenied,
            studentDashboard.Error);

        // -------------------------------------------------
        // Academic Year
        // -------------------------------------------------

        var createYear =
            await service.CreateAcademicYearAsync(
                supervisorA.Id,
                new CreateAcademicYearRequest(
                    "2026/2027",
                    new DateOnly(2026, 9, 1),
                    new DateOnly(2027, 6, 30),
                    AcademicStructureStatus.Active));

        Assert.True(
            createYear.Succeeded);

        var dashboard =
            (await service.GetDashboardAsync(
                adminA.Id)).Value!;

        var year =
            Assert.Single(
                dashboard.AcademicYears);

        Assert.Equal(
            "2026/2027",
            year.Name);

        // -------------------------------------------------
        // Term
        // -------------------------------------------------

        var createTerm =
            await service.CreateTermAsync(
                supervisorA.Id,
                new CreateTermRequest(
                    year.Id,
                    "Term 1",
                    new DateOnly(2026, 9, 1),
                    new DateOnly(2027, 1, 31),
                    AcademicStructureStatus.Active));

        Assert.True(
            createTerm.Succeeded);

        // -------------------------------------------------
        // Grade Level
        // -------------------------------------------------

        var createGrade =
            await service.CreateGradeLevelAsync(
                supervisorA.Id,
                new CreateGradeLevelRequest(
                    "Grade 6",
                    6));

        Assert.True(
            createGrade.Succeeded);

        dashboard =
            (await service.GetDashboardAsync(
                adminA.Id)).Value!;

        var grade =
            Assert.Single(
                dashboard.GradeLevels);

        Assert.Equal(
            "Grade 6",
            grade.Name);

        // -------------------------------------------------
        // Program / Stream offering for this academic year
        // -------------------------------------------------

        var offerBritish =
            await service.OfferAcademicProgramAsync(
                supervisorA.Id,
                new OfferAcademicProgramRequest(
                    year.Id,
                    "british"));

        Assert.True(
            offerBritish.Succeeded);

        dashboard =
            (await service.GetDashboardAsync(
                adminA.Id)).Value!;

        var britishProgram =
            Assert.Single(
                dashboard.AcademicPrograms,
                x =>
                    x.Code ==
                    "BRITISH");

        Assert.Contains(
            dashboard.AcademicYearProgramOfferings,
            x =>
                x.AcademicYearId ==
                    year.Id &&
                x.AcademicProgramId ==
                    britishProgram.Id &&
                x.IsOffered);

        // -------------------------------------------------
        // Class
        // -------------------------------------------------

        var createClass =
            await service.CreateClassGroupAsync(
                supervisorA.Id,
                new CreateClassGroupRequest(
                    year.Id,
                    grade.Id,
                    "Grade 6A",
                    "6A",
                    AcademicStructureStatus.Active,
                    britishProgram.Id));

        Assert.True(
            createClass.Succeeded);

        dashboard =
            (await service.GetDashboardAsync(
                adminA.Id)).Value!;

        var classGroup =
            Assert.Single(
                dashboard.ClassGroups);

        Assert.Equal(
            "6A",
            classGroup.Code);

        // -------------------------------------------------
        // Subject
        // -------------------------------------------------

        var createSubject =
            await service.CreateSubjectAsync(
                supervisorA.Id,
                new CreateSubjectRequest(
                    "Mathematics",
                    "MATH",
                    AcademicStructureStatus.Active));

        Assert.True(
            createSubject.Succeeded);

        dashboard =
            (await service.GetDashboardAsync(
                adminA.Id)).Value!;

        var subject =
            Assert.Single(
                dashboard.Subjects);

        Assert.Equal(
            "MATH",
            subject.Code);

        // -------------------------------------------------
        // Teacher assignment
        // -------------------------------------------------

        var teacherAssignment =
            await service.CreateTeacherAssignmentAsync(
                supervisorA.Id,
                new CreateTeacherAssignmentRequest(
                    teacherA.Id,
                    classGroup.Id,
                    subject.Id));

        Assert.True(
            teacherAssignment.Succeeded);

        var duplicateTeacherAssignment =
            await service.CreateTeacherAssignmentAsync(
                supervisorA.Id,
                new CreateTeacherAssignmentRequest(
                    teacherA.Id,
                    classGroup.Id,
                    subject.Id));

        Assert.False(
            duplicateTeacherAssignment.Succeeded);

        Assert.Contains(
            duplicateTeacherAssignment.Errors,
            error =>
                error.Code ==
                AcademicStructureErrorCode
                    .DuplicateTeacherAssignment);

        // -------------------------------------------------
        // Student profile linked to real Student account
        // -------------------------------------------------

        var createStudent =
            await service.CreateStudentProfileAsync(
                supervisorA.Id,
                new CreateStudentProfileRequest(
                    "ST-001",
                    "Jan",
                    "Kowalski",
                    studentA.Id,
                    AcademicStructureStatus.Active));

        Assert.True(
            createStudent.Succeeded);

        dashboard =
            (await service.GetDashboardAsync(
                adminA.Id)).Value!;

        var studentProfile =
            Assert.Single(
                dashboard.StudentProfiles);

        Assert.Equal(
            "ST-001",
            studentProfile.StudentNumber);

        Assert.Equal(
            studentA.Email,
            studentProfile.UserEmail);

        // -------------------------------------------------
        // Student enrollment
        // -------------------------------------------------

        var enrollment =
            await service.CreateStudentEnrollmentAsync(
                supervisorA.Id,
                new CreateStudentEnrollmentRequest(
                    studentProfile.Id,
                    classGroup.Id));

        Assert.True(
            enrollment.Succeeded);

        var duplicateEnrollment =
            await service.CreateStudentEnrollmentAsync(
                supervisorA.Id,
                new CreateStudentEnrollmentRequest(
                    studentProfile.Id,
                    classGroup.Id));

        Assert.False(
            duplicateEnrollment.Succeeded);

        Assert.Contains(
            duplicateEnrollment.Errors,
            error =>
                error.Code ==
                AcademicStructureErrorCode
                    .DuplicateEnrollment);

        // -------------------------------------------------
        // Final persistence verification
        // -------------------------------------------------

        dashboard =
            (await service.GetDashboardAsync(
                adminA.Id)).Value!;

        Assert.Single(
            dashboard.AcademicYears);

        Assert.Single(
            dashboard.Terms);

        Assert.Single(
            dashboard.GradeLevels);

        Assert.Single(
            dashboard.ClassGroups);

        Assert.Single(
            dashboard.Subjects);

        Assert.Single(
            dashboard.TeacherAssignments);

        Assert.Single(
            dashboard.StudentProfiles);

        Assert.Single(
            dashboard.StudentEnrollments);

        var assignment =
            Assert.Single(
                dashboard.TeacherAssignments);

        Assert.Equal(
            teacherA.Email,
            assignment.TeacherEmail);

        Assert.Equal(
            "Grade 6A",
            assignment.ClassName);

        Assert.Equal(
            "Mathematics",
            assignment.SubjectName);

        var savedEnrollment =
            Assert.Single(
                dashboard.StudentEnrollments);

        Assert.Equal(
            "Grade 6A",
            savedEnrollment.ClassName);

        Assert.Equal(
            studentProfile.Id,
            savedEnrollment.StudentProfileId);

        // -------------------------------------------------
        // Tenant isolation
        // -------------------------------------------------

        var schoolBDashboardResult =
            await service.GetDashboardAsync(
                adminB.Id);

        Assert.NotNull(
            schoolBDashboardResult.Value);

        var schoolBDashboard =
            schoolBDashboardResult.Value!;

        Assert.Equal(
            schoolB.Id,
            schoolBDashboard.SchoolId);

        Assert.Empty(
            schoolBDashboard.AcademicYears);

        Assert.Empty(
            schoolBDashboard.Terms);

        Assert.Empty(
            schoolBDashboard.GradeLevels);

        Assert.Empty(
            schoolBDashboard.ClassGroups);

        Assert.Empty(
            schoolBDashboard.Subjects);

        Assert.Empty(
            schoolBDashboard.TeacherAssignments);

        Assert.Empty(
            schoolBDashboard.StudentProfiles);

        Assert.Empty(
            schoolBDashboard.StudentEnrollments);

        var crossTenantYearRead =
            await service.GetAcademicYearAsync(
                adminB.Id,
                year.Id);

        Assert.Null(
            crossTenantYearRead.Value);

        Assert.Equal(
            AcademicStructureErrorCode
                .AcademicYearNotFound,
            crossTenantYearRead.Error);

        var crossTenantTerm =
            await service.CreateTermAsync(
                supervisorB.Id,
                new CreateTermRequest(
                    year.Id,
                    "Illegal cross-tenant term",
                    new DateOnly(2026, 9, 1),
                    new DateOnly(2026, 12, 31),
                    AcademicStructureStatus.Active));

        Assert.False(
            crossTenantTerm.Succeeded);

        Assert.Contains(
            crossTenantTerm.Errors,
            error =>
                error.Code ==
                AcademicStructureErrorCode
                    .AcademicYearNotFound);
    }

    private static async Task EnsureRoleAsync(
        RoleManager<ApplicationRole> roleManager,
        string roleName)
    {
        if (await roleManager.RoleExistsAsync(
                roleName))
        {
            return;
        }

        var result =
            await roleManager.CreateAsync(
                new ApplicationRole
                {
                    Id = Guid.NewGuid(),
                    Name = roleName,
                    NormalizedName =
                        roleName.ToUpperInvariant()
                });

        Assert.True(
            result.Succeeded,
            string.Join(
                "; ",
                result.Errors.Select(
                    error =>
                        $"{error.Code}: {error.Description}")));
    }

    private static async Task<ApplicationUser>
        CreateUserAsync(
            UserManager<ApplicationUser> userManager,
            Guid schoolId,
            string role,
            string prefix)
    {
        var now =
            DateTime.UtcNow;

        var email =
            $"{prefix}-{Guid.NewGuid():N}@example.test";

        var user =
            new ApplicationUser
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

        var create =
            await userManager.CreateAsync(
                user);

        Assert.True(
            create.Succeeded,
            string.Join(
                "; ",
                create.Errors.Select(
                    error =>
                        $"{error.Code}: {error.Description}")));

        var roleResult =
            await userManager.AddToRoleAsync(
                user,
                role);

        Assert.True(
            roleResult.Succeeded,
            string.Join(
                "; ",
                roleResult.Errors.Select(
                    error =>
                        $"{error.Code}: {error.Description}")));

        return user;
    }

    private static School NewSchool(
        string name)
    {
        var code =
            $"E2E-{Guid.NewGuid():N}"
                [..16]
                .ToUpperInvariant();

        var now =
            DateTime.UtcNow;

        return new School
        {
            Id = Guid.NewGuid(),
            Name = name,
            SchoolCode = code,
            NormalizedSchoolCode = code,
            Status = SchoolStatus.Active,
            CountryCode = "PL",
            City = "Warsaw",
            ContactEmail =
                $"{Guid.NewGuid():N}@example.test",
            DefaultCulture = "en",
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            RowVersion =
                BitConverter.GetBytes(1L)
        };
    }
}
