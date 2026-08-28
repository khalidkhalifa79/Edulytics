using Edulytics.Core.Enums;
using Edulytics.Core.Interfaces;
using Edulytics.Core.Lessons;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class LessonContentRepository : ILessonContentRepository
{
    private readonly EdulyticsDbContext _db;

    public LessonContentRepository(EdulyticsDbContext db) => _db = db;

    public async Task<IReadOnlyList<CanonicalCurriculumContextRecord>> ListStaffAdoptionsAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var adoptions = await _db.SchoolCurriculumAdoptions
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId &&
                x.IsActive &&
                x.IsPrimary)
            .ToArrayAsync(cancellationToken);

        return await HydrateContextsAsync(
            schoolId,
            adoptions,
            cancellationToken);
    }

    public async Task<IReadOnlyList<CanonicalCurriculumContextRecord>> ListStudentAdoptionsAsync(
        Guid actorUserId,
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var profile = await _db.StudentProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.UserId == actorUserId &&
                    !x.IsArchived &&
                    x.Status == AcademicStructureStatus.Active,
                cancellationToken);

        if (profile is null)
            return [];

        var enrollments = await _db.StudentEnrollments
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId &&
                x.StudentProfileId == profile.Id)
            .ToArrayAsync(cancellationToken);

        if (enrollments.Length == 0)
            return [];

        var classIds = enrollments
            .Select(x => x.ClassGroupId)
            .Distinct()
            .ToArray();

        var yearIds = enrollments
            .Select(x => x.AcademicYearId)
            .Distinct()
            .ToArray();

        var classes = await _db.ClassGroups
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId &&
                classIds.Contains(x.Id))
            .ToArrayAsync(cancellationToken);

        var classById = classes.ToDictionary(x => x.Id);
        var scopes = enrollments.Where(x => classById.ContainsKey(x.ClassGroupId)).Select(x => new { classById[x.ClassGroupId].AcademicProgramId, classById[x.ClassGroupId].GradeLevelId, x.AcademicYearId }).Distinct().ToArray();
        if (scopes.Length == 0) return [];
        var programIds = scopes.Select(x => x.AcademicProgramId).Distinct().ToArray();
        var gradeIds = scopes.Select(x => x.GradeLevelId).Distinct().ToArray();
        var candidates = await _db.SchoolCurriculumAdoptions.AsNoTracking().Where(x => x.SchoolId == schoolId && x.IsActive && x.IsPrimary && programIds.Contains(x.AcademicProgramId) && gradeIds.Contains(x.GradeLevelId) && (!x.AcademicYearId.HasValue || yearIds.Contains(x.AcademicYearId.Value))).ToArrayAsync(cancellationToken);
        var adoptions = candidates.Where(a => scopes.Any(q => a.AcademicProgramId == q.AcademicProgramId && a.GradeLevelId == q.GradeLevelId && (!a.AcademicYearId.HasValue || a.AcademicYearId.Value == q.AcademicYearId))).ToArray();

        return await HydrateContextsAsync(
            schoolId,
            adoptions,
            cancellationToken);
    }

    public async Task<IReadOnlyList<PedagogicalLessonRecord>> ListPedagogicalLessonsAsync(
        IReadOnlyCollection<Guid> frameworkVersionIds,
        CancellationToken cancellationToken = default)
    {
        if (frameworkVersionIds.Count == 0)
            return [];

        var versionIds = frameworkVersionIds
            .Distinct()
            .ToArray();

        var lessons = await _db.CurriculumPedagogicalLessons
            .AsNoTracking()
            .Where(x => versionIds.Contains(x.FrameworkVersionId))
            .OrderBy(x => x.FrameworkVersionId)
            .ThenBy(x => x.LogicalLevelFrom)
            .ThenBy(x => x.Pathway)
            .ThenBy(x => x.SortOrder)
            .ThenBy(x => x.Code)
            .ToArrayAsync(cancellationToken);

        if (lessons.Length == 0)
            return [];

        var lessonIds = lessons
            .Select(x => x.Id)
            .ToArray();

        var outcomeCounts = await _db.CurriculumPedagogicalLessonOutcomes
            .AsNoTracking()
            .Where(x => lessonIds.Contains(x.PedagogicalLessonId))
            .GroupBy(x => x.PedagogicalLessonId)
            .Select(x => new
            {
                LessonId = x.Key,
                Count = x.Count()
            })
            .ToArrayAsync(cancellationToken);

        var countByLesson = outcomeCounts
            .ToDictionary(x => x.LessonId, x => x.Count);

        return lessons
            .Select(x => new PedagogicalLessonRecord(
                x.Id,
                x.FrameworkVersionId,
                x.OfficialLessonNodeId,
                x.Code,
                x.UnitKey,
                x.UnitTitle,
                x.Title,
                x.Pathway,
                x.LogicalLevelFrom,
                x.LogicalLevelTo,
                x.SortOrder,
                countByLesson.GetValueOrDefault(x.Id)))
            .ToArray();
    }

    public async Task<IReadOnlyList<CanonicalLessonContentRecord>> ListCanonicalContentsAsync(
        IReadOnlyCollection<Guid> pedagogicalLessonIds,
        CancellationToken cancellationToken = default)
    {
        if (pedagogicalLessonIds.Count == 0)
            return [];

        var lessonIds = pedagogicalLessonIds
            .Distinct()
            .ToArray();

        var contents = await _db.CurriculumLessonContents
            .AsNoTracking()
            .Where(x => lessonIds.Contains(x.PedagogicalLessonId))
            .ToArrayAsync(cancellationToken);

        if (contents.Length == 0)
            return [];

        var contentIds = contents
            .Select(x => x.Id)
            .ToArray();

        var translations = await _db.CurriculumLessonContentTranslations
            .AsNoTracking()
            .Where(x => contentIds.Contains(x.CurriculumLessonContentId))
            .OrderBy(x => x.CultureCode)
            .ToArrayAsync(cancellationToken);

        return contents
            .Select(c => new CanonicalLessonContentRecord(
                c.Id,
                c.FrameworkVersionId,
                c.PedagogicalLessonId,
                c.Status,
                c.ContentVersion,
                c.VerifiedAtUtc,
                c.PublishedAtUtc,
                c.UpdatedAtUtc,
                translations
                    .Where(t => t.CurriculumLessonContentId == c.Id)
                    .Select(t => new CanonicalLessonTranslationRecord(
                        t.CultureCode,
                        t.Title,
                        t.Explanation,
                        t.KeyConceptsAndRules,
                        t.WorkedExamples,
                        t.StepByStepSolutions,
                        t.CommonMistakes,
                        t.QuickSummary))
                    .ToArray()))
            .ToArray();
    }

    public async Task<IReadOnlyList<LessonOutcomeRecord>> ListOfficialOutcomesAsync(
        Guid frameworkVersionId,
        Guid pedagogicalLessonId,
        CancellationToken cancellationToken = default)
    {
        var mappings = await _db.CurriculumPedagogicalLessonOutcomes
            .AsNoTracking()
            .Where(x =>
                x.FrameworkVersionId == frameworkVersionId &&
                x.PedagogicalLessonId == pedagogicalLessonId)
            .OrderBy(x => x.SortOrder)
            .ToArrayAsync(cancellationToken);

        if (mappings.Length == 0)
            return [];

        var outcomeIds = mappings
            .Select(x => x.OutcomeNodeId)
            .Distinct()
            .ToArray();

        var nodes = await _db.CurriculumPackContentNodes
            .AsNoTracking()
            .Where(x =>
                x.FrameworkVersionId == frameworkVersionId &&
                x.IsActive &&
                x.IsOfficial &&
                (x.NodeKind == "Standard" || x.NodeKind == "Outcome") &&
                outcomeIds.Contains(x.Id))
            .ToArrayAsync(cancellationToken);

        var byId = nodes.ToDictionary(x => x.Id);

        return mappings
            .Select((mapping, index) =>
            {
                if (!byId.TryGetValue(mapping.OutcomeNodeId, out var node))
                    return null;

                return new LessonOutcomeRecord(
                    node.Id,
                    node.Code,
                    node.OfficialText ??
                    node.AuthorDescription ??
                    node.Title,
                    mapping.SortOrder != 0
                        ? mapping.SortOrder
                        : index + 1);
            })
            .Where(x => x is not null)
            .Select(x => x!)
            .GroupBy(x => x.Id)
            .Select(x => x.First())
            .OrderBy(x => x.Order)
            .ToArray();
    }

    private async Task<IReadOnlyList<CanonicalCurriculumContextRecord>> HydrateContextsAsync(
        Guid schoolId,
        IReadOnlyCollection<Edulytics.Core.Entities.SchoolCurriculumAdoption> adoptions,
        CancellationToken cancellationToken)
    {
        if (adoptions.Count == 0)
            return [];

        var subjectIds = adoptions
            .Select(x => x.SubjectId)
            .Distinct()
            .ToArray();

        var gradeIds = adoptions
            .Select(x => x.GradeLevelId)
            .Distinct()
            .ToArray();

        var versionIds = adoptions
            .Select(x => x.FrameworkVersionId)
            .Distinct()
            .ToArray();

        var programIds = adoptions.Select(x => x.AcademicProgramId).Distinct().ToArray();
        var programs = await _db.AcademicPrograms.AsNoTracking().Where(x => x.SchoolId == schoolId && programIds.Contains(x.Id)).ToArrayAsync(cancellationToken);

        var subjects = await _db.Subjects
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId &&
                subjectIds.Contains(x.Id))
            .ToArrayAsync(cancellationToken);

        var grades = await _db.GradeLevels
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId &&
                gradeIds.Contains(x.Id))
            .ToArrayAsync(cancellationToken);

        var versions = await _db.CurriculumFrameworkVersions
            .AsNoTracking()
            .Where(x =>
                versionIds.Contains(x.Id) &&
                x.IsActive)
            .ToArrayAsync(cancellationToken);

        var frameworkIds = versions
            .Select(x => x.FrameworkId)
            .Distinct()
            .ToArray();

        var frameworks = await _db.CurriculumFrameworks
            .AsNoTracking()
            .Where(x =>
                frameworkIds.Contains(x.Id) &&
                x.IsActive)
            .ToArrayAsync(cancellationToken);

        var programsById = programs.ToDictionary(x => x.Id);
        var subjectsById = subjects.ToDictionary(x => x.Id);
        var gradesById = grades.ToDictionary(x => x.Id);
        var versionsById = versions.ToDictionary(x => x.Id);
        var frameworksById = frameworks.ToDictionary(x => x.Id);

        var result = new List<CanonicalCurriculumContextRecord>();

        foreach (var adoption in adoptions)
        {
            if (!programsById.TryGetValue(adoption.AcademicProgramId, out var program) ||
                !subjectsById.TryGetValue(adoption.SubjectId, out var subject) ||
                !gradesById.TryGetValue(adoption.GradeLevelId, out var grade) ||
                !versionsById.TryGetValue(adoption.FrameworkVersionId, out var version) ||
                !frameworksById.TryGetValue(version.FrameworkId, out var framework))
            {
                continue;
            }

            result.Add(new CanonicalCurriculumContextRecord(
                version.Id,
                framework.Code,
                framework.Name,
                version.Name,
                subject.Id,
                subject.Name,
                subject.Code,
                grade.Id,
                grade.Name,
                grade.Order) { AcademicProgramId = program.Id, AcademicProgramName = program.Name, AcademicProgramCode = program.Code });
        }

        return result
            .Distinct()
            .OrderBy(x => x.SubjectCode)
            .ThenBy(x => x.GradeOrder)
            .ThenBy(x => x.FrameworkName)
            .ToArray();
    }
}
