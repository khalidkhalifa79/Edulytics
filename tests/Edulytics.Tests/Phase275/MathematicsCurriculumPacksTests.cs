using Edulytics.Core.Curriculum;
using Edulytics.Core.Entities;

namespace Edulytics.Tests.Phase275;

public sealed class MathematicsCurriculumPacksTests
{
    [Fact]
    public void Registry_SelfValidation_Passes() =>
        MathematicsCurriculumPackRegistry.Validate();

    [Fact]
    public void LessonBlueprintRegistry_SelfValidation_Passes() =>
        MathematicsLessonBlueprintRegistry.Validate();

    [Fact]
    public void ExactlyFourApprovedMathematicsPacksExist()
    {
        Assert.Equal(4, MathematicsCurriculumPackRegistry.All.Count);
        Assert.All(
            MathematicsCurriculumPackRegistry.All,
            x => Assert.Equal(MathematicsCurriculumPackRegistry.MathematicsSubjectCode, x.SubjectCode));

        var expected = new[]
        {
            MathematicsCurriculumPackRegistry.EnglandCode,
            MathematicsCurriculumPackRegistry.CommonCoreCode,
            MathematicsCurriculumPackRegistry.UaeCode,
            MathematicsCurriculumPackRegistry.PolandCode
        }.OrderBy(x => x).ToArray();

        Assert.Equal(
            expected,
            MathematicsCurriculumPackRegistry.All.Select(x => x.Code).OrderBy(x => x).ToArray());
    }

    [Fact]
    public void CommonCore_ProductOwnerEvidence_EnablesOfficialTextContract()
    {
        var pack = MathematicsCurriculumPackRegistry.All.Single(
            x => x.Code == MathematicsCurriculumPackRegistry.CommonCoreCode);

        Assert.Equal(
            CurriculumReuseBasis.ProductOwnerConfirmedCommercialUseEvidence,
            pack.ReuseBasis);
        Assert.Equal(
            CurriculumTextMode.FullOfficialTextPermitted,
            pack.TextMode);
        Assert.Contains("Copyright 2010", pack.RequiredAttribution, StringComparison.Ordinal);
        Assert.DoesNotContain("licence number", pack.EvidenceNote, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CommonCore_MapsKindergartenThroughGrade12()
    {
        var pack = MathematicsCurriculumPackRegistry.All.Single(
            x => x.Code == MathematicsCurriculumPackRegistry.CommonCoreCode);

        Assert.Equal(
            Enumerable.Range(1, 13),
            pack.Levels.Select(x => x.LogicalLevel).Distinct().OrderBy(x => x));

        Assert.Contains(pack.Levels, x => x.LogicalLevel == 1 && x.NativeLabel == "Kindergarten");
        Assert.Contains(pack.Levels, x => x.LogicalLevel == 13 && x.NativeLabel == "Grade 12");
    }

    [Fact]
    public void England_UsesSeparatePost16SourceAndMapsYear13()
    {
        var pack = MathematicsCurriculumPackRegistry.All.Single(
            x => x.Code == MathematicsCurriculumPackRegistry.EnglandCode);

        Assert.Contains(pack.Sources, x => x.Url.Contains("gce-as-and-a-level-mathematics", StringComparison.Ordinal));
        Assert.Contains(pack.Levels, x => x.LogicalLevel == 13 && x.NativeLabel == "Year 13");
        Assert.Equal(CurriculumTextMode.FullOfficialTextPermitted, pack.TextMode);
    }

    [Fact]
    public void Uae_StopsAtGrade12()
    {
        var pack = MathematicsCurriculumPackRegistry.All.Single(
            x => x.Code == MathematicsCurriculumPackRegistry.UaeCode);

        Assert.Equal(12, pack.Levels.Max(x => x.LogicalLevel));
        Assert.DoesNotContain(pack.Levels, x => x.LogicalLevel == 13);
        Assert.Equal(CurriculumTextMode.OfficialSourceLinked, pack.TextMode);
    }

    [Fact]
    public void Poland_PreservesUpperSecondaryPathways()
    {
        var pack = MathematicsCurriculumPackRegistry.All.Single(
            x => x.Code == MathematicsCurriculumPackRegistry.PolandCode);

        Assert.Contains(
            pack.Levels,
            x => x.LogicalLevel == 12 &&
                 x.NativeLabel == "Klasa IV" &&
                 x.Pathway == "Liceum ogólnokształcące");

        Assert.Contains(
            pack.Levels,
            x => x.LogicalLevel == 13 &&
                 x.NativeLabel == "Klasa V" &&
                 x.Pathway == "Technikum");
    }

    [Fact]
    public void ExistingCurriculumArchitectureCarriesFrameworkGradeSubjectIdentity()
    {
        Assert.NotNull(typeof(CurriculumTopic).GetProperty(nameof(CurriculumTopic.FrameworkVersionId)));
        Assert.NotNull(typeof(CurriculumTopic).GetProperty(nameof(CurriculumTopic.SubjectId)));
        Assert.NotNull(typeof(CurriculumTopic).GetProperty(nameof(CurriculumTopic.GradeLevelId)));

        Assert.NotNull(typeof(LearningOutcome).GetProperty(nameof(LearningOutcome.FrameworkVersionId)));
        Assert.NotNull(typeof(LearningOutcome).GetProperty(nameof(LearningOutcome.SubjectId)));
        Assert.NotNull(typeof(LearningOutcome).GetProperty(nameof(LearningOutcome.GradeLevelId)));
    }

    [Fact]
    public void LessonLayerSitsBelowStandardsAndOutcomes()
    {
        var hierarchy = MathematicsCurriculumPackHierarchy.OrderedLevels.ToList();

        Assert.True(hierarchy.IndexOf("StandardOrLearningOutcome") < hierarchy.IndexOf("Unit"));
        Assert.True(hierarchy.IndexOf("Unit") < hierarchy.IndexOf("Lesson"));
        Assert.True(hierarchy.IndexOf("Lesson") < hierarchy.IndexOf("ActivityOrAssessmentQuestion"));
    }

    [Fact]
    public void EverySourceHasHttpsProvenance()
    {
        foreach (var source in MathematicsCurriculumPackRegistry.All.SelectMany(x => x.Sources))
        {
            Assert.True(Uri.TryCreate(source.Url, UriKind.Absolute, out var uri));
            Assert.Equal("https", uri!.Scheme);
            Assert.True(source.OfficialAuthority);
            Assert.False(string.IsNullOrWhiteSpace(source.Authority));
            Assert.False(string.IsNullOrWhiteSpace(source.VersionLabel));
        }
    }

    [Fact]
    public void LessonBlueprintsCoverEveryPackAndLogicalLevel()
    {
        var blueprints = MathematicsLessonBlueprintRegistry.CreateBlueprints();

        foreach (var pack in MathematicsCurriculumPackRegistry.All)
        {
            var expectedLevels = pack.Levels.Select(x => x.LogicalLevel).Distinct().OrderBy(x => x).ToArray();
            var actualLevels = blueprints
                .Where(x => x.PackCode == pack.Code)
                .Select(x => x.LogicalLevel)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            Assert.Equal(expectedLevels, actualLevels);
        }
    }
}
