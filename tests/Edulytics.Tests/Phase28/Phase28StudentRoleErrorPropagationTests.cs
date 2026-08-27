using Edulytics.Core.Constants;
using Edulytics.Services.Academics;
using Edulytics.Services.StudentSetup;

namespace Edulytics.Tests.Phase28;

public sealed class Phase28StudentRoleErrorPropagationTests
{
    private static readonly Guid SchoolId =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

    private static readonly Guid UserId =
        Guid.Parse(
            "22222222-2222-2222-2222-222222222222");

    private static readonly Guid ClassId =
        Guid.Parse(
            "33333333-3333-3333-3333-333333333333");

    private static readonly Guid YearId =
        Guid.Parse(
            "44444444-4444-4444-4444-444444444444");

    [Theory]
    [InlineData(
        AcademicStructureErrorCode.DuplicateStudentNumber)]
    [InlineData(
        AcademicStructureErrorCode.StudentSeatLimitReached)]
    public async Task ProfileFailure_PreservesAcademicCauseAfterRoleRollback(
        AcademicStructureErrorCode academicError)
    {
        var ops =
            new FakeOperations(
                TeacherContext())
            {
                ProfileFailure =
                    academicError
            };

        var service =
            new StudentRoleProvisioningService(ops);

        var result =
            await service.ConvertToStudentAsync(
                Guid.NewGuid(),
                SchoolId,
                UserId,
                Request());

        Assert.False(result.Succeeded);

        Assert.Equal(
            StudentRoleProvisioningErrorCode
                .UnderlyingOperationFailed,
            result.Error);

        Assert.Equal(
            academicError,
            result.AcademicError);

        Assert.Equal(
            RoleNames.Teacher,
            ops.Context!.Role);

        Assert.Equal(
            2,
            ops.RoleChangeCalls);
    }

    [Fact]
    public async Task EnrollmentFailure_PreservesAcademicCauseAfterCompensation()
    {
        var ops =
            new FakeOperations(
                TeacherContext())
            {
                EnrollmentFailure =
                    AcademicStructureErrorCode
                        .DuplicateEnrollment
            };

        var service =
            new StudentRoleProvisioningService(ops);

        var result =
            await service.ConvertToStudentAsync(
                Guid.NewGuid(),
                SchoolId,
                UserId,
                Request());

        Assert.False(result.Succeeded);

        Assert.Equal(
            StudentRoleProvisioningErrorCode
                .UnderlyingOperationFailed,
            result.Error);

        Assert.Equal(
            AcademicStructureErrorCode
                .DuplicateEnrollment,
            result.AcademicError);

        Assert.Equal(
            RoleNames.Teacher,
            ops.Context!.Role);

        Assert.True(
            ops.Context.ProfileArchived);

        Assert.Equal(
            1,
            ops.ArchiveProfileCalls);
    }

    [Fact]
    public async Task RecoveryFailure_HidesOriginalAcademicCause()
    {
        var ops =
            new FakeOperations(
                TeacherContext())
            {
                ProfileFailure =
                    AcademicStructureErrorCode
                        .DuplicateStudentNumber,

                FailRecoveryRoleChange =
                    true
            };

        var service =
            new StudentRoleProvisioningService(ops);

        var result =
            await service.ConvertToStudentAsync(
                Guid.NewGuid(),
                SchoolId,
                UserId,
                Request());

        Assert.False(result.Succeeded);

        Assert.Equal(
            StudentRoleProvisioningErrorCode
                .RecoveryFailed,
            result.Error);

        Assert.Null(
            result.AcademicError);
    }

    [Fact]
    public void ProductionPath_UsesTypedSafeLocalizedMapping()
    {
        var root = FindRoot();

        var operations =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Services",
                    "StudentSetup",
                    "StudentRoleProvisioningOperations.cs"));

        var service =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Services",
                    "StudentSetup",
                    "StudentRoleProvisioningService.cs"));

        var controller =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Controllers",
                    "SchoolUsersController.cs"));

        var english =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Resources",
                    "PlatformResource.resx"));

        var polish =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "src",
                    "Edulytics.Web",
                    "Resources",
                    "PlatformResource.pl.resx"));

        Assert.Contains(
            "first?.Code",
            operations);

        Assert.Contains(
            "create.AcademicError",
            service);

        Assert.Contains(
            "restore.AcademicError",
            service);

        Assert.Contains(
            "enrollment.AcademicError",
            service);

        Assert.Contains(
            "StudentSetupErrorKey(setup)",
            controller);

        Assert.DoesNotContain(
            "setup.AcademicError.ToString",
            controller);

        foreach (var key in new[]
                 {
                     "StudentSetupDuplicateStudentNumber",
                     "StudentSetupSeatLimitReached",
                     "StudentSetupInvalidStudentAccount",
                     "StudentSetupDuplicateStudentUserLink",
                     "StudentSetupDuplicateEnrollment",
                     "StudentSetupProfileStateChanged",
                     "StudentSetupConcurrencyConflict",
                     "StudentSetupInvalidStudentNumber",
                     "StudentSetupInvalidName",
                     "StudentSetupPersistenceError"
                 })
        {
            Assert.Contains(
                $"name=\"{key}\"",
                english);

            Assert.Contains(
                $"name=\"{key}\"",
                polish);

            Assert.Contains(
                $"\"{key}\"",
                controller);
        }
    }

    private static StudentRoleProvisioningRequest Request() =>
        new(
            "01",
            "Yassin",
            "Khalid",
            ClassId);

    private static StudentRoleProvisioningContext TeacherContext() =>
        new(
            SchoolId,
            UserId,
            "yassin2792019@gmail.com",
            RoleNames.Teacher,
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
                    ClassId,
                    YearId,
                    "2026/2027",
                    "Grade 8",
                    "Grade 8A",
                    "8A")
            ]);

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

            directory =
                directory.Parent;
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

        public AcademicStructureErrorCode?
            ProfileFailure { get; init; }

        public AcademicStructureErrorCode?
            EnrollmentFailure { get; init; }

        public bool
            FailRecoveryRoleChange { get; init; }

        public int
            RoleChangeCalls { get; private set; }

        public int
            ArchiveProfileCalls { get; private set; }

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

            if (FailRecoveryRoleChange &&
                RoleChangeCalls > 1)
            {
                return Failure("recovery");
            }

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
            if (ProfileFailure.HasValue)
            {
                return AcademicFailure(
                    ProfileFailure.Value);
            }

            Context =
                Context! with
                {
                    StudentProfileId =
                        Guid.Parse(
                            "55555555-5555-5555-5555-555555555555"),

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
            if (EnrollmentFailure.HasValue)
            {
                return AcademicFailure(
                    EnrollmentFailure.Value);
            }

            var selectedClass =
                Context!.Classes.Single(
                    x => x.Id == classGroupId);

            Context =
                Context with
                {
                    Enrollments =
                    [
                        new StudentRoleEnrollmentState(
                            selectedClass.Id,
                            selectedClass.AcademicYearId,
                            selectedClass.AcademicYearName,
                            selectedClass.Name,
                            selectedClass.Code)
                    ]
                };

            return Success();
        }

        private static Task<
            StudentRoleProvisioningOperationResult>
            AcademicFailure(
                AcademicStructureErrorCode error) =>
            Task.FromResult(
                StudentRoleProvisioningOperationResult
                    .Failure(
                        error.ToString(),
                        error));

        private static Task<
            StudentRoleProvisioningOperationResult>
            Failure(
                string error) =>
            Task.FromResult(
                StudentRoleProvisioningOperationResult
                    .Failure(error));

        private static Task<
            StudentRoleProvisioningOperationResult>
            Success() =>
            Task.FromResult(
                StudentRoleProvisioningOperationResult
                    .Success());
    }
}
