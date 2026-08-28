namespace Edulytics.Core.Academics;

/// <summary>
/// Product-controlled school program / curriculum-stream choices.
///
/// The user chooses the friendly stream name.
/// Code is a stable internal identifier and is never entered by the user.
///
/// MAIN is intentionally not part of this catalog. It is the historical
/// migration/default compatibility program and must not be newly created
/// through the school UI.
/// </summary>
public sealed record AcademicProgramCatalogItem(
    string Key,
    string Code,
    string Name);

public static class AcademicProgramCatalog
{
    public static IReadOnlyList<AcademicProgramCatalogItem> All { get; } =
    [
        new(
            Key: "british",
            Code: "BRITISH",
            Name: "British Stream"),

        new(
            Key: "american",
            Code: "AMERICAN",
            Name: "American Stream"),

        new(
            Key: "uae-moe",
            Code: "UAE",
            Name: "UAE MoE Stream"),

        new(
            Key: "polish",
            Code: "POLISH",
            Name: "Polish Stream")
    ];

    public static AcademicProgramCatalogItem? FindByKey(
        string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;

        var normalized = key.Trim();

        return All.SingleOrDefault(
            x =>
                string.Equals(
                    x.Key,
                    normalized,
                    StringComparison.OrdinalIgnoreCase));
    }

    public static AcademicProgramCatalogItem? FindByCode(
        string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        var normalized =
            code.Trim().ToUpperInvariant();

        return All.SingleOrDefault(
            x =>
                string.Equals(
                    x.Code,
                    normalized,
                    StringComparison.Ordinal));
    }
}
