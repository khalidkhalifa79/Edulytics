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
    public async Task<AssessmentCommandResult> CreateAssessmentAsync(
        Guid actorUserId,
        CreateAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var title = Clean(request.Title);
        if (title.Length == 0) return Fail(nameof(request.Title), AssessmentErrorCode.Required);
        if (title.Length > 200) return Fail(nameof(request.Title), AssessmentErrorCode.InvalidText);
        if (!ValidMax(request.MaxScore)) return Fail(nameof(request.MaxScore), AssessmentErrorCode.InvalidMaxScore);

        var schoolId = scope.School!.Id;
        var classGroup = await _repo.GetClassGroupAsync(schoolId, request.ClassGroupId, cancellationToken);
        if (classGroup is null) return Fail(nameof(request.ClassGroupId), AssessmentErrorCode.ClassGroupNotFound);

        var subject = await _repo.GetSubjectAsync(schoolId, request.SubjectId, cancellationToken);
        if (subject is null) return Fail(nameof(request.SubjectId), AssessmentErrorCode.SubjectNotFound);

        var term = await _repo.GetTermAsync(schoolId, request.TermId, cancellationToken);
        if (term is null || term.AcademicYearId != classGroup.AcademicYearId)
            return Fail(nameof(request.TermId), AssessmentErrorCode.TermNotFound);

        if (classGroup.Status != AcademicStructureStatus.Active ||
            subject.Status != AcademicStructureStatus.Active ||
            term.Status != AcademicStructureStatus.Active)
            return Fail(AssessmentErrorCode.PersistenceError);

        if (request.AssessmentDate < term.StartsOn || request.AssessmentDate > term.EndsOn)
            return Fail(nameof(request.AssessmentDate), AssessmentErrorCode.InvalidDate);

        if (!await CanManagePairAsync(scope, classGroup.Id, subject.Id, cancellationToken))
            return Fail(AssessmentErrorCode.TeacherNotAssigned);

        if (await _repo.AssessmentTitleExistsAsync(
                schoolId,
                classGroup.Id,
                term.Id,
                title.ToUpperInvariant(),
                cancellationToken: cancellationToken))
            return Fail(nameof(request.Title), AssessmentErrorCode.DuplicateAssessment);

        var now = DateTime.UtcNow;
        var entity = new Assessment
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            SubjectId = subject.Id,
            ClassGroupId = classGroup.Id,
            AcademicYearId = classGroup.AcademicYearId,
            TermId = term.Id,
            Title = title,
            AssessmentDate = request.AssessmentDate,
            MaxScore = Round(request.MaxScore),
            Status = AssessmentStatus.Draft,
            CreatedByUserId = actorUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _repo.AddAsync(
            entity,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "Assessment.Created",
            "Assessment",
            entity.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["subjectId"] =
                        entity.SubjectId,
                    ["classGroupId"] =
                        entity.ClassGroupId,
                    ["academicYearId"] =
                        entity.AcademicYearId,
                    ["termId"] =
                        entity.TermId,
                    ["title"] =
                        entity.Title,
                    ["assessmentDate"] =
                        entity.AssessmentDate,
                    ["maxScore"] =
                        entity.MaxScore,
                    ["status"] =
                        entity.Status.ToString()
                },
            "Assessment created.",
            cancellationToken);

        var saved =
            await _repo.SaveAsync(
                cancellationToken);

        return saved.Succeeded
            ? AssessmentCommandResult.Success(entity.Id)
            : MapPersistence(saved);
    }

    public async Task<AssessmentCommandResult> UpdateAssessmentAsync(
        Guid actorUserId,
        UpdateAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var assessment = await _repo.GetAssessmentAsync(scope.School!.Id, request.Id, cancellationToken);
        if (assessment is null) return Fail(AssessmentErrorCode.AssessmentNotFound);

        if (!await CanManageAssessmentAsync(scope, assessment, cancellationToken))
            return Fail(AssessmentErrorCode.AccessDenied);

        if (assessment.Status != AssessmentStatus.Draft)
            return Fail(AssessmentErrorCode.AssessmentNotDraft);

        var title = Clean(request.Title);
        if (title.Length == 0) return Fail(nameof(request.Title), AssessmentErrorCode.Required);
        if (title.Length > 200) return Fail(nameof(request.Title), AssessmentErrorCode.InvalidText);
        if (!ValidMax(request.MaxScore)) return Fail(nameof(request.MaxScore), AssessmentErrorCode.InvalidMaxScore);

        var term = await _repo.GetTermAsync(scope.School.Id, assessment.TermId, cancellationToken);
        if (term is null) return Fail(AssessmentErrorCode.TermNotFound);

        if (request.AssessmentDate < term.StartsOn || request.AssessmentDate > term.EndsOn)
            return Fail(nameof(request.AssessmentDate), AssessmentErrorCode.InvalidDate);

        var snapshot = await _repo.GetSnapshotAsync(scope.School.Id, cancellationToken);
        var questionTotal = snapshot.Questions
            .Where(x => x.AssessmentId == assessment.Id)
            .Sum(x => x.MaxScore);

        if (request.MaxScore < questionTotal)
            return Fail(nameof(request.MaxScore), AssessmentErrorCode.AssessmentScoreMismatch);

        if (await _repo.AssessmentTitleExistsAsync(
                scope.School.Id,
                assessment.ClassGroupId,
                assessment.TermId,
                title.ToUpperInvariant(),
                assessment.Id,
                cancellationToken))
            return Fail(nameof(request.Title), AssessmentErrorCode.DuplicateAssessment);

        var oldValues =
            new Dictionary<string, object?>
            {
                ["title"] =
                    assessment.Title,
                ["assessmentDate"] =
                    assessment.AssessmentDate,
                ["maxScore"] =
                    assessment.MaxScore
            };

        assessment.Title = title;
        assessment.AssessmentDate =
            request.AssessmentDate;
        assessment.MaxScore =
            Round(request.MaxScore);
        assessment.UpdatedAtUtc =
            DateTime.UtcNow;

        await QueueAuditAsync(
            scope,
            "Assessment.Updated",
            "Assessment",
            assessment.Id,
            oldValues,
            new Dictionary<string, object?>
            {
                ["title"] =
                    assessment.Title,
                ["assessmentDate"] =
                    assessment.AssessmentDate,
                ["maxScore"] =
                    assessment.MaxScore
            },
            "Assessment updated.",
            cancellationToken);

        return MapPersistence(
            await _repo.SaveWithRowVersionAsync(
                assessment,
                request.RowVersion,
                cancellationToken));
    }


    public async Task<AssessmentCommandResult> CreateQuestionAsync(
        Guid actorUserId,
        CreateAssessmentQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var assessment = await _repo.GetAssessmentAsync(
            scope.School!.Id,
            request.AssessmentId,
            cancellationToken);

        if (assessment is null)
            return Fail(AssessmentErrorCode.AssessmentNotFound);

        if (!await CanManageAssessmentAsync(
                scope,
                assessment,
                cancellationToken))
        {
            return Fail(AssessmentErrorCode.AccessDenied);
        }

        if (assessment.Status != AssessmentStatus.Draft)
            return Fail(AssessmentErrorCode.AssessmentNotDraft);

        var prompt = Clean(request.Prompt);

        if (prompt.Length == 0)
            return Fail(
                nameof(request.Prompt),
                AssessmentErrorCode.Required);

        if (prompt.Length > 1000)
            return Fail(
                nameof(request.Prompt),
                AssessmentErrorCode.InvalidText);

        if (!ValidMax(request.MaxScore))
            return Fail(
                nameof(request.MaxScore),
                AssessmentErrorCode.InvalidQuestionScore);

        if (request.Order <= 0)
            return Fail(
                nameof(request.Order),
                AssessmentErrorCode.InvalidOrder);

        var selectedOutcomeIds =
            NormalizeOutcomeIds(request.OutcomeIds);

        if (selectedOutcomeIds.Length == 0)
            return Fail(
                nameof(request.OutcomeIds),
                AssessmentErrorCode.Required);

        if (await _repo.QuestionOrderExistsAsync(
                scope.School.Id,
                assessment.Id,
                request.Order,
                cancellationToken: cancellationToken))
        {
            return Fail(
                nameof(request.Order),
                AssessmentErrorCode.DuplicateQuestionOrder);
        }

        var snapshot = await _repo.GetSnapshotAsync(
            scope.School.Id,
            cancellationToken);

        var currentTotal = snapshot.Questions
            .Where(x => x.AssessmentId == assessment.Id)
            .Sum(x => x.MaxScore);

        if (currentTotal + request.MaxScore > assessment.MaxScore)
        {
            return Fail(
                nameof(request.MaxScore),
                AssessmentErrorCode.AssessmentScoreMismatch);
        }

        var classGroup = snapshot.ClassGroups
            .FirstOrDefault(x => x.Id == assessment.ClassGroupId);

        if (classGroup is null)
            return Fail(AssessmentErrorCode.ClassGroupNotFound);

        var outcomeError =
            ValidateOutcomeSelection(
                snapshot,
                assessment,
                classGroup.GradeLevelId,
                selectedOutcomeIds);

        if (outcomeError.HasValue)
        {
            return Fail(
                nameof(request.OutcomeIds),
                outcomeError.Value);
        }

        var question = new AssessmentQuestion
        {
            Id = Guid.NewGuid(),
            SchoolId = scope.School.Id,
            AssessmentId = assessment.Id,
            Prompt = prompt,
            MaxScore = Round(request.MaxScore),
            Order = request.Order
        };

        await _repo.AddAsync(
            question,
            cancellationToken);

        var mappings = new List<QuestionLearningOutcome>();

        foreach (var outcomeId in selectedOutcomeIds)
        {
            var mapping = new QuestionLearningOutcome
            {
                Id = Guid.NewGuid(),
                SchoolId = scope.School.Id,
                AssessmentQuestionId = question.Id,
                LearningOutcomeId = outcomeId
            };

            mappings.Add(mapping);

            await _repo.AddAsync(
                mapping,
                cancellationToken);
        }

        assessment.UpdatedAtUtc = DateTime.UtcNow;

        await QueueAuditAsync(
            scope,
            "AssessmentQuestion.Created",
            "AssessmentQuestion",
            question.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["assessmentId"] = question.AssessmentId,
                    ["promptLength"] = question.Prompt.Length,
                    ["maxScore"] = question.MaxScore,
                    ["order"] = question.Order,
                    ["outcomeIds"] =
                        selectedOutcomeIds
                            .Select(x => x.ToString("D"))
                            .ToArray()
                },
            "Assessment question created with learning outcomes.",
            cancellationToken);

        foreach (var mapping in mappings)
        {
            await QueueAuditAsync(
                scope,
                "QuestionOutcome.Mapped",
                "QuestionLearningOutcome",
                mapping.Id,
                oldValues: null,
                newValues:
                    new Dictionary<string, object?>
                    {
                        ["assessmentQuestionId"] =
                            mapping.AssessmentQuestionId,
                        ["learningOutcomeId"] =
                            mapping.LearningOutcomeId
                    },
                "Learning outcome mapped during question creation.",
                cancellationToken);
        }

        var saved = await _repo.SaveWithRowVersionAsync(
            assessment,
            request.AssessmentRowVersion,
            cancellationToken);

        return saved.Succeeded
            ? AssessmentCommandResult.Success(question.Id)
            : MapPersistence(saved);
    }


    public async Task<AssessmentCommandResult> UpdateQuestionAsync(
        Guid actorUserId,
        UpdateAssessmentQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveQuestionContextAsync(
            actorUserId,
            request.QuestionId,
            cancellationToken);

        if (!context.Succeeded)
            return Fail(context.Error!.Value);

        var assessment = context.Assessment!;
        var question = context.Question!;

        if (assessment.Status != AssessmentStatus.Draft)
            return Fail(AssessmentErrorCode.AssessmentNotDraft);

        var prompt = Clean(request.Prompt);

        if (prompt.Length == 0)
            return Fail(
                nameof(request.Prompt),
                AssessmentErrorCode.Required);

        if (prompt.Length > 1000)
            return Fail(
                nameof(request.Prompt),
                AssessmentErrorCode.InvalidText);

        if (!ValidMax(request.MaxScore))
            return Fail(
                nameof(request.MaxScore),
                AssessmentErrorCode.InvalidQuestionScore);

        if (request.Order <= 0)
            return Fail(
                nameof(request.Order),
                AssessmentErrorCode.InvalidOrder);

        var selectedOutcomeIds =
            NormalizeOutcomeIds(request.OutcomeIds);

        if (selectedOutcomeIds.Length == 0)
            return Fail(
                nameof(request.OutcomeIds),
                AssessmentErrorCode.Required);

        if (await _repo.QuestionOrderExistsAsync(
                context.Scope!.School!.Id,
                assessment.Id,
                request.Order,
                question.Id,
                cancellationToken))
        {
            return Fail(
                nameof(request.Order),
                AssessmentErrorCode.DuplicateQuestionOrder);
        }

        var snapshot = await _repo.GetSnapshotAsync(
            context.Scope.School.Id,
            cancellationToken);

        var otherTotal = snapshot.Questions
            .Where(x =>
                x.AssessmentId == assessment.Id &&
                x.Id != question.Id)
            .Sum(x => x.MaxScore);

        if (otherTotal + request.MaxScore > assessment.MaxScore)
        {
            return Fail(
                nameof(request.MaxScore),
                AssessmentErrorCode.AssessmentScoreMismatch);
        }

        var classGroup = snapshot.ClassGroups
            .FirstOrDefault(x => x.Id == assessment.ClassGroupId);

        if (classGroup is null)
            return Fail(AssessmentErrorCode.ClassGroupNotFound);

        var outcomeError =
            ValidateOutcomeSelection(
                snapshot,
                assessment,
                classGroup.GradeLevelId,
                selectedOutcomeIds);

        if (outcomeError.HasValue)
        {
            return Fail(
                nameof(request.OutcomeIds),
                outcomeError.Value);
        }

        var existingIds = snapshot.OutcomeMappings
            .Where(x =>
                x.AssessmentQuestionId == question.Id)
            .Select(x => x.LearningOutcomeId)
            .ToHashSet();

        var desiredIds = selectedOutcomeIds.ToHashSet();

        var toAdd = desiredIds
            .Except(existingIds)
            .OrderBy(x => x)
            .ToArray();

        var toRemove = existingIds
            .Except(desiredIds)
            .OrderBy(x => x)
            .ToArray();

        var oldValues =
            new Dictionary<string, object?>
            {
                ["promptLength"] = question.Prompt.Length,
                ["maxScore"] = question.MaxScore,
                ["order"] = question.Order,
                ["outcomeIds"] =
                    existingIds
                        .OrderBy(x => x)
                        .Select(x => x.ToString("D"))
                        .ToArray()
            };

        question.Prompt = prompt;
        question.MaxScore = Round(request.MaxScore);
        question.Order = request.Order;
        assessment.UpdatedAtUtc = DateTime.UtcNow;

        foreach (var outcomeId in toRemove)
        {
            var mapping = await _repo.GetMappingAsync(
                context.Scope.School.Id,
                question.Id,
                outcomeId,
                cancellationToken);

            if (mapping is null)
                return Fail(AssessmentErrorCode.OutcomeNotFound);

            _repo.RemoveMapping(mapping);

            await QueueAuditAsync(
                context.Scope,
                "QuestionOutcome.Unmapped",
                "QuestionLearningOutcome",
                mapping.Id,
                oldValues:
                    new Dictionary<string, object?>
                    {
                        ["assessmentQuestionId"] =
                            mapping.AssessmentQuestionId,
                        ["learningOutcomeId"] =
                            mapping.LearningOutcomeId
                    },
                newValues: null,
                "Learning outcome removed during question update.",
                cancellationToken);
        }

        foreach (var outcomeId in toAdd)
        {
            var mapping = new QuestionLearningOutcome
            {
                Id = Guid.NewGuid(),
                SchoolId = context.Scope.School.Id,
                AssessmentQuestionId = question.Id,
                LearningOutcomeId = outcomeId
            };

            await _repo.AddAsync(
                mapping,
                cancellationToken);

            await QueueAuditAsync(
                context.Scope,
                "QuestionOutcome.Mapped",
                "QuestionLearningOutcome",
                mapping.Id,
                oldValues: null,
                newValues:
                    new Dictionary<string, object?>
                    {
                        ["assessmentQuestionId"] =
                            mapping.AssessmentQuestionId,
                        ["learningOutcomeId"] =
                            mapping.LearningOutcomeId
                    },
                "Learning outcome mapped during question update.",
                cancellationToken);
        }

        await QueueAuditAsync(
            context.Scope,
            "AssessmentQuestion.Updated",
            "AssessmentQuestion",
            question.Id,
            oldValues,
            new Dictionary<string, object?>
            {
                ["promptLength"] = question.Prompt.Length,
                ["maxScore"] = question.MaxScore,
                ["order"] = question.Order,
                ["outcomeIds"] =
                    selectedOutcomeIds
                        .Select(x => x.ToString("D"))
                        .ToArray()
            },
            "Assessment question and outcomes updated.",
            cancellationToken);

        return MapPersistence(
            await _repo.SaveWithRowVersionAsync(
                assessment,
                request.AssessmentRowVersion,
                cancellationToken));
    }


    public async Task<AssessmentCommandResult> DeleteAssessmentAsync(
        Guid actorUserId,
        DeleteAssessmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(
            actorUserId,
            cancellationToken);

        if (!scope.Succeeded)
            return Fail(scope.Error!.Value);

        var assessment = await _repo.GetAssessmentAsync(
            scope.School!.Id,
            request.AssessmentId,
            cancellationToken);

        if (assessment is null)
            return Fail(AssessmentErrorCode.AssessmentNotFound);

        if (!await CanManageAssessmentAsync(
                scope,
                assessment,
                cancellationToken))
        {
            return Fail(AssessmentErrorCode.AccessDenied);
        }

        if (assessment.Status != AssessmentStatus.Draft)
            return Fail(AssessmentErrorCode.AssessmentNotDraft);

        var oldValues =
            new Dictionary<string, object?>
            {
                ["subjectId"] = assessment.SubjectId,
                ["classGroupId"] = assessment.ClassGroupId,
                ["academicYearId"] = assessment.AcademicYearId,
                ["termId"] = assessment.TermId,
                ["title"] = assessment.Title,
                ["assessmentDate"] = assessment.AssessmentDate,
                ["maxScore"] = assessment.MaxScore,
                ["status"] = assessment.Status.ToString()
            };

        await _repo.RemoveAssessmentAsync(
            scope.School.Id,
            assessment,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "Assessment.Deleted",
            "Assessment",
            assessment.Id,
            oldValues,
            newValues: null,
            "Draft assessment deleted.",
            cancellationToken);

        return MapPersistence(
            await _repo.SaveWithRowVersionAsync(
                assessment,
                request.RowVersion,
                cancellationToken));
    }

    public async Task<AssessmentCommandResult> DeleteQuestionAsync(
        Guid actorUserId,
        DeleteAssessmentQuestionRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveQuestionContextAsync(
            actorUserId,
            request.QuestionId,
            cancellationToken);

        if (!context.Succeeded)
            return Fail(context.Error!.Value);

        var assessment = context.Assessment!;
        var question = context.Question!;

        if (assessment.Status != AssessmentStatus.Draft)
            return Fail(AssessmentErrorCode.AssessmentNotDraft);

        var oldValues =
            new Dictionary<string, object?>
            {
                ["assessmentId"] = question.AssessmentId,
                ["promptLength"] = question.Prompt.Length,
                ["maxScore"] = question.MaxScore,
                ["order"] = question.Order
            };

        await _repo.RemoveQuestionAsync(
            context.Scope!.School!.Id,
            question,
            cancellationToken);

        assessment.UpdatedAtUtc = DateTime.UtcNow;

        await QueueAuditAsync(
            context.Scope,
            "AssessmentQuestion.Deleted",
            "AssessmentQuestion",
            question.Id,
            oldValues,
            newValues: null,
            "Draft assessment question deleted.",
            cancellationToken);

        return MapPersistence(
            await _repo.SaveWithRowVersionAsync(
                assessment,
                request.AssessmentRowVersion,
                cancellationToken));
    }

    public async Task<AssessmentCommandResult> MapOutcomeAsync(
        Guid actorUserId,
        MapQuestionOutcomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveQuestionContextAsync(actorUserId, request.QuestionId, cancellationToken);
        if (!context.Succeeded) return Fail(context.Error!.Value);

        if (context.Assessment!.Status != AssessmentStatus.Draft)
            return Fail(AssessmentErrorCode.AssessmentNotDraft);

        var schoolId = context.Scope!.School!.Id;
        var outcome = await _repo.GetLearningOutcomeAsync(schoolId, request.OutcomeId, cancellationToken);
        if (outcome is null) return Fail(AssessmentErrorCode.OutcomeNotFound);

        var topic = await _repo.GetCurriculumTopicAsync(schoolId, outcome.TopicId, cancellationToken);
        if (topic is null) return Fail(AssessmentErrorCode.OutcomeNotFound);

        var classGroup = await _repo.GetClassGroupAsync(schoolId, context.Assessment.ClassGroupId, cancellationToken);
        if (classGroup is null) return Fail(AssessmentErrorCode.ClassGroupNotFound);

        var snapshot = await _repo.GetSnapshotAsync(schoolId, cancellationToken);
        var eligibleFrameworkVersionIds =
            ResolveEligibleFrameworkVersionIds(
                snapshot,
                context.Assessment.AcademicYearId,
                classGroup.GradeLevelId,
                context.Assessment.SubjectId,
                classGroup.AcademicProgramId);

        if (outcome.AcademicProgramId != classGroup.AcademicProgramId ||
            topic.AcademicProgramId != classGroup.AcademicProgramId ||
            outcome.SubjectId != context.Assessment.SubjectId ||
            outcome.GradeLevelId != classGroup.GradeLevelId ||
            topic.SubjectId != context.Assessment.SubjectId ||
            topic.GradeLevelId != classGroup.GradeLevelId ||
            topic.FrameworkVersionId != outcome.FrameworkVersionId ||
            !eligibleFrameworkVersionIds.Contains(outcome.FrameworkVersionId))
            return Fail(AssessmentErrorCode.OutcomeDoesNotMatchAssessment);

        if (await _repo.MappingExistsAsync(schoolId, context.Question!.Id, outcome.Id, cancellationToken))
            return Fail(AssessmentErrorCode.DuplicateOutcomeMapping);

        var mapping =
            new QuestionLearningOutcome
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                AssessmentQuestionId =
                    context.Question.Id,
                LearningOutcomeId =
                    outcome.Id
            };

        await _repo.AddAsync(
            mapping,
            cancellationToken);

        context.Assessment.UpdatedAtUtc =
            DateTime.UtcNow;

        await QueueAuditAsync(
            context.Scope!,
            "QuestionOutcome.Mapped",
            "QuestionLearningOutcome",
            mapping.Id,
            oldValues: null,
            newValues:
                new Dictionary<string, object?>
                {
                    ["assessmentQuestionId"] =
                        mapping.AssessmentQuestionId,
                    ["learningOutcomeId"] =
                        mapping.LearningOutcomeId
                },
            "Learning outcome mapped to question.",
            cancellationToken);

        return MapPersistence(
            await _repo.SaveWithRowVersionAsync(
                context.Assessment,
                request.AssessmentRowVersion,
                cancellationToken));
    }

    public async Task<AssessmentCommandResult> UnmapOutcomeAsync(
        Guid actorUserId,
        UnmapQuestionOutcomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var context = await ResolveQuestionContextAsync(actorUserId, request.QuestionId, cancellationToken);
        if (!context.Succeeded) return Fail(context.Error!.Value);

        if (context.Assessment!.Status != AssessmentStatus.Draft)
            return Fail(AssessmentErrorCode.AssessmentNotDraft);

        var mapping = await _repo.GetMappingAsync(
            context.Scope!.School!.Id,
            request.QuestionId,
            request.OutcomeId,
            cancellationToken);

        if (mapping is null) return Fail(AssessmentErrorCode.OutcomeNotFound);

        _repo.RemoveMapping(mapping);

        context.Assessment.UpdatedAtUtc =
            DateTime.UtcNow;

        await QueueAuditAsync(
            context.Scope!,
            "QuestionOutcome.Unmapped",
            "QuestionLearningOutcome",
            mapping.Id,
            oldValues:
                new Dictionary<string, object?>
                {
                    ["assessmentQuestionId"] =
                        mapping.AssessmentQuestionId,
                    ["learningOutcomeId"] =
                        mapping.LearningOutcomeId
                },
            newValues: null,
            "Learning outcome unmapped from question.",
            cancellationToken);

        return MapPersistence(
            await _repo.SaveWithRowVersionAsync(
                context.Assessment,
                request.AssessmentRowVersion,
                cancellationToken));
    }

    public async Task<AssessmentCommandResult> OpenAssessmentAsync(
        Guid actorUserId,
        Guid assessmentId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var assessment = await _repo.GetAssessmentAsync(scope.School!.Id, assessmentId, cancellationToken);
        if (assessment is null) return Fail(AssessmentErrorCode.AssessmentNotFound);

        if (!await CanManageAssessmentAsync(scope, assessment, cancellationToken))
            return Fail(AssessmentErrorCode.AccessDenied);

        if (assessment.Status != AssessmentStatus.Draft)
            return Fail(AssessmentErrorCode.AssessmentNotDraft);

        var snapshot = await _repo.GetSnapshotAsync(scope.School.Id, cancellationToken);
        var questions = snapshot.Questions.Where(x => x.AssessmentId == assessment.Id).ToArray();

        if (questions.Length == 0)
            return Fail(AssessmentErrorCode.AssessmentHasNoQuestions);

        if (questions.Sum(x => x.MaxScore) != assessment.MaxScore)
            return Fail(AssessmentErrorCode.AssessmentScoreMismatch);

        var mapped = snapshot.OutcomeMappings
            .Where(x => questions.Any(q => q.Id == x.AssessmentQuestionId))
            .Select(x => x.AssessmentQuestionId)
            .ToHashSet();

        if (questions.Any(x => !mapped.Contains(x.Id)))
            return Fail(AssessmentErrorCode.QuestionMissingOutcome);

        var classGroup = snapshot.ClassGroups
            .FirstOrDefault(x => x.Id == assessment.ClassGroupId);

        if (classGroup is null)
            return Fail(AssessmentErrorCode.ClassGroupNotFound);

        var eligibleFrameworkVersionIds =
            ResolveEligibleFrameworkVersionIds(
                snapshot,
                assessment.AcademicYearId,
                classGroup.GradeLevelId,
                assessment.SubjectId,
                classGroup.AcademicProgramId);

        var mappedOutcomeIds = snapshot.OutcomeMappings
            .Where(x => mapped.Contains(x.AssessmentQuestionId))
            .Select(x => x.LearningOutcomeId)
            .ToHashSet();

        var eligibleMappedOutcomeIds = snapshot.LearningOutcomes
            .Where(x =>
                mappedOutcomeIds.Contains(x.Id) &&
                x.AcademicProgramId == classGroup.AcademicProgramId &&
                x.SubjectId == assessment.SubjectId &&
                x.GradeLevelId == classGroup.GradeLevelId &&
                eligibleFrameworkVersionIds.Contains(x.FrameworkVersionId))
            .Select(x => x.Id)
            .ToHashSet();

        if (!mappedOutcomeIds.SetEquals(eligibleMappedOutcomeIds))
            return Fail(AssessmentErrorCode.OutcomeDoesNotMatchAssessment);

        var previousStatus =
            assessment.Status;

        assessment.Status =
            AssessmentStatus.Open;
        assessment.UpdatedAtUtc =
            DateTime.UtcNow;

        await QueueAuditAsync(
            scope,
            "Assessment.Opened",
            "Assessment",
            assessment.Id,
            oldValues:
                new Dictionary<string, object?>
                {
                    ["status"] =
                        previousStatus.ToString()
                },
            newValues:
                new Dictionary<string, object?>
                {
                    ["status"] =
                        assessment.Status.ToString()
                },
            "Assessment opened.",
            cancellationToken);

        return MapPersistence(
            await _repo.SaveWithRowVersionAsync(
                assessment,
                rowVersion,
                cancellationToken));
    }

    public async Task<AssessmentCommandResult> CloseAssessmentAsync(
        Guid actorUserId,
        Guid assessmentId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var assessment = await _repo.GetAssessmentAsync(scope.School!.Id, assessmentId, cancellationToken);
        if (assessment is null) return Fail(AssessmentErrorCode.AssessmentNotFound);

        if (!await CanManageAssessmentAsync(scope, assessment, cancellationToken))
            return Fail(AssessmentErrorCode.AccessDenied);

        if (assessment.Status == AssessmentStatus.Closed)
            return Fail(AssessmentErrorCode.AssessmentAlreadyClosed);

        if (assessment.Status != AssessmentStatus.Open)
            return Fail(AssessmentErrorCode.AssessmentNotOpen);

        var previousStatus =
            assessment.Status;

        assessment.Status =
            AssessmentStatus.Closed;
        assessment.UpdatedAtUtc =
            DateTime.UtcNow;

        await QueueAuditAsync(
            scope,
            "Assessment.Closed",
            "Assessment",
            assessment.Id,
            oldValues:
                new Dictionary<string, object?>
                {
                    ["status"] =
                        previousStatus.ToString()
                },
            newValues:
                new Dictionary<string, object?>
                {
                    ["status"] =
                        assessment.Status.ToString()
                },
            "Assessment closed.",
            cancellationToken);

        return MapPersistence(
            await _repo.SaveWithRowVersionAsync(
                assessment,
                rowVersion,
                cancellationToken));
    }

    public async Task<AssessmentCommandResult> SaveStudentResultAsync(
        Guid actorUserId,
        SaveStudentAssessmentResultRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded) return Fail(scope.Error!.Value);

        var schoolId = scope.School!.Id;
        var assessment = await _repo.GetAssessmentAsync(schoolId, request.AssessmentId, cancellationToken);

        if (assessment is null) return Fail(AssessmentErrorCode.AssessmentNotFound);
        if (!await CanManageAssessmentAsync(scope, assessment, cancellationToken))
            return Fail(AssessmentErrorCode.AccessDenied);
        if (assessment.Status != AssessmentStatus.Open)
            return Fail(AssessmentErrorCode.AssessmentNotOpen);

        var student = await _repo.GetStudentProfileAsync(schoolId, request.StudentProfileId, cancellationToken);
        if (student is null ||
            student.IsArchived ||
            student.Status != AcademicStructureStatus.Active)
            return Fail(AssessmentErrorCode.StudentNotFound);

        if (!await _repo.IsStudentEnrolledAsync(
                schoolId,
                assessment.AcademicYearId,
                assessment.ClassGroupId,
                student.Id,
                cancellationToken))
            return Fail(AssessmentErrorCode.StudentNotEnrolled);

        var snapshot = await _repo.GetSnapshotAsync(schoolId, cancellationToken);
        var questions = snapshot.Questions
            .Where(x => x.AssessmentId == assessment.Id)
            .OrderBy(x => x.Order)
            .ToArray();

        if (questions.Length == 0 ||
            request.QuestionIds.Count != questions.Length ||
            request.Scores.Count != questions.Length ||
            request.QuestionIds.Distinct().Count() != questions.Length)
            return Fail(AssessmentErrorCode.ResultQuestionMismatch);

        var byId = questions.ToDictionary(x => x.Id);
        decimal total = 0m;

        for (var i = 0; i < request.QuestionIds.Count; i++)
        {
            if (!byId.TryGetValue(request.QuestionIds[i], out var question))
                return Fail(AssessmentErrorCode.ResultQuestionMismatch);

            var score = Round(request.Scores[i]);
            if (score < 0m || score > question.MaxScore)
                return Fail(AssessmentErrorCode.InvalidQuestionScore);

            total += score;
        }

        total = Round(total);
        if (total > assessment.MaxScore)
            return Fail(AssessmentErrorCode.InvalidQuestionScore);

        var percentage = decimal.Round(
            total / assessment.MaxScore * 100m,
            2,
            MidpointRounding.AwayFromZero);

        var now = DateTime.UtcNow;
        var result = await _repo.GetResultAsync(schoolId, assessment.Id, student.Id, cancellationToken);
        var isNew = result is null;

        if (isNew)
        {
            result = new AssessmentResult
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                AssessmentId = assessment.Id,
                StudentProfileId = student.Id,
                Score = total,
                Percentage = percentage,
                EnteredByUserId = actorUserId,
                EnteredAtUtc = now,
                UpdatedAtUtc = now
            };
            await _repo.AddAsync(result, cancellationToken);
        }
        else
        {
            if (request.ResultRowVersion is null || request.ResultRowVersion.Length == 0)
                return Fail(AssessmentErrorCode.ConcurrencyConflict);

            result!.Score = total;
            result.Percentage = percentage;
            result.EnteredByUserId = actorUserId;
            result.UpdatedAtUtc = now;
        }

        for (var i = 0; i < request.QuestionIds.Count; i++)
        {
            var questionId = request.QuestionIds[i];
            var score = Round(request.Scores[i]);

            var answer = isNew
                ? null
                : await _repo.GetAnswerAsync(schoolId, result!.Id, questionId, cancellationToken);

            if (answer is null)
            {
                await _repo.AddAsync(
                    new StudentAnswer
                    {
                        Id = Guid.NewGuid(),
                        SchoolId = schoolId,
                        AssessmentResultId = result!.Id,
                        AssessmentQuestionId = questionId,
                        Score = score,
                        UpdatedAtUtc = now
                    },
                    cancellationToken);
            }
            else
            {
                answer.Score = score;
                answer.UpdatedAtUtc = now;
            }
        }

        var eventId = Guid.NewGuid();

        var resultChanged =
            new AssessmentResultChangedEvent(
                eventId,
                schoolId,
                assessment.Id,
                result!.Id,
                assessment.ClassGroupId,
                assessment.SubjectId,
                student.Id,
                now);

        await _repo.AddOutboxAsync(
            new OutboxMessage
            {
                Id = eventId,
                SchoolId = schoolId,
                EventType = isNew
                    ? RealtimeEventTypes.AssessmentResultEntered
                    : RealtimeEventTypes.AssessmentResultUpdated,
                PayloadJson = JsonSerializer.Serialize(
                    resultChanged),
                OccurredAtUtc = now,
                AvailableAtUtc = now,
                ProcessingAttempts = 0,
                CorrelationId =
                    $"assessment-result:{eventId:N}"
            },
            cancellationToken);

        await QueueAuditAsync(
            scope,
            isNew
                ? "AssessmentResult.Created"
                : "AssessmentResult.Updated",
            "AssessmentResult",
            result!.Id,
            oldValues:
                isNew
                    ? null
                    : new Dictionary<string, object?>
                    {
                        ["assessmentId"] =
                            assessment.Id,
                        ["studentProfileId"] =
                            student.Id,
                        ["resultExisted"] =
                            true
                    },
            newValues:
                new Dictionary<string, object?>
                {
                    ["assessmentId"] =
                        assessment.Id,
                    ["studentProfileId"] =
                        student.Id,
                    ["resultCreated"] =
                        isNew,
                    ["answerCount"] =
                        request.QuestionIds.Count
                },
            isNew
                ? "Assessment result entered."
                : "Assessment result updated.",
            cancellationToken);

        var saved = isNew
            ? await _repo.SaveAsync(
                cancellationToken)
            : await _repo.SaveWithRowVersionAsync(
                result!,
                request.ResultRowVersion!,
                cancellationToken);

        return saved.Succeeded
            ? AssessmentCommandResult.Success(
                result!.Id)
            : MapPersistence(saved);
    }
}
