using Edulytics.Core.Users;

namespace Edulytics.Core.Interfaces;

public interface ISchoolUserRepository
{
    Task<SchoolUserRecord?> GetActorAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SchoolUserRecord>> ListBySchoolAsync(
        Guid schoolId,
        CancellationToken cancellationToken = default);

    Task<SchoolUserRecord?> GetBySchoolAndIdAsync(
        Guid schoolId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SchoolUserPersistenceResult> CreateAsync(
        Guid schoolId,
        string email,
        string role,
        CancellationToken cancellationToken = default);

    Task<SchoolUserPersistenceResult> SetActiveAsync(
        Guid schoolId,
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<SchoolUserPersistenceResult> SetLockedAsync(
        Guid schoolId,
        Guid userId,
        bool isLocked,
        CancellationToken cancellationToken = default);

    Task<SchoolUserPersistenceResult> SetRoleAsync(
        Guid schoolId,
        Guid userId,
        string role,
        CancellationToken cancellationToken = default);

    Task<SchoolUserPersistenceResult> GeneratePasswordSetupAsync(
        Guid schoolId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<SchoolUserPersistenceResult> CompletePasswordSetupAsync(
        Guid userId,
        string token,
        string newPassword,
        CancellationToken cancellationToken = default);
}
