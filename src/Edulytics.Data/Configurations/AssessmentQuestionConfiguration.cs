using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class AssessmentQuestionConfiguration
    : IEntityTypeConfiguration<AssessmentQuestion>
{
    public void Configure(EntityTypeBuilder<AssessmentQuestion> builder)
    {
        builder.ToTable("AssessmentQuestions");
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.SchoolId, x.Id });

        builder.Property(x => x.Prompt).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.MaxScore).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.Order).IsRequired();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.AssessmentId,
            x.Order
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
    }
}
