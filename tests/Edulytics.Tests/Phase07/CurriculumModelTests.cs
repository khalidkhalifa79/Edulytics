using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase07;

public sealed class CurriculumModelTests
{
    [Fact]
    public void CurriculumEntities_AreMapped()
    {
        using var db = CreateDb();

        Assert.NotNull(db.Model.FindEntityType(typeof(CurriculumTopic)));
        Assert.NotNull(db.Model.FindEntityType(typeof(LearningOutcome)));
    }

    [Fact]
    public void Topic_SubjectAndGradeForeignKeysRemainTenantScoped()
    {
        using var db = CreateDb();

        var type = db.Model.FindEntityType(typeof(CurriculumTopic));
        Assert.NotNull(type);

        Assert.Contains(
            type!.GetForeignKeys(),
            fk =>
                fk.PrincipalEntityType.ClrType == typeof(Subject) &&
                fk.Properties.Select(x => x.Name).SequenceEqual(
                    new[]
                    {
                        nameof(CurriculumTopic.SchoolId),
                        nameof(CurriculumTopic.SubjectId)
                    }));

        Assert.Contains(
            type.GetForeignKeys(),
            fk =>
                fk.PrincipalEntityType.ClrType == typeof(GradeLevel) &&
                fk.Properties.Select(x => x.Name).SequenceEqual(
                    new[]
                    {
                        nameof(CurriculumTopic.SchoolId),
                        nameof(CurriculumTopic.GradeLevelId)
                    }));
    }

    [Fact]
    public void OutcomeTopicForeignKey_ContainsFullScope()
    {
        using var db = CreateDb();

        var type = db.Model.FindEntityType(typeof(LearningOutcome));
        Assert.NotNull(type);

        Assert.Contains(
            type!.GetForeignKeys(),
            fk =>
                fk.PrincipalEntityType.ClrType == typeof(CurriculumTopic) &&
                fk.Properties.Select(x => x.Name).SequenceEqual(
                    new[]
                    {
                        nameof(LearningOutcome.SchoolId),
                        nameof(LearningOutcome.AcademicProgramId),
                        nameof(LearningOutcome.FrameworkVersionId),
                        nameof(LearningOutcome.SubjectId),
                        nameof(LearningOutcome.GradeLevelId),
                        nameof(LearningOutcome.TopicId)
                    }));
    }

    [Fact]
    public void Weight_HasExplicitPrecision()
    {
        using var db = CreateDb();

        var property = db.Model
            .FindEntityType(typeof(LearningOutcome))!
            .FindProperty(nameof(LearningOutcome.Weight));

        Assert.NotNull(property);
        Assert.Equal(6, property!.GetPrecision());
        Assert.Equal(3, property.GetScale());
    }

    [Fact]
    public void OfficialOutcome_ReferencesImmutablePackContent()
    {
        using var db = CreateDb();
        var type = db.Model.FindEntityType(typeof(LearningOutcome));

        Assert.NotNull(type);
        Assert.Contains(
            type!.GetForeignKeys(),
            fk =>
                fk.PrincipalEntityType.ClrType ==
                    typeof(CurriculumPackContentNode) &&
                fk.Properties.Select(x => x.Name).SequenceEqual(
                    new[]
                    {
                        nameof(LearningOutcome.OfficialContentNodeId)
                    }));
        Assert.Equal(
            300,
            type.FindProperty(nameof(LearningOutcome.Code))!
                .GetMaxLength());
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase(
                    $"phase07-model-{Guid.NewGuid():N}")
                .Options;

        return new EdulyticsDbContext(options);
    }
}
