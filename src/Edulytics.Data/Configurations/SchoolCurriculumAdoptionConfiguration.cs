using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class SchoolCurriculumAdoptionConfiguration
    : IEntityTypeConfiguration<SchoolCurriculumAdoption>
{
    public void Configure(
        EntityTypeBuilder<SchoolCurriculumAdoption> builder)
    {
        builder.ToTable("SchoolCurriculumAdoptions");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.AcademicYearId,
            x.GradeLevelId,
            x.SubjectId,
            x.FrameworkVersionId
        })
        .IsUnique()
        .AreNullsDistinct(false);

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.AcademicYearId,
            x.GradeLevelId,
            x.SubjectId
        })
        .IsUnique()
        .AreNullsDistinct(false)
        .HasFilter("\"IsPrimary\" = TRUE");

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AcademicYear>()
            .WithMany()
            .HasForeignKey(x => new
            {
                x.SchoolId,
                x.AcademicYearId
            })
            .HasPrincipalKey(x => new
            {
                x.SchoolId,
                x.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<GradeLevel>()
            .WithMany()
            .HasForeignKey(x => new
            {
                x.SchoolId,
                x.GradeLevelId
            })
            .HasPrincipalKey(x => new
            {
                x.SchoolId,
                x.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(x => new
            {
                x.SchoolId,
                x.SubjectId
            })
            .HasPrincipalKey(x => new
            {
                x.SchoolId,
                x.Id
            })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CurriculumFrameworkVersion>()
            .WithMany()
            .HasForeignKey(x => x.FrameworkVersionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
