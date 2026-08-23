using System.Data;
using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Imports;
using Edulytics.Core.Interfaces;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Data.Repositories;

public sealed class ImportRepository : IImportRepository
{
    private readonly EdulyticsDbContext _db;

    public ImportRepository(EdulyticsDbContext db)
    {
        _db = db;
    }

    public async Task<ImportDataSnapshot> GetSnapshotAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        new()
        {
            AcademicYears =
                await _db.AcademicYears
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken),

            GradeLevels =
                await _db.GradeLevels
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken),

            ClassGroups =
                await _db.ClassGroups
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken),

            Subjects =
                await _db.Subjects
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken),

            StudentProfiles =
                await _db.StudentProfiles
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken),

            StudentEnrollments =
                await _db.StudentEnrollments
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken),

            TeacherAssignments =
                await _db.TeacherAssignments
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken),

            LearningOutcomes =
                await _db.LearningOutcomes
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken),

            CurriculumAdoptions =
                await _db.SchoolCurriculumAdoptions
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken),

            FrameworkVersions =
                await _db.CurriculumFrameworkVersions
                    .AsNoTracking()
                    .ToArrayAsync(cancellationToken),

            Assessments =
                await _db.Assessments
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken),

            AssessmentQuestions =
                await _db.AssessmentQuestions
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken),

            OutcomeMappings =
                await _db.QuestionLearningOutcomes
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken),

            AssessmentResults =
                await _db.AssessmentResults
                    .AsNoTracking()
                    .Where(x => x.SchoolId == schoolId)
                    .ToArrayAsync(cancellationToken)
        };

    public async Task<IReadOnlyList<ImportBatch>> ListAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default) =>
        await _db.ImportBatches
            .AsNoTracking()
            .Where(x => x.SchoolId == schoolId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(100)
            .ToArrayAsync(cancellationToken);

    public Task<ImportBatch?> GetAsync(
        Guid schoolId,
        Guid batchId,
        CancellationToken cancellationToken = default) =>
        _db.ImportBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.Id == batchId,
                cancellationToken);

    public async Task<IReadOnlyList<ImportValidationError>>
        GetErrorsAsync(
            Guid schoolId,
            Guid batchId,
            CancellationToken cancellationToken = default) =>
        await _db.ImportValidationErrors
            .AsNoTracking()
            .Where(x =>
                x.SchoolId == schoolId &&
                x.ImportBatchId == batchId)
            .OrderBy(x => x.RowNumber)
            .ThenBy(x => x.ColumnName)
            .ThenBy(x => x.Code)
            .ToArrayAsync(cancellationToken);

    public Task<ImportBatch?> FindIdempotentAsync(
        Guid schoolId,
        Guid actorUserId,
        ImportType importType,
        string fileHash,
        CancellationToken cancellationToken = default) =>
        _db.ImportBatches
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x =>
                    x.SchoolId == schoolId &&
                    x.UploadedByUserId == actorUserId &&
                    x.ImportType == importType &&
                    x.FileHash == fileHash,
                cancellationToken);

    public async Task<ImportPersistenceResult> AddBatchAsync(
        ImportBatch batch,
        IReadOnlyList<ImportValidationError> errors,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _db.ImportBatches.Add(batch);

            if (errors.Count > 0)
            {
                _db.ImportValidationErrors.AddRange(errors);
            }

            await _db.SaveChangesAsync(cancellationToken);

            return ImportPersistenceResult.Success();
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();

            return ImportPersistenceResult.Failure(
                ImportPersistenceError.Constraint);
        }
    }

    public async Task<ImportPersistenceResult>
        MarkValidationFailedAsync(
            Guid schoolId,
            Guid batchId,
            byte[] expectedRowVersion,
            int validRowCount,
            IReadOnlyList<ImportValidationError> errors,
            CancellationToken cancellationToken = default)
    {
        try
        {
            var batch =
                await _db.ImportBatches
                    .FirstOrDefaultAsync(
                        x =>
                            x.SchoolId == schoolId &&
                            x.Id == batchId,
                        cancellationToken);

            if (batch is null)
            {
                _db.ChangeTracker.Clear();

                return ImportPersistenceResult.Failure(
                    ImportPersistenceError.NotFound);
            }

            if (batch.Status == ImportBatchStatus.Completed)
            {
                _db.ChangeTracker.Clear();

                return ImportPersistenceResult.Success();
            }

            _db.Entry(batch)
                .Property(x => x.RowVersion)
                .OriginalValue =
                    expectedRowVersion;

            var oldErrors =
                await _db.ImportValidationErrors
                    .Where(x =>
                        x.SchoolId == schoolId &&
                        x.ImportBatchId == batchId)
                    .ToArrayAsync(cancellationToken);

            if (oldErrors.Length > 0)
            {
                _db.ImportValidationErrors
                    .RemoveRange(oldErrors);
            }

            if (errors.Count > 0)
            {
                _db.ImportValidationErrors
                    .AddRange(errors);
            }

            batch.Status =
                ImportBatchStatus.ValidationFailed;

            batch.ErrorCount = errors.Count;
            batch.ValidRowCount = validRowCount;

            await _db.SaveChangesAsync(cancellationToken);

            return ImportPersistenceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            _db.ChangeTracker.Clear();

            return ImportPersistenceResult.Failure(
                ImportPersistenceError.Concurrency);
        }
        catch (DbUpdateException)
        {
            _db.ChangeTracker.Clear();

            return ImportPersistenceResult.Failure(
                ImportPersistenceError.Constraint);
        }
    }

    public async Task<ImportPersistenceResult> ApplyAsync(
        Guid schoolId,
        Guid batchId,
        Guid actorUserId,
        byte[] expectedBatchRowVersion,
        ImportApplyPlan plan,
        DateTime completedAtUtc,
        CancellationToken cancellationToken = default)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable,
                cancellationToken);

        try
        {
            var batch =
                await _db.ImportBatches
                    .FirstOrDefaultAsync(
                        x =>
                            x.SchoolId == schoolId &&
                            x.Id == batchId,
                        cancellationToken);

            if (batch is null)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _db.ChangeTracker.Clear();

                return ImportPersistenceResult.Failure(
                    ImportPersistenceError.NotFound);
            }

            if (batch.Status == ImportBatchStatus.Completed)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _db.ChangeTracker.Clear();

                return ImportPersistenceResult.Success();
            }

            if (batch.Status != ImportBatchStatus.Validated ||
                batch.ErrorCount != 0)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                _db.ChangeTracker.Clear();

                return ImportPersistenceResult.Failure(
                    ImportPersistenceError.InvalidState);
            }

            _db.Entry(batch)
                .Property(x => x.RowVersion)
                .OriginalValue =
                    expectedBatchRowVersion;

            var incomingActiveSeats =
                plan.Students.Count(
                    x =>
                        !x.IsArchived &&
                        x.Status == AcademicStructureStatus.Active);

            if (incomingActiveSeats > 0)
            {
                SchoolSubscription? subscription;

                if (_db.Database.IsRelational())
                {
                    subscription =
                        await _db.SchoolSubscriptions
                            .FromSqlInterpolated(
                                $@"SELECT *
FROM ""SchoolSubscriptions""
WHERE ""SchoolId"" = {schoolId}
FOR UPDATE")
                            .SingleOrDefaultAsync(cancellationToken);
                }
                else
                {
                    subscription =
                        await _db.SchoolSubscriptions
                            .SingleOrDefaultAsync(
                                x => x.SchoolId == schoolId,
                                cancellationToken);
                }

                if (subscription is not null)
                {
                    var now = DateTime.UtcNow;

                    if (subscription.Status != SubscriptionStatus.Active ||
                        !subscription.CurrentTermStartsAtUtc.HasValue ||
                        !subscription.CurrentTermEndsAtUtc.HasValue ||
                        subscription.CurrentTermStartsAtUtc.Value > now ||
                        subscription.CurrentTermEndsAtUtc.Value <= now)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _db.ChangeTracker.Clear();
                        return ImportPersistenceResult.Failure(
                            ImportPersistenceError.InvalidState);
                    }

                    var currentActiveSeats =
                        await _db.StudentProfiles
                            .AsNoTracking()
                            .CountAsync(
                                x =>
                                    x.SchoolId == schoolId &&
                                    !x.IsArchived &&
                                    x.Status == AcademicStructureStatus.Active,
                                cancellationToken);

                    if (currentActiveSeats + incomingActiveSeats >
                        subscription.CommittedSeats)
                    {
                        await transaction.RollbackAsync(cancellationToken);
                        _db.ChangeTracker.Clear();
                        return ImportPersistenceResult.Failure(
                            ImportPersistenceError.SeatLimit);
                    }
                }
            }

            foreach (var guard in
                     plan.AcademicYearGuards
                         .DistinctBy(x => x.Id))
            {
                var entity =
                    await _db.AcademicYears
                        .FirstOrDefaultAsync(
                            x =>
                                x.SchoolId == schoolId &&
                                x.Id == guard.Id,
                            cancellationToken);

                if (entity is null ||
                    entity.Status !=
                        AcademicStructureStatus.Active ||
                    !entity.RowVersion.SequenceEqual(
                        guard.RowVersion))
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    return ImportPersistenceResult.Failure(
                        ImportPersistenceError.Concurrency);
                }
            }

            foreach (var guard in
                     plan.ClassGroupGuards
                         .DistinctBy(x => x.Id))
            {
                var entity =
                    await _db.ClassGroups
                        .FirstOrDefaultAsync(
                            x =>
                                x.SchoolId == schoolId &&
                                x.Id == guard.Id,
                            cancellationToken);

                if (entity is null ||
                    entity.Status !=
                        AcademicStructureStatus.Active ||
                    !entity.RowVersion.SequenceEqual(
                        guard.RowVersion))
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    return ImportPersistenceResult.Failure(
                        ImportPersistenceError.Concurrency);
                }
            }

            foreach (var guard in
                     plan.SubjectGuards
                         .DistinctBy(x => x.Id))
            {
                var entity =
                    await _db.Subjects
                        .FirstOrDefaultAsync(
                            x =>
                                x.SchoolId == schoolId &&
                                x.Id == guard.Id,
                            cancellationToken);

                if (entity is null ||
                    entity.Status !=
                        AcademicStructureStatus.Active ||
                    !entity.RowVersion.SequenceEqual(
                        guard.RowVersion))
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    return ImportPersistenceResult.Failure(
                        ImportPersistenceError.Concurrency);
                }
            }

            var trackedAssessments =
                new Dictionary<Guid, Assessment>();

            foreach (var guard in
                     plan.AssessmentGuards
                         .DistinctBy(x => x.Id))
            {
                var entity =
                    await _db.Assessments
                        .FirstOrDefaultAsync(
                            x =>
                                x.SchoolId == schoolId &&
                                x.Id == guard.Id,
                            cancellationToken);

                if (entity is null ||
                    entity.Status != guard.RequiredStatus ||
                    !entity.RowVersion.SequenceEqual(
                        guard.RowVersion))
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    return ImportPersistenceResult.Failure(
                        ImportPersistenceError.Concurrency);
                }

                trackedAssessments[entity.Id] = entity;
            }

            foreach (var result in plan.AssessmentResults)
            {
                if (!trackedAssessments.TryGetValue(
                        result.AssessmentId,
                        out var assessment))
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    return ImportPersistenceResult.Failure(
                        ImportPersistenceError.InvalidState);
                }

                var validStudent =
                    await _db.StudentProfiles
                        .AnyAsync(
                            x =>
                                x.SchoolId == schoolId &&
                                x.Id ==
                                    result.StudentProfileId &&
                                !x.IsArchived &&
                                x.Status ==
                                    AcademicStructureStatus.Active,
                            cancellationToken);

                var enrolled =
                    await _db.StudentEnrollments
                        .AnyAsync(
                            x =>
                                x.SchoolId == schoolId &&
                                x.StudentProfileId ==
                                    result.StudentProfileId &&
                                x.AcademicYearId ==
                                    assessment.AcademicYearId &&
                                x.ClassGroupId ==
                                    assessment.ClassGroupId,
                            cancellationToken);

                var alreadyExists =
                    await _db.AssessmentResults
                        .AnyAsync(
                            x =>
                                x.SchoolId == schoolId &&
                                x.AssessmentId ==
                                    result.AssessmentId &&
                                x.StudentProfileId ==
                                    result.StudentProfileId,
                            cancellationToken);

                if (!validStudent ||
                    !enrolled ||
                    alreadyExists)
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    return ImportPersistenceResult.Failure(
                        ImportPersistenceError.InvalidState);
                }
            }

            if (plan.Subjects.Count > 0)
                _db.Subjects.AddRange(plan.Subjects);

            if (plan.Classes.Count > 0)
                _db.ClassGroups.AddRange(plan.Classes);

            if (plan.Students.Count > 0)
                _db.StudentProfiles.AddRange(plan.Students);

            if (plan.Enrollments.Count > 0)
                _db.StudentEnrollments.AddRange(plan.Enrollments);

            if (plan.TeacherAssignments.Count > 0)
                _db.TeacherAssignments.AddRange(
                    plan.TeacherAssignments);

            if (plan.AssessmentResults.Count > 0)
                _db.AssessmentResults.AddRange(
                    plan.AssessmentResults);

            if (plan.StudentAnswers.Count > 0)
                _db.StudentAnswers.AddRange(
                    plan.StudentAnswers);

            if (plan.CurriculumMappings.Count > 0)
                _db.QuestionLearningOutcomes.AddRange(
                    plan.CurriculumMappings);

            if (plan.OutboxMessages.Count > 0)
                _db.OutboxMessages.AddRange(
                    plan.OutboxMessages);

            foreach (var assessmentId in
                     plan.AssessmentsToTouch.Distinct())
            {
                if (trackedAssessments.TryGetValue(
                        assessmentId,
                        out var assessment))
                {
                    assessment.UpdatedAtUtc =
                        completedAtUtc;
                }
            }

            batch.Status =
                ImportBatchStatus.Completed;

            batch.CompletedByUserId =
                actorUserId;

            batch.CompletedAtUtc =
                completedAtUtc;

            await _db.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            return ImportPersistenceResult.Success();
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _db.ChangeTracker.Clear();

            return ImportPersistenceResult.Failure(
                ImportPersistenceError.Concurrency);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _db.ChangeTracker.Clear();

            return ImportPersistenceResult.Failure(
                ImportPersistenceError.Constraint);
        }
        catch
        {
            await transaction.RollbackAsync(
                cancellationToken);

            _db.ChangeTracker.Clear();

            return ImportPersistenceResult.Failure(
                ImportPersistenceError.Unknown);
        }
    }
}
