using System.Text.RegularExpressions;
using Edulytics.Core.Academics;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;
using Edulytics.Services.Auditing;

namespace Edulytics.Services.Academics;

public sealed class AcademicStructureService : IAcademicStructureService
{
    private static readonly Regex CodePattern = new(
        "^[A-Z0-9-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly IAcademicStructureRepository _academic;
    private readonly ISchoolRepository _schools;
    private readonly ISchoolUserRepository _users;
    private readonly IAuditService? _audit;

    public AcademicStructureService(
        IAcademicStructureRepository academic,
        ISchoolRepository schools,
        ISchoolUserRepository users,
        IAuditService? audit = null)
    {
        _academic = academic;
        _schools = schools;
        _users = users;
        _audit = audit;
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
        var programs = snapshot.AcademicPrograms.ToDictionary(x => x.Id);
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
                x.RowVersion)
            {
                AcademicProgramId = x.AcademicProgramId,
                AcademicProgramName = programs.GetValueOrDefault(x.AcademicProgramId)?.Name ?? string.Empty,
                AcademicProgramCode = programs.GetValueOrDefault(x.AcademicProgramId)?.Code ?? string.Empty
            }).ToArray(),
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
                x.Status,
                x.IsArchived,
                x.ArchivedAtUtc,
                x.RowVersion.ToArray())).ToArray(),
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
            studentCandidates)
        {
            AcademicPrograms = snapshot.AcademicPrograms
                .OrderBy(x => x.Name)
                .Select(x => new AcademicProgramItem(
                    x.Id, x.Name, x.Code, x.Status, x.IsDefault, x.RowVersion))
                .ToArray(),

            AcademicYearProgramOfferings =
                snapshot.AcademicYearProgramOfferings
                    .Select(
                        x =>
                            new AcademicYearProgramOfferingItem(
                                x.Id,
                                x.AcademicYearId,
                                x.AcademicProgramId,
                                x.IsOffered,
                                x.RowVersion))
                    .ToArray()
        };

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

        var program = await _academic.GetAcademicProgramAsync(
            schoolId, entity.AcademicProgramId, cancellationToken);

        return AcademicQueryResult<ClassGroupItem>.Success(new ClassGroupItem(
            entity.Id,
            entity.AcademicYearId,
            year?.Name ?? string.Empty,
            entity.GradeLevelId,
            grade?.Name ?? string.Empty,
            entity.Name,
            entity.Code,
            entity.Status,
            entity.RowVersion)
        {
            AcademicProgramId = entity.AcademicProgramId,
            AcademicProgramName = program?.Name ?? string.Empty,
            AcademicProgramCode = program?.Code ?? string.Empty
        });
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
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);
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

        var entity = new AcademicYear
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            StartsOn = request.StartsOn,
            EndsOn = request.EndsOn,
            Status = request.Status,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _academic.AddAsync(
            entity,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "AcademicYear.Created",
            "AcademicYear",
            entity.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["name"] = entity.Name,
                    ["startsOn"] = entity.StartsOn,
                    ["endsOn"] = entity.EndsOn,
                    ["status"] =
                        entity.Status.ToString()
                },
            "Academic year created.",
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> UpdateAcademicYearAsync(
        Guid actorUserId,
        UpdateAcademicYearRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);
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

        var oldValues =
            new Dictionary<string, object?>
            {
                ["name"] = entity.Name,
                ["startsOn"] = entity.StartsOn,
                ["endsOn"] = entity.EndsOn,
                ["status"] =
                    entity.Status.ToString()
            };

        entity.Name = name;
        entity.StartsOn = request.StartsOn;
        entity.EndsOn = request.EndsOn;
        entity.Status = request.Status;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await QueueAuditAsync(
            scope,
            "AcademicYear.Updated",
            "AcademicYear",
            entity.Id,
            oldValues,
            new Dictionary<string, object?>
            {
                ["name"] = entity.Name,
                ["startsOn"] = entity.StartsOn,
                ["endsOn"] = entity.EndsOn,
                ["status"] =
                    entity.Status.ToString()
            },
            "Academic year updated.",
            cancellationToken);

        return MapPersistence(
            await _academic.SaveWithRowVersionAsync(
                entity,
                request.ExpectedRowVersion,
                cancellationToken));
    }

    public async Task<AcademicCommandResult> CreateTermAsync(
        Guid actorUserId,
        CreateTermRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);
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

        var entity = new Term
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            AcademicYearId = year.Id,
            Name = name,
            StartsOn = request.StartsOn,
            EndsOn = request.EndsOn,
            Status = request.Status
        };

        await _academic.AddAsync(
            entity,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "Term.Created",
            "Term",
            entity.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["academicYearId"] =
                        entity.AcademicYearId,
                    ["name"] = entity.Name,
                    ["startsOn"] = entity.StartsOn,
                    ["endsOn"] = entity.EndsOn,
                    ["status"] =
                        entity.Status.ToString()
                },
            "Academic term created.",
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> CreateGradeLevelAsync(
        Guid actorUserId,
        CreateGradeLevelRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);
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

        var entity = new GradeLevel
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Order = request.Order
        };

        await _academic.AddAsync(
            entity,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "GradeLevel.Created",
            "GradeLevel",
            entity.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["name"] = entity.Name,
                    ["order"] = entity.Order
                },
            "Grade level created.",
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> CreateAcademicProgramAsync(
        Guid actorUserId,
        CreateAcademicProgramRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope =
            await ResolveScopeAsync(
                actorUserId,
                cancellationToken);

        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) !=
            RoleNames.SubjectSupervisor)
        {
            return Fail(
                AcademicStructureErrorCode.AccessDenied);
        }

        var code =
            NormalizeCode(request.Code);

        var choice =
            AcademicProgramCatalog.FindByCode(code);

        if (choice is null)
        {
            return Fail(
                nameof(request.Code),
                AcademicStructureErrorCode
                    .AcademicProgramNotFound);
        }

        var schoolId =
            scope.School!.Id;

        // Duplicate is resolved before comparing the submitted name so
        // existing callers receive the stable duplicate-program result.
        if (await _academic
                .AcademicProgramCodeExistsAsync(
                    schoolId,
                    choice.Code,
                    cancellationToken))
        {
            return Fail(
                nameof(request.Code),
                AcademicStructureErrorCode
                    .DuplicateAcademicProgram);
        }

        var submittedName =
            Clean(request.Name);

        // The friendly name and technical code are a controlled pair.
        // A crafted/internal caller cannot create an arbitrary combination.
        if (!string.Equals(
                submittedName,
                choice.Name,
                StringComparison.Ordinal))
        {
            return Fail(
                nameof(request.Name),
                AcademicStructureErrorCode.InvalidName);
        }

        var now =
            DateTime.UtcNow;

        var entity =
            new AcademicProgram
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                Name = choice.Name,
                Code = choice.Code,
                NormalizedCode = choice.Code,
                Status = request.Status,
                IsDefault = false,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

        await _academic.AddAsync(
            entity,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "AcademicProgram.Created",
            "AcademicProgram",
            entity.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["name"] = entity.Name,
                    ["code"] = entity.Code,
                    ["status"] =
                        entity.Status.ToString(),
                    ["isDefault"] =
                        entity.IsDefault
                },
            "Academic program / curriculum stream created.",
            cancellationToken);

        return await PersistAsync(
            cancellationToken);
    }

    public async Task<AcademicCommandResult>
        OfferAcademicProgramAsync(
            Guid actorUserId,
            OfferAcademicProgramRequest request,
            CancellationToken cancellationToken = default)
    {
        var scope =
            await ResolveScopeAsync(
                actorUserId,
                cancellationToken);

        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) !=
            RoleNames.SubjectSupervisor)
        {
            return Fail(
                AcademicStructureErrorCode.AccessDenied);
        }

        var schoolId =
            scope.School!.Id;

        var year =
            await _academic.GetAcademicYearAsync(
                schoolId,
                request.AcademicYearId,
                cancellationToken);

        if (year is null)
        {
            return Fail(
                nameof(request.AcademicYearId),
                AcademicStructureErrorCode
                    .AcademicYearNotFound);
        }

        var choice =
            AcademicProgramCatalog.FindByKey(
                request.ProgramChoice);

        if (choice is null)
        {
            return Fail(
                nameof(request.ProgramChoice),
                AcademicStructureErrorCode
                    .AcademicProgramNotFound);
        }

        var program =
            await _academic
                .GetAcademicProgramByCodeAsync(
                    schoolId,
                    choice.Code,
                    cancellationToken);

        var now =
            DateTime.UtcNow;

        if (program is null)
        {
            program =
                new AcademicProgram
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    Name = choice.Name,
                    Code = choice.Code,
                    NormalizedCode = choice.Code,
                    Status =
                        AcademicStructureStatus.Active,
                    IsDefault = false,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };

            await _academic.AddAsync(
                program,
                cancellationToken);
        }
        else
        {
            if (program.IsDefault)
            {
                return Fail(
                    nameof(request.ProgramChoice),
                    AcademicStructureErrorCode
                        .AcademicProgramNotFound);
            }

            // Catalog identity is authoritative.
            program.Name = choice.Name;
            program.Code = choice.Code;
            program.NormalizedCode = choice.Code;
            program.Status =
                AcademicStructureStatus.Active;
            program.UpdatedAtUtc = now;
        }

        var offering =
            await _academic
                .GetAcademicYearProgramOfferingAsync(
                    schoolId,
                    year.Id,
                    program.Id,
                    cancellationToken);

        if (offering is not null &&
            offering.IsOffered)
        {
            return Fail(
                nameof(request.ProgramChoice),
                AcademicStructureErrorCode
                    .DuplicateAcademicYearProgramOffering);
        }

        if (offering is null)
        {
            offering =
                new AcademicYearProgramOffering
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolId,
                    AcademicYearId = year.Id,
                    AcademicProgramId = program.Id,
                    IsOffered = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };

            await _academic.AddAsync(
                offering,
                cancellationToken);
        }
        else
        {
            offering.IsOffered = true;
            offering.UpdatedAtUtc = now;
        }

        await QueueAuditAsync(
            scope,
            "AcademicProgram.OfferedForAcademicYear",
            "AcademicYearProgramOffering",
            offering.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["academicYearId"] =
                        year.Id,
                    ["academicProgramId"] =
                        program.Id,
                    ["programName"] =
                        program.Name,
                    ["isOffered"] =
                        true
                },
            "Academic program offered for academic year.",
            cancellationToken);

        return await PersistAsync(
            cancellationToken);
    }

    public async Task<AcademicCommandResult>
        StopAcademicProgramOfferingAsync(
            Guid actorUserId,
            StopAcademicProgramOfferingRequest request,
            CancellationToken cancellationToken = default)
    {
        var scope =
            await ResolveScopeAsync(
                actorUserId,
                cancellationToken);

        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) !=
            RoleNames.SubjectSupervisor)
        {
            return Fail(
                AcademicStructureErrorCode.AccessDenied);
        }

        if (request.ExpectedRowVersion.Length == 0)
        {
            return Fail(
                AcademicStructureErrorCode
                    .ConcurrencyConflict);
        }

        var schoolId =
            scope.School!.Id;

        var year =
            await _academic.GetAcademicYearAsync(
                schoolId,
                request.AcademicYearId,
                cancellationToken);

        if (year is null)
        {
            return Fail(
                nameof(request.AcademicYearId),
                AcademicStructureErrorCode
                    .AcademicYearNotFound);
        }

        var program =
            await _academic.GetAcademicProgramAsync(
                schoolId,
                request.AcademicProgramId,
                cancellationToken);

        if (program is null ||
            program.IsDefault)
        {
            return Fail(
                nameof(request.AcademicProgramId),
                AcademicStructureErrorCode
                    .AcademicProgramNotFound);
        }

        var offering =
            await _academic
                .GetAcademicYearProgramOfferingAsync(
                    schoolId,
                    year.Id,
                    program.Id,
                    cancellationToken);

        if (offering is null ||
            !offering.IsOffered)
        {
            return Fail(
                nameof(request.AcademicProgramId),
                AcademicStructureErrorCode
                    .AcademicProgramNotOffered);
        }

        if (await _academic
                .AcademicYearProgramHasUsageAsync(
                    schoolId,
                    year.Id,
                    program.Id,
                    cancellationToken))
        {
            return Fail(
                nameof(request.AcademicProgramId),
                AcademicStructureErrorCode
                    .AcademicProgramInUseForAcademicYear);
        }

        offering.IsOffered = false;
        offering.UpdatedAtUtc =
            DateTime.UtcNow;

        await QueueAuditAsync(
            scope,
            "AcademicProgram.StoppedForAcademicYear",
            "AcademicYearProgramOffering",
            offering.Id,
            oldValues:
                new Dictionary<string, object?>
                {
                    ["isOffered"] = true
                },
            newValues:
                new Dictionary<string, object?>
                {
                    ["academicYearId"] =
                        year.Id,
                    ["academicProgramId"] =
                        program.Id,
                    ["programName"] =
                        program.Name,
                    ["isOffered"] =
                        false
                },
            "Academic program stopped for academic year.",
            cancellationToken);

        return MapPersistence(
            await _academic
                .SaveWithRowVersionAsync(
                    offering,
                    request.ExpectedRowVersion,
                    cancellationToken));
    }

    public async Task<AcademicCommandResult> CreateClassGroupAsync(
        Guid actorUserId,
        CreateClassGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);
        var name = Clean(request.Name);
        var requestedCode = NormalizeCode(request.Code);
        var validation = ValidateName(name);
        if (validation is not null) return validation;

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

        if (request.AcademicProgramId == Guid.Empty)
        {
            return Fail(
                nameof(request.AcademicProgramId),
                AcademicStructureErrorCode
                    .AcademicProgramNotFound);
        }

        var program =
            await _academic.GetAcademicProgramAsync(
                schoolId,
                request.AcademicProgramId,
                cancellationToken);

        if (program is null ||
            program.IsDefault ||
            program.Status != AcademicStructureStatus.Active)
        {
            return Fail(
                nameof(request.AcademicProgramId),
                AcademicStructureErrorCode
                    .AcademicProgramNotFound);
        }

        if (!await _academic
                .AcademicYearProgramIsOfferedAsync(
                    schoolId,
                    year.Id,
                    program.Id,
                    cancellationToken))
        {
            return Fail(
                nameof(request.AcademicProgramId),
                AcademicStructureErrorCode
                    .AcademicProgramNotOffered);
        }

        var programId =
            program.Id;

        var entityId =
            Guid.NewGuid();

        var code =
            requestedCode.Length == 0
                ? GenerateInternalClassCode(entityId)
                : requestedCode;

        if (!ValidCode(code))
        {
            return Fail(
                nameof(request.Code),
                AcademicStructureErrorCode.InvalidCode);
        }

        if (await _academic.ClassCodeExistsInProgramAsync(
                schoolId, year.Id, programId, code, cancellationToken: cancellationToken))
            return Fail(nameof(request.Code),
                AcademicStructureErrorCode.DuplicateClassCode);

        var entity = new ClassGroup
        {
            Id = entityId,
            SchoolId = schoolId,
            AcademicYearId = year.Id,
            AcademicProgramId = programId,
            GradeLevelId = grade.Id,
            Name = name,
            Code = code,
            NormalizedCode = code,
            Status = request.Status
        };

        await _academic.AddAsync(
            entity,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "ClassGroup.Created",
            "ClassGroup",
            entity.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["academicYearId"] =
                        entity.AcademicYearId,
                    ["gradeLevelId"] =
                        entity.GradeLevelId,
                    ["name"] = entity.Name,
                    ["code"] = entity.Code,
                    ["status"] =
                        entity.Status.ToString()
                },
            "Class group created.",
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> UpdateClassGroupAsync(
        Guid actorUserId,
        UpdateClassGroupRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);
        if (request.ExpectedRowVersion.Length == 0)
            return Fail(AcademicStructureErrorCode.ConcurrencyConflict);

        var name = Clean(request.Name);
        var validation = ValidateName(name);
        if (validation is not null) return validation;

        var schoolId = scope.School!.Id;
        var entity = await _academic.GetClassGroupAsync(
            schoolId, request.Id, cancellationToken);

        if (entity is null)
            return Fail(AcademicStructureErrorCode.ClassGroupNotFound);

        var code =
            string.IsNullOrWhiteSpace(request.Code)
                ? entity.Code
                : NormalizeCode(request.Code);

        if (!ValidCode(code))
        {
            return Fail(
                nameof(request.Code),
                AcademicStructureErrorCode.InvalidCode);
        }

        var grade = await _academic.GetGradeLevelAsync(
            schoolId, request.GradeLevelId, cancellationToken);

        if (grade is null)
            return Fail(nameof(request.GradeLevelId),
                AcademicStructureErrorCode.GradeLevelNotFound);

        var requestedProgramId =
            request.AcademicProgramId == Guid.Empty
                ? entity.AcademicProgramId
                : request.AcademicProgramId;

        var program =
            await _academic.GetAcademicProgramAsync(
                schoolId,
                requestedProgramId,
                cancellationToken);

        if (program is null)
        {
            return Fail(
                nameof(request.AcademicProgramId),
                AcademicStructureErrorCode
                    .AcademicProgramNotFound);
        }

        if (!program.IsDefault &&
            !await _academic
                .AcademicYearProgramIsOfferedAsync(
                    schoolId,
                    entity.AcademicYearId,
                    program.Id,
                    cancellationToken))
        {
            return Fail(
                nameof(request.AcademicProgramId),
                AcademicStructureErrorCode
                    .AcademicProgramNotOffered);
        }

        var programId =
            program.Id;

        if (await _academic.ClassCodeExistsInProgramAsync(
                schoolId, entity.AcademicYearId, programId, code,
                request.Id, cancellationToken))
            return Fail(nameof(request.Code),
                AcademicStructureErrorCode.DuplicateClassCode);

        var oldValues =
            new Dictionary<string, object?>
            {
                ["gradeLevelId"] =
                    entity.GradeLevelId,
                ["name"] = entity.Name,
                ["code"] = entity.Code,
                ["status"] =
                    entity.Status.ToString()
            };

        entity.AcademicProgramId = programId;
        entity.GradeLevelId = grade.Id;
        entity.Name = name;
        entity.Code = code;
        entity.NormalizedCode = code;
        entity.Status = request.Status;

        await QueueAuditAsync(
            scope,
            "ClassGroup.Updated",
            "ClassGroup",
            entity.Id,
            oldValues,
            new Dictionary<string, object?>
            {
                ["gradeLevelId"] =
                    entity.GradeLevelId,
                ["name"] = entity.Name,
                ["code"] = entity.Code,
                ["status"] =
                    entity.Status.ToString()
            },
            "Class group updated.",
            cancellationToken);

        return MapPersistence(
            await _academic.SaveWithRowVersionAsync(
                entity,
                request.ExpectedRowVersion,
                cancellationToken));
    }

    public async Task<AcademicCommandResult> CreateSubjectAsync(
        Guid actorUserId,
        CreateSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);
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

        var entity = new Subject
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            Name = name,
            Code = code,
            NormalizedCode = code,
            Status = request.Status
        };

        await _academic.AddAsync(
            entity,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "Subject.Created",
            "Subject",
            entity.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["name"] = entity.Name,
                    ["code"] = entity.Code,
                    ["status"] =
                        entity.Status.ToString()
                },
            "Subject created.",
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> UpdateSubjectAsync(
        Guid actorUserId,
        UpdateSubjectRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);
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

        var oldValues =
            new Dictionary<string, object?>
            {
                ["name"] = entity.Name,
                ["code"] = entity.Code,
                ["status"] =
                    entity.Status.ToString()
            };

        entity.Name = name;
        entity.Code = code;
        entity.NormalizedCode = code;
        entity.Status = request.Status;

        await QueueAuditAsync(
            scope,
            "Subject.Updated",
            "Subject",
            entity.Id,
            oldValues,
            new Dictionary<string, object?>
            {
                ["name"] = entity.Name,
                ["code"] = entity.Code,
                ["status"] =
                    entity.Status.ToString()
            },
            "Subject updated.",
            cancellationToken);

        return MapPersistence(
            await _academic.SaveWithRowVersionAsync(
                entity,
                request.ExpectedRowVersion,
                cancellationToken));
    }

    public async Task<AcademicCommandResult> CreateTeacherAssignmentAsync(
        Guid actorUserId,
        CreateTeacherAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);
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

        var entity = new TeacherAssignment
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            TeacherUserId = teacher.Id,
            ClassGroupId = classGroup.Id,
            SubjectId = subject.Id,
            AcademicYearId =
                classGroup.AcademicYearId,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _academic.AddAsync(
            entity,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "TeacherAssignment.Created",
            "TeacherAssignment",
            entity.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["teacherUserId"] =
                        entity.TeacherUserId,
                    ["classGroupId"] =
                        entity.ClassGroupId,
                    ["subjectId"] =
                        entity.SubjectId,
                    ["academicYearId"] =
                        entity.AcademicYearId
                },
            "Teacher assignment created.",
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<AcademicCommandResult> CreateStudentProfileAsync(
        Guid actorUserId,
        CreateStudentProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);
        var studentNumber = NormalizeCode(request.StudentNumber);
        if (!ValidCode(studentNumber))
            return Fail(nameof(request.StudentNumber), AcademicStructureErrorCode.InvalidCode);

        var firstName = Clean(request.FirstName);
        var lastName = Clean(request.LastName);

        if (string.IsNullOrWhiteSpace(firstName))
            return Fail(nameof(request.FirstName), AcademicStructureErrorCode.Required);

        if (string.IsNullOrWhiteSpace(lastName))
            return Fail(nameof(request.LastName), AcademicStructureErrorCode.Required);

        if (firstName.Length > 100 || lastName.Length > 100)
            return Fail(AcademicStructureErrorCode.InvalidName);

        var schoolId = scope.School!.Id;

        if (await _academic.StudentNumberExistsAsync(
                schoolId,
                studentNumber,
                cancellationToken))
        {
            return Fail(
                nameof(request.StudentNumber),
                AcademicStructureErrorCode.DuplicateStudentNumber);
        }

        if (request.UserId.HasValue)
        {
            var user = await _users.GetBySchoolAndIdAsync(
                schoolId,
                request.UserId.Value,
                cancellationToken);

            if (user is null ||
                !user.IsActive ||
                user.IsLocked ||
                SingleRole(user.Roles) != RoleNames.Student)
            {
                return Fail(
                    nameof(request.UserId),
                    AcademicStructureErrorCode.InvalidStudentAccount);
            }

            if (await _academic.StudentUserLinkExistsAsync(
                    schoolId,
                    user.Id,
                    cancellationToken))
            {
                return Fail(
                    nameof(request.UserId),
                    AcademicStructureErrorCode.DuplicateStudentUserLink);
            }
        }

        var now = DateTime.UtcNow;
        var entity = new StudentProfile
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
            IsArchived = false,
            ArchivedAtUtc = null,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            RowVersion = []
        };

        await QueueAuditAsync(
            scope,
            "StudentProfile.Created",
            "StudentProfile",
            entity.Id,
            oldValues: null,
            new Dictionary<string, object?>
            {
                ["studentNumber"] = entity.StudentNumber,
                ["displayName"] = entity.DisplayName,
                ["status"] = entity.Status.ToString(),
                ["isArchived"] = false
            },
            "Student profile created.",
            cancellationToken);

        return MapPersistence(
            await _academic.AddStudentProfileWithSeatGuardAsync(
                entity,
                cancellationToken));
    }

    public async Task<AcademicCommandResult> ArchiveStudentProfileAsync(
        Guid actorUserId,
        Guid studentProfileId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);

        if (expectedRowVersion.Length == 0)
            return Fail(AcademicStructureErrorCode.ConcurrencyConflict);

        var entity = await _academic.GetStudentProfileAsync(
            scope.School!.Id,
            studentProfileId,
            cancellationToken);

        if (entity is null)
            return Fail(AcademicStructureErrorCode.StudentProfileNotFound);

        if (entity.IsArchived)
            return Fail(AcademicStructureErrorCode.StudentAlreadyArchived);

        var now = DateTime.UtcNow;
        entity.IsArchived = true;
        entity.ArchivedAtUtc = now;
        entity.UpdatedAtUtc = now;

        await QueueAuditAsync(
            scope,
            "StudentProfile.Archived",
            "StudentProfile",
            entity.Id,
            new Dictionary<string, object?> { ["isArchived"] = false },
            new Dictionary<string, object?>
            {
                ["isArchived"] = true,
                ["archivedAtUtc"] = entity.ArchivedAtUtc
            },
            "Student profile archived; active commercial seat released.",
            cancellationToken);

        return MapPersistence(
            await _academic.SaveStudentArchiveStateWithSeatGuardAsync(
                entity,
                expectedRowVersion,
                restoring: false,
                cancellationToken));
    }

    public async Task<AcademicCommandResult> RestoreStudentProfileAsync(
        Guid actorUserId,
        Guid studentProfileId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);

        if (expectedRowVersion.Length == 0)
            return Fail(AcademicStructureErrorCode.ConcurrencyConflict);

        var entity = await _academic.GetStudentProfileAsync(
            scope.School!.Id,
            studentProfileId,
            cancellationToken);

        if (entity is null)
            return Fail(AcademicStructureErrorCode.StudentProfileNotFound);

        if (!entity.IsArchived)
            return Fail(AcademicStructureErrorCode.StudentNotArchived);

        entity.IsArchived = false;
        entity.ArchivedAtUtc = null;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await QueueAuditAsync(
            scope,
            "StudentProfile.Restored",
            "StudentProfile",
            entity.Id,
            new Dictionary<string, object?> { ["isArchived"] = true },
            new Dictionary<string, object?> { ["isArchived"] = false },
            "Student profile restored; active commercial seat re-evaluated.",
            cancellationToken);

        return MapPersistence(
            await _academic.SaveStudentArchiveStateWithSeatGuardAsync(
                entity,
                expectedRowVersion,
                restoring: true,
                cancellationToken));
    }

    public async Task<AcademicCommandResult> CreateStudentEnrollmentAsync(
        Guid actorUserId,
        CreateStudentEnrollmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(AcademicStructureErrorCode.AccessDenied);
        var schoolId = scope.School!.Id;
        var profile = await _academic.GetStudentProfileAsync(
            schoolId, request.StudentProfileId, cancellationToken);

        if (profile is null)
            return Fail(nameof(request.StudentProfileId),
                AcademicStructureErrorCode.StudentProfileNotFound);

        if (profile.IsArchived)
            return Fail(nameof(request.StudentProfileId),
                AcademicStructureErrorCode.StudentAlreadyArchived);

        var classGroup = await _academic.GetClassGroupAsync(
            schoolId, request.ClassGroupId, cancellationToken);

        if (classGroup is null)
            return Fail(nameof(request.ClassGroupId),
                AcademicStructureErrorCode.ClassGroupNotFound);

        if (await _academic.StudentEnrollmentExistsAsync(
                schoolId, classGroup.AcademicYearId, profile.Id, cancellationToken))
            return Fail(AcademicStructureErrorCode.DuplicateEnrollment);

        var entity = new StudentEnrollment
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            StudentProfileId = profile.Id,
            ClassGroupId = classGroup.Id,
            AcademicYearId =
                classGroup.AcademicYearId,
            EnrolledAtUtc = DateTime.UtcNow
        };

        await _academic.AddAsync(
            entity,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "StudentEnrollment.Created",
            "StudentEnrollment",
            entity.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["studentProfileId"] =
                        entity.StudentProfileId,
                    ["classGroupId"] =
                        entity.ClassGroupId,
                    ["academicYearId"] =
                        entity.AcademicYearId
                },
            "Student enrollment created.",
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    private async Task QueueAuditAsync(
        ScopeResult scope,
        string action,
        string entityType,
        Guid entityId,
        IReadOnlyDictionary<string, object?>? oldValues,
        IReadOnlyDictionary<string, object?>? newValues,
        string resultSummary,
        CancellationToken cancellationToken)
    {
        if (_audit is null ||
            scope.School is null ||
            scope.Actor is null)
        {
            return;
        }

        await _audit.QueueAsync(
            new AuditEvent(
                SchoolId:
                    scope.School.Id,
                Action:
                    action,
                EntityType:
                    entityType,
                EntityId:
                    entityId.ToString("D"),
                Feature:
                    "AcademicStructure",
                OldValues:
                    oldValues,
                NewValues:
                    newValues,
                ResultSummary:
                    resultSummary,
                ActorUserIdOverride:
                    scope.Actor.Id,
                ActorRoleOverride:
                    SingleRole(
                        scope.Actor.Roles)
                    ?? string.Empty),
            cancellationToken);
    }

    private async Task<ScopeResult> ResolveScopeAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var actor = await _users.GetActorAsync(actorUserId, cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            !actor.SchoolId.HasValue)
            return ScopeResult.Fail(AcademicStructureErrorCode.AccessDenied);

        var role = SingleRole(actor.Roles);

        if (role != RoleNames.SchoolAdmin &&
            role != RoleNames.SubjectSupervisor &&
            role != RoleNames.Teacher)
        {
            return ScopeResult.Fail(
                AcademicStructureErrorCode.AccessDenied);
        }

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

        return result.Error switch
        {
            AcademicPersistenceError.Conflict =>
                Fail(AcademicStructureErrorCode.ConcurrencyConflict),

            AcademicPersistenceError.SeatLimit =>
                Fail(AcademicStructureErrorCode.StudentSeatLimitReached),

            _ =>
                Fail(AcademicStructureErrorCode.PersistenceError)
        };
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

    private static string GenerateInternalClassCode(Guid id) =>
        $"CLS-{id:N}".ToUpperInvariant();

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
