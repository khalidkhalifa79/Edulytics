using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class ClassGroupConfiguration : IEntityTypeConfiguration<ClassGroup>
{
    public void Configure(EntityTypeBuilder<ClassGroup> builder)
    {
        builder.ToTable("ClassGroups");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });
        builder.HasAlternateKey(x => new { x.SchoolId, x.AcademicYearId, x.Id });

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NormalizedCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.RowVersion).IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new { x.SchoolId, x.AcademicYearId, x.NormalizedCode })
            .IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AcademicYear>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.AcademicYearId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<GradeLevel>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.GradeLevelId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
