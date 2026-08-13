using Edulytics.Core.Entities;
using Edulytics.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using Edulytics.Data.Contexts;

namespace Edulytics.Data.Repositories;

public sealed class SchoolRepository : ISchoolRepository
{
    private readonly EdulyticsDbContext _dbContext;

    public SchoolRepository(EdulyticsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<School>> ListAsync(
        CancellationToken cancellationToken = default) =>
        await _dbContext
            .Set<School>()
            .AsNoTracking()
            .OrderBy(school => school.Name)
            .ThenBy(school => school.SchoolCode)
            .ToListAsync(cancellationToken);

    public Task<School?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        _dbContext
            .Set<School>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                school => school.Id == id,
                cancellationToken);

    public Task<School?> GetForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        _dbContext
            .Set<School>()
            .SingleOrDefaultAsync(
                school => school.Id == id,
                cancellationToken);

    public Task<bool> ExistsByNormalizedCodeAsync(
        string normalizedSchoolCode,
        CancellationToken cancellationToken = default) =>
        _dbContext
            .Set<School>()
            .AsNoTracking()
            .AnyAsync(
                school =>
                    school.NormalizedSchoolCode == normalizedSchoolCode,
                cancellationToken);

    public Task AddAsync(
        School school,
        CancellationToken cancellationToken = default) =>
        _dbContext
            .Set<School>()
            .AddAsync(school, cancellationToken)
            .AsTask();

    public async Task<SchoolRepositoryWriteResult> SaveAsync(
        School school,
        byte[]? expectedRowVersion,
        CancellationToken cancellationToken = default)
    {
        if (expectedRowVersion is { Length: > 0 })
        {
            _dbContext
                .Entry(school)
                .Property(entity => entity.RowVersion)
                .OriginalValue = expectedRowVersion;
        }

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return SchoolRepositoryWriteResult.Success;
        }
        catch (DbUpdateConcurrencyException)
        {
            return SchoolRepositoryWriteResult.ConcurrencyConflict;
        }
        catch (DbUpdateException)
        {
            return SchoolRepositoryWriteResult.ConstraintViolation;
        }
    }
}
