using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Edulytics.Tests.Phase29;

public sealed class Phase29MultiProgramCurriculumModelTests
{
    [Fact]
    public void ProgramScopeIsPartOfClassCurriculumTopicAndOutcomeIdentity()
    {
        var options = new DbContextOptionsBuilder<EdulyticsDbContext>()
            .UseInMemoryDatabase("p29-program-model-" + Guid.NewGuid())
            .Options;

        using var db = new EdulyticsDbContext(options);

        var program = db.Model.FindEntityType(typeof(AcademicProgram));
        Assert.NotNull(program);
        Assert.Contains(
            program!.GetIndexes(),
            i => i.IsUnique &&
                 string.Join("|", i.Properties.Select(p => p.Name)) ==
                 "SchoolId|NormalizedCode");

        var classGroup = db.Model.FindEntityType(typeof(ClassGroup));
        Assert.NotNull(classGroup);
        Assert.False(classGroup!.FindProperty(nameof(ClassGroup.AcademicProgramId))!.IsNullable);
        Assert.Contains(
            classGroup.GetIndexes(),
            i => i.IsUnique &&
                 string.Join("|", i.Properties.Select(p => p.Name)) ==
                 "SchoolId|AcademicYearId|AcademicProgramId|NormalizedCode");
        Assert.Contains(
            classGroup.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(AcademicProgram) &&
                  string.Join("|", fk.Properties.Select(p => p.Name)) ==
                  "SchoolId|AcademicProgramId");

        var adoption = db.Model.FindEntityType(typeof(SchoolCurriculumAdoption));
        Assert.NotNull(adoption);
        Assert.False(
            adoption!.FindProperty(nameof(SchoolCurriculumAdoption.AcademicProgramId))!
                .IsNullable);
        Assert.Contains(
            adoption.GetIndexes(),
            i => i.IsUnique &&
                 string.Join("|", i.Properties.Select(p => p.Name)) ==
                 "SchoolId|AcademicYearId|AcademicProgramId|GradeLevelId|SubjectId");

        var topic = db.Model.FindEntityType(typeof(CurriculumTopic));
        Assert.NotNull(topic);
        Assert.False(topic!.FindProperty(nameof(CurriculumTopic.AcademicProgramId))!.IsNullable);
        Assert.All(
            topic.GetIndexes().Where(i => i.IsUnique),
            i => Assert.Contains(
                i.Properties,
                p => p.Name == nameof(CurriculumTopic.AcademicProgramId)));

        var outcome = db.Model.FindEntityType(typeof(LearningOutcome));
        Assert.NotNull(outcome);
        Assert.False(
            outcome!.FindProperty(nameof(LearningOutcome.AcademicProgramId))!.IsNullable);
        Assert.Contains(
            outcome.GetIndexes(),
            i => i.IsUnique &&
                 string.Join("|", i.Properties.Select(p => p.Name)) ==
                 "SchoolId|AcademicProgramId|FrameworkVersionId|SubjectId|GradeLevelId|Code");
    }
}
