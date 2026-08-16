using Edulytics.Core.Entities;
using Edulytics.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edulytics.Data.Configurations;

public class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> builder)
    {
        builder.ToTable("Schools");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.SchoolCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.NormalizedSchoolCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(x => x.NormalizedSchoolCode)
            .IsUnique();

        builder.Property(x => x.Status)
            .HasConversion<int>();

        builder.Property(x => x.CountryCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.City)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(x => x.ContactEmail)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.DefaultCulture)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.TimeZoneId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRequired()
            .IsConcurrencyToken()
            .ValueGeneratedNever();
    }
}
