using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class ClassTopicSummaryConfiguration
    : IEntityTypeConfiguration<ClassTopicSummary>
{
    public void Configure(
        EntityTypeBuilder<ClassTopicSummary> builder)
    {
        builder.ToTable("ClassTopicSummaries");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.MasteryPercentage)
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(x => x.CalculatedAtUtc)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.AcademicYearId,
            x.ClassGroupId,
            x.SubjectId,
            x.CurriculumTopicId
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

        builder.HasOne<CurriculumTopic>()
            .WithMany()
            .HasForeignKey(
                x => new
                {
                    x.SchoolId,
                    x.CurriculumTopicId
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
