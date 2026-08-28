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

        var programs = await _db.AcademicPrograms.AsNoTracking().Where(x => x.SchoolId == schoolId).OrderBy(x => x.Name).ToListAsync(cancellationToken);
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

        return new CurriculumSnapshot(grades, subjects, topics, outcomes) { AcademicPrograms = programs };
    }

    public Task<AcademicProgram?> GetAcademicProgramAsync(Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.AcademicPrograms.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == id, cancellationToken);
    public Task<AcademicProgram?> GetDefaultAcademicProgramAsync(Guid schoolId, CancellationToken cancellationToken = default) =>
        _db.AcademicPrograms.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.IsDefault, cancellationToken);

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

    public Task<SchoolCurriculumAdoption?> GetPrimaryDefaultAdoptionAsync(
        Guid schoolId,
        Guid gradeLevelId,
        Guid subjectId,
        CancellationToken cancellationToken = default) =>
        _db.SchoolCurriculumAdoptions
            .FirstOrDefaultAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.AcademicYearId == null &&
                    x.GradeLevelId == gradeLevelId &&
                    x.SubjectId == subjectId &&
                    x.IsPrimary &&
                    x.IsActive,
                cancellationToken);

    public Task<SchoolCurriculumAdoption?> GetPrimaryAdoptionAsync(Guid schoolId, Guid academicProgramId, Guid gradeLevelId, Guid subjectId, CancellationToken cancellationToken = default) =>
        _db.SchoolCurriculumAdoptions.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.AcademicYearId == null && x.AcademicProgramId == academicProgramId && x.GradeLevelId == gradeLevelId && x.SubjectId == subjectId && x.IsPrimary && x.IsActive, cancellationToken);

    public Task<Guid?> GetActivePlatformFrameworkVersionIdAsync(
        string normalizedFrameworkCode,
        CancellationToken cancellationToken = default) =>
        (
            from version in _db.CurriculumFrameworkVersions
            join framework in _db.CurriculumFrameworks
                on version.FrameworkId equals framework.Id
            where framework.OwnerSchoolId == null &&
                  framework.NormalizedCode == normalizedFrameworkCode &&
                  framework.IsActive &&
                  version.IsActive
            orderby version.CreatedAtUtc descending
            select (Guid?)version.Id
        ).FirstOrDefaultAsync(cancellationToken);

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

    public Task<Guid?> GetPrimaryFrameworkVersionIdAsync(Guid schoolId, Guid academicProgramId, Guid gradeLevelId, Guid subjectId, CancellationToken cancellationToken = default) =>
        _db.SchoolCurriculumAdoptions.Where(x => x.SchoolId == schoolId && x.AcademicYearId == null && x.AcademicProgramId == academicProgramId && x.GradeLevelId == gradeLevelId && x.SubjectId == subjectId && x.IsPrimary && x.IsActive).Select(x => (Guid?)x.FrameworkVersionId).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AdoptedCurriculumContext>>
        GetAdoptedCurriculumContextsAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
        await (
            from adoption in _db.SchoolCurriculumAdoptions.AsNoTracking()
            join program in _db.AcademicPrograms.AsNoTracking() on new { adoption.SchoolId, adoption.AcademicProgramId } equals new { program.SchoolId, AcademicProgramId = program.Id }
            join version in _db.CurriculumFrameworkVersions.AsNoTracking()
                on adoption.FrameworkVersionId equals version.Id
            join framework in _db.CurriculumFrameworks.AsNoTracking()
                on version.FrameworkId equals framework.Id
            where adoption.SchoolId == schoolId &&
                  adoption.AcademicYearId == null &&
                  adoption.IsPrimary &&
                  adoption.IsActive &&
                  version.IsActive &&
                  framework.IsActive
            select new AdoptedCurriculumContext(
                adoption.GradeLevelId,
                adoption.SubjectId,
                version.Id,
                framework.Code,
                framework.Name)
            { AcademicProgramId = adoption.AcademicProgramId, AcademicProgramName = program.Name, AcademicProgramCode = program.Code }
        ).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OfficialCurriculumOutcomeSource>>
        GetOfficialOutcomeSourcesAsync(
            Guid frameworkVersionId,
            int logicalLevel,
            CancellationToken cancellationToken = default)
    {
        var nodes = await _db.CurriculumPackContentNodes
            .AsNoTracking()
            .Where(x =>
                x.FrameworkVersionId == frameworkVersionId &&
                x.IsOfficial &&
                x.IsActive &&
                (x.NodeKind == "Standard" ||
                 x.NodeKind == "Outcome") &&
                x.LogicalLevelFrom <= logicalLevel &&
                x.LogicalLevelTo >= logicalLevel)
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        if (nodes.Count == 0)
            return [];

        var nodeIds = nodes.Select(x => x.Id).ToArray();
        var parentIds = nodes
            .Where(x => x.ParentId.HasValue)
            .Select(x => x.ParentId!.Value)
            .Distinct()
            .ToArray();

        var parents = await _db.CurriculumPackContentNodes
            .AsNoTracking()
            .Where(x => parentIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var links = await _db.CurriculumPackNodeLinks
            .AsNoTracking()
            .Where(x =>
                x.FrameworkVersionId == frameworkVersionId &&
                x.LinkKind == "LessonStandardAlignment" &&
                nodeIds.Contains(x.ToNodeId))
            .OrderBy(x => x.SortOrder)
            .ToListAsync(cancellationToken);

        var lessonIds = links
            .Select(x => x.FromNodeId)
            .Distinct()
            .ToArray();
        var lessons = await _db.CurriculumPackContentNodes
            .AsNoTracking()
            .Where(x =>
                lessonIds.Contains(x.Id) &&
                x.IsActive &&
                x.LogicalLevelFrom <= logicalLevel &&
                x.LogicalLevelTo >= logicalLevel)
            .ToDictionaryAsync(x => x.Id, cancellationToken);
        var linksByOutcome = links
            .Where(x => lessons.ContainsKey(x.FromNodeId))
            .GroupBy(x => x.ToNodeId)
            .ToDictionary(x => x.Key, x => x.ToArray());

        var result = new List<OfficialCurriculumOutcomeSource>();
        foreach (var node in nodes)
        {
            var group = node.ParentId.HasValue &&
                        parents.TryGetValue(node.ParentId.Value, out var parent)
                ? parent.Title
                : node.NativeLevel;

            if (linksByOutcome.TryGetValue(node.Id, out var nodeLinks))
            {
                foreach (var link in nodeLinks)
                {
                    var lesson = lessons[link.FromNodeId];
                    result.Add(MapOfficialSource(node, lesson, group));
                }

                continue;
            }

            result.Add(MapOfficialSource(node, lesson: null, group));
        }

        return result
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.SelectionLabel)
            .ToArray();
    }

    public async Task<OfficialCurriculumOutcomeSource?>
        GetOfficialOutcomeSourceAsync(
            Guid frameworkVersionId,
            int logicalLevel,
            Guid contentNodeId,
            Guid? lessonNodeId,
            CancellationToken cancellationToken = default)
    {
        var sources = await GetOfficialOutcomeSourcesAsync(
            frameworkVersionId,
            logicalLevel,
            cancellationToken);

        return sources.SingleOrDefault(x =>
            x.ContentNodeId == contentNodeId &&
            x.LessonNodeId == lessonNodeId);
    }

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

    public Task<bool> TopicNameExistsInProgramAsync(Guid schoolId, Guid academicProgramId, Guid frameworkVersionId, Guid subjectId, Guid gradeLevelId, string normalizedName, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        _db.CurriculumTopics.AnyAsync(x => x.SchoolId == schoolId && x.AcademicProgramId == academicProgramId && x.FrameworkVersionId == frameworkVersionId && x.SubjectId == subjectId && x.GradeLevelId == gradeLevelId && x.Name.ToUpper() == normalizedName && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);
    public Task<bool> TopicOrderExistsInProgramAsync(Guid schoolId, Guid academicProgramId, Guid frameworkVersionId, Guid subjectId, Guid gradeLevelId, int order, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        _db.CurriculumTopics.AnyAsync(x => x.SchoolId == schoolId && x.AcademicProgramId == academicProgramId && x.FrameworkVersionId == frameworkVersionId && x.SubjectId == subjectId && x.GradeLevelId == gradeLevelId && x.Order == order && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

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

    public Task<bool> OutcomeCodeExistsInProgramAsync(Guid schoolId, Guid academicProgramId, Guid frameworkVersionId, Guid subjectId, Guid gradeLevelId, string normalizedCode, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        _db.LearningOutcomes.AnyAsync(x => x.SchoolId == schoolId && x.AcademicProgramId == academicProgramId && x.FrameworkVersionId == frameworkVersionId && x.SubjectId == subjectId && x.GradeLevelId == gradeLevelId && x.Code == normalizedCode && (!excludeId.HasValue || x.Id != excludeId.Value), cancellationToken);

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

    private static OfficialCurriculumOutcomeSource MapOfficialSource(
        CurriculumPackContentNode node,
        CurriculumPackContentNode? lesson,
        string group)
    {
        var code = node.Code
            .Replace("UAE:STD:", string.Empty, StringComparison.Ordinal)
            .Replace("UK:STD:", string.Empty, StringComparison.Ordinal)
            .Replace("CCSS:", string.Empty, StringComparison.Ordinal)
            .Replace("PL:REQ:", string.Empty, StringComparison.Ordinal);
        var rawDescription = lesson?.Title ??
            node.OfficialText ??
            node.AuthorDescription ??
            node.Title;
        var description = Compact(rawDescription, 1000);
        var selectionLabel = lesson?.Title ??
            Compact(node.OfficialText ?? node.Title, 180);

        return new OfficialCurriculumOutcomeSource(
            node.Id,
            lesson?.Id,
            code,
            description,
            selectionLabel,
            group,
            lesson?.SortOrder ?? node.SortOrder);
    }

    private static string Compact(string value, int maximumLength)
    {
        var compact = string.Join(
            " ",
            value.Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));

        if (compact.Length <= maximumLength)
            return compact;

        return compact[..(maximumLength - 1)].TrimEnd() + "…";
    }
}
