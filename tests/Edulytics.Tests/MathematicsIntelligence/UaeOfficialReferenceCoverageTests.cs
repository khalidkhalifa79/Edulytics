


using Edulytics.Core.Curriculum;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class UaeOfficialReferenceCoverageTests
{
    [Fact]
    public void UaeBlueprints_KeepOfficialOutcomesSeparateFromTextbookReferences()
    {
        var lessons = PedagogicalLessonBlueprintRegistry
            .LoadEmbeddedDocuments()
            .Where(x => x.PackCode == MathematicsCurriculumPackRegistry.UaeCode)
            .SelectMany(x => x.Lessons)
            .ToArray();

        Assert.NotEmpty(lessons);

        var referenceCodes = lessons
            .Where(x => !string.IsNullOrWhiteSpace(x.OfficialReferenceCode))
            .Select(x => x.OfficialReferenceCode!)
            .ToArray();

        Assert.Equal(
            referenceCodes.Length,
            referenceCodes.Distinct(StringComparer.Ordinal).Count());

        foreach (var lesson in lessons)
        {
            Assert.All(
                lesson.OutcomeCodes,
                code => Assert.StartsWith(
                    "UAE:STD:MAT.",
                    code,
                    StringComparison.Ordinal));

            if (string.IsNullOrWhiteSpace(lesson.OfficialReferenceCode))
                continue;

            Assert.StartsWith(
                "UAE:REF:TEXTBOOK:",
                lesson.OfficialReferenceCode,
                StringComparison.Ordinal);

            Assert.Contains(
                lesson.Alignments,
                x =>
                    x.Role == "Addressing" &&
                    x.ReferenceKind == "OfficialReference" &&
                    x.ResolutionKind == "ExactAcceptedReference" &&
                    x.ReferenceCode == lesson.OfficialReferenceCode &&
                    string.IsNullOrWhiteSpace(x.OutcomeCode));
        }
    }

    [Fact]
    public void UaeCurriculum_HasZeroSupportingLessons_AndNoGrade5Or6AdvancedPathway()
    {
        var blueprints = PedagogicalLessonBlueprintRegistry
            .LoadEmbeddedDocuments()
            .Where(x => x.PackCode == MathematicsCurriculumPackRegistry.UaeCode)
            .ToArray();

        Assert.DoesNotContain(
            blueprints,
            x => (x.LogicalLevel == 5 || x.LogicalLevel == 6) &&
                 string.Equals(x.Pathway, "Advanced", StringComparison.OrdinalIgnoreCase));

        foreach (var blueprint in blueprints)
        {
            Assert.All(
                blueprint.Lessons,
                lesson =>
                {
                    Assert.True(
                        CanonicalLessonRoleRegistry.TryGetIsSupporting(
                            lesson.LessonCode,
                            out var isSupporting));

                    Assert.False(isSupporting);

                    Assert.True(
                        lesson.OutcomeCodes.Count > 0 ||
                        !string.IsNullOrWhiteSpace(lesson.OfficialReferenceCode));
                });
        }
    }

}
