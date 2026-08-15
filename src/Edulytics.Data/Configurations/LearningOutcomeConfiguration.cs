using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class LearningOutcomeConfiguration
    : IEntityTypeConfiguration<LearningOutcome>
{
    public void Configure(EntityTypeBuilder<LearningOutcome> builder)
    {
        builder.ToTable("LearningOutcomes");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(x => x.Weight)
            .HasPrecision(6, 3)
            .IsRequired();

        builder.Property(x => x.Order)
            .IsRequired();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.FrameworkVersionId,
            x.SubjectId,
            x.GradeLevelId,
            x.Code
        }).IsUnique();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.TopicId,
            x.Order
        }).IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CurriculumTopic>()
            .WithMany()
            .HasForeignKey(x => new
            {
                x.SchoolId,
                x.FrameworkVersionId,
                x.SubjectId,
                x.GradeLevelId,
                x.TopicId
            })
            .HasPrincipalKey(x => new
            {
                x.SchoolId,
                x.FrameworkVersionId,
                x.SubjectId,
                x.GradeLevelId,
                x.Id
            })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
