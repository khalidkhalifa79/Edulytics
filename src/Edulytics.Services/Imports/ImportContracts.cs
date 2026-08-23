using Edulytics.Core.Enums;

namespace Edulytics.Services.Imports;

public enum ImportErrorCode
{
    AccessDenied = 1,
    SchoolNotActive = 2,
    UnsupportedFile = 3,
    InvalidFile = 4,
    FileTooLarge = 5,
    TooManyRows = 6,
    TooManyColumns = 7,
    DuplicateHeader = 8,
    EmptyFile = 9,
    BatchNotFound = 10,
    BatchHasErrors = 11,
    BatchStateChanged = 12,
    ConcurrencyConflict = 13,
    Persistence = 14,
    SeatLimitReached = 15
}

public sealed record ImportResult<T>(
    T? Value,
    ImportErrorCode? Error)
{
    public bool Succeeded =>
        Error is null;

    public static ImportResult<T> Success(
        T value) =>
        new(value, null);

    public static ImportResult<T> Failure(
        ImportErrorCode error) =>
        new(default, error);
}

public sealed record ImportTypeOption(
    ImportType Type,
    IReadOnlyList<string> RequiredHeaders);

public sealed record ImportBatchListItem(
    Guid Id,
    ImportType Type,
    ImportBatchStatus Status,
    string FileName,
    int RowCount,
    int ErrorCount,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc);

public sealed record ImportPreviewRow(
    int RowNumber,
    IReadOnlyDictionary<string, string> Values);

public sealed record ImportValidationErrorItem(
    int RowNumber,
    string ColumnName,
    string Code,
    string? RawValue);

public sealed record ImportBatchDetail(
    Guid Id,
    ImportType Type,
    ImportBatchStatus Status,
    string FileName,
    int RowCount,
    int ValidRowCount,
    int ErrorCount,
    DateTime CreatedAtUtc,
    DateTime? CompletedAtUtc,
    byte[] RowVersion,
    IReadOnlyList<string> Headers,
    IReadOnlyList<ImportPreviewRow> PreviewRows,
    IReadOnlyList<ImportValidationErrorItem> Errors,
    bool CanConfirm);

public sealed record ImportWorkspace(
    IReadOnlyList<ImportTypeOption> AllowedTypes,
    IReadOnlyList<ImportBatchListItem> Batches);
