using Edulytics.Core.Entities;

namespace Edulytics.Core.Interfaces;

public enum SchoolRepositoryWriteResult
{
    Success = 1,
    ConcurrencyConflict = 2,
    ConstraintViolation = 3
}

public interface ISchoolRepository
{
    Task<IReadOnlyList<School>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<School?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<School?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedCodeAsync(
        string normalizedSchoolCode,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        School school,
        CancellationToken cancellationToken = default);

    Task<SchoolRepositoryWriteResult> SaveAsync(
        School school,
        byte[]? expectedRowVersion,
        CancellationToken cancellationToken = default);
}
