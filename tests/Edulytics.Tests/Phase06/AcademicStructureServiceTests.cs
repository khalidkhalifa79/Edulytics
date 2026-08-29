using Edulytics.Core.Academics;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Edulytics.Services.Academics;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase06;

public sealed class AcademicStructureServiceTests
{
    [Fact]
    public async Task SubjectSupervisor_CanCreateAcademicYear()
    {
        using var f = CreateFixture();

        var result = await f.Service.CreateAcademicYearAsync(
            f.Supervisor.Id,
            new CreateAcademicYearRequest(
                "2026/2027",
                new DateOnly(2026, 9, 1),
                new DateOnly(2027, 6, 30),
                AcademicStructureStatus.Active));

        Assert.True(result.Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        Assert.Single(dashboard.Value!.AcademicYears);
    }

    [Fact]
    public async Task TeacherAndSchoolAdmin_CanRead_ButCannotAdministerAcademicStructure()
    {
        using var f = CreateFixture();

        var teacher = NewUser(f.School.Id, RoleNames.Teacher);
        f.Users.Seed(teacher);

        Assert.NotNull(
            (await f.Service.GetDashboardAsync(teacher.Id)).Value);

        Assert.NotNull(
            (await f.Service.GetDashboardAsync(f.Admin.Id)).Value);

        foreach (var actorId in new[] { teacher.Id, f.Admin.Id })
        {
            var denied =
                await f.Service.CreateAcademicYearAsync(
                    actorId,
                    new CreateAcademicYearRequest(
                        "Blocked",
                        new DateOnly(2026, 9, 1),
                        new DateOnly(2027, 6, 30),
                        AcademicStructureStatus.Active));

            Assert.False(denied.Succeeded);

            Assert.Contains(
                denied.Errors,
                x =>
                    x.Code ==
                    AcademicStructureErrorCode.AccessDenied);
        }
    }

    [Fact]
    public async Task TermMustStayInsideAcademicYear()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);

        var result = await f.Service.CreateTermAsync(
            f.Supervisor.Id,
            new CreateTermRequest(
                year.Id,
                "Bad term",
                year.StartsOn.AddDays(-1),
                year.EndsOn,
                AcademicStructureStatus.Active));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            x => x.Code == AcademicStructureErrorCode.TermOutsideAcademicYear);
    }

    [Fact]
    public async Task TeacherAssignment_RequiresTeacherRole()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);
        var grade = await CreateGrade(f);
        var classGroup = await CreateClass(f, year.Id, grade.Id);
        var subject = await CreateSubject(f);

        var supervisor = NewUser(f.School.Id, RoleNames.SubjectSupervisor);
        f.Users.Seed(supervisor);

        var result = await f.Service.CreateTeacherAssignmentAsync(
            f.Supervisor.Id,
            new CreateTeacherAssignmentRequest(
                supervisor.Id,
                classGroup.Id,
                subject.Id));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            x => x.Code == AcademicStructureErrorCode.InvalidTeacher);
    }

    [Fact]
    public async Task StudentProfile_CanExistWithoutLoginAccount()
    {
        using var f = CreateFixture();

        var result = await f.Service.CreateStudentProfileAsync(
            f.Supervisor.Id,
            new CreateStudentProfileRequest(
                "ST-001",
                "Jan",
                "Kowalski",
                null,
                AcademicStructureStatus.Active));

        Assert.True(result.Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        var student = Assert.Single(dashboard.Value!.StudentProfiles);
        Assert.Null(student.UserEmail);
    }

    [Fact]
    public async Task Enrollment_IsUniquePerStudentAndYear()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);
        var grade = await CreateGrade(f);
        var classGroup = await CreateClass(f, year.Id, grade.Id);

        Assert.True((await f.Service.CreateStudentProfileAsync(
            f.Supervisor.Id,
            new CreateStudentProfileRequest(
                "ST-002",
                "Anna",
                "Nowak",
                null,
                AcademicStructureStatus.Active))).Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        var student = Assert.Single(dashboard.Value!.StudentProfiles);

        var first = await f.Service.CreateStudentEnrollmentAsync(
            f.Supervisor.Id,
            new CreateStudentEnrollmentRequest(student.Id, classGroup.Id));

        Assert.True(first.Succeeded);

        var second = await f.Service.CreateStudentEnrollmentAsync(
            f.Supervisor.Id,
            new CreateStudentEnrollmentRequest(student.Id, classGroup.Id));

        Assert.False(second.Succeeded);
        Assert.Contains(
            second.Errors,
            x => x.Code == AcademicStructureErrorCode.DuplicateEnrollment);
    }

    [Fact]
    public async Task CrossSchoolGrade_IsRejected()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);
        var other = NewSchool();
        f.Schools.Seed(other);

        var foreignGrade = new GradeLevel
        {
            Id = Guid.NewGuid(),
            SchoolId = other.Id,
            Name = "Foreign grade",
            Order = 1
        };

        await f.Academic.AddAsync(foreignGrade);
        Assert.True((await f.Academic.SaveAsync()).Succeeded);

        var result = await f.Service.CreateClassGroupAsync(
            f.Supervisor.Id,
            new CreateClassGroupRequest(
                year.Id,
                foreignGrade.Id,
                "6A",
                "6A",
                AcademicStructureStatus.Active));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            x => x.Code == AcademicStructureErrorCode.GradeLevelNotFound);
    }


    [Fact]
    public async Task AcademicPrograms_ScopeClassCodes_PerProgram()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);
        var grade = await CreateGrade(f);

        Assert.True((await f.Service.CreateAcademicProgramAsync(
            f.Supervisor.Id,
            new CreateAcademicProgramRequest(
                "British Stream",
                "BRITISH",
                AcademicStructureStatus.Active))).Succeeded);

        Assert.True((await f.Service.CreateAcademicProgramAsync(
            f.Supervisor.Id,
            new CreateAcademicProgramRequest(
                "American Stream",
                "AMERICAN",
                AcademicStructureStatus.Active))).Succeeded);

        Assert.True((await f.Service.OfferAcademicProgramAsync(
            f.Supervisor.Id,
            new OfferAcademicProgramRequest(
                year.Id,
                "british"))).Succeeded);

        Assert.True((await f.Service.OfferAcademicProgramAsync(
            f.Supervisor.Id,
            new OfferAcademicProgramRequest(
                year.Id,
                "american"))).Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        var british = Assert.Single(
            dashboard.Value!.AcademicPrograms,
            x => x.Code == "BRITISH");
        var american = Assert.Single(
            dashboard.Value.AcademicPrograms,
            x => x.Code == "AMERICAN");

        var britishClass = await f.Service.CreateClassGroupAsync(
            f.Supervisor.Id,
            new CreateClassGroupRequest(
                year.Id,
                grade.Id,
                "British 6A",
                "6A",
                AcademicStructureStatus.Active,
                british.Id));

        var americanClass = await f.Service.CreateClassGroupAsync(
            f.Supervisor.Id,
            new CreateClassGroupRequest(
                year.Id,
                grade.Id,
                "American 6A",
                "6A",
                AcademicStructureStatus.Active,
                american.Id));

        Assert.True(britishClass.Succeeded);
        Assert.True(americanClass.Succeeded);

        var duplicateInsideBritish = await f.Service.CreateClassGroupAsync(
            f.Supervisor.Id,
            new CreateClassGroupRequest(
                year.Id,
                grade.Id,
                "British duplicate",
                "6A",
                AcademicStructureStatus.Active,
                british.Id));

        Assert.False(duplicateInsideBritish.Succeeded);
        Assert.Contains(
            duplicateInsideBritish.Errors,
            x => x.Code == AcademicStructureErrorCode.DuplicateClassCode);

        dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        Assert.Equal(2, dashboard.Value!.ClassGroups.Count);
        Assert.Contains(
            dashboard.Value.ClassGroups,
            x => x.AcademicProgramId == british.Id &&
                 x.AcademicProgramName == "British Stream");
        Assert.Contains(
            dashboard.Value.ClassGroups,
            x => x.AcademicProgramId == american.Id &&
                 x.AcademicProgramName == "American Stream");
    }

    [Fact]
    public async Task AcademicProgram_DuplicateAndUnauthorizedCreation_AreRejected()
    {
        using var f = CreateFixture();

        var created = await f.Service.CreateAcademicProgramAsync(
            f.Supervisor.Id,
            new CreateAcademicProgramRequest(
                "British Stream",
                "BRITISH",
                AcademicStructureStatus.Active));

        Assert.True(created.Succeeded);

        var duplicate = await f.Service.CreateAcademicProgramAsync(
            f.Supervisor.Id,
            new CreateAcademicProgramRequest(
                "Another British",
                "BRITISH",
                AcademicStructureStatus.Active));

        Assert.False(duplicate.Succeeded);
        Assert.Contains(
            duplicate.Errors,
            x => x.Code == AcademicStructureErrorCode.DuplicateAcademicProgram);

        var denied = await f.Service.CreateAcademicProgramAsync(
            f.Admin.Id,
            new CreateAcademicProgramRequest(
                "Admin Stream",
                "ADMIN",
                AcademicStructureStatus.Active));

        Assert.False(denied.Succeeded);
        Assert.Contains(
            denied.Errors,
            x => x.Code == AcademicStructureErrorCode.AccessDenied);

        var unsupported = await f.Service.CreateAcademicProgramAsync(
            f.Supervisor.Id,
            new CreateAcademicProgramRequest(
                "Custom Stream",
                "CUSTOM",
                AcademicStructureStatus.Active));

        Assert.False(unsupported.Succeeded);
        Assert.Contains(
            unsupported.Errors,
            x =>
                x.Code ==
                AcademicStructureErrorCode.AcademicProgramNotFound);

        var forgedPair = await f.Service.CreateAcademicProgramAsync(
            f.Supervisor.Id,
            new CreateAcademicProgramRequest(
                "Forged American Name",
                "AMERICAN",
                AcademicStructureStatus.Active));

        Assert.False(forgedPair.Succeeded);
        Assert.Contains(
            forgedPair.Errors,
            x =>
                x.Code ==
                AcademicStructureErrorCode.InvalidName);
    }

    [Fact]
    public async Task ClassProgram_CanBeChanged_AndGetClassReturnsProgramMetadata()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);
        var grade = await CreateGrade(f);

        Assert.True((await f.Service.CreateAcademicProgramAsync(
            f.Supervisor.Id,
            new CreateAcademicProgramRequest(
                "British Stream",
                "BRITISH",
                AcademicStructureStatus.Active))).Succeeded);

        Assert.True((await f.Service.CreateAcademicProgramAsync(
            f.Supervisor.Id,
            new CreateAcademicProgramRequest(
                "American Stream",
                "AMERICAN",
                AcademicStructureStatus.Active))).Succeeded);

        Assert.True((await f.Service.OfferAcademicProgramAsync(
            f.Supervisor.Id,
            new OfferAcademicProgramRequest(
                year.Id,
                "british"))).Succeeded);

        Assert.True((await f.Service.OfferAcademicProgramAsync(
            f.Supervisor.Id,
            new OfferAcademicProgramRequest(
                year.Id,
                "american"))).Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        var british = dashboard.Value!.AcademicPrograms.Single(x => x.Code == "BRITISH");
        var american = dashboard.Value.AcademicPrograms.Single(x => x.Code == "AMERICAN");

        Assert.True((await f.Service.CreateClassGroupAsync(
            f.Supervisor.Id,
            new CreateClassGroupRequest(
                year.Id,
                grade.Id,
                "6A",
                "6A",
                AcademicStructureStatus.Active,
                british.Id))).Succeeded);

        dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        var item = Assert.Single(dashboard.Value!.ClassGroups);

        var read = await f.Service.GetClassGroupAsync(
            f.Supervisor.Id,
            item.Id);

        Assert.NotNull(read.Value);
        Assert.Equal(british.Id, read.Value!.AcademicProgramId);
        Assert.Equal("BRITISH", read.Value.AcademicProgramCode);

        var moved = await f.Service.UpdateClassGroupAsync(
            f.Supervisor.Id,
            new UpdateClassGroupRequest(
                item.Id,
                grade.Id,
                "6A",
                "6A",
                AcademicStructureStatus.Active,
                item.RowVersion,
                american.Id));

        Assert.True(moved.Succeeded);

        read = await f.Service.GetClassGroupAsync(
            f.Supervisor.Id,
            item.Id);

        Assert.NotNull(read.Value);
        Assert.Equal(american.Id, read.Value!.AcademicProgramId);
        Assert.Equal("American Stream", read.Value.AcademicProgramName);
    }

    [Fact]
    public async Task CrossSchoolAcademicProgram_IsRejectedForClass()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);
        var grade = await CreateGrade(f);

        var other = NewSchool();
        f.Db.Schools.Add(other);

        var foreignProgram = new AcademicProgram
        {
            Id = Guid.NewGuid(),
            SchoolId = other.Id,
            Name = "Foreign Stream",
            Code = "FOREIGN",
            NormalizedCode = "FOREIGN",
            Status = AcademicStructureStatus.Active,
            IsDefault = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = BitConverter.GetBytes(1L)
        };

        f.Db.AcademicPrograms.Add(foreignProgram);
        await f.Db.SaveChangesAsync();

        var result = await f.Service.CreateClassGroupAsync(
            f.Supervisor.Id,
            new CreateClassGroupRequest(
                year.Id,
                grade.Id,
                "6A",
                "6A",
                AcademicStructureStatus.Active,
                foreignProgram.Id));

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Errors,
            x => x.Code == AcademicStructureErrorCode.AcademicProgramNotFound);
    }


    [Fact]
    public async Task FullAcademicCrud_HappyPath_CoversReadUpdateAssignmentArchiveRestore()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);

        var readYear = await f.Service.GetAcademicYearAsync(
            f.Supervisor.Id,
            year.Id);

        Assert.NotNull(readYear.Value);

        var updateYear = await f.Service.UpdateAcademicYearAsync(
            f.Supervisor.Id,
            new UpdateAcademicYearRequest(
                year.Id,
                "2026/2027 Updated",
                year.StartsOn,
                year.EndsOn,
                AcademicStructureStatus.Active,
                year.RowVersion));

        Assert.True(updateYear.Succeeded);

        var refreshedYear = await f.Service.GetAcademicYearAsync(
            f.Supervisor.Id,
            year.Id);

        Assert.NotNull(refreshedYear.Value);

        var term = await f.Service.CreateTermAsync(
            f.Supervisor.Id,
            new CreateTermRequest(
                year.Id,
                "Term 1",
                year.StartsOn,
                year.StartsOn.AddMonths(3),
                AcademicStructureStatus.Active));

        Assert.True(term.Succeeded);

        var grade = await CreateGrade(f);

        var programCreate = await f.Service.CreateAcademicProgramAsync(
            f.Supervisor.Id,
            new CreateAcademicProgramRequest(
                "British Stream",
                "BRITISH",
                AcademicStructureStatus.Active));

        Assert.True(programCreate.Succeeded);

        Assert.True((await f.Service.OfferAcademicProgramAsync(
            f.Supervisor.Id,
            new OfferAcademicProgramRequest(
                year.Id,
                "british"))).Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        var program = Assert.Single(
            dashboard.Value!.AcademicPrograms,
            x => x.Code == "BRITISH");

        var classCreate = await f.Service.CreateClassGroupAsync(
            f.Supervisor.Id,
            new CreateClassGroupRequest(
                year.Id,
                grade.Id,
                "6A",
                "6A",
                AcademicStructureStatus.Active,
                program.Id));

        Assert.True(classCreate.Succeeded);

        dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        var classGroup = Assert.Single(dashboard.Value!.ClassGroups);

        var classRead = await f.Service.GetClassGroupAsync(
            f.Supervisor.Id,
            classGroup.Id);

        Assert.NotNull(classRead.Value);
        Assert.Equal(program.Id, classRead.Value!.AcademicProgramId);

        var subject = await CreateSubject(f);

        var subjectRead = await f.Service.GetSubjectAsync(
            f.Supervisor.Id,
            subject.Id);

        Assert.NotNull(subjectRead.Value);

        var subjectUpdate = await f.Service.UpdateSubjectAsync(
            f.Supervisor.Id,
            new UpdateSubjectRequest(
                subject.Id,
                "Mathematics Updated",
                "MATH",
                AcademicStructureStatus.Active,
                subject.RowVersion));

        Assert.True(subjectUpdate.Succeeded);

        var teacher = NewUser(f.School.Id, RoleNames.Teacher);
        f.Users.Seed(teacher);

        var assignment = await f.Service.CreateTeacherAssignmentAsync(
            f.Supervisor.Id,
            new CreateTeacherAssignmentRequest(
                teacher.Id,
                classGroup.Id,
                subject.Id));

        Assert.True(assignment.Succeeded);

        var studentUser = NewUser(f.School.Id, RoleNames.Student);
        f.Users.Seed(studentUser);

        var studentCreate = await f.Service.CreateStudentProfileAsync(
            f.Supervisor.Id,
            new CreateStudentProfileRequest(
                "ST-CRUD",
                "Marta",
                "Kowalska",
                studentUser.Id,
                AcademicStructureStatus.Active));

        Assert.True(studentCreate.Succeeded);

        dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        var student = Assert.Single(
            dashboard.Value!.StudentProfiles,
            x => x.StudentNumber == "ST-CRUD");

        Assert.NotNull(student.RowVersion);

        var archived = await f.Service.ArchiveStudentProfileAsync(
            f.Supervisor.Id,
            student.Id,
            student.RowVersion!);

        Assert.True(archived.Succeeded);

        dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        var archivedStudent = Assert.Single(
            dashboard.Value!.StudentProfiles,
            x => x.Id == student.Id);

        Assert.True(archivedStudent.IsArchived);
        Assert.NotNull(archivedStudent.RowVersion);

        var restored = await f.Service.RestoreStudentProfileAsync(
            f.Supervisor.Id,
            archivedStudent.Id,
            archivedStudent.RowVersion!);

        Assert.True(restored.Succeeded);

        var enrollment = await f.Service.CreateStudentEnrollmentAsync(
            f.Supervisor.Id,
            new CreateStudentEnrollmentRequest(
                student.Id,
                classGroup.Id));

        Assert.True(enrollment.Succeeded);

        dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);

        Assert.Single(dashboard.Value!.TeacherAssignments);
        Assert.Single(dashboard.Value.StudentEnrollments);

        Assert.NotNull(
            await f.Academic.GetAcademicProgramAsync(
                f.School.Id,
                program.Id));

        Assert.Null(
            await f.Academic.GetAcademicProgramAsync(
                f.School.Id,
                Guid.NewGuid()));

        Assert.True(
            await f.Academic.ClassCodeExistsInProgramAsync(
                f.School.Id,
                year.Id,
                program.Id,
                "6A"));

        Assert.False(
            await f.Academic.ClassCodeExistsInProgramAsync(
                f.School.Id,
                year.Id,
                Guid.NewGuid(),
                "6A"));
    }

    [Fact]
    public async Task ProgramOffering_IsScopedByAcademicYear()
    {
        using var f = CreateFixture();

        var firstYear = await CreateYear(f);
        var grade = await CreateGrade(f);

        Assert.True(
            (await f.Service.OfferAcademicProgramAsync(
                f.Supervisor.Id,
                new OfferAcademicProgramRequest(
                    firstYear.Id,
                    "british")))
            .Succeeded);

        var firstDashboard =
            await f.Service.GetDashboardAsync(
                f.Supervisor.Id);

        var british =
            firstDashboard.Value!.AcademicPrograms
                .Single(
                    x =>
                        x.Code ==
                        "BRITISH");

        Assert.True(
            (await f.Service.CreateClassGroupAsync(
                f.Supervisor.Id,
                new CreateClassGroupRequest(
                    firstYear.Id,
                    grade.Id,
                    "British 6A",
                    "6A",
                    AcademicStructureStatus.Active,
                    british.Id)))
            .Succeeded);

        Assert.True(
            (await f.Service.CreateAcademicYearAsync(
                f.Supervisor.Id,
                new CreateAcademicYearRequest(
                    "2027/2028",
                    new DateOnly(2027, 9, 1),
                    new DateOnly(2028, 6, 30),
                    AcademicStructureStatus.Active)))
            .Succeeded);

        var dashboard =
            await f.Service.GetDashboardAsync(
                f.Supervisor.Id);

        var secondYear =
            dashboard.Value!.AcademicYears
                .Single(
                    x =>
                        x.Name ==
                        "2027/2028");

        var rejected =
            await f.Service.CreateClassGroupAsync(
                f.Supervisor.Id,
                new CreateClassGroupRequest(
                    secondYear.Id,
                    grade.Id,
                    "British 7A",
                    "7A",
                    AcademicStructureStatus.Active,
                    british.Id));

        Assert.False(
            rejected.Succeeded);

        Assert.Contains(
            rejected.Errors,
            x =>
                x.Code ==
                AcademicStructureErrorCode
                    .AcademicProgramNotOffered);

        Assert.True(
            (await f.Service.OfferAcademicProgramAsync(
                f.Supervisor.Id,
                new OfferAcademicProgramRequest(
                    secondYear.Id,
                    "american")))
            .Succeeded);

        dashboard =
            await f.Service.GetDashboardAsync(
                f.Supervisor.Id);

        var american =
            dashboard.Value!.AcademicPrograms
                .Single(
                    x =>
                        x.Code ==
                        "AMERICAN");

        Assert.True(
            (await f.Service.CreateClassGroupAsync(
                f.Supervisor.Id,
                new CreateClassGroupRequest(
                    secondYear.Id,
                    grade.Id,
                    "American 7A",
                    "7A",
                    AcademicStructureStatus.Active,
                    american.Id)))
            .Succeeded);
    }

    [Fact]
    public async Task ProgramOffering_CannotStopWhenYearAlreadyUsesProgram()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);
        var grade = await CreateGrade(f);

        Assert.True(
            (await f.Service.OfferAcademicProgramAsync(
                f.Supervisor.Id,
                new OfferAcademicProgramRequest(
                    year.Id,
                    "british")))
            .Succeeded);

        var dashboard =
            await f.Service.GetDashboardAsync(
                f.Supervisor.Id);

        var british =
            dashboard.Value!.AcademicPrograms
                .Single(
                    x =>
                        x.Code ==
                        "BRITISH");

        Assert.True(
            (await f.Service.CreateClassGroupAsync(
                f.Supervisor.Id,
                new CreateClassGroupRequest(
                    year.Id,
                    grade.Id,
                    "6A",
                    "6A",
                    AcademicStructureStatus.Active,
                    british.Id)))
            .Succeeded);

        dashboard =
            await f.Service.GetDashboardAsync(
                f.Supervisor.Id);

        var offering =
            dashboard.Value!
                .AcademicYearProgramOfferings
                .Single(
                    x =>
                        x.AcademicYearId ==
                            year.Id &&
                        x.AcademicProgramId ==
                            british.Id &&
                        x.IsOffered);

        var stopped =
            await f.Service
                .StopAcademicProgramOfferingAsync(
                    f.Supervisor.Id,
                    new StopAcademicProgramOfferingRequest(
                        year.Id,
                        british.Id,
                        offering.RowVersion));

        Assert.False(
            stopped.Succeeded);

        Assert.Contains(
            stopped.Errors,
            x =>
                x.Code ==
                AcademicStructureErrorCode
                    .AcademicProgramInUseForAcademicYear);
    }

    [Fact]
    public async Task FutureYearProgramCanBeStoppedWithoutDeletingHistory()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);

        Assert.True(
            (await f.Service.OfferAcademicProgramAsync(
                f.Supervisor.Id,
                new OfferAcademicProgramRequest(
                    year.Id,
                    "british")))
            .Succeeded);

        var dashboard =
            await f.Service.GetDashboardAsync(
                f.Supervisor.Id);

        var british =
            dashboard.Value!.AcademicPrograms
                .Single(
                    x =>
                        x.Code ==
                        "BRITISH");

        var offering =
            dashboard.Value
                .AcademicYearProgramOfferings
                .Single(
                    x =>
                        x.AcademicYearId ==
                            year.Id &&
                        x.AcademicProgramId ==
                            british.Id);

        var stopped =
            await f.Service
                .StopAcademicProgramOfferingAsync(
                    f.Supervisor.Id,
                    new StopAcademicProgramOfferingRequest(
                        year.Id,
                        british.Id,
                        offering.RowVersion));

        Assert.True(
            stopped.Succeeded);

        dashboard =
            await f.Service.GetDashboardAsync(
                f.Supervisor.Id);

        Assert.Contains(
            dashboard.Value!.AcademicPrograms,
            x =>
                x.Id ==
                british.Id);

        Assert.Contains(
            dashboard.Value
                .AcademicYearProgramOfferings,
            x =>
                x.AcademicProgramId ==
                    british.Id &&
                x.AcademicYearId ==
                    year.Id &&
                !x.IsOffered);
    }

    [Fact]
    public async Task ClassCode_IsGeneratedInternally_AndPreservedOnEdit()
    {
        using var f = CreateFixture();

        var year = await CreateYear(f);
        var grade = await CreateGrade(f);

        Assert.True(
            (await f.Service.OfferAcademicProgramAsync(
                f.Supervisor.Id,
                new OfferAcademicProgramRequest(
                    year.Id,
                    "british")))
            .Succeeded);

        var dashboard =
            await f.Service.GetDashboardAsync(
                f.Supervisor.Id);

        var british =
            dashboard.Value!.AcademicPrograms
                .Single(
                    x =>
                        x.Code ==
                        "BRITISH");

        var created =
            await f.Service.CreateClassGroupAsync(
                f.Supervisor.Id,
                new CreateClassGroupRequest(
                    year.Id,
                    grade.Id,
                    "Grade 6A",
                    string.Empty,
                    AcademicStructureStatus.Active,
                    british.Id));

        Assert.True(created.Succeeded);

        dashboard =
            await f.Service.GetDashboardAsync(
                f.Supervisor.Id);

        var classGroup =
            Assert.Single(
                dashboard.Value!.ClassGroups);

        Assert.Matches(
            "^CLS-[A-F0-9]{32}$",
            classGroup.Code);

        var originalCode =
            classGroup.Code;

        var updated =
            await f.Service.UpdateClassGroupAsync(
                f.Supervisor.Id,
                new UpdateClassGroupRequest(
                    classGroup.Id,
                    grade.Id,
                    "Grade 6A Updated",
                    string.Empty,
                    AcademicStructureStatus.Active,
                    classGroup.RowVersion,
                    british.Id));

        Assert.True(updated.Succeeded);

        dashboard =
            await f.Service.GetDashboardAsync(
                f.Supervisor.Id);

        classGroup =
            Assert.Single(
                dashboard.Value!.ClassGroups);

        Assert.Equal(
            originalCode,
            classGroup.Code);

        Assert.Equal(
            "Grade 6A Updated",
            classGroup.Name);
    }

    [Fact]
    public async Task AcademicValidationBranches_CoverDuplicateAndStateErrors()
    {
        using var f = CreateFixture();

        var firstYear = await f.Service.CreateAcademicYearAsync(
            f.Supervisor.Id,
            new CreateAcademicYearRequest(
                "2026/2027",
                new DateOnly(2026, 9, 1),
                new DateOnly(2027, 6, 30),
                AcademicStructureStatus.Active));

        Assert.True(firstYear.Succeeded);

        var duplicateYear = await f.Service.CreateAcademicYearAsync(
            f.Supervisor.Id,
            new CreateAcademicYearRequest(
                "2026/2027",
                new DateOnly(2026, 9, 1),
                new DateOnly(2027, 6, 30),
                AcademicStructureStatus.Active));

        Assert.False(duplicateYear.Succeeded);

        var invalidYear = await f.Service.CreateAcademicYearAsync(
            f.Supervisor.Id,
            new CreateAcademicYearRequest(
                "Bad",
                new DateOnly(2027, 1, 1),
                new DateOnly(2026, 1, 1),
                AcademicStructureStatus.Active));

        Assert.False(invalidYear.Succeeded);

        Assert.True((await f.Service.CreateGradeLevelAsync(
            f.Supervisor.Id,
            new CreateGradeLevelRequest(
                "Grade 6",
                6))).Succeeded);

        Assert.False((await f.Service.CreateGradeLevelAsync(
            f.Supervisor.Id,
            new CreateGradeLevelRequest(
                "Grade 6",
                7))).Succeeded);

        Assert.False((await f.Service.CreateGradeLevelAsync(
            f.Supervisor.Id,
            new CreateGradeLevelRequest(
                "Other",
                6))).Succeeded);

        Assert.True((await f.Service.CreateSubjectAsync(
            f.Supervisor.Id,
            new CreateSubjectRequest(
                "Mathematics",
                "MATH",
                AcademicStructureStatus.Active))).Succeeded);

        Assert.False((await f.Service.CreateSubjectAsync(
            f.Supervisor.Id,
            new CreateSubjectRequest(
                "Mathematics 2",
                "MATH",
                AcademicStructureStatus.Active))).Succeeded);

        Assert.False((await f.Service.CreateAcademicProgramAsync(
            f.Supervisor.Id,
            new CreateAcademicProgramRequest(
                " ",
                "BLANK",
                AcademicStructureStatus.Active))).Succeeded);

        f.School.Status = SchoolStatus.Suspended;
        f.Db.SaveChanges();

        Assert.False((await f.Service.CreateAcademicProgramAsync(
            f.Supervisor.Id,
            new CreateAcademicProgramRequest(
                "Blocked",
                "BLOCKED",
                AcademicStructureStatus.Active))).Succeeded);
    }

    private static async Task<AcademicYearItem> CreateYear(Fixture f)
    {
        Assert.True((await f.Service.CreateAcademicYearAsync(
            f.Supervisor.Id,
            new CreateAcademicYearRequest(
                "2026/2027",
                new DateOnly(2026, 9, 1),
                new DateOnly(2027, 6, 30),
                AcademicStructureStatus.Active))).Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        return Assert.Single(dashboard.Value!.AcademicYears);
    }

    private static async Task<GradeLevelItem> CreateGrade(Fixture f)
    {
        Assert.True((await f.Service.CreateGradeLevelAsync(
            f.Supervisor.Id,
            new CreateGradeLevelRequest("Grade 6", 6))).Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        return Assert.Single(dashboard.Value!.GradeLevels);
    }

    private static async Task<ClassGroupItem> CreateClass(
        Fixture f,
        Guid yearId,
        Guid gradeId)
    {
        var dashboard =
            await f.Service.GetDashboardAsync(
                f.Supervisor.Id);

        var program =
            dashboard.Value!.AcademicPrograms
                .SingleOrDefault(
                    x => x.Code == "BRITISH");

        if (program is null)
        {
            Assert.True(
                (await f.Service.CreateAcademicProgramAsync(
                    f.Supervisor.Id,
                    new CreateAcademicProgramRequest(
                        "British Stream",
                        "BRITISH",
                        AcademicStructureStatus.Active)))
                .Succeeded);

            dashboard =
                await f.Service.GetDashboardAsync(
                    f.Supervisor.Id);

            program =
                dashboard.Value!.AcademicPrograms
                    .Single(
                        x =>
                            x.Code ==
                            "BRITISH");
        }

        var alreadyOffered =
            dashboard.Value
                .AcademicYearProgramOfferings
                .Any(
                    x =>
                        x.AcademicYearId ==
                            yearId &&
                        x.AcademicProgramId ==
                            program.Id &&
                        x.IsOffered);

        if (!alreadyOffered)
        {
            Assert.True(
                (await f.Service
                    .OfferAcademicProgramAsync(
                        f.Supervisor.Id,
                        new OfferAcademicProgramRequest(
                            yearId,
                            "british")))
                .Succeeded);
        }

        Assert.True(
            (await f.Service.CreateClassGroupAsync(
                f.Supervisor.Id,
                new CreateClassGroupRequest(
                    yearId,
                    gradeId,
                    "6A",
                    "6A",
                    AcademicStructureStatus.Active,
                    program.Id)))
            .Succeeded);

        dashboard =
            await f.Service.GetDashboardAsync(
                f.Supervisor.Id);

        return Assert.Single(
            dashboard.Value!.ClassGroups);
    }

    private static async Task<SubjectItem> CreateSubject(Fixture f)
    {
        Assert.True((await f.Service.CreateSubjectAsync(
            f.Supervisor.Id,
            new CreateSubjectRequest(
                "Mathematics",
                "MATH",
                AcademicStructureStatus.Active))).Succeeded);

        var dashboard = await f.Service.GetDashboardAsync(f.Supervisor.Id);
        return Assert.Single(dashboard.Value!.Subjects);
    }

    private static Fixture CreateFixture()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase($"phase06-{Guid.NewGuid():N}")
                .Options;

        var db = new EdulyticsDbContext(options);
        var school = NewSchool();

        db.Schools.Add(school);
        db.SaveChanges();

        var schools = new FakeSchoolRepository();
        schools.Seed(school);

        var users = new FakeUserRepository();
        var admin = NewUser(school.Id, RoleNames.SchoolAdmin);
        var supervisor = NewUser(
            school.Id,
            RoleNames.SubjectSupervisor);

        users.Seed(admin);
        users.Seed(supervisor);

        var academic = new AcademicStructureRepository(db);
        var service = new AcademicStructureService(academic, schools, users);

        return new Fixture(
            db,
            school,
            admin,
            supervisor,
            schools,
            users,
            academic,
            service);
    }

    private static School NewSchool()
    {
        var code = $"P6-{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        return new School
        {
            Id = Guid.NewGuid(),
            Name = "Phase 06 School",
            SchoolCode = code,
            NormalizedSchoolCode = code,
            Status = SchoolStatus.Active,
            CountryCode = "PL",
            City = "Warsaw",
            ContactEmail = $"{Guid.NewGuid():N}@example.com",
            DefaultCulture = "en",
            TimeZoneId = "Europe/Warsaw",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow,
            RowVersion = BitConverter.GetBytes(1L)
        };
    }

    private static SchoolUserRecord NewUser(Guid schoolId, string role) =>
        new(
            Guid.NewGuid(),
            schoolId,
            $"{Guid.NewGuid():N}@example.com",
            true,
            false,
            DateTime.UtcNow,
            DateTime.UtcNow,
            [role]);

    private sealed record Fixture(
        EdulyticsDbContext Db,
        School School,
        SchoolUserRecord Admin,
        SchoolUserRecord Supervisor,
        FakeSchoolRepository Schools,
        FakeUserRepository Users,
        AcademicStructureRepository Academic,
        AcademicStructureService Service)
        : IDisposable
    {
        public void Dispose() => Db.Dispose();
    }

    private sealed class FakeSchoolRepository : ISchoolRepository
    {
        private readonly List<School> _schools = [];

        public void Seed(School school) => _schools.Add(school);

        public Task<IReadOnlyList<School>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<School>>(_schools.ToArray());

        public Task<School?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_schools.SingleOrDefault(x => x.Id == id));

        public Task<School?> GetForUpdateAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            GetByIdAsync(id, cancellationToken);

        public Task<bool> ExistsByNormalizedCodeAsync(
            string normalizedSchoolCode,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(
                _schools.Any(x => x.NormalizedSchoolCode == normalizedSchoolCode));

        public Task AddAsync(
            School school,
            CancellationToken cancellationToken = default)
        {
            _schools.Add(school);
            return Task.CompletedTask;
        }

        public Task<SchoolRepositoryWriteResult> SaveAsync(
            School school,
            byte[]? expectedRowVersion,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(SchoolRepositoryWriteResult.Success);
    }

    private sealed class FakeUserRepository : ISchoolUserRepository
    {
        private readonly Dictionary<Guid, SchoolUserRecord> _users = [];

        public void Seed(SchoolUserRecord user) => _users[user.Id] = user;

        public Task<SchoolUserRecord?> GetActorAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_users.GetValueOrDefault(userId));

        public Task<IReadOnlyList<SchoolUserRecord>> ListBySchoolAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SchoolUserRecord>>(
                _users.Values.Where(x => x.SchoolId == schoolId).ToArray());

        public Task<SchoolUserRecord?> GetBySchoolAndIdAsync(
            Guid schoolId,
            Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = _users.GetValueOrDefault(userId);
            return Task.FromResult(user?.SchoolId == schoolId ? user : null);
        }

        public Task<SchoolUserPersistenceResult> CreateAsync(
            Guid schoolId, string email, string role,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> SetActiveAsync(
            Guid schoolId, Guid userId, bool isActive,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> SetLockedAsync(
            Guid schoolId, Guid userId, bool isLocked,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> SetRoleAsync(
            Guid schoolId, Guid userId, string role,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> GeneratePasswordSetupAsync(
            Guid schoolId, Guid userId,
            CancellationToken cancellationToken = default) =>
            Failure();

        public Task<SchoolUserPersistenceResult> CompletePasswordSetupAsync(
            Guid userId, string token, string newPassword,
            CancellationToken cancellationToken = default) =>
            Failure();

        private static Task<SchoolUserPersistenceResult> Failure() =>
            Task.FromResult(
                SchoolUserPersistenceResult.Failure(
                    SchoolUserPersistenceError.IdentityFailure));
    }
}
