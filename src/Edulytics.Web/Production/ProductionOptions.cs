namespace Edulytics.Web.Production;

public sealed class ProductionOptions
{
    public const string SectionName =
        "Edulytics:Production";

    public int WorkerStaleAfterSeconds { get; set; } = 60;

    public int DatabaseTimeoutSeconds { get; set; } = 5;
}
