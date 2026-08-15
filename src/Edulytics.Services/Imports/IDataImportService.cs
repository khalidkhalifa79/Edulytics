using Edulytics.Core.Enums;

namespace Edulytics.Services.Imports;

public interface IDataImportService
{
    Task<ImportResult<ImportWorkspace>> GetWorkspaceAsync(
        Guid actorUserId,
        CancellationToken cancellationToken = default);

    Task<ImportResult<ImportBatchDetail>> GetBatchAsync(
        Guid actorUserId,
        Guid batchId,
        CancellationToken cancellationToken = default);

    Task<ImportResult<ImportBatchDetail>> UploadAsync(
        Guid actorUserId,
        ImportType importType,
        string fileName,
        byte[] bytes,
        CancellationToken cancellationToken = default);

    Task<ImportResult<ImportBatchDetail>> ConfirmAsync(
        Guid actorUserId,
        Guid batchId,
        byte[] expectedRowVersion,
        CancellationToken cancellationToken = default);

    IReadOnlyList<string> GetTemplateHeaders(
        ImportType type);
}
