using Edulytics.Core.Curriculum;
using Edulytics.Services.LessonContent;

namespace Edulytics.Tests.MathematicsIntelligence;

public sealed class UaeStudentLessonExposurePolicyTests
{
    [Fact]
    public void UaeSupportingLesson_IsNotLearnerVisible()
    {
        Assert.False(
            LessonContentPolicy.CanExposeStudentLesson(
                MathematicsCurriculumPackRegistry.UaeCode,
                isSupporting: true));
    }

    [Fact]
    public void UaeOfficialReferenceOrOutcomeLesson_RemainsLearnerVisible()
    {
        Assert.True(
            LessonContentPolicy.CanExposeStudentLesson(
                MathematicsCurriculumPackRegistry.UaeCode,
                isSupporting: false));
    }

    [Fact]
    public void CambridgeSupportingLesson_IsNotLearnerVisible()
    {
        Assert.False(
            LessonContentPolicy.CanExposeStudentLesson(
                MathematicsCurriculumPackRegistry.CambridgeCode,
                isSupporting: true));
    }

    [Fact]
    public void CambridgeOfficialOutcomeLesson_RemainsLearnerVisible()
    {
        Assert.True(
            LessonContentPolicy.CanExposeStudentLesson(
                MathematicsCurriculumPackRegistry.CambridgeCode,
                isSupporting: false));
    }

    [Fact]
    public void OtherCurricula_AreUnaffected()
    {
        Assert.True(
            LessonContentPolicy.CanExposeStudentLesson(
                MathematicsCurriculumPackRegistry.CommonCoreCode,
                isSupporting: true));
    }
}
