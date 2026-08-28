using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Edulytics.Data.Configurations;
public sealed class CurriculumTopicConfiguration : IEntityTypeConfiguration<CurriculumTopic>
{
 public void Configure(EntityTypeBuilder<CurriculumTopic> b)
 {
  b.ToTable("CurriculumTopics"); b.HasKey(x=>x.Id); b.HasAlternateKey(x=>new{x.SchoolId,x.Id});
  b.HasAlternateKey(x=>new{x.SchoolId,x.AcademicProgramId,x.FrameworkVersionId,x.SubjectId,x.GradeLevelId,x.Id});
  b.Property(x=>x.Name).HasMaxLength(200).IsRequired(); b.Property(x=>x.Order).IsRequired();
  b.HasIndex(x=>new{x.SchoolId,x.AcademicProgramId,x.FrameworkVersionId,x.SubjectId,x.GradeLevelId,x.Name}).IsUnique();
  b.HasIndex(x=>new{x.SchoolId,x.AcademicProgramId,x.FrameworkVersionId,x.SubjectId,x.GradeLevelId,x.Order}).IsUnique();
  b.HasOne<School>().WithMany().HasForeignKey(x=>x.SchoolId).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<AcademicProgram>().WithMany().HasForeignKey(x=>new{x.SchoolId,x.AcademicProgramId}).HasPrincipalKey(x=>new{x.SchoolId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<CurriculumFrameworkVersion>().WithMany().HasForeignKey(x=>x.FrameworkVersionId).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<Subject>().WithMany().HasForeignKey(x=>new{x.SchoolId,x.SubjectId}).HasPrincipalKey(x=>new{x.SchoolId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<GradeLevel>().WithMany().HasForeignKey(x=>new{x.SchoolId,x.GradeLevelId}).HasPrincipalKey(x=>new{x.SchoolId,x.Id}).OnDelete(DeleteBehavior.Restrict);
 }
}
