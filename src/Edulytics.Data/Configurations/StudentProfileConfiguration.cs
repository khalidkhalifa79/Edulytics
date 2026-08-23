using Edulytics.Core.Entities;
using Edulytics.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("StudentProfiles");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.Property(x => x.StudentNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.NormalizedStudentNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(205).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.IsArchived).IsRequired();
        builder.Property(x => x.ArchivedAtUtc);
        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new { x.SchoolId, x.NormalizedStudentNumber })
            .IsUnique();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.IsArchived,
            x.Status
        });

        builder.HasIndex(x => x.UserId)
            .IsUnique()
            .HasFilter("\"UserId\" IS NOT NULL");

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
