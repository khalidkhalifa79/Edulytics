using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class CurriculumRepository : ICurriculumRepository
{
    private readonly EdulyticsDbContext _db;

    public CurriculumRepository(EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<CurriculumSnapshot> GetSnapshotAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var grades = await _db.GradeLevels
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Order)
            .ToListAsync(cancellationToken);

        var subjects = await _db.Subjects
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var topics = await _db.CurriculumTopics
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.SubjectId)
            .ThenBy(x => x.GradeLevelId)
            .ThenBy(x => x.Order)
            .ToListAsync(cancellationToken);

        var outcomes = await _db.LearningOutcomes
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderBy(x => x.TopicId)
            .ThenBy(x => x.Order)
            .ToListAsync(cancellationToken);

        return new CurriculumSnapshot(
            grades,
            subjects,
            topics,
            outcomes);
    }

    public Task<GradeLevel?> GetGradeLevelAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        _db.GradeLevels.FirstOrDefaultAsync(
            x => x.SchoolId == schoolId && x.Id == id,
            cancellationToken);

    public Task<Subject?> GetSubjectAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        _db.Subjects.FirstOrDefaultAsync(
            x => x.SchoolId == schoolId && x.Id == id,
            cancellationToken);

    public Task<CurriculumTopic?> GetTopicAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        _db.CurriculumTopics.FirstOrDefaultAsync(
            x => x.SchoolId == schoolId && x.Id == id,
            cancellationToken);

    public Task<LearningOutcome?> GetOutcomeAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default) =>
        _db.LearningOutcomes.FirstOrDefaultAsync(
            x => x.SchoolId == schoolId && x.Id == id,
            cancellationToken);

    public Task<Guid?> GetPrimaryDefaultFrameworkVersionIdAsync(
        Guid schoolId,
        Guid gradeLevelId,
        Guid subjectId,
        CancellationToken cancellationToken = default) =>
        _db.SchoolCurriculumAdoptions
            .Where(x =>
                x.SchoolId == schoolId &&
                x.AcademicYearId == null &&
                x.GradeLevelId == gradeLevelId &&
                x.SubjectId == subjectId &&
                x.IsPrimary &&
                x.IsActive)
            .Select(x => (Guid?)x.FrameworkVersionId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<Guid?> GetPlatformDefaultFrameworkVersionIdAsync(
        CancellationToken cancellationToken = default)
    {
        return await (
            from version in _db.CurriculumFrameworkVersions
            join framework in _db.CurriculumFrameworks
                on version.FrameworkId equals framework.Id
            where framework.OwnerSchoolId == null &&
                  framework.NormalizedCode == "EDULYTICS-DEFAULT" &&
                  framework.IsActive &&
                  version.NormalizedVersionCode == "V1" &&
                  version.IsActive
            select (Guid?)version.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> TopicNameExistsAsync(
        Guid schoolId,
        Guid frameworkVersionId,
        Guid subjectId,
        Guid gradeLevelId,
        string normalizedName,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        _db.CurriculumTopics.AnyAsync(
            x =>
                x.SchoolId == schoolId &&
                x.FrameworkVersionId == frameworkVersionId &&
                x.SubjectId == subjectId &&
                x.GradeLevelId == gradeLevelId &&
                x.Name.ToUpper() == normalizedName &&
                (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task<bool> TopicOrderExistsAsync(
        Guid schoolId,
        Guid frameworkVersionId,
        Guid subjectId,
        Guid gradeLevelId,
        int order,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        _db.CurriculumTopics.AnyAsync(
            x =>
                x.SchoolId == schoolId &&
                x.FrameworkVersionId == frameworkVersionId &&
                x.SubjectId == subjectId &&
                x.GradeLevelId == gradeLevelId &&
                x.Order == order &&
                (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task<bool> OutcomeCodeExistsAsync(
        Guid schoolId,
        Guid frameworkVersionId,
        Guid subjectId,
        Guid gradeLevelId,
        string normalizedCode,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        _db.LearningOutcomes.AnyAsync(
            x =>
                x.SchoolId == schoolId &&
                x.FrameworkVersionId == frameworkVersionId &&
                x.SubjectId == subjectId &&
                x.GradeLevelId == gradeLevelId &&
                x.Code == normalizedCode &&
                (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task<bool> OutcomeOrderExistsAsync(
        Guid schoolId,
        Guid topicId,
        int order,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        _db.LearningOutcomes.AnyAsync(
            x =>
                x.SchoolId == schoolId &&
                x.TopicId == topicId &&
                x.Order == order &&
                (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task AddDefaultAdoptionAsync(
        SchoolCurriculumAdoption adoption,
        CancellationToken cancellationToken = default)
    {
        _db.SchoolCurriculumAdoptions.Add(adoption);
        return Task.CompletedTask;
    }

    public Task AddTopicAsync(
        CurriculumTopic topic,
        CancellationToken cancellationToken = default)
    {
        _db.CurriculumTopics.Add(topic);
        return Task.CompletedTask;
    }

    public Task AddOutcomeAsync(
        LearningOutcome outcome,
        CancellationToken cancellationToken = default)
    {
        _db.LearningOutcomes.Add(outcome);
        return Task.CompletedTask;
    }

    public async Task<CurriculumPersistenceResult> SaveAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return CurriculumPersistenceResult.Success();
        }
        catch (DbUpdateException)
        {
            return CurriculumPersistenceResult.Failure(
                CurriculumPersistenceError.Constraint);
        }
    }
}
