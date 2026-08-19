using Edulytics.Core.Entities;
using Edulytics.Data.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class SubjectSupervisorAssignmentConfiguration
    : IEntityTypeConfiguration<SubjectSupervisorAssignment>
{
    public void Configure(
        EntityTypeBuilder<SubjectSupervisorAssignment> builder)
    {
        builder.ToTable("SubjectSupervisorAssignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.HasIndex(
                x => new
                {
                    x.SchoolId,
                    x.SupervisorUserId,
                    x.SubjectId
                })
            .IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(
                x => new
                {
                    x.SchoolId,
                    x.SubjectId
                })
            .HasPrincipalKey(
                x => new
                {
                    x.SchoolId,
                    x.Id
                })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(x => x.SupervisorUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
