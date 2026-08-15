using Edulytics.Core.Assessments;
using Edulytics.Core.Entities;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class AssessmentRepository : IAssessmentRepository
{
    private readonly EdulyticsDbContext _db;

    public AssessmentRepository(EdulyticsDbContext db) => _db = db;

    public async Task<AssessmentSnapshot> GetSnapshotAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        new(
            await _db.AcademicYears.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.Terms.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.GradeLevels.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.ClassGroups.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.Subjects.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.StudentProfiles.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.TeacherAssignments.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.StudentEnrollments.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.CurriculumTopics.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.LearningOutcomes.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.SchoolCurriculumAdoptions.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.CurriculumFrameworkVersions.AsNoTracking().ToListAsync(cancellationToken),
            await _db.Assessments.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.AssessmentQuestions.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.QuestionLearningOutcomes.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.AssessmentResults.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken),
            await _db.StudentAnswers.AsNoTracking().Where(x => x.SchoolId == schoolId).ToListAsync(cancellationToken));

    public Task<Assessment?> GetAssessmentAsync(Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.Assessments.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<AssessmentQuestion?> GetQuestionAsync(Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.AssessmentQuestions.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<QuestionLearningOutcome?> GetMappingAsync(
        Guid schoolId, Guid questionId, Guid outcomeId,
        CancellationToken cancellationToken = default) =>
        _db.QuestionLearningOutcomes.FirstOrDefaultAsync(
            x => x.SchoolId == schoolId &&
                 x.AssessmentQuestionId == questionId &&
                 x.LearningOutcomeId == outcomeId,
            cancellationToken);

    public Task<AssessmentResult?> GetResultAsync(
        Guid schoolId, Guid assessmentId, Guid studentProfileId,
        CancellationToken cancellationToken = default) =>
        _db.AssessmentResults.FirstOrDefaultAsync(
            x => x.SchoolId == schoolId &&
                 x.AssessmentId == assessmentId &&
                 x.StudentProfileId == studentProfileId,
            cancellationToken);

    public Task<StudentAnswer?> GetAnswerAsync(
        Guid schoolId, Guid resultId, Guid questionId,
        CancellationToken cancellationToken = default) =>
        _db.StudentAnswers.FirstOrDefaultAsync(
            x => x.SchoolId == schoolId &&
                 x.AssessmentResultId == resultId &&
                 x.AssessmentQuestionId == questionId,
            cancellationToken);

    public Task<Term?> GetTermAsync(Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.Terms.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<ClassGroup?> GetClassGroupAsync(Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.ClassGroups.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<Subject?> GetSubjectAsync(Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.Subjects.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<StudentProfile?> GetStudentProfileAsync(Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.StudentProfiles.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<LearningOutcome?> GetLearningOutcomeAsync(Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.LearningOutcomes.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<CurriculumTopic?> GetCurriculumTopicAsync(Guid schoolId, Guid id, CancellationToken cancellationToken = default) =>
        _db.CurriculumTopics.FirstOrDefaultAsync(x => x.SchoolId == schoolId && x.Id == id, cancellationToken);

    public Task<bool> IsTeacherAssignedAsync(
        Guid schoolId, Guid teacherUserId, Guid classGroupId, Guid subjectId,
        CancellationToken cancellationToken = default) =>
        _db.TeacherAssignments.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.TeacherUserId == teacherUserId &&
                 x.ClassGroupId == classGroupId &&
                 x.SubjectId == subjectId,
            cancellationToken);

    public Task<bool> IsStudentEnrolledAsync(
        Guid schoolId, Guid academicYearId, Guid classGroupId, Guid studentProfileId,
        CancellationToken cancellationToken = default) =>
        _db.StudentEnrollments.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.AcademicYearId == academicYearId &&
                 x.ClassGroupId == classGroupId &&
                 x.StudentProfileId == studentProfileId,
            cancellationToken);

    public Task<bool> AssessmentTitleExistsAsync(
        Guid schoolId, Guid classGroupId, Guid termId, string normalizedTitle,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        _db.Assessments.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.ClassGroupId == classGroupId &&
                 x.TermId == termId &&
                 x.Title.ToUpper() == normalizedTitle &&
                 (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task<bool> QuestionOrderExistsAsync(
        Guid schoolId, Guid assessmentId, int order, Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        _db.AssessmentQuestions.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.AssessmentId == assessmentId &&
                 x.Order == order &&
                 (!excludeId.HasValue || x.Id != excludeId.Value),
            cancellationToken);

    public Task<bool> MappingExistsAsync(
        Guid schoolId, Guid questionId, Guid outcomeId,
        CancellationToken cancellationToken = default) =>
        _db.QuestionLearningOutcomes.AnyAsync(
            x => x.SchoolId == schoolId &&
                 x.AssessmentQuestionId == questionId &&
                 x.LearningOutcomeId == outcomeId,
            cancellationToken);

    public async Task AddAsync<T>(T entity, CancellationToken cancellationToken = default)
        where T : class, ISchoolScoped =>
        await _db.Set<T>().AddAsync(entity, cancellationToken);

    public async Task AddOutboxAsync(
        OutboxMessage message,
        CancellationToken cancellationToken = default) =>
        await _db.OutboxMessages.AddAsync(
            message,
            cancellationToken);

    public void RemoveMapping(QuestionLearningOutcome mapping) =>
        _db.QuestionLearningOutcomes.Remove(mapping);

    public Task<AssessmentPersistenceResult> SaveAsync(
        CancellationToken cancellationToken = default) =>
        SaveInternalAsync(cancellationToken);

    public Task<AssessmentPersistenceResult> SaveWithRowVersionAsync<T>(
        T entity,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default)
        where T : class, ISchoolScoped
    {
        _db.Entry(entity).Property("RowVersion").OriginalValue = expectedRowVersion;
        return SaveInternalAsync(cancellationToken);
    }

    private async Task<AssessmentPersistenceResult> SaveInternalAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return AssessmentPersistenceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            return AssessmentPersistenceResult.Failure(AssessmentPersistenceError.Conflict);
        }
        catch (DbUpdateException)
        {
            return AssessmentPersistenceResult.Failure(AssessmentPersistenceError.Constraint);
        }
    }
}
