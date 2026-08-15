using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase08;

public sealed class AssessmentModelTests
{
    [Fact]
    public void Phase08Entities_AreMapped_AndCriticalRowsUseConcurrency()
    {
        using var db = CreateDb();

        foreach (var type in new[]
                 {
                     typeof(Assessment),
                     typeof(AssessmentQuestion),
                     typeof(QuestionLearningOutcome),
                     typeof(AssessmentResult),
                     typeof(StudentAnswer)
                 })
            Assert.NotNull(db.Model.FindEntityType(type));

        Assert.True(db.Model.FindEntityType(typeof(Assessment))!
            .FindProperty(nameof(Assessment.RowVersion))!.IsConcurrencyToken);

        Assert.True(db.Model.FindEntityType(typeof(AssessmentResult))!
            .FindProperty(nameof(AssessmentResult.RowVersion))!.IsConcurrencyToken);
    }

    [Fact]
    public void CriticalRelationships_AreSchoolScoped()
    {
        using var db = CreateDb();

        var assessment = db.Model.FindEntityType(typeof(Assessment))!;
        Assert.Contains(
            assessment.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(ClassGroup) &&
                  fk.Properties.Select(x => x.Name).SequenceEqual(
                      new[]
                      {
                          nameof(Assessment.SchoolId),
                          nameof(Assessment.AcademicYearId),
                          nameof(Assessment.ClassGroupId)
                      }));

        var mapping = db.Model.FindEntityType(typeof(QuestionLearningOutcome))!;
        Assert.Contains(
            mapping.GetForeignKeys(),
            fk => fk.PrincipalEntityType.ClrType == typeof(LearningOutcome) &&
                  fk.Properties.Select(x => x.Name).SequenceEqual(
                      new[]
                      {
                          nameof(QuestionLearningOutcome.SchoolId),
                          nameof(QuestionLearningOutcome.LearningOutcomeId)
                      }));
    }

    [Fact]
    public void ScorePrecision_IsExplicit()
    {
        using var db = CreateDb();

        Assert.Equal(10, db.Model.FindEntityType(typeof(Assessment))!
            .FindProperty(nameof(Assessment.MaxScore))!.GetPrecision());
        Assert.Equal(2, db.Model.FindEntityType(typeof(Assessment))!
            .FindProperty(nameof(Assessment.MaxScore))!.GetScale());

        Assert.Equal(5, db.Model.FindEntityType(typeof(AssessmentResult))!
            .FindProperty(nameof(AssessmentResult.Percentage))!.GetPrecision());
        Assert.Equal(2, db.Model.FindEntityType(typeof(AssessmentResult))!
            .FindProperty(nameof(AssessmentResult.Percentage))!.GetScale());
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EdulyticsDbContext>()
            .UseInMemoryDatabase($"phase08-model-{Guid.NewGuid():N}")
            .Options;

        return new EdulyticsDbContext(options);
    }
}
