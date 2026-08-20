using System.Text.Json;
using Edulytics.Core.Assessments;
using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Realtime;
using Edulytics.Core.Users;
using Edulytics.Services.Auditing;

namespace Edulytics.Services.Assessments;

public sealed partial class AssessmentService
{
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
                    "Assessments",
                OldValues:
                    oldValues,
                NewValues:
                    newValues,
                ResultSummary:
                    resultSummary,
                ActorUserIdOverride:
                    scope.Actor.Id,
                ActorRoleOverride:
                    scope.Role ?? string.Empty),
            cancellationToken);
    }

    private static HashSet<Guid> ResolveEligibleFrameworkVersionIds(
        AssessmentSnapshot snapshot,
        Guid academicYearId,
        Guid gradeLevelId,
        Guid subjectId)
    {
        var activeVersionIds = snapshot.FrameworkVersions
            .Where(x => x.IsActive)
            .Select(x => x.Id)
            .ToHashSet();

        var yearSpecific = snapshot.CurriculumAdoptions
            .Where(x =>
                x.IsActive &&
                x.AcademicYearId == academicYearId &&
                x.GradeLevelId == gradeLevelId &&
                x.SubjectId == subjectId &&
                activeVersionIds.Contains(x.FrameworkVersionId))
            .ToArray();

        var resolved = yearSpecific.Length > 0
            ? yearSpecific
            : snapshot.CurriculumAdoptions
                .Where(x =>
                    x.IsActive &&
                    x.AcademicYearId is null &&
                    x.GradeLevelId == gradeLevelId &&
                    x.SubjectId == subjectId &&
                    activeVersionIds.Contains(x.FrameworkVersionId))
                .ToArray();

        return resolved
            .Select(x => x.FrameworkVersionId)
            .ToHashSet();
    }

    private AssessmentWorkspace BuildWorkspace(
        ScopeResult scope,
        AssessmentSnapshot snapshot,
        IEnumerable<Assessment> visibleAssessments)
    {
        IEnumerable<ClassGroup> classes =
            snapshot.ClassGroups.Where(x => x.Status == AcademicStructureStatus.Active);

        IEnumerable<Subject> subjects =
            snapshot.Subjects.Where(x => x.Status == AcademicStructureStatus.Active);

        if (scope.Role == RoleNames.Teacher)
        {
            var assignments = snapshot.TeacherAssignments
                .Where(x => x.TeacherUserId == scope.Actor!.Id)
                .ToArray();

            var classIds = assignments.Select(x => x.ClassGroupId).ToHashSet();
            var subjectIds = assignments.Select(x => x.SubjectId).ToHashSet();

            classes = classes.Where(x => classIds.Contains(x.Id));
            subjects = subjects.Where(x => subjectIds.Contains(x.Id));
        }

        return new AssessmentWorkspace(
            visibleAssessments
                .OrderByDescending(x => x.AssessmentDate)
                .ThenBy(x => x.Title)
                .Select(MapAssessment)
                .ToArray(),
            snapshot.Terms
                .Where(x => x.Status == AcademicStructureStatus.Active)
                .OrderBy(x => x.StartsOn)
                .Select(x => new AssessmentTermItem(x.Id, x.AcademicYearId, x.Name))
                .ToArray(),
            classes
                .OrderBy(x => x.Name)
                .Select(x => new AssessmentClassItem(
                    x.Id,
                    x.AcademicYearId,
                    x.GradeLevelId,
                    x.Name,
                    x.Code))
                .ToArray(),
            subjects
                .OrderBy(x => x.Name)
                .Select(x => new AssessmentSubjectItem(x.Id, x.Name, x.Code))
                .ToArray());
    }

    private static IReadOnlyList<AssessmentQuestionItem> BuildQuestions(
        AssessmentSnapshot snapshot,
        Guid assessmentId) =>
        snapshot.Questions
            .Where(x => x.AssessmentId == assessmentId)
            .OrderBy(x => x.Order)
            .Select(x => MapQuestion(snapshot, x))
            .ToArray();

    private static AssessmentQuestionItem MapQuestion(
        AssessmentSnapshot snapshot,
        AssessmentQuestion question) =>
        new(
            question.Id,
            question.Prompt,
            question.MaxScore,
            question.Order,
            snapshot.OutcomeMappings
                .Where(x => x.AssessmentQuestionId == question.Id)
                .Select(x => x.LearningOutcomeId)
                .OrderBy(x => x)
                .ToArray());

    private static AssessmentListItem MapAssessment(Assessment x) =>
        new(
            x.Id,
            x.SubjectId,
            x.ClassGroupId,
            x.AcademicYearId,
            x.TermId,
            x.Title,
            x.AssessmentDate,
            x.MaxScore,
            x.Status,
            x.RowVersion);

    private async Task<ScopeResult> ResolveScopeAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var actor = await _users.GetActorAsync(actorUserId, cancellationToken);

        if (actor is null ||
            !actor.IsActive ||
            actor.IsLocked ||
            !actor.SchoolId.HasValue)
            return ScopeResult.Fail(AssessmentErrorCode.AccessDenied);

        var role = actor.Roles.Count == 1 ? actor.Roles[0] : null;
        if (role != RoleNames.SchoolAdmin && role != RoleNames.Teacher)
            return ScopeResult.Fail(AssessmentErrorCode.AccessDenied);

        var school = await _schools.GetByIdAsync(actor.SchoolId.Value, cancellationToken);
        if (school is null || school.Status != SchoolStatus.Active)
            return ScopeResult.Fail(AssessmentErrorCode.SchoolNotActive);

        return ScopeResult.Ok(actor, school, role);
    }

    private async Task<bool> CanManagePairAsync(
        ScopeResult scope,
        Guid classGroupId,
        Guid subjectId,
        CancellationToken cancellationToken)
    {
        if (scope.Role == RoleNames.SchoolAdmin)
            return true;

        return await _repo.IsTeacherAssignedAsync(
            scope.School!.Id,
            scope.Actor!.Id,
            classGroupId,
            subjectId,
            cancellationToken);
    }

    private Task<bool> CanManageAssessmentAsync(
        ScopeResult scope,
        Assessment assessment,
        CancellationToken cancellationToken) =>
        CanManagePairAsync(
            scope,
            assessment.ClassGroupId,
            assessment.SubjectId,
            cancellationToken);

    private static bool CanManage(
        ScopeResult scope,
        AssessmentSnapshot snapshot,
        Assessment assessment)
    {
        if (scope.Role == RoleNames.SchoolAdmin)
            return true;

        return snapshot.TeacherAssignments.Any(
            x => x.TeacherUserId == scope.Actor!.Id &&
                 x.ClassGroupId == assessment.ClassGroupId &&
                 x.SubjectId == assessment.SubjectId);
    }

    private async Task<QuestionContext> ResolveQuestionContextAsync(
        Guid actorUserId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return QuestionContext.Fail(scope.Error!.Value);

        var question = await _repo.GetQuestionAsync(scope.School!.Id, questionId, cancellationToken);
        if (question is null) return QuestionContext.Fail(AssessmentErrorCode.QuestionNotFound);

        var assessment = await _repo.GetAssessmentAsync(scope.School.Id, question.AssessmentId, cancellationToken);
        if (assessment is null) return QuestionContext.Fail(AssessmentErrorCode.AssessmentNotFound);

        if (!await CanManageAssessmentAsync(scope, assessment, cancellationToken))
            return QuestionContext.Fail(AssessmentErrorCode.AccessDenied);

        return QuestionContext.Ok(scope, assessment, question);
    }

    private static bool ValidMax(decimal value) => value > 0m && value <= 10000m;
    private static decimal Round(decimal value) =>
        decimal.Round(value, 2, MidpointRounding.AwayFromZero);
    private static string Clean(string? value) => value?.Trim() ?? string.Empty;

    private static AssessmentCommandResult MapPersistence(AssessmentPersistenceResult result)
    {
        if (result.Succeeded) return AssessmentCommandResult.Success();

        return result.Error == AssessmentPersistenceError.Conflict
            ? Fail(AssessmentErrorCode.ConcurrencyConflict)
            : Fail(AssessmentErrorCode.PersistenceError);
    }

    private static AssessmentCommandResult Fail(AssessmentErrorCode code) =>
        AssessmentCommandResult.Failure(string.Empty, code);

    private static AssessmentCommandResult Fail(string field, AssessmentErrorCode code) =>
        AssessmentCommandResult.Failure(field, code);

    private sealed record ScopeResult(
        bool Succeeded,
        SchoolUserRecord? Actor,
        School? School,
        string? Role,
        AssessmentErrorCode? Error)
    {
        public static ScopeResult Ok(SchoolUserRecord actor, School school, string role) =>
            new(true, actor, school, role, null);

        public static ScopeResult Fail(AssessmentErrorCode error) =>
            new(false, null, null, null, error);
    }

    private sealed record QuestionContext(
        bool Succeeded,
        ScopeResult? Scope,
        Assessment? Assessment,
        AssessmentQuestion? Question,
        AssessmentErrorCode? Error)
    {
        public static QuestionContext Ok(
            ScopeResult scope,
            Assessment assessment,
            AssessmentQuestion question) =>
            new(true, scope, assessment, question, null);

        public static QuestionContext Fail(AssessmentErrorCode error) =>
            new(false, null, null, null, error);
    }
}
