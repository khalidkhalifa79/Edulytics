using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Edulytics.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase075;

public sealed class CurriculumFrameworkFoundationTests
{
    [Fact]
    public void TopicNameAndOrderIndexes_AreFrameworkScoped()
    {
        using var db = CreateDb();
        var type = db.Model.FindEntityType(typeof(CurriculumTopic))!;

        Assert.Contains(
            type.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Select(x => x.Name).SequenceEqual(
                    new[]
                    {
                        nameof(CurriculumTopic.SchoolId),
                        nameof(CurriculumTopic.FrameworkVersionId),
                        nameof(CurriculumTopic.SubjectId),
                        nameof(CurriculumTopic.GradeLevelId),
                        nameof(CurriculumTopic.Name)
                    }));

        Assert.Contains(
            type.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Select(x => x.Name).SequenceEqual(
                    new[]
                    {
                        nameof(CurriculumTopic.SchoolId),
                        nameof(CurriculumTopic.FrameworkVersionId),
                        nameof(CurriculumTopic.SubjectId),
                        nameof(CurriculumTopic.GradeLevelId),
                        nameof(CurriculumTopic.Order)
                    }));
    }

    [Fact]
    public void OutcomeCodeIndex_IsFrameworkGradeSubjectScoped()
    {
        using var db = CreateDb();
        var type = db.Model.FindEntityType(typeof(LearningOutcome))!;

        Assert.Contains(
            type.GetIndexes(),
            index =>
                index.IsUnique &&
                index.Properties.Select(x => x.Name).SequenceEqual(
                    new[]
                    {
                        nameof(LearningOutcome.SchoolId),
                        nameof(LearningOutcome.FrameworkVersionId),
                        nameof(LearningOutcome.SubjectId),
                        nameof(LearningOutcome.GradeLevelId),
                        nameof(LearningOutcome.Code)
                    }));
    }

    [Fact]
    public void FrameworkVersionAndAdoption_AreMappedWithConcurrency()
    {
        using var db = CreateDb();

        foreach (var pair in new[]
                 {
                     (typeof(CurriculumFramework), nameof(CurriculumFramework.RowVersion)),
                     (typeof(CurriculumFrameworkVersion), nameof(CurriculumFrameworkVersion.RowVersion)),
                     (typeof(SchoolCurriculumAdoption), nameof(SchoolCurriculumAdoption.RowVersion))
                 })
        {
            var property = db.Model
                .FindEntityType(pair.Item1)!
                .FindProperty(pair.Item2);

            Assert.NotNull(property);
            Assert.True(property!.IsConcurrencyToken);
        }
    }

    [Fact]
    public async Task Repository_DistinguishesSameTopicNameAcrossFrameworks()
    {
        using var db = CreateDb();
        var repo = new CurriculumRepository(db);

        var schoolId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var gradeId = Guid.NewGuid();
        var versionA = Guid.NewGuid();
        var versionB = Guid.NewGuid();

        db.CurriculumTopics.Add(new CurriculumTopic
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            FrameworkVersionId = versionA,
            SubjectId = subjectId,
            GradeLevelId = gradeId,
            Name = "Numbers",
            Order = 1
        });
        await db.SaveChangesAsync();

        Assert.True(await repo.TopicNameExistsAsync(
            schoolId, versionA, subjectId, gradeId, "NUMBERS"));

        Assert.False(await repo.TopicNameExistsAsync(
            schoolId, versionB, subjectId, gradeId, "NUMBERS"));
    }

    [Fact]
    public async Task Repository_DistinguishesSameOutcomeCodeAcrossFrameworksAndGrades()
    {
        using var db = CreateDb();
        var repo = new CurriculumRepository(db);

        var schoolId = Guid.NewGuid();
        var subjectId = Guid.NewGuid();
        var grade6 = Guid.NewGuid();
        var grade7 = Guid.NewGuid();
        var versionA = Guid.NewGuid();
        var versionB = Guid.NewGuid();

        db.LearningOutcomes.Add(new LearningOutcome
        {
            Id = Guid.NewGuid(),
            SchoolId = schoolId,
            FrameworkVersionId = versionA,
            SubjectId = subjectId,
            GradeLevelId = grade6,
            TopicId = Guid.NewGuid(),
            Code = "MATH.1",
            Description = "Outcome",
            Weight = 10m,
            Order = 1
        });
        await db.SaveChangesAsync();

        Assert.True(await repo.OutcomeCodeExistsAsync(
            schoolId, versionA, subjectId, grade6, "MATH.1"));

        Assert.False(await repo.OutcomeCodeExistsAsync(
            schoolId, versionB, subjectId, grade6, "MATH.1"));

        Assert.False(await repo.OutcomeCodeExistsAsync(
            schoolId, versionA, subjectId, grade7, "MATH.1"));
    }

    [Fact]
    public void AdoptionGradeAndSubjectForeignKeys_AreTenantScoped()
    {
        using var db = CreateDb();
        var type = db.Model.FindEntityType(typeof(SchoolCurriculumAdoption))!;

        Assert.Contains(
            type.GetForeignKeys(),
            fk =>
                fk.PrincipalEntityType.ClrType == typeof(GradeLevel) &&
                fk.Properties.Select(x => x.Name).SequenceEqual(
                    new[]
                    {
                        nameof(SchoolCurriculumAdoption.SchoolId),
                        nameof(SchoolCurriculumAdoption.GradeLevelId)
                    }));

        Assert.Contains(
            type.GetForeignKeys(),
            fk =>
                fk.PrincipalEntityType.ClrType == typeof(Subject) &&
                fk.Properties.Select(x => x.Name).SequenceEqual(
                    new[]
                    {
                        nameof(SchoolCurriculumAdoption.SchoolId),
                        nameof(SchoolCurriculumAdoption.SubjectId)
                    }));
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<EdulyticsDbContext>()
            .UseInMemoryDatabase($"phase075-{Guid.NewGuid():N}")
            .Options;

        return new EdulyticsDbContext(options);
    }
}
