using System.Text.RegularExpressions;
using Edulytics.Core.Constants;
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
            .Select(x => new CurriculumTopicItem(
                x.Id,
                x.SubjectId,
                x.GradeLevelId,
                x.Name,
                x.Order,
                outcomesByTopic.GetValueOrDefault(
                    x.Id,
                    Array.Empty<LearningOutcomeItem>())))
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
                topics));
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
                    .ToArray()));
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

        var frameworkVersionId =
            await ResolveOrCreateDefaultAdoptionAsync(
                scope,
                request.GradeLevelId,
                request.SubjectId,
                cancellationToken);

        if (!frameworkVersionId.HasValue)
            return Fail(CurriculumErrorCode.PersistenceError);

        if (await _curriculum.TopicNameExistsAsync(
                schoolId,
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

        if (await _curriculum.TopicOrderExistsAsync(
                schoolId,
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

        if (await _curriculum.TopicNameExistsAsync(
                schoolId,
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

        if (await _curriculum.TopicOrderExistsAsync(
                schoolId,
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

    public async Task<CurriculumCommandResult> CreateOutcomeAsync(
        Guid actorUserId,
        CreateLearningOutcomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        var schoolId = scope.School!.Id;

        var topic = await _curriculum.GetTopicAsync(
            schoolId,
            request.TopicId,
            cancellationToken);

        if (topic is null)
            return Fail(
                "TopicId",
                CurriculumErrorCode.TopicNotFound);

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

        if (request.Weight <= 0 || request.Weight > 100)
            return Fail("Weight", CurriculumErrorCode.InvalidWeight);

        if (request.Order <= 0)
            return Fail("Order", CurriculumErrorCode.InvalidOrder);

        if (await _curriculum.OutcomeCodeExistsAsync(
                schoolId,
                topic.FrameworkVersionId,
                topic.SubjectId,
                topic.GradeLevelId,
                code,
                cancellationToken: cancellationToken))
        {
            return Fail(
                "Code",
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
            FrameworkVersionId =
                topic.FrameworkVersionId,
            SubjectId = topic.SubjectId,
            GradeLevelId = topic.GradeLevelId,
            TopicId = request.TopicId,
            Code = code,
            Description = description,
            Weight = request.Weight,
            Order = request.Order
        };

        await _curriculum.AddOutcomeAsync(
            outcome,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "LearningOutcome.Created",
            "LearningOutcome",
            outcome.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["frameworkVersionId"] =
                        outcome.FrameworkVersionId,
                    ["subjectId"] =
                        outcome.SubjectId,
                    ["gradeLevelId"] =
                        outcome.GradeLevelId,
                    ["topicId"] =
                        outcome.TopicId,
                    ["code"] =
                        outcome.Code,
                    ["descriptionLength"] =
                        outcome.Description.Length,
                    ["weight"] =
                        outcome.Weight,
                    ["order"] =
                        outcome.Order
                },
            "Learning outcome created.",
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

        var schoolId = scope.School!.Id;
        var outcome = await _curriculum.GetOutcomeAsync(
            schoolId,
            request.Id,
            cancellationToken);

        if (outcome is null)
            return Fail(CurriculumErrorCode.OutcomeNotFound);

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

        if (request.Weight <= 0 || request.Weight > 100)
            return Fail("Weight", CurriculumErrorCode.InvalidWeight);

        if (request.Order <= 0)
            return Fail("Order", CurriculumErrorCode.InvalidOrder);

        if (await _curriculum.OutcomeCodeExistsAsync(
                schoolId,
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
        outcome.Weight = request.Weight;
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

    private async Task<Guid?> ResolveOrCreateDefaultAdoptionAsync(
        ScopeResult scope,
        Guid gradeLevelId,
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        if (scope.School is null)
            return null;

        var schoolId = scope.School.Id;

        var existing =
            await _curriculum.GetPrimaryDefaultFrameworkVersionIdAsync(
                schoolId,
                gradeLevelId,
                subjectId,
                cancellationToken);

        if (existing.HasValue)
            return existing.Value;

        var platformDefault =
            await _curriculum.GetPlatformDefaultFrameworkVersionIdAsync(
                cancellationToken);

        if (!platformDefault.HasValue)
            return null;

        var now = DateTime.UtcNow;

        var adoption =
            new SchoolCurriculumAdoption
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                AcademicYearId = null,
                GradeLevelId = gradeLevelId,
                SubjectId = subjectId,
                FrameworkVersionId =
                    platformDefault.Value,
                IsPrimary = true,
                IsActive = true,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

        await _curriculum.AddDefaultAdoptionAsync(
            adoption,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "CurriculumAdoption.Created",
            "SchoolCurriculumAdoption",
            adoption.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["academicYearId"] =
                        adoption.AcademicYearId,
                    ["gradeLevelId"] =
                        adoption.GradeLevelId,
                    ["subjectId"] =
                        adoption.SubjectId,
                    ["frameworkVersionId"] =
                        adoption.FrameworkVersionId,
                    ["isPrimary"] =
                        adoption.IsPrimary,
                    ["isActive"] =
                        adoption.IsActive
                },
            "Default curriculum adoption created.",
            cancellationToken);

        return platformDefault.Value;
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
            actor.SchoolId is null ||
            SingleRole(actor.Roles) != RoleNames.SchoolAdmin)
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
            x.Order);

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
