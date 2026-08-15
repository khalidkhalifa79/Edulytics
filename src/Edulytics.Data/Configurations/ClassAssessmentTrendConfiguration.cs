using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class ClassAssessmentTrendConfiguration
    : IEntityTypeConfiguration<ClassAssessmentTrend>
{
    public void Configure(
        EntityTypeBuilder<ClassAssessmentTrend> builder)
    {
        builder.ToTable("ClassAssessmentTrends");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.AssessmentTitle)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.AssessmentDate)
            .HasColumnType("date");

        builder.Property(x => x.AveragePercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(x => x.CalculatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.AssessmentId
        }).IsUnique();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.AcademicYearId,
            x.ClassGroupId,
            x.SubjectId,
            x.AssessmentDate
        });

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

        builder.HasOne<Assessment>()
            .WithMany()
            .HasForeignKey(
                x => new
                {
                    x.SchoolId,
                    x.AssessmentId
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
