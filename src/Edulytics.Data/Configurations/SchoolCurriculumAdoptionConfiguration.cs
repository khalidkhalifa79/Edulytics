using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Edulytics.Data.Configurations;
public sealed class SchoolCurriculumAdoptionConfiguration : IEntityTypeConfiguration<SchoolCurriculumAdoption>
{
 public void Configure(EntityTypeBuilder<SchoolCurriculumAdoption> b)
 {
  b.ToTable("SchoolCurriculumAdoptions"); b.HasKey(x=>x.Id); b.Property(x=>x.CreatedAtUtc).IsRequired(); b.Property(x=>x.UpdatedAtUtc).IsRequired(); b.Property(x=>x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
  b.HasIndex(x=>new{x.SchoolId,x.AcademicYearId,x.AcademicProgramId,x.GradeLevelId,x.SubjectId,x.FrameworkVersionId}).IsUnique().AreNullsDistinct(false);
  b.HasIndex(x=>new{x.SchoolId,x.AcademicYearId,x.AcademicProgramId,x.GradeLevelId,x.SubjectId}).IsUnique().AreNullsDistinct(false).HasFilter("\"IsPrimary\" = TRUE");
  b.HasOne<School>().WithMany().HasForeignKey(x=>x.SchoolId).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<AcademicYear>().WithMany().HasForeignKey(x=>new{x.SchoolId,x.AcademicYearId}).HasPrincipalKey(x=>new{x.SchoolId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<AcademicProgram>().WithMany().HasForeignKey(x=>new{x.SchoolId,x.AcademicProgramId}).HasPrincipalKey(x=>new{x.SchoolId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<GradeLevel>().WithMany().HasForeignKey(x=>new{x.SchoolId,x.GradeLevelId}).HasPrincipalKey(x=>new{x.SchoolId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<Subject>().WithMany().HasForeignKey(x=>new{x.SchoolId,x.SubjectId}).HasPrincipalKey(x=>new{x.SchoolId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<CurriculumFrameworkVersion>().WithMany().HasForeignKey(x=>x.FrameworkVersionId).OnDelete(DeleteBehavior.Restrict);
 }
}
