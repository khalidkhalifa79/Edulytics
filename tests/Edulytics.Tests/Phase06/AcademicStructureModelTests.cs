using Edulytics.Core.Entities;
using Edulytics.Data.Contexts;
using Microsoft.EntityFrameworkCore;

namespace Edulytics.Tests.Phase06;

public sealed class AcademicStructureModelTests
{
    [Fact]
    public void AllPhase06Entities_AreMapped()
    {
        using var db = CreateDb();

        var types = new[]
        {
            typeof(AcademicYear),
            typeof(Term),
            typeof(GradeLevel),
            typeof(ClassGroup),
            typeof(Subject),
            typeof(StudentProfile),
            typeof(TeacherAssignment),
            typeof(StudentEnrollment)
        };

        foreach (var type in types)
        {
            Assert.NotNull(db.Model.FindEntityType(type));
        }
    }

    [Fact]
    public void CriticalEditableEntities_HaveConcurrencyTokens()
    {
        using var db = CreateDb();

        AssertConcurrency<AcademicYear>(db, nameof(AcademicYear.RowVersion));
        AssertConcurrency<ClassGroup>(db, nameof(ClassGroup.RowVersion));
        AssertConcurrency<Subject>(db, nameof(Subject.RowVersion));
    }

    [Fact]
    public void EnrollmentClassRelation_ContainsSchoolAndYear()
    {
        using var db = CreateDb();

        var type = db.Model.FindEntityType(typeof(StudentEnrollment));
        Assert.NotNull(type);

        Assert.Contains(
            type!.GetForeignKeys(),
            fk =>
                fk.PrincipalEntityType.ClrType == typeof(ClassGroup) &&
                fk.Properties.Select(x => x.Name).SequenceEqual(
                    new[]
                    {
                        nameof(StudentEnrollment.SchoolId),
                        nameof(StudentEnrollment.AcademicYearId),
                        nameof(StudentEnrollment.ClassGroupId)
                    }));
    }

    private static void AssertConcurrency<T>(
        EdulyticsDbContext db,
        string propertyName)
    {
        var entity = db.Model.FindEntityType(typeof(T));
        Assert.NotNull(entity);

        var property = entity!.FindProperty(propertyName);
        Assert.NotNull(property);
        Assert.True(property!.IsConcurrencyToken);
    }

    private static EdulyticsDbContext CreateDb()
    {
        var options =
            new DbContextOptionsBuilder<EdulyticsDbContext>()
                .UseInMemoryDatabase($"phase06-model-{Guid.NewGuid():N}")
                .Options;

        return new EdulyticsDbContext(options);
    }
}
