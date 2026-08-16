using Edulytics.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public sealed class CurriculumFrameworkConfiguration
    : IEntityTypeConfiguration<CurriculumFramework>
{
    public void Configure(EntityTypeBuilder<CurriculumFramework> builder)
    {
        builder.ToTable("CurriculumFrameworks");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.NormalizedCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(x => x.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.CountryCode)
            .HasMaxLength(2);

        builder.Property(x => x.ProviderName)
            .HasMaxLength(200);

        builder.Property(x => x.CreatedAtUtc).IsRequired();
        builder.Property(x => x.UpdatedAtUtc).IsRequired();
        builder.Property(x => x.RowVersion).IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();

        builder.HasIndex(x => new
        {
            x.OwnerSchoolId,
            x.NormalizedCode
        })
            .IsUnique()
            .AreNullsDistinct(false);

        builder.HasOne<School>()
            .WithMany()
            .HasForeignKey(x => x.OwnerSchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
