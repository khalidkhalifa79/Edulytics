using System.Text.RegularExpressions;
using Edulytics.Core.Constants;
using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Users;
using Edulytics.Services.Auditing;

namespace Edulytics.Services.Curriculum;

public sealed class CurriculumService : ICurriculumService
{
    private static readonly Regex CodePattern = new(
        "^[A-Z0-9._-]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly ICurriculumRepository _curriculum;
    private readonly ISchoolRepository _schools;
    private readonly ISchoolUserRepository _users;
    private readonly IAuditService? _audit;

    public CurriculumService(
        ICurriculumRepository curriculum,
        ISchoolRepository schools,
        ISchoolUserRepository users,
        IAuditService? audit = null)
    {
        _curriculum = curriculum;
        _schools = schools;
        _users = users;
        _audit = audit;
    }

    public async Task<CurriculumQueryResult<CurriculumDashboard>>
        GetDashboardAsync(
            Guid actorUserId,
            CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (!scope.Succeeded)
            return CurriculumQueryResult<CurriculumDashboard>
                .Failure(scope.Error!.Value);

        var snapshot = await _curriculum.GetSnapshotAsync(
            scope.School!.Id,
            cancellationToken);

        var adoptedContexts =
            await _curriculum.GetAdoptedCurriculumContextsAsync(
                scope.School.Id,
                cancellationToken);
        var adoptedByScope = adoptedContexts.ToDictionary(x => (x.AcademicProgramId, x.GradeLevelId, x.SubjectId));
        var gradesById = snapshot.GradeLevels.ToDictionary(x => x.Id);
        var officialByScope = new Dictionary<
            (Guid FrameworkVersionId, int LogicalLevel),
            IReadOnlyList<OfficialCurriculumOutcomeSource>>();

        foreach (var adoption in adoptedContexts)
        {
            if (!gradesById.TryGetValue(
                    adoption.GradeLevelId,
                    out var grade))
            {
                continue;
            }

            var logicalLevel = ResolveLogicalLevel(
                adoption.FrameworkCode,
                grade);
            var key = (adoption.FrameworkVersionId, logicalLevel);
            if (!officialByScope.ContainsKey(key))
            {
                officialByScope[key] =
                    await _curriculum.GetOfficialOutcomeSourcesAsync(
                        adoption.FrameworkVersionId,
                        logicalLevel,
                        cancellationToken);
            }
        }

        var outcomesByTopic = snapshot.Outcomes
            .GroupBy(x => x.TopicId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<LearningOutcomeItem>)x
                    .OrderBy(y => y.Order)
                    .Select(MapOutcome)
                    .ToArray());

        var topics = snapshot.Topics
            .OrderBy(x => x.SubjectId)
            .ThenBy(x => x.GradeLevelId)
            .ThenBy(x => x.Order)
            .Select(x =>
            {
                adoptedByScope.TryGetValue(
                    (x.AcademicProgramId, x.GradeLevelId, x.SubjectId),
                    out var adoption);
                var official = Array.Empty<OfficialCurriculumOutcomeOption>();

                if (adoption is not null &&
                    gradesById.TryGetValue(x.GradeLevelId, out var grade))
                {
                    var logicalLevel = ResolveLogicalLevel(
                        adoption.FrameworkCode,
                        grade);
                    var sources = officialByScope.TryGetValue(
                        (adoption.FrameworkVersionId, logicalLevel),
                        out var matchedSources)
                        ? matchedSources
                        : Array.Empty<OfficialCurriculumOutcomeSource>();
                    official = sources
                        .Select(MapOfficialOutcome)
                        .ToArray();
                }

                return new CurriculumTopicItem(
                    x.Id,
                    x.SubjectId,
                    x.GradeLevelId,
                    x.Name,
                    x.Order,
                    outcomesByTopic.GetValueOrDefault(
                        x.Id,
                        Array.Empty<LearningOutcomeItem>()))
                {
                    AcademicProgramId = x.AcademicProgramId,
                    AcademicProgramName = adoption?.AcademicProgramName ?? snapshot.AcademicPrograms.FirstOrDefault(p => p.Id == x.AcademicProgramId)?.Name ?? string.Empty,
                    FrameworkCode = adoption?.FrameworkCode ?? string.Empty,
                    FrameworkDisplayName =
                        adoption?.FrameworkName ?? string.Empty,
                    OfficialOutcomes = official
                };
            })
            .ToArray();

        return CurriculumQueryResult<CurriculumDashboard>.Success(
            new CurriculumDashboard(
                scope.School.Id,
                snapshot.GradeLevels
                    .OrderBy(x => x.Order)
                    .Select(x => new CurriculumGradeItem(
                        x.Id,
                        x.Name,
                        x.Order))
                    .ToArray(),
                snapshot.Subjects
                    .OrderBy(x => x.Name)
                    .Select(x => new CurriculumSubjectItem(
                        x.Id,
                        x.Name,
                        x.Code))
                    .ToArray(),
                topics)
            {
                AcademicPrograms = snapshot.AcademicPrograms.Where(x => x.Status == AcademicStructureStatus.Active).OrderBy(x => x.Name).Select(x => new CurriculumProgramItem(x.Id,x.Name,x.Code)).ToArray(),
                Frameworks = MathematicsCurriculumPackRegistry.All
                    .Select(x => new CurriculumFrameworkItem(
                        x.Code,
                        x.DisplayName))
                    .OrderBy(x => x.DisplayName)
                    .ToArray(),
                Adoptions = adoptedContexts
                    .Select(x => new CurriculumAdoptionItem(
                        x.GradeLevelId,
                        x.SubjectId,
                        x.FrameworkCode,
                        x.FrameworkName) { AcademicProgramId = x.AcademicProgramId, AcademicProgramName = x.AcademicProgramName, AcademicProgramCode = x.AcademicProgramCode })
                    .ToArray()
            });
    }

    public async Task<CurriculumQueryResult<CurriculumTopicItem>>
        GetTopicAsync(
            Guid actorUserId,
            Guid id,
            CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (!scope.Succeeded)
            return CurriculumQueryResult<CurriculumTopicItem>
                .Failure(scope.Error!.Value);

        var topic = await _curriculum.GetTopicAsync(
            scope.School!.Id,
            id,
            cancellationToken);

        if (topic is null)
            return CurriculumQueryResult<CurriculumTopicItem>
                .Failure(CurriculumErrorCode.TopicNotFound);

        var snapshot = await _curriculum.GetSnapshotAsync(
            scope.School.Id,
            cancellationToken);

        var adoption =
            (await _curriculum.GetAdoptedCurriculumContextsAsync(
                scope.School.Id,
                cancellationToken))
            .SingleOrDefault(x =>
                x.AcademicProgramId == topic.AcademicProgramId &&
                x.GradeLevelId == topic.GradeLevelId &&
                x.SubjectId == topic.SubjectId &&
                x.FrameworkVersionId == topic.FrameworkVersionId);
        var grade = snapshot.GradeLevels.Single(
            x => x.Id == topic.GradeLevelId);
        IReadOnlyList<OfficialCurriculumOutcomeOption> official =
            adoption is null
            ? Array.Empty<OfficialCurriculumOutcomeOption>()
            : (await _curriculum.GetOfficialOutcomeSourcesAsync(
                    topic.FrameworkVersionId,
                    ResolveLogicalLevel(adoption.FrameworkCode, grade),
                    cancellationToken))
                .Select(MapOfficialOutcome)
                .ToArray();

        return CurriculumQueryResult<CurriculumTopicItem>.Success(
            new CurriculumTopicItem(
                topic.Id,
                topic.SubjectId,
                topic.GradeLevelId,
                topic.Name,
                topic.Order,
                snapshot.Outcomes
                    .Where(x => x.TopicId == topic.Id)
                    .OrderBy(x => x.Order)
                    .Select(MapOutcome)
                    .ToArray())
            {
                AcademicProgramId = topic.AcademicProgramId,
                AcademicProgramName = adoption?.AcademicProgramName ?? string.Empty,
                FrameworkCode = adoption?.FrameworkCode ?? string.Empty,
                FrameworkDisplayName =
                    adoption?.FrameworkName ?? string.Empty,
                OfficialOutcomes = official
            });
    }

    public async Task<CurriculumQueryResult<LearningOutcomeItem>>
        GetOutcomeAsync(
            Guid actorUserId,
            Guid id,
            CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (!scope.Succeeded)
            return CurriculumQueryResult<LearningOutcomeItem>
                .Failure(scope.Error!.Value);

        var outcome = await _curriculum.GetOutcomeAsync(
            scope.School!.Id,
            id,
            cancellationToken);

        return outcome is null
            ? CurriculumQueryResult<LearningOutcomeItem>
                .Failure(CurriculumErrorCode.OutcomeNotFound)
            : CurriculumQueryResult<LearningOutcomeItem>
                .Success(MapOutcome(outcome));
    }


    public async Task<CurriculumCommandResult> SelectFrameworkAsync(
        Guid actorUserId,
        SelectCurriculumFrameworkRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(CurriculumErrorCode.AccessDenied);

        var schoolId = scope.School!.Id;

        if (await _curriculum.GetSubjectAsync(
                schoolId,
                request.SubjectId,
                cancellationToken) is null)
        {
            return Fail(
                "SubjectId",
                CurriculumErrorCode.SubjectNotFound);
        }

        if (await _curriculum.GetGradeLevelAsync(
                schoolId,
                request.GradeLevelId,
                cancellationToken) is null)
        {
            return Fail(
                "GradeLevelId",
                CurriculumErrorCode.GradeLevelNotFound);
        }

        var program = request.AcademicProgramId == Guid.Empty
            ? await _curriculum.GetDefaultAcademicProgramAsync(schoolId,cancellationToken)
            : await _curriculum.GetAcademicProgramAsync(schoolId,request.AcademicProgramId,cancellationToken);
        var programId = program?.Id ?? request.AcademicProgramId;
        if (request.AcademicProgramId != Guid.Empty && program is null)
            return Fail("AcademicProgramId", CurriculumErrorCode.AcademicProgramNotFound);

        var frameworkCode = Clean(request.FrameworkCode)
            .ToUpperInvariant();

        if (!MathematicsCurriculumPackRegistry.All.Any(
                x => string.Equals(
                    x.Code,
                    frameworkCode,
                    StringComparison.Ordinal)))
        {
            return Fail(
                "FrameworkCode",
                CurriculumErrorCode.FrameworkNotFound);
        }

        var frameworkVersionId =
            await _curriculum.GetActivePlatformFrameworkVersionIdAsync(
                frameworkCode,
                cancellationToken);

        if (!frameworkVersionId.HasValue)
        {
            return Fail(
                "FrameworkCode",
                CurriculumErrorCode.FrameworkNotFound);
        }

        var adoption =
            await _curriculum.GetPrimaryAdoptionAsync(
                schoolId,
                programId,
                request.GradeLevelId,
                request.SubjectId,
                cancellationToken);

        if (adoption is not null &&
            adoption.FrameworkVersionId == frameworkVersionId.Value)
        {
            return CurriculumCommandResult.Success();
        }

        if (adoption is not null)
        {
            var snapshot = await _curriculum.GetSnapshotAsync(
                schoolId,
                cancellationToken);

            if (snapshot.Topics.Any(
                    x =>
                        x.AcademicProgramId == programId &&
                        x.SubjectId == request.SubjectId &&
                        x.GradeLevelId == request.GradeLevelId))
            {
                return Fail(
                    "FrameworkCode",
                    CurriculumErrorCode.CurriculumFrameworkInUse);
            }
        }

        var now = DateTime.UtcNow;
        IReadOnlyDictionary<string, object?>? oldValues = null;

        if (adoption is null)
        {
            adoption = new SchoolCurriculumAdoption
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                AcademicYearId = null,
                AcademicProgramId = programId,
                GradeLevelId = request.GradeLevelId,
                SubjectId = request.SubjectId,
                FrameworkVersionId = frameworkVersionId.Value,
                IsPrimary = true,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            await _curriculum.AddDefaultAdoptionAsync(
                adoption,
                cancellationToken);
        }
        else
        {
            oldValues =
                new Dictionary<string, object?>
                {
                    ["frameworkVersionId"] =
                        adoption.FrameworkVersionId
                };

            adoption.FrameworkVersionId =
                frameworkVersionId.Value;
            adoption.UpdatedAtUtc = now;
        }

        await QueueAuditAsync(
            scope,
            "CurriculumAdoption.Selected",
            "SchoolCurriculumAdoption",
            adoption.Id,
            oldValues,
            new Dictionary<string, object?>
            {
                ["gradeLevelId"] =
                    adoption.GradeLevelId,
                ["subjectId"] =
                    adoption.SubjectId,
                ["frameworkCode"] =
                    frameworkCode,
                ["frameworkVersionId"] =
                    adoption.FrameworkVersionId,
                ["isPrimary"] =
                    adoption.IsPrimary,
                ["isActive"] =
                    adoption.IsActive
            },
            "Verified curriculum framework selected.",
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<CurriculumCommandResult> CreateTopicAsync(
        Guid actorUserId,
        CreateCurriculumTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(CurriculumErrorCode.AccessDenied);

        var name = Clean(request.Name);
        var nameError = ValidateName(name);
        if (nameError is not null)
            return nameError;

        if (request.Order <= 0)
            return Fail("Order", CurriculumErrorCode.InvalidOrder);

        var schoolId = scope.School!.Id;

        if (await _curriculum.GetSubjectAsync(
                schoolId,
                request.SubjectId,
                cancellationToken) is null)
        {
            return Fail(
                "SubjectId",
                CurriculumErrorCode.SubjectNotFound);
        }

        if (await _curriculum.GetGradeLevelAsync(
                schoolId,
                request.GradeLevelId,
                cancellationToken) is null)
        {
            return Fail(
                "GradeLevelId",
                CurriculumErrorCode.GradeLevelNotFound);
        }

        var program = request.AcademicProgramId == Guid.Empty
            ? await _curriculum.GetDefaultAcademicProgramAsync(schoolId,cancellationToken)
            : await _curriculum.GetAcademicProgramAsync(schoolId,request.AcademicProgramId,cancellationToken);
        var programId = program?.Id ?? request.AcademicProgramId;
        if (request.AcademicProgramId != Guid.Empty && program is null)
            return Fail("AcademicProgramId", CurriculumErrorCode.AcademicProgramNotFound);

        var frameworkVersionId = await _curriculum.GetPrimaryFrameworkVersionIdAsync(
            schoolId, programId, request.GradeLevelId, request.SubjectId, cancellationToken);

        if (!frameworkVersionId.HasValue)
        {
            return Fail(
                "FrameworkCode",
                CurriculumErrorCode.CurriculumNotSelected);
        }

        if (await _curriculum.TopicNameExistsInProgramAsync(
                schoolId,
                programId,
                frameworkVersionId.Value,
                request.SubjectId,
                request.GradeLevelId,
                name.ToUpperInvariant(),
                cancellationToken: cancellationToken))
        {
            return Fail(
                "Name",
                CurriculumErrorCode.DuplicateTopicName);
        }

        if (await _curriculum.TopicOrderExistsInProgramAsync(
                schoolId,
                programId,
                frameworkVersionId.Value,
                request.SubjectId,
                request.GradeLevelId,
                request.Order,
                cancellationToken: cancellationToken))
        {
            return Fail(
                "Order",
                CurriculumErrorCode.DuplicateTopicOrder);
        }

        var topic = new CurriculumTopic
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            AcademicProgramId = programId,
            FrameworkVersionId =
                frameworkVersionId.Value,
            SubjectId = request.SubjectId,
            GradeLevelId = request.GradeLevelId,
            Name = name,
            Order = request.Order
        };

        await _curriculum.AddTopicAsync(
            topic,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "CurriculumTopic.Created",
            "CurriculumTopic",
            topic.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["frameworkVersionId"] =
                        topic.FrameworkVersionId,
                    ["subjectId"] =
                        topic.SubjectId,
                    ["gradeLevelId"] =
                        topic.GradeLevelId,
                    ["name"] =
                        topic.Name,
                    ["order"] =
                        topic.Order
                },
            "Curriculum topic created.",
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<CurriculumCommandResult> UpdateTopicAsync(
        Guid actorUserId,
        UpdateCurriculumTopicRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(CurriculumErrorCode.AccessDenied);

        var schoolId = scope.School!.Id;
        var topic = await _curriculum.GetTopicAsync(
            schoolId,
            request.Id,
            cancellationToken);

        if (topic is null)
            return Fail(CurriculumErrorCode.TopicNotFound);

        var name = Clean(request.Name);
        var nameError = ValidateName(name);
        if (nameError is not null)
            return nameError;

        if (request.Order <= 0)
            return Fail("Order", CurriculumErrorCode.InvalidOrder);

        if (await _curriculum.TopicNameExistsInProgramAsync(
                schoolId,
                topic.AcademicProgramId,
                topic.FrameworkVersionId,
                topic.SubjectId,
                topic.GradeLevelId,
                name.ToUpperInvariant(),
                topic.Id,
                cancellationToken))
        {
            return Fail(
                "Name",
                CurriculumErrorCode.DuplicateTopicName);
        }

        if (await _curriculum.TopicOrderExistsInProgramAsync(
                schoolId,
                topic.AcademicProgramId,
                topic.FrameworkVersionId,
                topic.SubjectId,
                topic.GradeLevelId,
                request.Order,
                topic.Id,
                cancellationToken))
        {
            return Fail(
                "Order",
                CurriculumErrorCode.DuplicateTopicOrder);
        }

        var oldValues =
            new Dictionary<string, object?>
            {
                ["name"] =
                    topic.Name,
                ["order"] =
                    topic.Order
            };

        topic.Name = name;
        topic.Order = request.Order;

        await QueueAuditAsync(
            scope,
            "CurriculumTopic.Updated",
            "CurriculumTopic",
            topic.Id,
            oldValues,
            new Dictionary<string, object?>
            {
                ["name"] =
                    topic.Name,
                ["order"] =
                    topic.Order
            },
            "Curriculum topic updated.",
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<CurriculumCommandResult> CreateOfficialOutcomeAsync(
        Guid actorUserId,
        CreateOfficialLearningOutcomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(CurriculumErrorCode.AccessDenied);

if (request.Order <= 0)
            return Fail("Order", CurriculumErrorCode.InvalidOrder);

        var schoolId = scope.School!.Id;
        var topic = await _curriculum.GetTopicAsync(
            schoolId,
            request.TopicId,
            cancellationToken);

        if (topic is null)
        {
            return Fail(
                "TopicId",
                CurriculumErrorCode.TopicNotFound);
        }

        var grade = await _curriculum.GetGradeLevelAsync(
            schoolId,
            topic.GradeLevelId,
            cancellationToken);
        var adoption =
            (await _curriculum.GetAdoptedCurriculumContextsAsync(
                schoolId,
                cancellationToken))
            .SingleOrDefault(x =>
                x.AcademicProgramId == topic.AcademicProgramId &&
                x.GradeLevelId == topic.GradeLevelId &&
                x.SubjectId == topic.SubjectId &&
                x.FrameworkVersionId == topic.FrameworkVersionId);

        if (grade is null || adoption is null)
        {
            return Fail(
                "ContentNodeId",
                CurriculumErrorCode.OfficialOutcomeNotFound);
        }

        var source = await _curriculum.GetOfficialOutcomeSourceAsync(
            topic.FrameworkVersionId,
            ResolveLogicalLevel(adoption.FrameworkCode, grade),
            request.ContentNodeId,
            request.LessonNodeId,
            cancellationToken);

        if (source is null)
        {
            return Fail(
                "ContentNodeId",
                CurriculumErrorCode.OfficialOutcomeNotFound);
        }

        if (await _curriculum.OutcomeCodeExistsInProgramAsync(
                schoolId,
                topic.AcademicProgramId,
                topic.FrameworkVersionId,
                topic.SubjectId,
                topic.GradeLevelId,
                source.Code,
                cancellationToken: cancellationToken))
        {
            return Fail(
                "ContentNodeId",
                CurriculumErrorCode.DuplicateOutcomeCode);
        }

        if (await _curriculum.OutcomeOrderExistsAsync(
                schoolId,
                request.TopicId,
                request.Order,
                cancellationToken: cancellationToken))
        {
            return Fail(
                "Order",
                CurriculumErrorCode.DuplicateOutcomeOrder);
        }

        var outcome = new LearningOutcome
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            AcademicProgramId = topic.AcademicProgramId,
            FrameworkVersionId = topic.FrameworkVersionId,
            SubjectId = topic.SubjectId,
            GradeLevelId = topic.GradeLevelId,
            TopicId = topic.Id,
            OfficialContentNodeId = source.ContentNodeId,
            Code = source.Code,
            Description = source.Description,
            // Legacy schema compatibility only; official outcomes are unweighted.
            Weight = 1m,
            Order = request.Order
        };

        await _curriculum.AddOutcomeAsync(
            outcome,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "LearningOutcome.OfficialSelected",
            "LearningOutcome",
            outcome.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["frameworkVersionId"] =
                        outcome.FrameworkVersionId,
                    ["topicId"] = outcome.TopicId,
                    ["officialContentNodeId"] =
                        outcome.OfficialContentNodeId,
                    ["officialLessonNodeId"] =
                        request.LessonNodeId,
                    ["code"] = outcome.Code,
                    ["weight"] = outcome.Weight,
                    ["order"] = outcome.Order
                },
            "Official curriculum outcome selected.",
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public async Task<CurriculumCommandResult> UpdateOutcomeAsync(
        Guid actorUserId,
        UpdateLearningOutcomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        if (SingleRole(scope.Actor!.Roles) != RoleNames.SubjectSupervisor)
            return Fail(CurriculumErrorCode.AccessDenied);

        var schoolId = scope.School!.Id;
        var outcome = await _curriculum.GetOutcomeAsync(
            schoolId,
            request.Id,
            cancellationToken);

        if (outcome is null)
            return Fail(CurriculumErrorCode.OutcomeNotFound);

        if (outcome.OfficialContentNodeId.HasValue)
        {
            return Fail(
                CurriculumErrorCode.OfficialOutcomeReadOnly);
        }

        var code = NormalizeCode(request.Code);
        if (!ValidCode(code))
            return Fail("Code", CurriculumErrorCode.InvalidCode);

        var description = Clean(request.Description);
        if (description.Length == 0)
            return Fail("Description", CurriculumErrorCode.Required);

        if (description.Length > 1000)
            return Fail(
                "Description",
                CurriculumErrorCode.InvalidName);
if (request.Order <= 0)
            return Fail("Order", CurriculumErrorCode.InvalidOrder);

        if (await _curriculum.OutcomeCodeExistsInProgramAsync(
                schoolId,
                outcome.AcademicProgramId,
                outcome.FrameworkVersionId,
                outcome.SubjectId,
                outcome.GradeLevelId,
                code,
                outcome.Id,
                cancellationToken))
        {
            return Fail(
                "Code",
                CurriculumErrorCode.DuplicateOutcomeCode);
        }

        if (await _curriculum.OutcomeOrderExistsAsync(
                schoolId,
                outcome.TopicId,
                request.Order,
                outcome.Id,
                cancellationToken))
        {
            return Fail(
                "Order",
                CurriculumErrorCode.DuplicateOutcomeOrder);
        }

        var oldValues =
            new Dictionary<string, object?>
            {
                ["code"] =
                    outcome.Code,
                ["descriptionLength"] =
                    outcome.Description.Length,
                ["weight"] =
                    outcome.Weight,
                ["order"] =
                    outcome.Order
            };

        outcome.Code = code;
        outcome.Description = description;
outcome.Order = request.Order;

        await QueueAuditAsync(
            scope,
            "LearningOutcome.Updated",
            "LearningOutcome",
            outcome.Id,
            oldValues,
            new Dictionary<string, object?>
            {
                ["code"] =
                    outcome.Code,
                ["descriptionLength"] =
                    outcome.Description.Length,
                ["weight"] =
                    outcome.Weight,
                ["order"] =
                    outcome.Order
            },
            "Learning outcome updated.",
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
                    "Curriculum",
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
        var actor = await _users.GetActorAsync(
            actorUserId,
            cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            actor.SchoolId is null)
        {
            return ScopeResult.Fail(
                CurriculumErrorCode.AccessDenied);
        }

        var role = SingleRole(actor.Roles);

        if (role != RoleNames.SchoolAdmin &&
            role != RoleNames.SubjectSupervisor &&
            role != RoleNames.Teacher)
        {
            return ScopeResult.Fail(
                CurriculumErrorCode.AccessDenied);
        }

        var school = await _schools.GetByIdAsync(
            actor.SchoolId.Value,
            cancellationToken);

        if (school is null ||
            school.Status != SchoolStatus.Active)
        {
            return ScopeResult.Fail(
                CurriculumErrorCode.SchoolNotActive);
        }

        return ScopeResult.Ok(actor, school);
    }

    private async Task<CurriculumCommandResult> PersistAsync(
        CancellationToken cancellationToken)
    {
        var result = await _curriculum.SaveAsync(
            cancellationToken);

        return result.Succeeded
            ? CurriculumCommandResult.Success()
            : Fail(CurriculumErrorCode.PersistenceError);
    }

    private static CurriculumCommandResult? ValidateName(string value)
    {
        if (value.Length == 0)
            return Fail("Name", CurriculumErrorCode.Required);

        return value.Length > 200
            ? Fail("Name", CurriculumErrorCode.InvalidName)
            : null;
    }

    private static bool ValidCode(string value) =>
        value.Length is > 0 and <= 50 &&
        CodePattern.IsMatch(value);

    private static string Clean(string? value) =>
        value?.Trim() ?? string.Empty;

    private static string NormalizeCode(string? value) =>
        Clean(value).ToUpperInvariant();

    private static string? SingleRole(IReadOnlyList<string> roles) =>
        roles.Count == 1 ? roles[0] : null;

    private static LearningOutcomeItem MapOutcome(
        LearningOutcome x) =>
        new(
            x.Id,
            x.TopicId,
            x.Code,
            x.Description,
            x.Weight,
            x.Order)
        {
            IsOfficial = x.OfficialContentNodeId.HasValue
        };

    private static OfficialCurriculumOutcomeOption MapOfficialOutcome(
        OfficialCurriculumOutcomeSource x) =>
        new(
            x.ContentNodeId,
            x.LessonNodeId,
            x.Code,
            x.Description,
            x.SelectionLabel,
            x.GroupLabel,
            x.SortOrder);

    private static int ResolveLogicalLevel(
        string frameworkCode,
        CurriculumGradeItem grade) =>
        ResolveLogicalLevel(
            frameworkCode,
            new GradeLevel
            {
                Id = grade.Id,
                Name = grade.Name,
                Order = grade.Order
            });

    private static int ResolveLogicalLevel(
        string frameworkCode,
        GradeLevel grade)
    {
        var pack = MathematicsCurriculumPackRegistry.All
            .Single(x => string.Equals(
                x.Code,
                frameworkCode,
                StringComparison.Ordinal));
        var exact = pack.Levels.FirstOrDefault(x =>
            string.Equals(
                x.NativeLabel,
                grade.Name,
                StringComparison.OrdinalIgnoreCase));

        if (exact is not null)
            return exact.LogicalLevel;

        var gradeNumberMatch = Regex.Match(grade.Name, "[0-9]+");
        if (gradeNumberMatch.Success &&
            int.TryParse(gradeNumberMatch.Value, out var gradeNumber))
        {
            var native = pack.Levels.FirstOrDefault(x =>
                string.Equals(
                    x.NativeLabel,
                    $"Grade {gradeNumber}",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    x.NativeLabel,
                    $"Year {gradeNumber}",
                    StringComparison.OrdinalIgnoreCase));

            if (native is not null)
                return native.LogicalLevel;
        }

        return grade.Order;
    }

    private static CurriculumCommandResult Fail(
        CurriculumErrorCode error) =>
        CurriculumCommandResult.Failure(
            string.Empty,
            error);

    private static CurriculumCommandResult Fail(
        string field,
        CurriculumErrorCode error) =>
        CurriculumCommandResult.Failure(
            field,
            error);

    private sealed record ScopeResult(
        bool Succeeded,
        SchoolUserRecord? Actor,
        School? School,
        CurriculumErrorCode? Error)
    {
        public static ScopeResult Ok(
            SchoolUserRecord actor,
            School school) =>
            new(true, actor, school, null);

        public static ScopeResult Fail(
            CurriculumErrorCode error) =>
            new(false, null, null, error);
    }
}
