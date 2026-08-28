using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace Edulytics.Data.Configurations;
public sealed class AcademicProgramConfiguration : IEntityTypeConfiguration<AcademicProgram>
{
    public void Configure(EntityTypeBuilder<AcademicProgram> b)
    {
        b.ToTable("AcademicPrograms"); b.HasKey(x=>x.Id); b.HasAlternateKey(x=>new{x.SchoolId,x.Id});
        b.Property(x=>x.Name).HasMaxLength(150).IsRequired();
        b.Property(x=>x.Code).HasMaxLength(50).IsRequired();
        b.Property(x=>x.NormalizedCode).HasMaxLength(50).IsRequired();
        b.Property(x=>x.Status).HasConversion<int>();
        b.Property(x=>x.RowVersion).IsRequired().IsConcurrencyToken().ValueGeneratedNever();
        b.HasIndex(x=>new{x.SchoolId,x.NormalizedCode}).IsUnique();
        b.HasIndex(x=>x.SchoolId).IsUnique().HasFilter("\"IsDefault\" = TRUE");
        b.HasOne<School>().WithMany().HasForeignKey(x=>x.SchoolId).OnDelete(DeleteBehavior.Restrict);
    }
}
