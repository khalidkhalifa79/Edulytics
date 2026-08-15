using Edulytics.Core.Enums;
using Edulytics.Services.Imports;

namespace Edulytics.Web.ViewModels.Imports;

public sealed record ImportIndexViewModel(
    ImportWorkspace Workspace);

public sealed record ImportDetailsViewModel(
    ImportBatchDetail Batch)
{
    public string TypeResourceKey =>
        $"Type{Batch.Type}";

    public string StatusResourceKey =>
        Batch.Status switch
        {
            ImportBatchStatus.Validated =>
                "StatusValidated",

            ImportBatchStatus.ValidationFailed =>
                "StatusValidationFailed",

            _ =>
                "StatusCompleted"
        };

    public string RowVersionBase64 =>
        Convert.ToBase64String(
            Batch.RowVersion);
}
