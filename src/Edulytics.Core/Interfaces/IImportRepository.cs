using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Edulytics.Core.Imports;

namespace Edulytics.Core.Interfaces;

public interface IImportRepository
{
    Task<ImportDataSnapshot> GetSnapshotAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImportBatch>> ListAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<ImportBatch?> GetAsync(
        Guid schoolId,
        Guid batchId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ImportValidationError>> GetErrorsAsync(
        Guid schoolId,
        Guid batchId,
        CancellationToken cancellationToken = default);

    Task<ImportBatch?> FindIdempotentAsync(
        Guid schoolId,
        Guid actorUserId,
        ImportType importType,
        string fileHash,
        CancellationToken cancellationToken = default);

    Task<ImportPersistenceResult> AddBatchAsync(
        ImportBatch batch,
        IReadOnlyList<ImportValidationError> errors,
        CancellationToken cancellationToken = default);

    Task<ImportPersistenceResult> MarkValidationFailedAsync(
        Guid schoolId,
        Guid batchId,
        byte[] expectedRowVersion,
        int validRowCount,
        IReadOnlyList<ImportValidationError> errors,
        CancellationToken cancellationToken = default);

    Task<ImportPersistenceResult> ApplyAsync(
        Guid schoolId,
        Guid batchId,
        Guid actorUserId,
        byte[] expectedBatchRowVersion,
        ImportApplyPlan plan,
        DateTime completedAtUtc,
        CancellationToken cancellationToken = default);
}
