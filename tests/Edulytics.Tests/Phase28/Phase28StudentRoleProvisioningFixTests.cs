using Edulytics.Core.Constants;
using Edulytics.Services.StudentSetup;

namespace Edulytics.Tests.Phase28;

public sealed class Phase28StudentRoleProvisioningFixTests
{
    private static readonly Guid SchoolId =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

    private static readonly Guid UserId =
        Guid.Parse(
            "22222222-2222-2222-2222-222222222222");

    private static readonly Guid ClassA =
        Guid.Parse(
            "33333333-3333-3333-3333-333333333333");

    private static readonly Guid ClassB =
        Guid.Parse(
            "44444444-4444-4444-4444-444444444444");

    private static readonly Guid YearId =
        Guid.Parse(
            "55555555-5555-5555-5555-555555555555");

    [Fact]
    public async Task TeacherToStudent_ProvisionsProfileAndEnrollment()
    {
        var ops =
            new FakeOperations(
                Context(RoleNames.Teacher));

        var service =
            new StudentRoleProvisioningService(ops);

        var result =
            await service.ConvertToStudentAsync(
                Guid.NewGuid(),
                SchoolId,
                UserId,
                Request(ClassA));

        Assert.True(result.Succeeded);
        Assert.Equal(
            RoleNames.Student,
            ops.Context!.Role);

        Assert.True(
            ops.Context.StudentProfileId.HasValue);

        Assert.False(
            ops.Context.ProfileArchived);

        Assert.Single(
            ops.Context.Enrollments);

        Assert.Equal(
            1,
            ops.CreateProfileCalls);

        Assert.Equal(
            1,
            ops.CreateEnrollmentCalls);

        Assert.Equal(
            1,
            ops.RoleChangeCalls);
    }

    [Fact]
    public async Task ExistingStudentWithoutProfile_IsRepaired()
    {
        var ops =
            new FakeOperations(
                Context(RoleNames.Student));

        var service =
            new StudentRoleProvisioningService(ops);

        var result =
            await service.ConvertToStudentAsync(
                Guid.NewGuid(),
                SchoolId,
                UserId,
                Request(ClassA));

        Assert.True(result.Succeeded);
        Assert.Equal(0, ops.RoleChangeCalls);
        Assert.Equal(1, ops.CreateProfileCalls);
        Assert.Equal(1, ops.CreateEnrollmentCalls);
        Assert.True(ops.Context!.IsComplete);
    }

    [Fact]
    public async Task EnrollmentFailure_CompensatesProfileAndTeacherRole()
    {
        var ops =
            new FakeOperations(
                Context(RoleNames.Teacher))
            {
                FailEnrollment = true
            };

        var service =
            new StudentRoleProvisioningService(ops);

        var result =
            await service.ConvertToStudentAsync(
                Guid.NewGuid(),
                SchoolId,
                UserId,
                Request(ClassA));

        Assert.False(result.Succeeded);

        Assert.Equal(
            StudentRoleProvisioningErrorCode
                .UnderlyingOperationFailed,
            result.Error);

        Assert.Equal(
            RoleNames.Teacher,
            ops.Context!.Role);

        Assert.True(
            ops.Context.ProfileArchived);

        Assert.Equal(
            2,
            ops.RoleChangeCalls);

        Assert.Equal(
            1,
            ops.ArchiveProfileCalls);
    }

    [Fact]
    public async Task DifferentClassSameYear_IsRejectedWithoutSilentMove()
    {
        var context =
            WithProfile(
                Context(RoleNames.Student));

        context =
            context with
            {
                Enrollments =
                [
                    new StudentRoleEnrollmentState(
                        ClassA,
                        YearId,
                        "2026/2027",
                        "Grade 8A",
                        "8A")
                ]
            };

        var ops =
            new FakeOperations(context);

        var service =
            new StudentRoleProvisioningService(ops);

        var result =
            await service.ConvertToStudentAsync(
                Guid.NewGuid(),
                SchoolId,
                UserId,
                Request(ClassB));

        Assert.False(result.Succeeded);

        Assert.Equal(
            StudentRoleProvisioningErrorCode
                .EnrollmentConflict,
            result.Error);

        Assert.Equal(
            0,
            ops.CreateEnrollmentCalls);
    }

    [Fact]
    public async Task ExistingCompleteStudent_IsIdempotent()
    {
        var context =
            WithProfile(
                Context(RoleNames.Student));

        context =
            context with
            {
                Enrollments =
                [
                    new StudentRoleEnrollmentState(
                        ClassA,
                        YearId,
                        "2026/2027",
                        "Grade 8A",
                        "8A")
                ]
            };

        var ops =
            new FakeOperations(context);

        var service =
            new StudentRoleProvisioningService(ops);

        var result =
            await service.ConvertToStudentAsync(
                Guid.NewGuid(),
                SchoolId,
                UserId,
                Request(ClassA));

        Assert.True(result.Succeeded);
        Assert.Equal(0, ops.RoleChangeCalls);
        Assert.Equal(0, ops.CreateProfileCalls);
        Assert.Equal(0, ops.CreateEnrollmentCalls);
        Assert.Equal(0, ops.ArchiveProfileCalls);
    }

    [Fact]
    public async Task ForeignOrUnknownClass_IsRejectedBeforeRoleChange()
    {
        var ops =
            new FakeOperations(
                Context(RoleNames.Teacher));

        var service =
            new StudentRoleProvisioningService(ops);

        var result =
            await service.ConvertToStudentAsync(
                Guid.NewGuid(),
                SchoolId,
                UserId,
                Request(Guid.NewGuid()));

        Assert.False(result.Succeeded);

        Assert.Equal(
            StudentRoleProvisioningErrorCode
                .ClassNotFound,
            result.Error);

        Assert.Equal(
            0,
            ops.RoleChangeCalls);
    }

    [Fact]
    public void StudentAccessDenied_UsesSafeLayoutWithoutAppNavigation()
    {
        var root = FindRoot();

        var denied =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Views",
                    "Home",
                    "AccessDenied.cshtml"));

        var layout =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Views",
                    "Shared",
                    "_StudentAccessLayout.cshtml"));

        Assert.Contains(
            "RoleNames.Student",
            denied);

        Assert.Contains(
            "Layout = \"_StudentAccessLayout\"",
            denied);

        foreach (var forbidden in new[]
                 {
                     "SchoolUsers",
                     "AcademicStructure",
                     "Curriculum",
                     "Assessments",
                     "Reports",
                     "DataImport",
                     "StudentPortal"
                 })
        {
            Assert.DoesNotContain(
                forbidden,
                layout);
        }
    }

    private static StudentRoleProvisioningRequest Request(
        Guid classId) =>
        new(
            "ST-100",
            "Yassin",
            "Khalid",
            classId);

    private static StudentRoleProvisioningContext Context(
        string role) =>
        new(
            SchoolId,
            UserId,
            "student@example.com",
            role,
            true,
            false,
            null,
            null,
            null,
            null,
            false,
            false,
            null,
            [],
            [
                new StudentRoleClassOption(
                    ClassA,
                    YearId,
                    "2026/2027",
                    "Grade 8",
                    "Grade 8A",
                    "8A"),

                new StudentRoleClassOption(
                    ClassB,
                    YearId,
                    "2026/2027",
                    "Grade 8",
                    "Grade 8B",
                    "8B")
            ]);

    private static StudentRoleProvisioningContext WithProfile(
        StudentRoleProvisioningContext context) =>
        context with
        {
            StudentProfileId =
                Guid.Parse(
                    "66666666-6666-6666-6666-666666666666"),

            StudentNumber =
                "ST-100",

            FirstName =
                "Yassin",

            LastName =
                "Khalid",

            ProfileIsActive =
                true,

            ProfileArchived =
                false,

            ProfileRowVersion =
                [1, 2, 3]
        };

    private static string FindRoot()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "Edulytics.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root not found.");
    }

    private sealed class FakeOperations
        : IStudentRoleProvisioningOperations
    {
        public FakeOperations(
            StudentRoleProvisioningContext context)
        {
            Context = context;
        }

        public StudentRoleProvisioningContext?
            Context { get; private set; }

        public bool FailEnrollment { get; init; }

        public int RoleChangeCalls { get; private set; }

        public int CreateProfileCalls { get; private set; }

        public int ArchiveProfileCalls { get; private set; }

        public int CreateEnrollmentCalls { get; private set; }

        public Task<StudentRoleProvisioningContext?>
            ReadContextAsync(
                Guid actorUserId,
                Guid schoolId,
                Guid targetUserId,
                CancellationToken cancellationToken = default) =>
            Task.FromResult(Context);

        public Task<StudentRoleProvisioningOperationResult>
            ChangeRoleAsync(
                Guid actorUserId,
                Guid schoolId,
                Guid targetUserId,
                string role,
                CancellationToken cancellationToken = default)
        {
            RoleChangeCalls++;

            Context =
                Context! with
                {
                    Role = role
                };

            return Success();
        }

        public Task<StudentRoleProvisioningOperationResult>
            CreateProfileAsync(
                Guid actorUserId,
                Guid targetUserId,
                string studentNumber,
                string firstName,
                string lastName,
                CancellationToken cancellationToken = default)
        {
            CreateProfileCalls++;

            Context =
                Context! with
                {
                    StudentProfileId =
                        Guid.Parse(
                            "66666666-6666-6666-6666-666666666666"),

                    StudentNumber =
                        studentNumber,

                    FirstName =
                        firstName,

                    LastName =
                        lastName,

                    ProfileIsActive =
                        true,

                    ProfileArchived =
                        false,

                    ProfileRowVersion =
                        [1, 2, 3]
                };

            return Success();
        }

        public Task<StudentRoleProvisioningOperationResult>
            ArchiveProfileAsync(
                Guid actorUserId,
                Guid studentProfileId,
                byte[] expectedRowVersion,
                CancellationToken cancellationToken = default)
        {
            ArchiveProfileCalls++;

            Context =
                Context! with
                {
                    ProfileArchived = true,
                    ProfileRowVersion = [4, 5, 6]
                };

            return Success();
        }

        public Task<StudentRoleProvisioningOperationResult>
            RestoreProfileAsync(
                Guid actorUserId,
                Guid studentProfileId,
                byte[] expectedRowVersion,
                CancellationToken cancellationToken = default)
        {
            Context =
                Context! with
                {
                    ProfileArchived = false,
                    ProfileIsActive = true,
                    ProfileRowVersion = [7, 8, 9]
                };

            return Success();
        }

        public Task<StudentRoleProvisioningOperationResult>
            CreateEnrollmentAsync(
                Guid actorUserId,
                Guid studentProfileId,
                Guid classGroupId,
                CancellationToken cancellationToken = default)
        {
            CreateEnrollmentCalls++;

            if (FailEnrollment)
            {
                return Task.FromResult(
                    StudentRoleProvisioningOperationResult
                        .Failure("simulated"));
            }

            var item =
                Context!.Classes.Single(
                    x => x.Id == classGroupId);

            Context =
                Context with
                {
                    Enrollments =
                    [
                        .. Context.Enrollments,

                        new StudentRoleEnrollmentState(
                            item.Id,
                            item.AcademicYearId,
                            item.AcademicYearName,
                            item.Name,
                            item.Code)
                    ]
                };

            return Success();
        }

        private static Task<
            StudentRoleProvisioningOperationResult>
            Success() =>
            Task.FromResult(
                StudentRoleProvisioningOperationResult
                    .Success());
    }
}
