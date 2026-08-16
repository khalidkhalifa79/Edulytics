using Edulytics.Core.Entities;
using Edulytics.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class AssessmentResultConfiguration
    : IEntityTypeConfiguration<AssessmentResult>
{
    public void Configure(EntityTypeBuilder<AssessmentResult> builder)
    {
        builder.ToTable("AssessmentResults");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.Property(x => x.Score).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Percentage).HasPrecision(5, 2).IsRequired();
        builder.Property(x => x.EnteredAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.AssessmentId,
            x.StudentProfileId
        }).IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Assessment>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.AssessmentId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<StudentProfile>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.StudentProfileId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.EnteredByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
