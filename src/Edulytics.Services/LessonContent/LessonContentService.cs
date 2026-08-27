using Edulytics.Core.Constants;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Lessons;
using Edulytics.Core.Users;
using Edulytics.Services.Auditing;

namespace Edulytics.Services.LessonContent;

public sealed class LessonContentService : ILessonContentService
{
    private readonly ILessonContentRepository _lessons;
    private readonly ISchoolUserRepository _users;
    private readonly ISchoolRepository _schools;
    private readonly IAuditService? _audit;

    public LessonContentService(
        ILessonContentRepository lessons,
        ISchoolUserRepository users,
        ISchoolRepository schools,
        IAuditService? audit = null)
    {
        _lessons = lessons;
        _users = users;
        _schools = schools;
        _audit = audit;
    }

    public async Task<LessonContentQueryResult<LessonContentDashboard>> GetDashboardAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return LessonContentQueryResult<LessonContentDashboard>.Failure(scope.Error!.Value);
        if (!LessonContentPolicy.CanReadStaff(scope.Actor!.Roles))
            return LessonContentQueryResult<LessonContentDashboard>.Failure(LessonContentErrorCode.AccessDenied);

        var topics = await _lessons.ListTopicContextsAsync(scope.School!.Id, cancellationToken);
        var lessons = await _lessons.ListLessonAggregatesAsync(scope.School.Id, cancellationToken);

        var groups = topics.Select(topic => new LessonContentTopicGroup(
            topic,
            lessons
                .Where(x => x.TopicId == topic.TopicId)
                .OrderBy(x => x.Order)
                .Select(x => new LessonContentSummary(
                    x.Id,
                    x.Order,
                    x.Status,
                    SelectTranslation(x.Translations, "en")?.Title ?? string.Empty,
                    x.PublishedAtUtc,
                    x.OutcomeIds))
                .ToArray()))
            .ToArray();

        return LessonContentQueryResult<LessonContentDashboard>.Success(
            new LessonContentDashboard(scope.School.Id, groups));
    }

    public async Task<LessonContentQueryResult<LessonContentEditor>> GetCreateEditorAsync(
        Guid actorUserId,
        Guid topicId,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return LessonContentQueryResult<LessonContentEditor>.Failure(scope.Error!.Value);
        if (!LessonContentPolicy.CanAuthor(scope.Actor!.Roles))
            return LessonContentQueryResult<LessonContentEditor>.Failure(LessonContentErrorCode.AccessDenied);

        var topic = await _lessons.GetTopicContextAsync(scope.School!.Id, topicId, cancellationToken);
        if (topic is null)
            return LessonContentQueryResult<LessonContentEditor>.Failure(LessonContentErrorCode.TopicNotFound);

        var existing = await _lessons.ListLessonAggregatesAsync(scope.School.Id, cancellationToken);
        var nextOrder = existing.Where(x => x.TopicId == topicId).Select(x => x.Order).DefaultIfEmpty(0).Max() + 1;

        return LessonContentQueryResult<LessonContentEditor>.Success(
            new LessonContentEditor(
                topic,
                null,
                nextOrder,
                LearningLessonStatus.Draft,
                Array.Empty<Guid>(),
                EmptyTranslation(),
                null,
                true));
    }

    public async Task<LessonContentQueryResult<LessonContentEditor>> GetEditEditorAsync(
        Guid actorUserId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return LessonContentQueryResult<LessonContentEditor>.Failure(scope.Error!.Value);
        if (!LessonContentPolicy.CanReadStaff(scope.Actor!.Roles))
            return LessonContentQueryResult<LessonContentEditor>.Failure(LessonContentErrorCode.AccessDenied);

        var aggregate = await _lessons.GetLessonAggregateAsync(scope.School!.Id, lessonId, cancellationToken);
        if (aggregate is null)
            return LessonContentQueryResult<LessonContentEditor>.Failure(LessonContentErrorCode.LessonNotFound);

        var topic = await _lessons.GetTopicContextAsync(scope.School.Id, aggregate.TopicId, cancellationToken);
        if (topic is null)
            return LessonContentQueryResult<LessonContentEditor>.Failure(LessonContentErrorCode.TopicNotFound);

        return LessonContentQueryResult<LessonContentEditor>.Success(
            new LessonContentEditor(
                topic,
                aggregate.Id,
                aggregate.Order,
                aggregate.Status,
                aggregate.OutcomeIds,
                ToInput(SelectTranslation(aggregate.Translations, "en")),
                ToNullableInput(SelectTranslation(aggregate.Translations, "pl")),
                false));
    }

    public async Task<LessonContentCommandResult> CreateAsync(
        Guid actorUserId,
        CreateLessonContentRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return LessonContentCommandResult.Failure(scope.Error!.Value);
        if (!LessonContentPolicy.CanAuthor(scope.Actor!.Roles))
            return LessonContentCommandResult.Failure(LessonContentErrorCode.AccessDenied);

        var validation = await ValidateDraftAsync(
            scope.School!.Id,
            request.TopicId,
            request.Order,
            request.OutcomeIds,
            request.English,
            excludeLessonId: null,
            cancellationToken);
        if (validation is not null)
            return validation;

        var now = DateTime.UtcNow;
        var lesson = new LearningLesson
        {
            Id = Guid.NewGuid(),
            SchoolId = scope.School.Id,
            TopicId = request.TopicId,
            Order = request.Order,
            Status = LearningLessonStatus.Draft,
            CreatedByUserId = actorUserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _lessons.AddLessonAsync(lesson, cancellationToken);
        await _lessons.AddTranslationAsync(
            NewTranslation(scope.School.Id, lesson.Id, "en", request.English, now),
            cancellationToken);

        if (LessonContentPolicy.HasAnyContent(request.Polish))
        {
            await _lessons.AddTranslationAsync(
                NewTranslation(scope.School.Id, lesson.Id, "pl", request.Polish!, now),
                cancellationToken);
        }

        foreach (var outcomeId in request.OutcomeIds.Distinct())
        {
            await _lessons.AddOutcomeLinkAsync(
                new LearningLessonOutcome
                {
                    SchoolId = scope.School.Id,
                    LessonId = lesson.Id,
                    LearningOutcomeId = outcomeId
                },
                cancellationToken);
        }

        await QueueAuditAsync(
            scope,
            "LessonContent.Created",
            lesson,
            oldStatus: null,
            newStatus: lesson.Status,
            request.OutcomeIds.Count,
            cancellationToken);

        var saved = await PersistAsync(cancellationToken);
        return saved.Succeeded
            ? LessonContentCommandResult.Success(lesson.Id)
            : saved;
    }

    public async Task<LessonContentCommandResult> UpdateDraftAsync(
        Guid actorUserId,
        UpdateLessonContentRequest request,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return LessonContentCommandResult.Failure(scope.Error!.Value);
        if (!LessonContentPolicy.CanAuthor(scope.Actor!.Roles))
            return LessonContentCommandResult.Failure(LessonContentErrorCode.AccessDenied);

        var lesson = await _lessons.GetLessonForUpdateAsync(
            scope.School!.Id,
            request.LessonId,
            cancellationToken);
        if (lesson is null)
            return LessonContentCommandResult.Failure(LessonContentErrorCode.LessonNotFound);
        if (lesson.Status == LearningLessonStatus.Published)
            return LessonContentCommandResult.Failure(LessonContentErrorCode.PublishedImmutable);
        if (lesson.Status != LearningLessonStatus.Draft)
            return LessonContentCommandResult.Failure(LessonContentErrorCode.InvalidState);

        var validation = await ValidateDraftAsync(
            scope.School.Id,
            lesson.TopicId,
            request.Order,
            request.OutcomeIds,
            request.English,
            lesson.Id,
            cancellationToken);
        if (validation is not null)
            return validation;

        lesson.Order = request.Order;
        lesson.UpdatedAtUtc = DateTime.UtcNow;

        var translations = await _lessons.GetTranslationsForUpdateAsync(
            scope.School.Id,
            lesson.Id,
            cancellationToken);
        UpsertTrackedTranslation(
            translations,
            scope.School.Id,
            lesson.Id,
            "en",
            request.English,
            lesson.UpdatedAtUtc,
            out var enNew);
        if (enNew is not null)
            await _lessons.AddTranslationAsync(enNew, cancellationToken);

        if (LessonContentPolicy.HasAnyContent(request.Polish))
        {
            UpsertTrackedTranslation(
                translations,
                scope.School.Id,
                lesson.Id,
                "pl",
                request.Polish!,
                lesson.UpdatedAtUtc,
                out var plNew);
            if (plNew is not null)
                await _lessons.AddTranslationAsync(plNew, cancellationToken);
        }

        await _lessons.ReplaceOutcomeLinksAsync(
            scope.School.Id,
            lesson.Id,
            request.OutcomeIds,
            cancellationToken);

        await QueueAuditAsync(
            scope,
            "LessonContent.DraftUpdated",
            lesson,
            lesson.Status,
            lesson.Status,
            request.OutcomeIds.Count,
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    public Task<LessonContentCommandResult> SubmitForReviewAsync(
        Guid actorUserId,
        Guid lessonId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            actorUserId,
            lessonId,
            LearningLessonStatus.Draft,
            LearningLessonStatus.InReview,
            "LessonContent.SubmittedForReview",
            cancellationToken);

    public Task<LessonContentCommandResult> ReturnToDraftAsync(
        Guid actorUserId,
        Guid lessonId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            actorUserId,
            lessonId,
            LearningLessonStatus.InReview,
            LearningLessonStatus.Draft,
            "LessonContent.ReturnedToDraft",
            cancellationToken);

    public Task<LessonContentCommandResult> PublishAsync(
        Guid actorUserId,
        Guid lessonId,
        CancellationToken cancellationToken = default) =>
        TransitionAsync(
            actorUserId,
            lessonId,
            LearningLessonStatus.InReview,
            LearningLessonStatus.Published,
            "LessonContent.Published",
            cancellationToken);

    public async Task<LessonContentQueryResult<IReadOnlyList<StudentLessonSummary>>> ListPublishedForStudentAsync(
        Guid actorUserId,
        string cultureCode,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return LessonContentQueryResult<IReadOnlyList<StudentLessonSummary>>.Failure(scope.Error!.Value);
        if (!LessonContentPolicy.IsStudent(scope.Actor!.Roles))
            return LessonContentQueryResult<IReadOnlyList<StudentLessonSummary>>.Failure(LessonContentErrorCode.AccessDenied);

        var rows = await _lessons.ListPublishedForStudentAsync(
            actorUserId,
            scope.School!.Id,
            cancellationToken);

        var items = rows.Select(x =>
        {
            var translation = SelectTranslation(x.Translations, cultureCode)
                ?? SelectTranslation(x.Translations, "en");
            return new StudentLessonSummary(
                x.Id,
                translation?.Title ?? string.Empty,
                x.TopicName,
                x.SubjectName,
                x.SubjectCode,
                x.GradeName,
                x.FrameworkName,
                x.Order);
        }).ToArray();

        return LessonContentQueryResult<IReadOnlyList<StudentLessonSummary>>.Success(items);
    }

    public async Task<LessonContentQueryResult<StudentLessonDetail>> GetPublishedForStudentAsync(
        Guid actorUserId,
        Guid lessonId,
        string cultureCode,
        CancellationToken cancellationToken = default)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return LessonContentQueryResult<StudentLessonDetail>.Failure(scope.Error!.Value);
        if (!LessonContentPolicy.IsStudent(scope.Actor!.Roles))
            return LessonContentQueryResult<StudentLessonDetail>.Failure(LessonContentErrorCode.AccessDenied);

        var row = await _lessons.GetPublishedForStudentAsync(
            actorUserId,
            scope.School!.Id,
            lessonId,
            cancellationToken);
        if (row is null)
            return LessonContentQueryResult<StudentLessonDetail>.Failure(LessonContentErrorCode.LessonNotFound);

        var translation = SelectTranslation(row.Translations, cultureCode)
            ?? SelectTranslation(row.Translations, "en");
        if (translation is null)
            return LessonContentQueryResult<StudentLessonDetail>.Failure(LessonContentErrorCode.LessonNotFound);

        return LessonContentQueryResult<StudentLessonDetail>.Success(
            new StudentLessonDetail(
                row.Id,
                translation.Title,
                row.TopicName,
                row.SubjectName,
                row.SubjectCode,
                row.GradeName,
                row.FrameworkName,
                translation.Explanation,
                translation.KeyConceptsAndRules,
                translation.WorkedExamples,
                translation.StepByStepSolutions,
                translation.CommonMistakes,
                translation.QuickSummary,
                row.Outcomes,
                row.PublishedAtUtc));
    }

    private async Task<LessonContentCommandResult?> ValidateDraftAsync(
        Guid schoolId,
        Guid topicId,
        int order,
        IReadOnlyList<Guid> outcomeIds,
        LessonTranslationInput english,
        Guid? excludeLessonId,
        CancellationToken cancellationToken)
    {
        if (order <= 0)
            return LessonContentCommandResult.Failure(LessonContentErrorCode.InvalidOrder, "Order");
        if (string.IsNullOrWhiteSpace(english.Title))
            return LessonContentCommandResult.Failure(LessonContentErrorCode.EnglishTitleRequired, "English.Title");
        if (outcomeIds.Count == 0)
            return LessonContentCommandResult.Failure(LessonContentErrorCode.OutcomeRequired, "OutcomeIds");

        var topic = await _lessons.GetTopicContextAsync(schoolId, topicId, cancellationToken);
        if (topic is null)
            return LessonContentCommandResult.Failure(LessonContentErrorCode.TopicNotFound, "TopicId");

        var allowed = topic.Outcomes.Select(x => x.Id).ToHashSet();
        if (outcomeIds.Distinct().Any(x => !allowed.Contains(x)))
            return LessonContentCommandResult.Failure(LessonContentErrorCode.OutcomeNotInTopic, "OutcomeIds");

        if (await _lessons.LessonOrderExistsAsync(
                schoolId,
                topicId,
                order,
                excludeLessonId,
                cancellationToken))
        {
            return LessonContentCommandResult.Failure(LessonContentErrorCode.DuplicateOrder, "Order");
        }

        return null;
    }

    private async Task<LessonContentCommandResult> TransitionAsync(
        Guid actorUserId,
        Guid lessonId,
        LearningLessonStatus expected,
        LearningLessonStatus target,
        string action,
        CancellationToken cancellationToken)
    {
        var scope = await ResolveScopeAsync(actorUserId, cancellationToken);
        if (!scope.Succeeded)
            return LessonContentCommandResult.Failure(scope.Error!.Value);
        if (!LessonContentPolicy.CanAuthor(scope.Actor!.Roles))
            return LessonContentCommandResult.Failure(LessonContentErrorCode.AccessDenied);

        var lesson = await _lessons.GetLessonForUpdateAsync(scope.School!.Id, lessonId, cancellationToken);
        if (lesson is null)
            return LessonContentCommandResult.Failure(LessonContentErrorCode.LessonNotFound);
        if (lesson.Status == LearningLessonStatus.Published)
            return LessonContentCommandResult.Failure(LessonContentErrorCode.PublishedImmutable);
        if (lesson.Status != expected || !LessonContentPolicy.CanTransition(lesson.Status, target))
            return LessonContentCommandResult.Failure(LessonContentErrorCode.InvalidState);

        var aggregate = await _lessons.GetLessonAggregateAsync(scope.School.Id, lesson.Id, cancellationToken);
        if (aggregate is null)
            return LessonContentCommandResult.Failure(LessonContentErrorCode.LessonNotFound);

        if (target is LearningLessonStatus.InReview or LearningLessonStatus.Published)
        {
            var english = ToInput(SelectTranslation(aggregate.Translations, "en"));
            if (!LessonContentPolicy.IsComplete(english))
                return LessonContentCommandResult.Failure(LessonContentErrorCode.EnglishContentIncomplete);
            if (aggregate.OutcomeIds.Count == 0)
                return LessonContentCommandResult.Failure(LessonContentErrorCode.OutcomeRequired);
        }

        var oldStatus = lesson.Status;
        var now = DateTime.UtcNow;
        lesson.Status = target;
        lesson.UpdatedAtUtc = now;

        if (target == LearningLessonStatus.InReview)
        {
            lesson.SubmittedAtUtc = now;
            lesson.SubmittedByUserId = actorUserId;
        }
        else if (target == LearningLessonStatus.Draft)
        {
            lesson.SubmittedAtUtc = null;
            lesson.SubmittedByUserId = null;
        }
        else if (target == LearningLessonStatus.Published)
        {
            lesson.PublishedAtUtc = now;
            lesson.PublishedByUserId = actorUserId;
        }

        await QueueAuditAsync(
            scope,
            action,
            lesson,
            oldStatus,
            target,
            aggregate.OutcomeIds.Count,
            cancellationToken);

        return await PersistAsync(cancellationToken);
    }

    private async Task<ScopeResult> ResolveScopeAsync(
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var actor = await _users.GetActorAsync(actorUserId, cancellationToken);
        if (actor is null || !actor.IsActive || actor.IsLocked || !actor.SchoolId.HasValue)
            return ScopeResult.Fail(LessonContentErrorCode.AccessDenied);

        var school = await _schools.GetByIdAsync(actor.SchoolId.Value, cancellationToken);
        if (school is null || school.Status != SchoolStatus.Active)
            return ScopeResult.Fail(LessonContentErrorCode.SchoolNotActive);

        return ScopeResult.Success(actor, school);
    }

    private async Task QueueAuditAsync(
        ScopeResult scope,
        string action,
        LearningLesson lesson,
        LearningLessonStatus? oldStatus,
        LearningLessonStatus newStatus,
        int outcomeCount,
        CancellationToken cancellationToken)
    {
        if (_audit is null)
            return;

        // Deliberately do NOT write lesson text/content to audit logs.
        await _audit.QueueAsync(
            new AuditEvent(
                scope.School!.Id,
                action,
                "LearningLesson",
                lesson.Id.ToString(),
                "LessonContent",
                oldStatus.HasValue
                    ? new Dictionary<string, object?> { ["status"] = oldStatus.Value.ToString() }
                    : null,
                new Dictionary<string, object?>
                {
                    ["status"] = newStatus.ToString(),
                    ["topicId"] = lesson.TopicId,
                    ["order"] = lesson.Order,
                    ["outcomeCount"] = outcomeCount
                },
                "Lesson content workflow changed."),
            cancellationToken);
    }

    private async Task<LessonContentCommandResult> PersistAsync(
        CancellationToken cancellationToken) =>
        (await _lessons.SaveAsync(cancellationToken)) switch
        {
            LessonContentWriteResult.Success => LessonContentCommandResult.Success(),
            LessonContentWriteResult.ConcurrencyConflict =>
                LessonContentCommandResult.Failure(LessonContentErrorCode.ConcurrencyConflict),
            _ => LessonContentCommandResult.Failure(LessonContentErrorCode.ConstraintViolation)
        };

    private static LearningLessonTranslation NewTranslation(
        Guid schoolId,
        Guid lessonId,
        string culture,
        LessonTranslationInput input,
        DateTime now) =>
        new()
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            LessonId = lessonId,
            CultureCode = culture,
            Title = Clean(input.Title),
            Explanation = Clean(input.Explanation),
            KeyConceptsAndRules = Clean(input.KeyConceptsAndRules),
            WorkedExamples = Clean(input.WorkedExamples),
            StepByStepSolutions = Clean(input.StepByStepSolutions),
            CommonMistakes = Clean(input.CommonMistakes),
            QuickSummary = Clean(input.QuickSummary),
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

    private static void UpsertTrackedTranslation(
        IReadOnlyList<LearningLessonTranslation> existing,
        Guid schoolId,
        Guid lessonId,
        string culture,
        LessonTranslationInput input,
        DateTime now,
        out LearningLessonTranslation? created)
    {
        var current = existing.SingleOrDefault(x =>
            string.Equals(x.CultureCode, culture, StringComparison.OrdinalIgnoreCase));
        if (current is null)
        {
            created = NewTranslation(schoolId, lessonId, culture, input, now);
            return;
        }

        current.Title = Clean(input.Title);
        current.Explanation = Clean(input.Explanation);
        current.KeyConceptsAndRules = Clean(input.KeyConceptsAndRules);
        current.WorkedExamples = Clean(input.WorkedExamples);
        current.StepByStepSolutions = Clean(input.StepByStepSolutions);
        current.CommonMistakes = Clean(input.CommonMistakes);
        current.QuickSummary = Clean(input.QuickSummary);
        current.UpdatedAtUtc = now;
        created = null;
    }

    private static LessonTranslationInput EmptyTranslation() =>
        new("", "", "", "", "", "", "");

    private static LessonTranslationInput ToInput(LessonTranslationRecord? record) =>
        record is null
            ? EmptyTranslation()
            : new LessonTranslationInput(
                record.Title,
                record.Explanation,
                record.KeyConceptsAndRules,
                record.WorkedExamples,
                record.StepByStepSolutions,
                record.CommonMistakes,
                record.QuickSummary);

    private static LessonTranslationInput? ToNullableInput(LessonTranslationRecord? record) =>
        record is null ? null : ToInput(record);

    private static LessonTranslationRecord? SelectTranslation(
        IReadOnlyList<LessonTranslationRecord> translations,
        string cultureCode)
    {
        var normalized = NormalizeCulture(cultureCode);
        return translations.FirstOrDefault(x =>
            string.Equals(x.CultureCode, normalized, StringComparison.OrdinalIgnoreCase));
    }

    private static string NormalizeCulture(string cultureCode) =>
        string.IsNullOrWhiteSpace(cultureCode)
            ? "en"
            : cultureCode.Trim().Split('-', '_')[0].ToLowerInvariant();

    private static string Clean(string value) => value?.Trim() ?? string.Empty;

    private sealed record ScopeResult(
        bool Succeeded,
        SchoolUserRecord? Actor,
        School? School,
        LessonContentErrorCode? Error)
    {
        public static ScopeResult Success(SchoolUserRecord actor, School school) =>
            new(true, actor, school, null);

        public static ScopeResult Fail(LessonContentErrorCode error) =>
            new(false, null, null, error);
    }
}
