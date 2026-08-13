namespace Edulytics.Services.Schools;

public interface ISchoolManagementService
{
    Task<IReadOnlyList<SchoolListItem>> ListAsync(
        CancellationToken cancellationToken = default);

    Task<SchoolDetails?> GetAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<SchoolCommandResult> CreateAsync(
        CreateSchoolRequest request,
        CancellationToken cancellationToken = default);

    Task<SchoolCommandResult> UpdateAsync(
        UpdateSchoolRequest request,
        CancellationToken cancellationToken = default);

    Task<SchoolCommandResult> ChangeStatusAsync(
        SchoolStatusChangeRequest request,
        CancellationToken cancellationToken = default);
}
