using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class StudentOutcomeMasteryConfiguration
    : IEntityTypeConfiguration<StudentOutcomeMastery>
{
    public void Configure(
        EntityTypeBuilder<StudentOutcomeMastery> builder)
    {
        builder.ToTable("StudentOutcomeMasteries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EarnedScore)
            .HasPrecision(12, 4)
            .IsRequired();

        builder.Property(x => x.PossibleScore)
            .HasPrecision(12, 4)
            .IsRequired();

        builder.Property(x => x.MasteryPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(x => x.Band)
            .HasConversion<int>();

        builder.Property(x => x.CalculatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.AcademicYearId,
            x.ClassGroupId,
            x.SubjectId,
            x.StudentProfileId,
            x.LearningOutcomeId
        }).IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AcademicYear>()
            .WithMany()
            .HasForeignKey(
                x => new
                {
                    x.SchoolId,
                    x.AcademicYearId
                })
            .HasPrincipalKey(
                x => new
                {
                    x.SchoolId,
                    x.Id
                })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ClassGroup>()
            .WithMany()
            .HasForeignKey(
                x => new
                {
                    x.SchoolId,
                    x.AcademicYearId,
                    x.ClassGroupId
                })
            .HasPrincipalKey(
                x => new
                {
                    x.SchoolId,
                    x.AcademicYearId,
                    x.Id
                })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(
                x => new
                {
                    x.SchoolId,
                    x.SubjectId
                })
            .HasPrincipalKey(
                x => new
                {
                    x.SchoolId,
                    x.Id
                })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StudentProfile>()
            .WithMany()
            .HasForeignKey(
                x => new
                {
                    x.SchoolId,
                    x.StudentProfileId
                })
            .HasPrincipalKey(
                x => new
                {
                    x.SchoolId,
                    x.Id
                })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<LearningOutcome>()
            .WithMany()
            .HasForeignKey(
                x => new
                {
                    x.SchoolId,
                    x.LearningOutcomeId
                })
            .HasPrincipalKey(
                x => new
                {
                    x.SchoolId,
                    x.Id
                })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
