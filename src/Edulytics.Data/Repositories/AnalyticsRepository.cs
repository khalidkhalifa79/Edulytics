using System.Data;
using Edulytics.Core.Analytics;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Edulytics.Data.Repositories;

public sealed class AnalyticsRepository : IAnalyticsRepository
{
    private readonly EdulyticsDbContext _db;

    public AnalyticsRepository(EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<AnalyticsSourceSnapshot> GetSourceSnapshotAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;

        if (_db.Database.IsRelational())
        {
            transaction =
                await _db.Database.BeginTransactionAsync(
                    IsolationLevel.RepeatableRead,
                    cancellationToken);
        }

        try
        {
            var snapshot = new AnalyticsSourceSnapshot(
                await _db.AcademicYears
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken),

                await _db.ClassGroups
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken),

                await _db.Subjects
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken),

                await _db.StudentProfiles
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken),

                await _db.StudentEnrollments
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken),

                await _db.TeacherAssignments
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken),

                await _db.CurriculumTopics
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken),

                await _db.LearningOutcomes
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken),

                await _db.Assessments
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken),

                await _db.AssessmentQuestions
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken),

                await _db.QuestionLearningOutcomes
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken),

                await _db.AssessmentResults
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken),

                await _db.StudentAnswers
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken));

            if (transaction is not null)
            {
                await transaction.CommitAsync(
                    cancellationToken);
            }

            return snapshot;
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }

    public async Task<AnalyticsProjectionSnapshot>
        GetProjectionSnapshotAsync(
            Guid schoolId,
            CancellationToken cancellationToken = default) =>
        new(
            await _db.AcademicYears
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .ToListAsync(cancellationToken),

            await _db.ClassGroups
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .ToListAsync(cancellationToken),

            await _db.Subjects
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .ToListAsync(cancellationToken),

            await _db.StudentProfiles
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .ToListAsync(cancellationToken),

            await _db.TeacherAssignments
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .ToListAsync(cancellationToken),

            await _db.CurriculumTopics
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .ToListAsync(cancellationToken),

            await _db.LearningOutcomes
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .ToListAsync(cancellationToken),

            await _db.StudentOutcomeMasteries
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .ToListAsync(cancellationToken),

            await _db.ClassOutcomeSummaries
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .ToListAsync(cancellationToken),

            await _db.ClassTopicSummaries
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .ToListAsync(cancellationToken),

            await _db.ClassAssessmentTrends
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .ToListAsync(cancellationToken),

            await _db.SchoolAnalyticsSnapshots
                .AsNoTracking()
                .Where(x => x.SchoolId == schoolId)
                .ToListAsync(cancellationToken));

    public async Task<DateTime?> GetLatestSourceUpdateAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default)
    {
        var resultMax = await _db.AssessmentResults
            .Where(x => x.SchoolId == schoolId)
            .Select(x => (DateTime?)x.UpdatedAtUtc)
            .MaxAsync(cancellationToken);

        var answerMax = await _db.StudentAnswers
            .Where(x => x.SchoolId == schoolId)
            .Select(x => (DateTime?)x.UpdatedAtUtc)
            .MaxAsync(cancellationToken);

        if (!resultMax.HasValue)
            return answerMax;

        if (!answerMax.HasValue)
            return resultMax;

        return resultMax.Value >= answerMax.Value
            ? resultMax
            : answerMax;
    }

    public async Task<AnalyticsPersistenceResult>
        ReplaceProjectionsAsync(
            Guid schoolId,
            AnalyticsProjectionSet projections,
            CancellationToken cancellationToken = default)
    {
        IDbContextTransaction? transaction = null;

        try
        {
            if (_db.Database.IsRelational())
            {
                transaction =
                    await _db.Database.BeginTransactionAsync(
                        IsolationLevel.Serializable,
                        cancellationToken);
            }

            var oldStudent =
                await _db.StudentOutcomeMasteries
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken);

            var oldOutcome =
                await _db.ClassOutcomeSummaries
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken);

            var oldTopic =
                await _db.ClassTopicSummaries
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken);

            var oldTrend =
                await _db.ClassAssessmentTrends
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken);

            var oldSchool =
                await _db.SchoolAnalyticsSnapshots
                    .Where(x => x.SchoolId == schoolId)
                    .ToListAsync(cancellationToken);

            _db.RemoveRange(oldStudent);
            _db.RemoveRange(oldOutcome);
            _db.RemoveRange(oldTopic);
            _db.RemoveRange(oldTrend);
            _db.RemoveRange(oldSchool);

            await _db.SaveChangesAsync(cancellationToken);

            await _db.StudentOutcomeMasteries.AddRangeAsync(
                projections.StudentOutcomeMasteries,
                cancellationToken);

            await _db.ClassOutcomeSummaries.AddRangeAsync(
                projections.ClassOutcomeSummaries,
                cancellationToken);

            await _db.ClassTopicSummaries.AddRangeAsync(
                projections.ClassTopicSummaries,
                cancellationToken);

            await _db.ClassAssessmentTrends.AddRangeAsync(
                projections.ClassAssessmentTrends,
                cancellationToken);

            await _db.SchoolAnalyticsSnapshots.AddRangeAsync(
                projections.SchoolSnapshots,
                cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);

            if (transaction is not null)
            {
                await transaction.CommitAsync(
                    cancellationToken);
            }

            _db.ChangeTracker.Clear();

            return AnalyticsPersistenceResult.Success();
        }
        catch (DbUpdateException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);
            }

            _db.ChangeTracker.Clear();

            return AnalyticsPersistenceResult.Failure(
                AnalyticsPersistenceError.Constraint);
        }
        catch
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);
            }

            _db.ChangeTracker.Clear();

            return AnalyticsPersistenceResult.Failure(
                AnalyticsPersistenceError.Unknown);
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }
    }
}
