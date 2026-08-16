using Edulytics.Core.Entities;
using Edulytics.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class AssessmentConfiguration : IEntityTypeConfiguration<Assessment>
{
    public void Configure(EntityTypeBuilder<Assessment> builder)
    {
        builder.ToTable("Assessments");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AssessmentDate).HasColumnType("date");
        builder.Property(x => x.MaxScore).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.ClassGroupId,
            x.TermId,
            x.Title
        }).IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AcademicYear>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.AcademicYearId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Term>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.AcademicYearId, x.TermId })
            .HasPrincipalKey(x => new { x.SchoolId, x.AcademicYearId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ClassGroup>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.AcademicYearId, x.ClassGroupId })
            .HasPrincipalKey(x => new { x.SchoolId, x.AcademicYearId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.SubjectId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
