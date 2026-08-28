using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Edulytics.Data.Configurations;
public sealed class LearningOutcomeConfiguration : IEntityTypeConfiguration<LearningOutcome>
{
 public void Configure(EntityTypeBuilder<LearningOutcome> b)
 {
  b.ToTable("LearningOutcomes"); b.HasKey(x=>x.Id); b.HasAlternateKey(x=>new{x.SchoolId,x.Id});
  b.Property(x=>x.Code).HasMaxLength(300).IsRequired(); b.Property(x=>x.Description).HasMaxLength(1000).IsRequired(); b.Property(x=>x.Weight).HasPrecision(6,3).IsRequired(); b.Property(x=>x.Order).IsRequired();
  b.HasIndex(x=>new{x.SchoolId,x.AcademicProgramId,x.FrameworkVersionId,x.SubjectId,x.GradeLevelId,x.Code}).IsUnique();
  b.HasIndex(x=>new{x.SchoolId,x.TopicId,x.OfficialContentNodeId}).IsUnique(); b.HasIndex(x=>new{x.SchoolId,x.TopicId,x.Order}).IsUnique();
  b.HasOne<School>().WithMany().HasForeignKey(x=>x.SchoolId).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<AcademicProgram>().WithMany().HasForeignKey(x=>new{x.SchoolId,x.AcademicProgramId}).HasPrincipalKey(x=>new{x.SchoolId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<CurriculumTopic>().WithMany().HasForeignKey(x=>new{x.SchoolId,x.AcademicProgramId,x.FrameworkVersionId,x.SubjectId,x.GradeLevelId,x.TopicId}).HasPrincipalKey(x=>new{x.SchoolId,x.AcademicProgramId,x.FrameworkVersionId,x.SubjectId,x.GradeLevelId,x.Id}).OnDelete(DeleteBehavior.Restrict);
  b.HasOne<CurriculumPackContentNode>().WithMany().HasForeignKey(x=>x.OfficialContentNodeId).OnDelete(DeleteBehavior.Restrict);
 }
}
