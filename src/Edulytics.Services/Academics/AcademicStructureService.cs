using System.Text.RegularExpressions;
using Edulytics.Core.Academics;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;

namespace Edulytics.Services.Academics;

public sealed class AcademicStructureService : IAcademicStructureService
{
    private static readonly Regex CodePattern = new(
        "^[A-Z0-9-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IAcademicStructureRepository _academic;
    private readonly ISchoolRepository _schools;
    private readonly ISchoolUserRepository _users;

    public AcademicStructureService(
        IAcademicStructureRepository academic,
        ISchoolRepository schools,
        ISchoolUserRepository users)
    {
        _academic = academic;
        _schools = schools;
        _users = users;
    }

    public async Task<AcademicQueryResult<AcademicStructureDashboard>> GetDashboardAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);

        if (!scope.Succeeded)
        {
            return AcademicQueryResult<AcademicStructureDashboard>
                .Failure(scope.Error!.Value);
        }

        var schoolId = scope.School!.Id;
        var snapshot = await _academic.GetSnapshotAsync(schoolId, cancellationToken);
        var users = await _users.ListBySchoolAsync(schoolId, cancellationToken);

        var years = snapshot.AcademicYears.ToDictionary(x => x.Id);
        var grades = snapshot.GradeLevels.ToDictionary(x => x.Id);
        var classes = snapshot.ClassGroups.ToDictionary(x => x.Id);
        var subjects = snapshot.Subjects.ToDictionary(x => x.Id);
        var profiles = snapshot.StudentProfiles.ToDictionary(x => x.Id);
        var userMap = users.ToDictionary(x => x.Id);

        var linkedStudentUsers = snapshot.StudentProfiles
            .Where(x => x.UserId.HasValue)
            .Select(x => x.UserId!.Value)
            .ToHashSet();

        var teacherCandidates = users
            .Where(x =>
                x.IsActive &&
                !x.IsLocked &&
                SingleRole(x.Roles) == RoleNames.Teacher)
            .OrderBy(x => x.Email)
            .Select(x => new UserCandidate(x.Id, x.Email))
            .ToArray();

        var studentCandidates = users
            .Where(x =>
                x.IsActive &&
                !x.IsLocked &&
                SingleRole(x.Roles) == RoleNames.Student &&
                !linkedStudentUsers.Contains(x.Id))
            .OrderBy(x => x.Email)
            .Select(x => new UserCandidate(x.Id, x.Email))
            .ToArray();

        var dashboard = new AcademicStructureDashboard(
            schoolId,
            scope.School.Name,
            snapshot.AcademicYears.Select(MapYear).ToArray(),
            snapshot.Terms.Select(x => new TermItem(
                x.Id,
                x.AcademicYearId,
                years.GetValueOrDefault(x.AcademicYearId)?.Name ?? string.Empty,
                x.Name,
                x.StartsOn,
                x.EndsOn,
                x.Status)).ToArray(),
            snapshot.GradeLevels.Select(x =>
                new GradeLevelItem(x.Id, x.Name, x.Order)).ToArray(),
            snapshot.ClassGroups.Select(x => new ClassGroupItem(
                x.Id,
                x.AcademicYearId,
                years.GetValueOrDefault(x.AcademicYearId)?.Name ?? string.Empty,
                x.GradeLevelId,
                grades.GetValueOrDefault(x.GradeLevelId)?.Name ?? string.Empty,
                x.Name,
                x.Code,
                x.Status,
                x.RowVersion)).ToArray(),
            snapshot.Subjects.Select(MapSubject).ToArray(),
            snapshot.TeacherAssignments.Select(x =>
            {
                var classGroup = classes.GetValueOrDefault(x.ClassGroupId);
                return new TeacherAssignmentItem(
                    x.Id,
                    userMap.GetValueOrDefault(x.TeacherUserId)?.Email ?? string.Empty,
                    classGroup?.Name ?? string.Empty,
                    classGroup?.Code ?? string.Empty,
                    subjects.GetValueOrDefault(x.SubjectId)?.Name ?? string.Empty,
                    years.GetValueOrDefault(x.AcademicYearId)?.Name ?? string.Empty);
            }).ToArray(),
            snapshot.StudentProfiles.Select(x => new StudentProfileItem(
                x.Id,
                x.StudentNumber,
                x.FirstName,
                x.LastName,
                x.DisplayName,
                x.UserId.HasValue
                    ? userMap.GetValueOrDefault(x.UserId.Value)?.Email
                    : null,
                x.Status)).ToArray(),
            snapshot.StudentEnrollments.Select(x =>
            {
                var classGroup = classes.GetValueOrDefault(x.ClassGroupId);
                return new StudentEnrollmentItem(
                    x.Id,
                    x.StudentProfileId,
                    profiles.GetValueOrDefault(x.StudentProfileId)?.DisplayName ??
                        string.Empty,
                    classGroup?.Name ?? string.Empty,
                    classGroup?.Code ?? string.Empty,
                    years.GetValueOrDefault(x.AcademicYearId)?.Name ?? string.Empty);
            }).ToArray(),
            teacherCandidates,
            studentCandidates);

        return AcademicQueryResult<AcademicStructureDashboard>.Success(dashboard);
    }

    public async Task<AcademicQueryResult<AcademicYearItem>> GetAcademicYearAsync(
        Guid actorUserId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);

        if (!scope.Succeeded)
        {
            return AcademicQueryResult<AcademicYearItem>.Failure(scope.Error!.Value);
        }

        var entity = await _academic.GetAcademicYearAsync(
            scope.School!.Id, id, cancellationToken);

        return entity is null
            ? AcademicQueryResult<AcademicYearItem>.Failure(
                AcademicStructureErrorCode.AcademicYearNotFound)
            : AcademicQueryResult<AcademicYearItem>.Success(MapYear(entity));
    }

    public async Task<AcademicQueryResult<ClassGroupItem>> GetClassGroupAsync(
        Guid actorUserId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);

        if (!scope.Succeeded)
        {
            return AcademicQueryResult<ClassGroupItem>.Failure(scope.Error!.Value);
        }

        var schoolId = scope.School!.Id;
        var entity = await _academic.GetClassGroupAsync(schoolId, id, cancellationToken);

        if (entity is null)
        {
            return AcademicQueryResult<ClassGroupItem>.Failure(
                AcademicStructureErrorCode.ClassGroupNotFound);
        }

        var year = await _academic.GetAcademicYearAsync(
            schoolId, entity.AcademicYearId, cancellationToken);

        var grade = await _academic.GetGradeLevelAsync(
            schoolId, entity.GradeLevelId, cancellationToken);

        return AcademicQueryResult<ClassGroupItem>.Success(new ClassGroupItem(
            entity.Id,
            entity.AcademicYearId,
            year?.Name ?? string.Empty,
            entity.GradeLevelId,
            grade?.Name ?? string.Empty,
            entity.Name,
            entity.Code,
            entity.Status,
            entity.RowVersion));
    }

    public async Task<AcademicQueryResult<SubjectItem>> GetSubjectAsync(
        Guid actorUserId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);

        if (!scope.Succeeded)
        {
            return AcademicQueryResult<SubjectItem>.Failure(scope.Error!.Value);
        }

        var entity = await _academic.GetSubjectAsync(
            scope.School!.Id, id, cancellationToken);

        return entity is null
            ? AcademicQueryResult<SubjectItem>.Failure(
                AcademicStructureErrorCode.SubjectNotFound)
            : AcademicQueryResult<SubjectItem>.Success(MapSubject(entity));
    }

    public async Task<AcademicCommandResult> CreateAcademicYearAsync(
        Guid actorUserId,
        CreateAcademicYearRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var name = Clean(request.Name);
        var validation = ValidateName(name);
        if (validation is not null) return validation;

        if (request.StartsOn >= request.EndsOn)
            return Fail(nameof(request.StartsOn), AcademicStructureErrorCode.InvalidDateRange);

        var schoolId = scope.School!.Id;

        if (await _academic.AcademicYearNameExistsAsync(
                schoolId, Normalize(name), cancellationToken: cancellationToken))
            return Fail(nameof(request.Name), AcademicStructureErrorCode.DuplicateAcademicYear);

        var now = DateTime.UtcNow;

        await _academic.AddAsync(new AcademicYear
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            StartsOn = request.StartsOn,
            EndsOn = request.EndsOn,
            Status = request.Status,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> UpdateAcademicYearAsync(
        Guid actorUserId,
        UpdateAcademicYearRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        if (request.ExpectedRowVersion.Length == 0)
            return Fail(AcademicStructureErrorCode.ConcurrencyConflict);

        var name = Clean(request.Name);
        var validation = ValidateName(name);
        if (validation is not null) return validation;

        if (request.StartsOn >= request.EndsOn)
            return Fail(nameof(request.StartsOn), AcademicStructureErrorCode.InvalidDateRange);

        var schoolId = scope.School!.Id;
        var entity = await _academic.GetAcademicYearAsync(
            schoolId, request.Id, cancellationToken);

        if (entity is null)
            return Fail(AcademicStructureErrorCode.AcademicYearNotFound);

        if (await _academic.AcademicYearNameExistsAsync(
                schoolId, Normalize(name), request.Id, cancellationToken))
            return Fail(nameof(request.Name), AcademicStructureErrorCode.DuplicateAcademicYear);

        var snapshot = await _academic.GetSnapshotAsync(schoolId, cancellationToken);

        if (snapshot.Terms.Any(x =>
                x.AcademicYearId == request.Id &&
                (x.StartsOn < request.StartsOn || x.EndsOn > request.EndsOn)))
            return Fail(nameof(request.StartsOn),
                AcademicStructureErrorCode.TermOutsideAcademicYear);

        entity.Name = name;
        entity.StartsOn = request.StartsOn;
        entity.EndsOn = request.EndsOn;
        entity.Status = request.Status;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        return MapPersistence(await _academic.SaveWithRowVersionAsync(
            entity, request.ExpectedRowVersion, cancellationToken));
    }

    public async Task<AcademicCommandResult> CreateTermAsync(
        Guid actorUserId,
        CreateTermRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var name = Clean(request.Name);
        var validation = ValidateName(name);
        if (validation is not null) return validation;

        if (request.StartsOn >= request.EndsOn)
            return Fail(nameof(request.StartsOn), AcademicStructureErrorCode.InvalidDateRange);

        var schoolId = scope.School!.Id;
        var year = await _academic.GetAcademicYearAsync(
            schoolId, request.AcademicYearId, cancellationToken);

        if (year is null)
            return Fail(nameof(request.AcademicYearId),
                AcademicStructureErrorCode.AcademicYearNotFound);

        if (request.StartsOn < year.StartsOn || request.EndsOn > year.EndsOn)
            return Fail(nameof(request.StartsOn),
                AcademicStructureErrorCode.TermOutsideAcademicYear);

        if (await _academic.TermNameExistsAsync(
                schoolId, year.Id, Normalize(name), cancellationToken))
            return Fail(nameof(request.Name), AcademicStructureErrorCode.DuplicateTerm);

        await _academic.AddAsync(new Term
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            AcademicYearId = year.Id,
            Name = name,
            StartsOn = request.StartsOn,
            EndsOn = request.EndsOn,
            Status = request.Status
        }, cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> CreateGradeLevelAsync(
        Guid actorUserId,
        CreateGradeLevelRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var name = Clean(request.Name);
        var validation = ValidateName(name);
        if (validation is not null) return validation;

        if (request.Order <= 0)
            return Fail(nameof(request.Order), AcademicStructureErrorCode.InvalidOrder);

        var schoolId = scope.School!.Id;

        if (await _academic.GradeLevelNameExistsAsync(
                schoolId, Normalize(name), cancellationToken))
            return Fail(nameof(request.Name),
                AcademicStructureErrorCode.DuplicateGradeLevel);

        if (await _academic.GradeLevelOrderExistsAsync(
                schoolId, request.Order, cancellationToken))
            return Fail(nameof(request.Order),
                AcademicStructureErrorCode.DuplicateGradeOrder);

        await _academic.AddAsync(new GradeLevel
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Order = request.Order
        }, cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> CreateClassGroupAsync(
        Guid actorUserId,
        CreateClassGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var name = Clean(request.Name);
        var code = NormalizeCode(request.Code);
        var validation = ValidateName(name);
        if (validation is not null) return validation;

        if (!ValidCode(code))
            return Fail(nameof(request.Code), AcademicStructureErrorCode.InvalidCode);

        var schoolId = scope.School!.Id;
        var year = await _academic.GetAcademicYearAsync(
            schoolId, request.AcademicYearId, cancellationToken);

        if (year is null)
            return Fail(nameof(request.AcademicYearId),
                AcademicStructureErrorCode.AcademicYearNotFound);

        var grade = await _academic.GetGradeLevelAsync(
            schoolId, request.GradeLevelId, cancellationToken);

        if (grade is null)
            return Fail(nameof(request.GradeLevelId),
                AcademicStructureErrorCode.GradeLevelNotFound);

        if (await _academic.ClassCodeExistsAsync(
                schoolId, year.Id, code, cancellationToken: cancellationToken))
            return Fail(nameof(request.Code),
                AcademicStructureErrorCode.DuplicateClassCode);

        await _academic.AddAsync(new ClassGroup
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            AcademicYearId = year.Id,
            GradeLevelId = grade.Id,
            Name = name,
            Code = code,
            NormalizedCode = code,
            Status = request.Status
        }, cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> UpdateClassGroupAsync(
        Guid actorUserId,
        UpdateClassGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        if (request.ExpectedRowVersion.Length == 0)
            return Fail(AcademicStructureErrorCode.ConcurrencyConflict);

        var name = Clean(request.Name);
        var code = NormalizeCode(request.Code);
        var validation = ValidateName(name);
        if (validation is not null) return validation;

        if (!ValidCode(code))
            return Fail(nameof(request.Code), AcademicStructureErrorCode.InvalidCode);

        var schoolId = scope.School!.Id;
        var entity = await _academic.GetClassGroupAsync(
            schoolId, request.Id, cancellationToken);

        if (entity is null)
            return Fail(AcademicStructureErrorCode.ClassGroupNotFound);

        var grade = await _academic.GetGradeLevelAsync(
            schoolId, request.GradeLevelId, cancellationToken);

        if (grade is null)
            return Fail(nameof(request.GradeLevelId),
                AcademicStructureErrorCode.GradeLevelNotFound);

        if (await _academic.ClassCodeExistsAsync(
                schoolId, entity.AcademicYearId, code,
                request.Id, cancellationToken))
            return Fail(nameof(request.Code),
                AcademicStructureErrorCode.DuplicateClassCode);

        entity.GradeLevelId = grade.Id;
        entity.Name = name;
        entity.Code = code;
        entity.NormalizedCode = code;
        entity.Status = request.Status;

        return MapPersistence(await _academic.SaveWithRowVersionAsync(
            entity, request.ExpectedRowVersion, cancellationToken));
    }

    public async Task<AcademicCommandResult> CreateSubjectAsync(
        Guid actorUserId,
        CreateSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var name = Clean(request.Name);
        var code = NormalizeCode(request.Code);
        var validation = ValidateName(name);
        if (validation is not null) return validation;

        if (!ValidCode(code))
            return Fail(nameof(request.Code), AcademicStructureErrorCode.InvalidCode);

        var schoolId = scope.School!.Id;

        if (await _academic.SubjectCodeExistsAsync(
                schoolId, code, cancellationToken: cancellationToken))
            return Fail(nameof(request.Code),
                AcademicStructureErrorCode.DuplicateSubjectCode);

        await _academic.AddAsync(new Subject
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Code = code,
            NormalizedCode = code,
            Status = request.Status
        }, cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> UpdateSubjectAsync(
        Guid actorUserId,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        if (request.ExpectedRowVersion.Length == 0)
            return Fail(AcademicStructureErrorCode.ConcurrencyConflict);

        var name = Clean(request.Name);
        var code = NormalizeCode(request.Code);
        var validation = ValidateName(name);
        if (validation is not null) return validation;

        if (!ValidCode(code))
            return Fail(nameof(request.Code), AcademicStructureErrorCode.InvalidCode);

        var schoolId = scope.School!.Id;
        var entity = await _academic.GetSubjectAsync(
            schoolId, request.Id, cancellationToken);

        if (entity is null)
            return Fail(AcademicStructureErrorCode.SubjectNotFound);

        if (await _academic.SubjectCodeExistsAsync(
                schoolId, code, request.Id, cancellationToken))
            return Fail(nameof(request.Code),
                AcademicStructureErrorCode.DuplicateSubjectCode);

        entity.Name = name;
        entity.Code = code;
        entity.NormalizedCode = code;
        entity.Status = request.Status;

        return MapPersistence(await _academic.SaveWithRowVersionAsync(
            entity, request.ExpectedRowVersion, cancellationToken));
    }

    public async Task<AcademicCommandResult> CreateTeacherAssignmentAsync(
        Guid actorUserId,
        CreateTeacherAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var schoolId = scope.School!.Id;
        var teacher = await _users.GetBySchoolAndIdAsync(
            schoolId, request.TeacherUserId, cancellationToken);

        if (teacher is null ||
            !teacher.IsActive ||
            teacher.IsLocked ||
            SingleRole(teacher.Roles) != RoleNames.Teacher)
            return Fail(nameof(request.TeacherUserId),
                AcademicStructureErrorCode.InvalidTeacher);

        var classGroup = await _academic.GetClassGroupAsync(
            schoolId, request.ClassGroupId, cancellationToken);

        if (classGroup is null)
            return Fail(nameof(request.ClassGroupId),
                AcademicStructureErrorCode.ClassGroupNotFound);

        var subject = await _academic.GetSubjectAsync(
            schoolId, request.SubjectId, cancellationToken);

        if (subject is null)
            return Fail(nameof(request.SubjectId),
                AcademicStructureErrorCode.SubjectNotFound);

        if (await _academic.TeacherAssignmentExistsAsync(
                schoolId, teacher.Id, classGroup.Id, subject.Id, cancellationToken))
            return Fail(AcademicStructureErrorCode.DuplicateTeacherAssignment);

        await _academic.AddAsync(new TeacherAssignment
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            TeacherUserId = teacher.Id,
            ClassGroupId = classGroup.Id,
            SubjectId = subject.Id,
            AcademicYearId = classGroup.AcademicYearId,
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> CreateStudentProfileAsync(
        Guid actorUserId,
        CreateStudentProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var studentNumber = NormalizeCode(request.StudentNumber);

        if (!ValidCode(studentNumber))
            return Fail(nameof(request.StudentNumber),
                AcademicStructureErrorCode.InvalidCode);

        var firstName = Clean(request.FirstName);
        var lastName = Clean(request.LastName);

        if (firstName.Length == 0)
            return Fail(nameof(request.FirstName), AcademicStructureErrorCode.Required);

        if (lastName.Length == 0)
            return Fail(nameof(request.LastName), AcademicStructureErrorCode.Required);

        if (firstName.Length > 100 || lastName.Length > 100)
            return Fail(AcademicStructureErrorCode.InvalidName);

        var schoolId = scope.School!.Id;

        if (await _academic.StudentNumberExistsAsync(
                schoolId, studentNumber, cancellationToken))
            return Fail(nameof(request.StudentNumber),
                AcademicStructureErrorCode.DuplicateStudentNumber);

        if (request.UserId.HasValue)
        {
            var user = await _users.GetBySchoolAndIdAsync(
                schoolId, request.UserId.Value, cancellationToken);

            if (user is null ||
                !user.IsActive ||
                user.IsLocked ||
                SingleRole(user.Roles) != RoleNames.Student)
                return Fail(nameof(request.UserId),
                    AcademicStructureErrorCode.InvalidStudentAccount);

            if (await _academic.StudentUserLinkExistsAsync(
                    schoolId, user.Id, cancellationToken))
                return Fail(nameof(request.UserId),
                    AcademicStructureErrorCode.DuplicateStudentUserLink);
        }

        var now = DateTime.UtcNow;

        await _academic.AddAsync(new StudentProfile
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            UserId = request.UserId,
            StudentNumber = studentNumber,
            NormalizedStudentNumber = studentNumber,
            FirstName = firstName,
            LastName = lastName,
            DisplayName = $"{firstName} {lastName}".Trim(),
            Status = request.Status,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        }, cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> CreateStudentEnrollmentAsync(
        Guid actorUserId,
        CreateStudentEnrollmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var schoolId = scope.School!.Id;
        var profile = await _academic.GetStudentProfileAsync(
            schoolId, request.StudentProfileId, cancellationToken);

        if (profile is null)
            return Fail(nameof(request.StudentProfileId),
                AcademicStructureErrorCode.StudentProfileNotFound);

        var classGroup = await _academic.GetClassGroupAsync(
            schoolId, request.ClassGroupId, cancellationToken);

        if (classGroup is null)
            return Fail(nameof(request.ClassGroupId),
                AcademicStructureErrorCode.ClassGroupNotFound);

        if (await _academic.StudentEnrollmentExistsAsync(
                schoolId, classGroup.AcademicYearId, profile.Id, cancellationToken))
            return Fail(AcademicStructureErrorCode.DuplicateEnrollment);

        await _academic.AddAsync(new StudentEnrollment
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            StudentProfileId = profile.Id,
            ClassGroupId = classGroup.Id,
            AcademicYearId = classGroup.AcademicYearId,
            EnrolledAtUtc = DateTime.UtcNow
        }, cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    private async Task<ScopeResult> ResolveScopeAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var actor = await _users.GetActorAsync(actorUserId, cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            !actor.SchoolId.HasValue ||
            SingleRole(actor.Roles) != RoleNames.SchoolAdmin)
            return ScopeResult.Fail(AcademicStructureErrorCode.AccessDenied);

        var school = await _schools.GetByIdAsync(
            actor.SchoolId.Value, cancellationToken);

        if (school is null || school.Status != SchoolStatus.Active)
            return ScopeResult.Fail(AcademicStructureErrorCode.SchoolNotActive);

        return ScopeResult.Ok(actor, school);
    }

    private async Task<AcademicCommandResult> PersistAsync(
        CancellationToken cancellationToken) =>
        MapPersistence(await _academic.SaveAsync(cancellationToken));

    private static AcademicCommandResult MapPersistence(
        AcademicPersistenceResult result)
    {
        if (result.Succeeded)
            return AcademicCommandResult.Success();

        return result.Error == AcademicPersistenceError.Conflict
            ? Fail(AcademicStructureErrorCode.ConcurrencyConflict)
            : Fail(AcademicStructureErrorCode.PersistenceError);
    }

    private static AcademicCommandResult? ValidateName(string value)
    {
        if (value.Length == 0)
            return Fail("Name", AcademicStructureErrorCode.Required);

        return value.Length > 150
            ? Fail("Name", AcademicStructureErrorCode.InvalidName)
            : null;
    }

    private static bool ValidCode(string value) =>
        value.Length is > 0 and <= 50 && CodePattern.IsMatch(value);

    private static string Clean(string? value) =>
        value?.Trim() ?? string.Empty;

    private static string Normalize(string value) =>
        value.Trim().ToUpperInvariant();

    private static string NormalizeCode(string? value) =>
        Normalize(value ?? string.Empty);

    private static string? SingleRole(IReadOnlyList<string> roles) =>
        roles.Count == 1 ? roles[0] : null;

    private static AcademicYearItem MapYear(AcademicYear x) =>
        new(x.Id, x.Name, x.StartsOn, x.EndsOn, x.Status, x.RowVersion);

    private static SubjectItem MapSubject(Subject x) =>
        new(x.Id, x.Name, x.Code, x.Status, x.RowVersion);

    private static AcademicCommandResult Fail(AcademicStructureErrorCode code) =>
        AcademicCommandResult.Failure(string.Empty, code);

    private static AcademicCommandResult Fail(
        string field,
        AcademicStructureErrorCode code) =>
        AcademicCommandResult.Failure(field, code);

    private sealed record ScopeResult(
        bool Succeeded,
        SchoolUserRecord? Actor,
        School? School,
        AcademicStructureErrorCode? Error)
    {
        public static ScopeResult Ok(SchoolUserRecord actor, School school) =>
            new(true, actor, school, null);

        public static ScopeResult Fail(AcademicStructureErrorCode error) =>
            new(false, null, null, error);
    }
}
