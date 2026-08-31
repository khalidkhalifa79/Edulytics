using System.Text.Json;
using Edulytics.Core.Curriculum;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29PedagogicalSourceLicensePolicyTests
{
    [Theory]
    [InlineData("Public Domain")]
    [InlineData("CC0 1.0")]
    [InlineData("CC BY 4.0")]
    [InlineData("Open Government Licence v3.0")]
    public void ApprovedLicensesAllowCommercialReuseAndAdaptation(
        string sourceLicense)
    {
        Assert.True(
            PedagogicalSourceLicensePolicy
                .IsApproved(
                    sourceLicense));

        PedagogicalSourceLicensePolicy
            .Validate(
                sourceLicense);
    }

    [Theory]
    [InlineData("CC BY-NC 4.0")]
    [InlineData("CC BY-NC-SA 4.0")]
    [InlineData("CC BY-ND 4.0")]
    [InlineData("CC BY-NC-ND 4.0")]
    [InlineData("CC BY-SA 4.0")]
    [InlineData("All Rights Reserved")]
    [InlineData("Free for educational use")]
    [InlineData("Unknown")]
    public void RestrictiveOrUnknownLicensesFailClosed(
        string sourceLicense)
    {
        Assert.False(
            PedagogicalSourceLicensePolicy
                .IsApproved(
                    sourceLicense));

        var exception =
            Assert.Throws<
                InvalidOperationException>(
                () =>
                    PedagogicalSourceLicensePolicy
                        .Validate(
                            sourceLicense));

        Assert.Contains(
            "royalty-free commercial reuse and adaptation",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void EveryCurrentSourceDrivenBlueprintPassesLicenseGate()
    {
        var blueprints =
            PedagogicalLessonBlueprintRegistry
                .LoadEmbeddedDocuments();

        Assert.NotEmpty(
            blueprints);

        Assert.All(
            blueprints,
            blueprint =>
            {
                if (blueprint.SchemaVersion == 1)
                {
                    Assert.True(
                        PedagogicalSourceLicensePolicy
                            .IsApproved(
                                blueprint.SourceLicense),
                        $"{blueprint.BlueprintCode}: " +
                        $"{blueprint.SourceLicense}");

                    Assert.False(
                        string.IsNullOrWhiteSpace(
                            blueprint
                                .RequiredDigitalAttribution));

                    Assert.NotEmpty(
                        blueprint.SourceEvidenceUrls);

                    return;
                }

                Assert.Equal(
                    2,
                    blueprint.SchemaVersion);

                Assert.NotEmpty(
                    blueprint.Sources);

                Assert.All(
                    blueprint.Sources,
                    source =>
                    {
                        Assert.True(
                            PedagogicalSourceLicensePolicy
                                .IsApproved(
                                    source.License),
                            $"{blueprint.BlueprintCode}/" +
                            $"{source.SourceKey}: " +
                            $"{source.License}");

                        Assert.False(
                            string.IsNullOrWhiteSpace(
                                source
                                    .RequiredDigitalAttribution));

                        Assert.NotEmpty(
                            source.EvidenceUrls);
                    });
            });
    }

    [Fact]
    public void BlueprintContractRejectsNonCommercialSource()
    {
        var blueprint =
            PedagogicalLessonBlueprintRegistry
                .LoadEmbeddedDocuments()
                .First();

        if (blueprint.SchemaVersion == 1)
        {
            blueprint.SourceLicense =
                "CC BY-NC 4.0";
        }
        else
        {
            Assert.Equal(
                2,
                blueprint.SchemaVersion);

            Assert.NotEmpty(
                blueprint.Sources);

            blueprint.Sources[0].License =
                "CC BY-NC 4.0";
        }

        var exception =
            Assert.Throws<
                InvalidOperationException>(
                () =>
                    PedagogicalLessonBlueprintContract
                        .Validate(
                            blueprint));

        Assert.Contains(
            "not approved",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void MatrixLicensePolicyMatchesRuntimeAllowlist()
    {
        var root =
            FindRepositoryRoot();

        using var document =
            JsonDocument.Parse(
                File.ReadAllText(
                    Path.Combine(
                        root,
                        "docs",
                        "PHASE_29_SOURCE_ACQUISITION_MATRIX.json")));

        var rules =
            document.RootElement
                .GetProperty(
                    "globalRules");

        Assert.True(
            rules.GetProperty(
                    "pedagogicalSourceMustPermitCommercialReuse")
                .GetBoolean());

        Assert.True(
            rules.GetProperty(
                    "pedagogicalSourceMustPermitAdaptation")
                .GetBoolean());

        Assert.True(
            rules.GetProperty(
                    "pedagogicalSourceMustBeRoyaltyFree")
                .GetBoolean());

        Assert.True(
            rules.GetProperty(
                    "pedagogicalSourceMustPermitRedistribution")
                .GetBoolean());

        Assert.True(
            rules.GetProperty(
                    "licenseEvidenceRequiredBeforeResolution")
                .GetBoolean());

        Assert.True(
            rules.GetProperty(
                    "unknownOrRestrictivePedagogicalLicenseIsBlocking")
                .GetBoolean());

        var matrixAllowlist =
            rules.GetProperty(
                    "approvedPedagogicalSourceLicenses")
                .EnumerateArray()
                .Select(
                    x => x.GetString()!)
                .OrderBy(
                    x => x,
                    StringComparer.Ordinal)
                .ToArray();

        var runtimeAllowlist =
            PedagogicalSourceLicensePolicy
                .ApprovedCommercialReuseAndAdaptationLicenses
                .OrderBy(
                    x => x,
                    StringComparer.Ordinal)
                .ToArray();

        Assert.Equal(
            runtimeAllowlist,
            matrixAllowlist);
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? current =
            new(
                AppContext.BaseDirectory);

        while (current is not null)
        {
            if (File.Exists(
                    Path.Combine(
                        current.FullName,
                        "Edulytics.sln")))
            {
                return current.FullName;
            }

            current =
                current.Parent;
        }

        throw new InvalidOperationException(
            "Repository root not found.");
    }
}
