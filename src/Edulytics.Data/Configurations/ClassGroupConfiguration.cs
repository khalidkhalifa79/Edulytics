using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Edulytics.Data.Configurations;
public sealed class ClassGroupConfiguration : IEntityTypeConfiguration<ClassGroup>
{
 public void Configure(EntityTypeBuilder<ClassGroup> b)
 {
  b.ToTable("ClassGroups"); b.HasKey(x=>x.Id); b.HasAlternateKey(x=>new{x.SchoolId,x.Id}); b.HasAlternateKey(x=>new{x.SchoolId,x.AcademicYearId,x.Id});
  b.Property(x=>x.Name).HasMaxLength(150).IsRequired(); b.Property(x=>x.Code).HasMaxLength(50).IsRequired(); b.Property(x=>x.NormalizedCode).HasMaxLength(50).IsRequired(); b.Property(x=>x.Status).HasConversion<int>(); b.Property(x=>x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
  b.HasIndex(x=>new{x.SchoolId,x.AcademicYearId,x.AcademicProgramId,x.NormalizedCode}).IsUnique();
  b.HasOne<School>().WithMany().HasForeignKey(x=>x.SchoolId).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<AcademicYear>().WithMany().HasForeignKey(x=>new{x.SchoolId,x.AcademicYearId}).HasPrincipalKey(x=>new{x.SchoolId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<AcademicProgram>().WithMany().HasForeignKey(x=>new{x.SchoolId,x.AcademicProgramId}).HasPrincipalKey(x=>new{x.SchoolId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<GradeLevel>().WithMany().HasForeignKey(x=>new{x.SchoolId,x.GradeLevelId}).HasPrincipalKey(x=>new{x.SchoolId,x.Id}).OnDelete(DeleteBehavior.Restrict);
 }
}
