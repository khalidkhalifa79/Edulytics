using Edulytics.Data.Seeding;

namespace Edulytics.Tests.Acceptance;

public sealed class CambridgeStage6LessonContentAlignmentTests
{
    [Fact]
    public void EmbeddedStage6Pack_AppliesExactSkillCorrections()
    {
        var document = Stage6Document();

        var twoUnknowns = document.Lessons.Single(
            x => string.Equals(
                x.LessonCode,
                CambridgePrimaryStage6LessonContentCorrections.TwoUnknownsLessonCode,
                StringComparison.Ordinal));
        var scaleReadingBuild = document.Lessons.Single(
            x => string.Equals(
                x.LessonCode,
                CambridgePrimaryStage6LessonContentCorrections.ScaleReadingBuildLessonCode,
                StringComparison.Ordinal));
        var scaleReading = document.Lessons.Single(
            x => string.Equals(
                x.LessonCode,
                CambridgePrimaryStage6LessonContentCorrections.ScaleReadingLessonCode,
                StringComparison.Ordinal));
        var fractionComparison = document.Lessons.Single(
            x => string.Equals(
                x.LessonCode,
                CambridgePrimaryStage6LessonContentCorrections.FractionComparisonLessonCode,
                StringComparison.Ordinal));

        var twoUnknownsEnglish = English(twoUnknowns);
        Assert.Contains("x + y = 46", twoUnknownsEnglish.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("y − x = 8", twoUnknownsEnglish.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("both original relationships", twoUnknownsEnglish.StepByStepSolutions, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("48+27", twoUnknownsEnglish.WorkedExamples, StringComparison.Ordinal);

        var scaleBuildEnglish = English(scaleReadingBuild);
        Assert.Contains("nearest tenth or whole number", scaleBuildEnglish.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("4.36", scaleBuildEnglish.WorkedExamples, StringComparison.Ordinal);

        var scaleEnglish = English(scaleReading);
        Assert.Contains("nearest tenth or whole number", scaleEnglish.Title, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("12.64", scaleEnglish.WorkedExamples, StringComparison.Ordinal);
        Assert.DoesNotContain("interval", scaleEnglish.Explanation, StringComparison.OrdinalIgnoreCase);

        var fractionEnglish = English(fractionComparison);
        Assert.Contains("2/3 = 8/12", fractionEnglish.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("3/4 = 9/12", fractionEnglish.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("3/5 = 24/40", fractionEnglish.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("5/8 = 25/40", fractionEnglish.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("1/2 = 4/8", fractionEnglish.WorkedExamples, StringComparison.Ordinal);
        Assert.Contains("fraction bars", fractionEnglish.WorkedExamples, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CorrectionVersion_IsRestrictedToFourExactLessonCodes()
    {
        var document = Stage6Document();

        var targetCodes = new HashSet<string>(StringComparer.Ordinal)
        {
            CambridgePrimaryStage6LessonContentCorrections.TwoUnknownsLessonCode,
            CambridgePrimaryStage6LessonContentCorrections.ScaleReadingBuildLessonCode,
            CambridgePrimaryStage6LessonContentCorrections.ScaleReadingLessonCode,
            CambridgePrimaryStage6LessonContentCorrections.FractionComparisonLessonCode
        };

        var corrected =
            document.Lessons
                .Where(
                    lesson => string.Equals(
                        CambridgePrimaryStage6LessonContentCorrections
                            .GetExpectedContentVersion(document, lesson),
                        CambridgePrimaryStage6LessonContentCorrections
                            .CorrectionContentVersion,
                        StringComparison.Ordinal))
                .Select(x => x.LessonCode)
                .ToHashSet(StringComparer.Ordinal);

        Assert.True(targetCodes.SetEquals(corrected), $"target=[{string.Join(",", targetCodes.OrderBy(x => x))}] corrected=[{string.Join(",", corrected.OrderBy(x => x))}]");

        foreach (var lesson in document.Lessons.Where(x => targetCodes.Contains(x.LessonCode)))
        {
            Assert.True(
                CambridgePrimaryStage6LessonContentCorrections.CanUpgradeExisting(
                    document,
                    lesson,
                    CambridgePrimaryStage6LessonContentCorrections.BaseContentVersion));

            Assert.True(
                CambridgePrimaryStage6LessonContentCorrections.CanUpgradeExisting(
                    document,
                    lesson,
                    CambridgePrimaryStage6LessonContentCorrections.PriorCorrectionContentVersion));

            Assert.False(
                CambridgePrimaryStage6LessonContentCorrections.CanUpgradeExisting(
                    document,
                    lesson,
                    "unexpected-version"));
        }

        var nonTarget =
            document.Lessons.First(
                x => !targetCodes.Contains(x.LessonCode));

        Assert.Equal(
            document.ContentVersion,
            CambridgePrimaryStage6LessonContentCorrections
                .GetExpectedContentVersion(document, nonTarget));
        Assert.False(
            CambridgePrimaryStage6LessonContentCorrections.CanUpgradeExisting(
                document,
                nonTarget,
                document.ContentVersion));
    }

    [Fact]
    public void CorrectedLessons_UseOnlyReviewedCambridgeMappings()
    {
        var document = Stage6Document();

        var twoUnknowns = document.Lessons.Single(
            x => string.Equals(
                x.LessonCode,
                CambridgePrimaryStage6LessonContentCorrections.TwoUnknownsLessonCode,
                StringComparison.Ordinal));
        var scaleReadingBuild = document.Lessons.Single(
            x => string.Equals(
                x.LessonCode,
                CambridgePrimaryStage6LessonContentCorrections.ScaleReadingBuildLessonCode,
                StringComparison.Ordinal));
        var scaleReading = document.Lessons.Single(
            x => string.Equals(
                x.LessonCode,
                CambridgePrimaryStage6LessonContentCorrections.ScaleReadingLessonCode,
                StringComparison.Ordinal));
        var fractionComparison = document.Lessons.Single(
            x => string.Equals(
                x.LessonCode,
                CambridgePrimaryStage6LessonContentCorrections.FractionComparisonLessonCode,
                StringComparison.Ordinal));

        Assert.Equal(
            new[] { "CAM:OUT:0096:6Nc.02" },
            twoUnknowns.OutcomeCodes);
        Assert.Equal(
            new[] { "CAM:OUT:0096:6Nf.08" },
            fractionComparison.OutcomeCodes);

        Assert.Equal(
            new[] { "CAM:OUT:0096:6Np.04" },
            scaleReadingBuild.OutcomeCodes);
        Assert.False(scaleReadingBuild.IsSupporting);
        Assert.Equal(
            new[] { "CAM:OUT:0096:6Np.04" },
            scaleReading.OutcomeCodes);
        Assert.False(scaleReading.IsSupporting);
    }


    [Fact]
    public void ProductionStartupCorrectionPath_IsNarrowAndIndependentOfBroadMaintenance()
    {
        var root = FindRoot();

        var seeder = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Data/Seeding/MathematicsCanonicalLessonContentSeeder.cs"));

        var program = File.ReadAllText(Path.Combine(
            root,
            "src/Edulytics.Web/Program.cs"));

        Assert.Contains(
            "SeedApprovedProductionCorrectionsAsync",
            seeder,
            StringComparison.Ordinal);
        Assert.Contains(
            "CanonicalLessonContentMaterializer",
            seeder,
            StringComparison.Ordinal);
        Assert.Contains(
            "IsApprovedProductionCorrectionTarget",
            seeder,
            StringComparison.Ordinal);
        Assert.Contains(
            ".IsReviewedCorrectionTarget(document, lesson)",
            seeder,
            StringComparison.Ordinal);

        var broadMaintenanceGate = program.IndexOf(
            "if (runStartupDataMaintenance)",
            StringComparison.Ordinal);
        var pedagogicalSeedCall = program.IndexOf(
            "await mathematicsPedagogicalLessonSeeder",
            StringComparison.Ordinal);
        var firstCorrectionCall = program.IndexOf(
            ".SeedApprovedProductionCorrectionsAsync()",
            StringComparison.Ordinal);
        var secondCorrectionCall = program.LastIndexOf(
            ".SeedApprovedProductionCorrectionsAsync()",
            StringComparison.Ordinal);
        var maintenanceSkippedMarker = program.IndexOf(
            "STARTUP_DATA_MAINTENANCE_SKIPPED",
            StringComparison.Ordinal);

        Assert.True(broadMaintenanceGate >= 0);
        Assert.True(pedagogicalSeedCall > broadMaintenanceGate);
        Assert.True(firstCorrectionCall > pedagogicalSeedCall);
        Assert.True(secondCorrectionCall > firstCorrectionCall);
        Assert.True(maintenanceSkippedMarker > secondCorrectionCall);
        Assert.Contains(
            "STARTUP_APPROVED_CONTENT_CORRECTIONS_COMPLETED",
            program,
            StringComparison.Ordinal);
    }

    [Fact]
    public void StudentFacingProductionCorrectionTargets_AreExactlyTheFourForensicFailures()
    {
        var expected = new HashSet<string>(StringComparer.Ordinal)
        {
            StudentFacingLessonContentCorrections.Stage3UnitFractionBuild,
            StudentFacingLessonContentCorrections.Stage3UnitFractionApply,
            StudentFacingLessonContentCorrections.Stage5NonUnitFractionBuild,
            StudentFacingLessonContentCorrections.Stage5NonUnitFractionApply
        };

        Assert.True(
            expected.SetEquals(
                StudentFacingLessonContentCorrections.AllLessonCodes));
    }

    private static Edulytics.Core.Curriculum.CanonicalLessonContentPackDocument Stage6Document() =>
        MathematicsCanonicalLessonContentSeeder
            .LoadEmbeddedDocuments()
            .Single(
                x =>
                    string.Equals(
                        x.PackCode,
                        CambridgePrimaryStage6LessonContentCorrections.PackCode,
                        StringComparison.Ordinal) &&
                    string.Equals(
                        x.ContentVersion,
                        CambridgePrimaryStage6LessonContentCorrections.BaseContentVersion,
                        StringComparison.Ordinal));

    private static Edulytics.Core.Curriculum.CanonicalLessonContentPackTranslation English(
        Edulytics.Core.Curriculum.CanonicalLessonContentPackLesson lesson) =>
        lesson.Translations.Single(
            x => string.Equals(
                x.CultureCode,
                "en",
                StringComparison.Ordinal));

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Edulytics.sln")))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Edulytics solution root not found.");
    }

}
