using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class StudentAnswerConfiguration
    : IEntityTypeConfiguration<StudentAnswer>
{
    public void Configure(EntityTypeBuilder<StudentAnswer> builder)
    {
        builder.ToTable("StudentAnswers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Score).HasPrecision(10, 2).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();

        builder.HasIndex(x => new
        {
            x.SchoolId,
            x.AssessmentResultId,
            x.AssessmentQuestionId
        }).IsUnique();

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AssessmentResult>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.AssessmentResultId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AssessmentQuestion>()
            .WithMany()
            .HasForeignKey(x => new { x.SchoolId, x.AssessmentQuestionId })
            .HasPrincipalKey(x => new { x.SchoolId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
