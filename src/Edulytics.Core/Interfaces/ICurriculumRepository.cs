using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;

namespace Edulytics.Core.Interfaces;

public interface ICurriculumRepository
{
    Task<CurriculumSnapshot> GetSnapshotAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<AcademicProgram?> GetAcademicProgramAsync(Guid schoolId, Guid id, CancellationToken cancellationToken = default) => Task.FromResult<AcademicProgram?>(null);
    Task<AcademicProgram?> GetDefaultAcademicProgramAsync(Guid schoolId, CancellationToken cancellationToken = default) => Task.FromResult<AcademicProgram?>(null);

    Task<GradeLevel?> GetGradeLevelAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<Subject?> GetSubjectAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<CurriculumTopic?> GetTopicAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<LearningOutcome?> GetOutcomeAsync(
        Guid schoolId,
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SchoolCurriculumAdoption?> GetPrimaryDefaultAdoptionAsync(
        Guid schoolId,
        Guid gradeLevelId,
        Guid subjectId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SchoolCurriculumAdoption?>(null);

    Task<SchoolCurriculumAdoption?> GetPrimaryAdoptionAsync(Guid schoolId, Guid academicProgramId, Guid gradeLevelId, Guid subjectId, CancellationToken cancellationToken = default) =>
        GetPrimaryDefaultAdoptionAsync(schoolId,gradeLevelId,subjectId,cancellationToken);

    Task<Guid?> GetActivePlatformFrameworkVersionIdAsync(
        string normalizedFrameworkCode,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<Guid?>(null);

    Task<Guid?> GetPrimaryDefaultFrameworkVersionIdAsync(
        Guid schoolId,
        Guid gradeLevelId,
        Guid subjectId,
        CancellationToken cancellationToken = default);

    Task<Guid?> GetPrimaryFrameworkVersionIdAsync(Guid schoolId, Guid academicProgramId, Guid gradeLevelId, Guid subjectId, CancellationToken cancellationToken = default) =>
        GetPrimaryDefaultFrameworkVersionIdAsync(schoolId,gradeLevelId,subjectId,cancellationToken);

    Task<IReadOnlyList<AdoptedCurriculumContext>>
        GetAdoptedCurriculumContextsAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<AdoptedCurriculumContext>>([]);

    Task<IReadOnlyList<OfficialCurriculumOutcomeSource>>
        GetOfficialOutcomeSourcesAsync(
            Guid frameworkVersionId,
            int logicalLevel,
            CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<OfficialCurriculumOutcomeSource>>([]);

    Task<OfficialCurriculumOutcomeSource?>
        GetOfficialOutcomeSourceAsync(
            Guid frameworkVersionId,
            int logicalLevel,
            Guid contentNodeId,
            Guid? lessonNodeId,
            CancellationToken cancellationToken = default) =>
        Task.FromResult<OfficialCurriculumOutcomeSource?>(null);

    Task<Guid?> GetPlatformDefaultFrameworkVersionIdAsync(
        CancellationToken cancellationToken = default);

    Task<bool> TopicNameExistsAsync(
        Guid schoolId,
        Guid frameworkVersionId,
        Guid subjectId,
        Guid gradeLevelId,
        string normalizedName,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> TopicOrderExistsAsync(
        Guid schoolId,
        Guid frameworkVersionId,
        Guid subjectId,
        Guid gradeLevelId,
        int order,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> TopicNameExistsInProgramAsync(Guid schoolId, Guid academicProgramId, Guid frameworkVersionId, Guid subjectId, Guid gradeLevelId, string normalizedName, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        TopicNameExistsAsync(schoolId,frameworkVersionId,subjectId,gradeLevelId,normalizedName,excludeId,cancellationToken);
    Task<bool> TopicOrderExistsInProgramAsync(Guid schoolId, Guid academicProgramId, Guid frameworkVersionId, Guid subjectId, Guid gradeLevelId, int order, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        TopicOrderExistsAsync(schoolId,frameworkVersionId,subjectId,gradeLevelId,order,excludeId,cancellationToken);

    Task<bool> OutcomeCodeExistsAsync(
        Guid schoolId,
        Guid frameworkVersionId,
        Guid subjectId,
        Guid gradeLevelId,
        string normalizedCode,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> OutcomeCodeExistsInProgramAsync(Guid schoolId, Guid academicProgramId, Guid frameworkVersionId, Guid subjectId, Guid gradeLevelId, string normalizedCode, Guid? excludeId = null, CancellationToken cancellationToken = default) =>
        OutcomeCodeExistsAsync(schoolId,frameworkVersionId,subjectId,gradeLevelId,normalizedCode,excludeId,cancellationToken);

    Task<bool> OutcomeOrderExistsAsync(
        Guid schoolId,
        Guid topicId,
        int order,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default);

    Task AddDefaultAdoptionAsync(
        SchoolCurriculumAdoption adoption,
        CancellationToken cancellationToken = default);

    Task AddTopicAsync(
        CurriculumTopic topic,
        CancellationToken cancellationToken = default);

    Task AddOutcomeAsync(
        LearningOutcome outcome,
        CancellationToken cancellationToken = default);

    Task<CurriculumPersistenceResult> SaveAsync(
        CancellationToken cancellationToken = default);
}
