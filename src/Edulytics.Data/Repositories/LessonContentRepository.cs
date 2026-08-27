using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Lessons;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class LessonContentRepository : ILessonContentRepository
{
    private readonly EdulyticsDbContext _db;

    public LessonContentRepository(EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<LessonTopicRecord>> ListTopicContextsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var topics = await (
            from topic in _db.CurriculumTopics.AsNoTracking()
            join subject in _db.Subjects.AsNoTracking()
                on new { topic.SchoolId, Id = topic.SubjectId }
                equals new { subject.SchoolId, subject.Id }
            join grade in _db.GradeLevels.AsNoTracking()
                on new { topic.SchoolId, Id = topic.GradeLevelId }
                equals new { grade.SchoolId, grade.Id }
            join version in _db.CurriculumFrameworkVersions.AsNoTracking()
                on topic.FrameworkVersionId equals version.Id
            join framework in _db.CurriculumFrameworks.AsNoTracking()
                on version.FrameworkId equals framework.Id
            where topic.SchoolId == schoolId
            orderby subject.Name, grade.Order, topic.Order
            select new
            {
                topic.Id,
                topic.FrameworkVersionId,
                FrameworkName = framework.Name,
                FrameworkVersionName = version.Name,
                topic.SubjectId,
                SubjectName = subject.Name,
                SubjectCode = subject.Code,
                topic.GradeLevelId,
                GradeName = grade.Name,
                TopicName = topic.Name,
                TopicOrder = topic.Order
            })
            .ToListAsync(cancellationToken);

        var topicIds = topics.Select(x => x.Id).ToArray();
        var outcomes = await _db.LearningOutcomes.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && topicIds.Contains(x.TopicId))
            .OrderBy(x => x.TopicId)
            .ThenBy(x => x.Order)
            .Select(x => new
            {
                x.TopicId,
                Item = new LessonOutcomeRecord(
                    x.Id,
                    x.Code,
                    x.Description,
                    x.Order)
            })
            .ToListAsync(cancellationToken);

        var outcomeMap = outcomes
            .GroupBy(x => x.TopicId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<LessonOutcomeRecord>)x
                    .Select(y => y.Item)
                    .ToArray());

        return topics
            .Select(x => new LessonTopicRecord(
                x.Id,
                x.FrameworkVersionId,
                x.FrameworkName,
                x.FrameworkVersionName,
                x.SubjectId,
                x.SubjectName,
                x.SubjectCode,
                x.GradeLevelId,
                x.GradeName,
                x.TopicName,
                x.TopicOrder,
                outcomeMap.GetValueOrDefault(
                    x.Id,
                    Array.Empty<LessonOutcomeRecord>())))
            .ToArray();
    }

    public async Task<LessonTopicRecord?> GetTopicContextAsync(
        Guid schoolId,
        Guid topicId,
        CancellationToken cancellationToken = default) =>
        (await ListTopicContextsAsync(schoolId, cancellationToken))
            .SingleOrDefault(x => x.TopicId == topicId);

    public async Task<IReadOnlyList<LessonAggregateRecord>> ListLessonAggregatesAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var lessons = await _db.LearningLessons.AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.TopicId)
            .ThenBy(x => x.Order)
            .ToListAsync(cancellationToken);

        return await BuildAggregatesAsync(
            schoolId,
            lessons,
            cancellationToken);
    }

    public async Task<LessonAggregateRecord?> GetLessonAggregateAsync(
        Guid schoolId,
        Guid lessonId,
        CancellationToken cancellationToken = default)
    {
        var lesson = await _db.LearningLessons.AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.SchoolId == schoolId && x.Id == lessonId,
                cancellationToken);

        if (lesson is null)
            return null;

        return (await BuildAggregatesAsync(
            schoolId,
            [lesson],
            cancellationToken)).Single();
    }

    public Task<LearningLesson?> GetLessonForUpdateAsync(
        Guid schoolId,
        Guid lessonId,
        CancellationToken cancellationToken = default) =>
        _db.LearningLessons.SingleOrDefaultAsync(
            x => x.SchoolId == schoolId && x.Id == lessonId,
            cancellationToken);

    public async Task<IReadOnlyList<LearningLessonTranslation>> GetTranslationsForUpdateAsync(
        Guid schoolId,
        Guid lessonId,
        CancellationToken cancellationToken = default) =>
        await _db.LearningLessonTranslations
            .Where(x => x.SchoolId == schoolId && x.LessonId == lessonId)
            .OrderBy(x => x.CultureCode)
            .ToListAsync(cancellationToken);

    public Task<bool> LessonOrderExistsAsync(
        Guid schoolId,
        Guid topicId,
        int order,
        Guid? excludeLessonId = null,
        CancellationToken cancellationToken = default) =>
        _db.LearningLessons.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.TopicId == topicId &&
                 x.Order == order &&
                 (!excludeLessonId.HasValue || x.Id != excludeLessonId.Value),
            cancellationToken);

    public async Task AddLessonAsync(
        LearningLesson lesson,
        CancellationToken cancellationToken = default) =>
        await _db.LearningLessons.AddAsync(lesson, cancellationToken);

    public async Task AddTranslationAsync(
        LearningLessonTranslation translation,
        CancellationToken cancellationToken = default) =>
        await _db.LearningLessonTranslations.AddAsync(
            translation,
            cancellationToken);

    public async Task AddOutcomeLinkAsync(
        LearningLessonOutcome link,
        CancellationToken cancellationToken = default) =>
        await _db.LearningLessonOutcomes.AddAsync(link, cancellationToken);

    public async Task ReplaceOutcomeLinksAsync(
        Guid schoolId,
        Guid lessonId,
        IReadOnlyCollection<Guid> outcomeIds,
        CancellationToken cancellationToken = default)
    {
        var existing = await _db.LearningLessonOutcomes
            .Where(x => x.SchoolId == schoolId && x.LessonId == lessonId)
            .ToListAsync(cancellationToken);

        _db.LearningLessonOutcomes.RemoveRange(existing);

        foreach (var outcomeId in outcomeIds.Distinct())
        {
            await _db.LearningLessonOutcomes.AddAsync(
                new LearningLessonOutcome
                {
                    SchoolId = schoolId,
                    LessonId = lessonId,
                    LearningOutcomeId = outcomeId
                },
                cancellationToken);
        }
    }

    public async Task<IReadOnlyList<StudentPublishedLessonRecord>> ListPublishedForStudentAsync(
        Guid actorUserId,
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var profileId = await _db.StudentProfiles.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && x.UserId == actorUserId)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (!profileId.HasValue)
            return Array.Empty<StudentPublishedLessonRecord>();

        var accessible = await (
            from lesson in _db.LearningLessons.AsNoTracking()
            join topic in _db.CurriculumTopics.AsNoTracking()
                on new { lesson.SchoolId, Id = lesson.TopicId }
                equals new { topic.SchoolId, topic.Id }
            join subject in _db.Subjects.AsNoTracking()
                on new { topic.SchoolId, Id = topic.SubjectId }
                equals new { subject.SchoolId, subject.Id }
            join grade in _db.GradeLevels.AsNoTracking()
                on new { topic.SchoolId, Id = topic.GradeLevelId }
                equals new { grade.SchoolId, grade.Id }
            join version in _db.CurriculumFrameworkVersions.AsNoTracking()
                on topic.FrameworkVersionId equals version.Id
            join framework in _db.CurriculumFrameworks.AsNoTracking()
                on version.FrameworkId equals framework.Id
            where lesson.SchoolId == schoolId &&
                  lesson.Status == LearningLessonStatus.Published &&
                  lesson.PublishedAtUtc != null &&
                  _db.StudentEnrollments.Any(e =>
                      e.SchoolId == schoolId &&
                      e.StudentProfileId == profileId.Value &&
                      _db.ClassGroups.Any(c =>
                          c.SchoolId == schoolId &&
                          c.Id == e.ClassGroupId &&
                          c.GradeLevelId == topic.GradeLevelId)) &&
                  _db.SchoolCurriculumAdoptions.Any(a =>
                      a.SchoolId == schoolId &&
                      a.GradeLevelId == topic.GradeLevelId &&
                      a.SubjectId == topic.SubjectId &&
                      a.FrameworkVersionId == topic.FrameworkVersionId &&
                      a.IsPrimary && a.IsActive)
            orderby subject.Name, grade.Order, topic.Order, lesson.Order
            select new
            {
                lesson.Id,
                lesson.TopicId,
                TopicName = topic.Name,
                SubjectName = subject.Name,
                SubjectCode = subject.Code,
                GradeName = grade.Name,
                FrameworkName = framework.Name,
                lesson.Order,
                PublishedAtUtc = lesson.PublishedAtUtc!.Value
            })
            .ToListAsync(cancellationToken);

        if (accessible.Count == 0)
            return Array.Empty<StudentPublishedLessonRecord>();

        var lessonIds = accessible.Select(x => x.Id).ToArray();
        var translations = await LoadTranslationRecordsAsync(
            schoolId,
            lessonIds,
            cancellationToken);
        var outcomes = await LoadOutcomeRecordsAsync(
            schoolId,
            lessonIds,
            cancellationToken);

        return accessible.Select(x => new StudentPublishedLessonRecord(
            x.Id,
            x.TopicId,
            x.TopicName,
            x.SubjectName,
            x.SubjectCode,
            x.GradeName,
            x.FrameworkName,
            x.Order,
            x.PublishedAtUtc,
            outcomes.GetValueOrDefault(x.Id, Array.Empty<LessonOutcomeRecord>()),
            translations.GetValueOrDefault(x.Id, Array.Empty<LessonTranslationRecord>())))
            .ToArray();
    }

    public async Task<StudentPublishedLessonRecord?> GetPublishedForStudentAsync(
        Guid actorUserId,
        Guid schoolId,
        Guid lessonId,
        CancellationToken cancellationToken = default) =>
        (await ListPublishedForStudentAsync(
            actorUserId,
            schoolId,
            cancellationToken))
        .SingleOrDefault(x => x.Id == lessonId);

    public async Task<LessonContentWriteResult> SaveAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return LessonContentWriteResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return LessonContentWriteResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            return LessonContentWriteResult.ConstraintViolation;
        }
    }

    private async Task<IReadOnlyList<LessonAggregateRecord>> BuildAggregatesAsync(
        Guid schoolId,
        IReadOnlyList<LearningLesson> lessons,
        CancellationToken cancellationToken)
    {
        if (lessons.Count == 0)
            return Array.Empty<LessonAggregateRecord>();

        var ids = lessons.Select(x => x.Id).ToArray();
        var translations = await LoadTranslationRecordsAsync(
            schoolId,
            ids,
            cancellationToken);

        var outcomeIds = await _db.LearningLessonOutcomes.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && ids.Contains(x.LessonId))
            .OrderBy(x => x.LessonId)
            .ThenBy(x => x.LearningOutcomeId)
            .ToListAsync(cancellationToken);

        var outcomeMap = outcomeIds
            .GroupBy(x => x.LessonId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<Guid>)x.Select(y => y.LearningOutcomeId).ToArray());

        return lessons.Select(x => new LessonAggregateRecord(
            x.Id,
            x.TopicId,
            x.Order,
            x.Status,
            x.CreatedAtUtc,
            x.UpdatedAtUtc,
            x.SubmittedAtUtc,
            x.PublishedAtUtc,
            outcomeMap.GetValueOrDefault(x.Id, Array.Empty<Guid>()),
            translations.GetValueOrDefault(x.Id, Array.Empty<LessonTranslationRecord>())))
            .ToArray();
    }

    private async Task<Dictionary<Guid, IReadOnlyList<LessonTranslationRecord>>>
        LoadTranslationRecordsAsync(
            Guid schoolId,
            IReadOnlyCollection<Guid> lessonIds,
            CancellationToken cancellationToken)
    {
        var rows = await _db.LearningLessonTranslations.AsNoTracking()
            .Where(x => x.SchoolId == schoolId && lessonIds.Contains(x.LessonId))
            .OrderBy(x => x.LessonId)
            .ThenBy(x => x.CultureCode)
            .Select(x => new
            {
                x.LessonId,
                Item = new LessonTranslationRecord(
                    x.CultureCode,
                    x.Title,
                    x.Explanation,
                    x.KeyConceptsAndRules,
                    x.WorkedExamples,
                    x.StepByStepSolutions,
                    x.CommonMistakes,
                    x.QuickSummary)
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.LessonId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<LessonTranslationRecord>)x.Select(y => y.Item).ToArray());
    }

    private async Task<Dictionary<Guid, IReadOnlyList<LessonOutcomeRecord>>>
        LoadOutcomeRecordsAsync(
            Guid schoolId,
            IReadOnlyCollection<Guid> lessonIds,
            CancellationToken cancellationToken)
    {
        var rows = await (
            from link in _db.LearningLessonOutcomes.AsNoTracking()
            join outcome in _db.LearningOutcomes.AsNoTracking()
                on new { link.SchoolId, Id = link.LearningOutcomeId }
                equals new { outcome.SchoolId, outcome.Id }
            where link.SchoolId == schoolId && lessonIds.Contains(link.LessonId)
            orderby link.LessonId, outcome.Order
            select new
            {
                link.LessonId,
                Item = new LessonOutcomeRecord(
                    outcome.Id,
                    outcome.Code,
                    outcome.Description,
                    outcome.Order)
            })
            .ToListAsync(cancellationToken);

        return rows
            .GroupBy(x => x.LessonId)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<LessonOutcomeRecord>)x.Select(y => y.Item).ToArray());
    }
}
