using System.Text.Json;
using Edulytics.Core.Curriculum;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29SourceAcquisitionMatrixTests
{
    [Fact]
    public void Matrix_CoversEveryRegisteredCurriculumScope()
    {
        using var document = Load();

        var curricula =
            document.RootElement
                .GetProperty("curricula")
                .EnumerateArray()
                .ToArray();

        Assert.Equal(4, curricula.Length);

        var actualCodes =
            curricula
                .Select(
                    x =>
                        x.GetProperty("packCode")
                            .GetString()!)
                .ToHashSet(StringComparer.Ordinal);

        var expectedCodes =
            MathematicsCurriculumPackRegistry.All
                .Select(x => x.Code)
                .ToHashSet(StringComparer.Ordinal);

        Assert.True(
            expectedCodes.SetEquals(actualCodes));

        foreach (var pack in MathematicsCurriculumPackRegistry.All)
        {
            var matrixPack =
                curricula.Single(
                    x =>
                        x.GetProperty("packCode")
                            .GetString() ==
                        pack.Code);

            var scopes =
                matrixPack.GetProperty("scopes")
                    .EnumerateArray()
                    .ToArray();

            Assert.Equal(
                pack.Levels.Count,
                scopes.Length);

            var expected =
                pack.Levels
                    .Select(
                        x =>
                            Key(
                                x.LogicalLevel,
                                x.NativeLabel,
                                x.Pathway))
                    .ToHashSet(StringComparer.Ordinal);

            var actual =
                scopes
                    .Select(
                        x =>
                            Key(
                                x.GetProperty("logicalLevel")
                                    .GetInt32(),
                                x.GetProperty("nativeLevel")
                                    .GetString()!,
                                NullableString(
                                    x.GetProperty("pathway"))))
                    .ToHashSet(StringComparer.Ordinal);

            Assert.True(
                expected.SetEquals(actual),
                $"Matrix scope drift for {pack.Code}.");
        }
    }

    [Fact]
    public void Poland_AllScopesUseComplete2025_2026Baseline()
    {
        using var document = Load();

        var poland =
            document.RootElement
                .GetProperty("curricula")
                .EnumerateArray()
                .Single(
                    x =>
                        x.GetProperty("packCode")
                            .GetString() ==
                        MathematicsCurriculumPackRegistry.PolandCode);

        Assert.Equal(
            "2025-2026",
            poland.GetProperty(
                    "defaultOfficialSourcePeriod")
                .GetString());

        Assert.Equal(
            "PreviousOfficialFallback",
            poland.GetProperty(
                    "defaultOfficialSourceResolution")
                .GetString());

        Assert.Equal(
            "PL-MATH-2025-2026",
            poland.GetProperty(
                    "acceptedPackVersion")
                .GetString());

        Assert.True(
            poland.GetProperty(
                    "exclude2026TransitionalCurriculum")
                .GetBoolean());

        var scopes =
            poland.GetProperty("scopes")
                .EnumerateArray()
                .ToArray();

        Assert.Equal(17, scopes.Length);

        Assert.All(
            scopes,
            scope =>
            {
                Assert.Equal(
                    "2025-2026",
                    scope.GetProperty(
                            "officialSourcePeriod")
                        .GetString());

                Assert.Equal(
                    "PreviousOfficialFallback",
                    scope.GetProperty(
                            "officialSourceResolution")
                        .GetString());

                Assert.Equal(
                    "ResearchRequired",
                    scope.GetProperty(
                            "pedagogicalSelectionStatus")
                        .GetString());

                Assert.Contains(
                    "2025-2026",
                    scope.GetProperty(
                            "blockingReason")
                        .GetString()!,
                    StringComparison.Ordinal);
            });
    }

    [Fact]
    public void Poland_MatrixContainsNo2026TransitionMixing()
    {
        var root = FindRepositoryRoot();

        var json =
            File.ReadAllText(
                Path.Combine(
                    root,
                    "docs",
                    "PHASE_29_SOURCE_ACQUISITION_MATRIX.json"));

        using var document =
            JsonDocument.Parse(json);

        var poland =
            document.RootElement
                .GetProperty("curricula")
                .EnumerateArray()
                .Single(
                    x =>
                        x.GetProperty("packCode")
                            .GetString() ==
                        MathematicsCurriculumPackRegistry.PolandCode);

        var serialized =
            poland.GetRawText();

        Assert.DoesNotContain(
            "New 2026 curriculum applies",
            serialized,
            StringComparison.OrdinalIgnoreCase);

        Assert.DoesNotContain(
            "new Mathematics curriculum applies",
            serialized,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "exclude2026TransitionalCurriculum",
            serialized,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ResearchRequiredScopesRemainBlocking()
    {
        using var document = Load();

        var scopes =
            document.RootElement
                .GetProperty("curricula")
                .EnumerateArray()
                .SelectMany(
                    x =>
                        x.GetProperty("scopes")
                            .EnumerateArray()
                            .ToArray())
                .ToArray();

        foreach (var scope in scopes)
        {
            if (scope.GetProperty(
                    "pedagogicalSelectionStatus")
                .GetString() != "ResearchRequired")
            {
                continue;
            }

            Assert.False(
                string.IsNullOrWhiteSpace(
                    scope.GetProperty(
                            "blockingReason")
                        .GetString()));
        }
    }

    [Fact]
    public void CommonCoreGradesOneToEightAreResolvedKindergartenIsOutOfScopeAndNextTargetAdvancesToCambridgePrimaryStage1()
    {
        using var document = Load();

        var commonCore =
            document.RootElement
                .GetProperty("curricula")
                .EnumerateArray()
                .Single(
                    x =>
                        x.GetProperty("packCode")
                            .GetString() ==
                        MathematicsCurriculumPackRegistry.CommonCoreCode);

        var scopes =
            commonCore
                .GetProperty("scopes")
                .EnumerateArray()
                .ToArray();

        foreach (var grade in
                 Enumerable.Range(1, 8))
        {
            var scope =
                scopes.Single(
                    x =>
                        x.GetProperty("nativeLevel")
                            .GetString() ==
                        $"Grade {grade}");

            Assert.Equal(
                "ResolvedExact",
                scope.GetProperty(
                        "pedagogicalSelectionStatus")
                    .GetString());

            Assert.True(
                string.IsNullOrWhiteSpace(
                    scope.GetProperty(
                            "blockingReason")
                        .GetString()));
        }

        var kindergarten =
            scopes.Single(
                x =>
                    x.GetProperty("nativeLevel")
                        .GetString() ==
                    "Kindergarten");

        Assert.Equal(
            "OutOfCurrentProductScope",
            kindergarten.GetProperty(
                    "pedagogicalSelectionStatus")
                .GetString());

        Assert.True(
            string.IsNullOrWhiteSpace(
                kindergarten.GetProperty(
                        "blockingReason")
                    .GetString()));

        var next =
            document.RootElement
                .GetProperty(
                    "nextResearchAndContentTarget");

        Assert.Equal(
            MathematicsCurriculumPackRegistry.CambridgeCode,
            next.GetProperty("packCode")
                .GetString());

        Assert.Equal(
            "Cambridge Primary Stage 1",
            next.GetProperty("nativeLevel")
                .GetString());
    }

    private static JsonDocument Load()
    {
        var root = FindRepositoryRoot();

        return JsonDocument.Parse(
            File.ReadAllText(
                Path.Combine(
                    root,
                    "docs",
                    "PHASE_29_SOURCE_ACQUISITION_MATRIX.json")));
    }

    private static string? NullableString(
        JsonElement element) =>
        element.ValueKind == JsonValueKind.Null
            ? null
            : element.GetString();

    private static string Key(
        int logical,
        string native,
        string? pathway) =>
        string.Join(
            "\u001f",
            logical.ToString(),
            native,
            pathway ?? string.Empty);

    private static string FindRepositoryRoot()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        directory.FullName,
                        "Edulytics.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Repository root not found.");
    }
}
