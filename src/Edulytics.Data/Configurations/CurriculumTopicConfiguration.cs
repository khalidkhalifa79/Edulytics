using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class CurriculumTopicConfiguration
    : IEntityTypeConfiguration<CurriculumTopic>
{
    public void Configure(EntityTypeBuilder<CurriculumTopic> builder)
    {
        builder.ToTable("CurriculumTopics");
        builder.HasKey(x => x.Id);

        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.HasAlternateKey(x => new
        {
            x.SchoolId,
            x.FrameworkVersionId,
            x.SubjectId,
            x.GradeLevelId,
            x.Id
        });

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Order)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.FrameworkVersionId,
            x.SubjectId,
            x.GradeLevelId,
            x.Name
        }).IsUnique();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.FrameworkVersionId,
            x.SubjectId,
            x.GradeLevelId,
            x.Order
        }).IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CurriculumFrameworkVersion>()
            .WithMany()
            .HasForeignKey(x => x.FrameworkVersionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Subject>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.SubjectId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<GradeLevel>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.GradeLevelId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
